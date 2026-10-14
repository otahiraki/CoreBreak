using System;
using Unity.Mathematics;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.UI;

public class CoreTracker : MonoBehaviour
{
    [Header("Models & Data")]
    public ModelAsset poseDetector;
    public ModelAsset poseLandmarker;
    public TextAsset anchorsCSV;

    [Header("Scene refs")]
    public RawImage cameraView;
    public RectTransform coreDot;

    [Header("Tuning")]
    public float scoreThreshold = 0.75f;
    public float inferenceInterval = 0.01f;   // 推論の間隔
    public bool flipX = false;
    public bool flipY = false;
    [Range(0f, 1.5f)] public float chestOffset = 0.75f;  // 肩の中点から肩幅の何倍ぶん下げるか

    [Header("Smoothing (speed-adaptive)")]
    public float stillThreshold = 6f;
    public float moveThreshold  = 60f;
    [Range(0.01f, 1f)] public float stillFollow = 0.08f;
    [Range(0.1f, 1f)]  public float moveFollow  = 0.9f;

    const int k_NumAnchors = 2254;
    const int detectorInputSize = 224;
    const int landmarkerInputSize = 256;
    const int kNose = 0;
    const int kLeftShoulder = 11;
    const int kRightShoulder = 12;

    float[,] m_Anchors;
    Worker m_DetectorWorker, m_LandmarkerWorker;
    Tensor<float> m_DetectorInput, m_LandmarkerInput;
    WebCamTexture m_Webcam;
    Awaitable m_Loop;
    float m_TexW, m_TexH;

    Vector2 m_Target;
    Vector2 m_Smoothed;
    bool m_HasTarget;
    bool m_SmoothInit;

    public async void Start()
    {
        m_Anchors = BlazeUtils.LoadAnchors(anchorsCSV.text, k_NumAnchors);

        var det = ModelLoader.Load(poseDetector);
        var graph = new FunctionalGraph();
        var input = graph.AddInput(det, 0);
        var outs = Functional.Forward(det, input);
        var picked = BlazeUtils.ArgMaxFiltering(outs[0], outs[1]);
        det = graph.Compile(picked.Item1, picked.Item2, picked.Item3);
        m_DetectorWorker = new Worker(det, BackendType.GPUCompute);

        m_LandmarkerWorker = new Worker(ModelLoader.Load(poseLandmarker), BackendType.GPUCompute);

        m_DetectorInput = new Tensor<float>(new TensorShape(1, detectorInputSize, detectorInputSize, 3));
        m_LandmarkerInput = new Tensor<float>(new TensorShape(1, landmarkerInputSize, landmarkerInputSize, 3));

        m_Webcam = new WebCamTexture();
        m_Webcam.Play();
        while (m_Webcam.width <= 16) await Awaitable.NextFrameAsync();
        if (cameraView) cameraView.texture = m_Webcam;

        while (true)
        {
            try { m_Loop = Detect(m_Webcam); await m_Loop; }
            catch (OperationCanceledException) { break; }
            await Awaitable.WaitForSecondsAsync(inferenceInterval); 
        }

        m_DetectorWorker.Dispose(); m_LandmarkerWorker.Dispose();
        m_DetectorInput.Dispose(); m_LandmarkerInput.Dispose();
    }

    void Update()
    {
        if (!m_HasTarget || coreDot == null) return;

        if (!m_SmoothInit) { m_Smoothed = m_Target; m_SmoothInit = true; }
        else
        {
            float dist = Vector2.Distance(m_Smoothed, m_Target);
            float t = Mathf.InverseLerp(stillThreshold, moveThreshold, dist);
            float follow = Mathf.Lerp(stillFollow, moveFollow, t);
            float a = 1f - Mathf.Pow(1f - follow, Time.deltaTime * 60f);
            m_Smoothed = Vector2.Lerp(m_Smoothed, m_Target, a);
        }
        coreDot.anchoredPosition = m_Smoothed;
    }

    async Awaitable Detect(Texture tex)
    {
        m_TexW = tex.width; m_TexH = tex.height;
        var size = Mathf.Max(tex.width, tex.height);

        var scale = size / (float)detectorInputSize;
        var M = BlazeUtils.mul(
            BlazeUtils.TranslationMatrix(0.5f * (new Vector2(tex.width, tex.height) + new Vector2(-size, size))),
            BlazeUtils.ScaleMatrix(new Vector2(scale, -scale)));
        BlazeUtils.SampleImageAffine(tex, m_DetectorInput, M);
        m_DetectorWorker.Schedule(m_DetectorInput);

        var idxAw = (m_DetectorWorker.PeekOutput(0) as Tensor<int>).ReadbackAndCloneAsync();
        var scoreAw = (m_DetectorWorker.PeekOutput(1) as Tensor<float>).ReadbackAndCloneAsync();
        var boxAw = (m_DetectorWorker.PeekOutput(2) as Tensor<float>).ReadbackAndCloneAsync();
        using var outIdx = await idxAw;
        using var outScore = await scoreAw;
        using var outBox = await boxAw;

        if (outScore[0] < scoreThreshold)
        {
            if (coreDot) coreDot.gameObject.SetActive(false);
            m_HasTarget = false;
            m_SmoothInit = false;
            return;
        }

        var idx = outIdx[0];
        var anchor = detectorInputSize * new float2(m_Anchors[idx, 0], m_Anchors[idx, 1]);
        var kp1 = BlazeUtils.mul(M, anchor + new float2(outBox[0, 0, 4], outBox[0, 0, 5]));
        var kp2 = BlazeUtils.mul(M, anchor + new float2(outBox[0, 0, 6], outBox[0, 0, 7]));
        var delta = kp2 - kp1;
        var radius = 1.25f * math.length(delta);
        var theta = math.atan2(delta.y, delta.x);
        var origin2 = new float2(0.5f * landmarkerInputSize, 0.5f * landmarkerInputSize);
        var s2 = radius / (0.5f * landmarkerInputSize);
        var M2 = BlazeUtils.mul(BlazeUtils.mul(BlazeUtils.mul(
            BlazeUtils.TranslationMatrix(kp1),
            BlazeUtils.ScaleMatrix(new float2(s2, -s2))),
            BlazeUtils.RotationMatrix(0.5f * Mathf.PI - theta)),
            BlazeUtils.TranslationMatrix(-origin2));
        BlazeUtils.SampleImageAffine(tex, m_LandmarkerInput, M2);
        m_LandmarkerWorker.Schedule(m_LandmarkerInput);

        var lmAw = (m_LandmarkerWorker.PeekOutput("Identity") as Tensor<float>).ReadbackAndCloneAsync();
        using var lm = await lmAw;

        // 肩の中点から「体の下方向」へ肩幅ぶん押し下げて胸の中心を作る（腰は使わない）
        var lSh  = BlazeUtils.mul(M2, new float2(lm[5 * kLeftShoulder + 0],  lm[5 * kLeftShoulder + 1]));
        var rSh  = BlazeUtils.mul(M2, new float2(lm[5 * kRightShoulder + 0], lm[5 * kRightShoulder + 1]));
        var nose = BlazeUtils.mul(M2, new float2(lm[5 * kNose + 0],          lm[5 * kNose + 1]));
        var shouldersMid = 0.5f * (lSh + rSh);
        var shoulderVec = rSh - lSh;
        float shoulderW = math.length(shoulderVec);
        var perp = new float2(-shoulderVec.y, shoulderVec.x);     // 肩に垂直
        var downDir = shouldersMid - nose;                        // 鼻→肩=だいたい下
        if (math.dot(perp, downDir) < 0f) perp = -perp;           // 下向きに揃える
        perp = math.normalizesafe(perp, new float2(0f, 1f));
        var chest = shouldersMid + perp * (chestOffset * shoulderW);

        float u = chest.x / m_TexW;
        float v = chest.y / m_TexH;
        if (flipX) u = 1f - u;
        if (flipY) v = 1f - v;

        if (coreDot)
        {
            var rect = (coreDot.parent as RectTransform).rect;
            m_Target = new Vector2((u - 0.5f) * rect.width, (v - 0.5f) * rect.height);
            m_HasTarget = true;
            if (!coreDot.gameObject.activeSelf) coreDot.gameObject.SetActive(true);
        }
    }

    void OnDestroy() { if (m_Loop != null) m_Loop.Cancel(); }
}