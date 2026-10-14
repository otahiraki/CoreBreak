using System;
using Unity.Mathematics;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.UI;

public class CoreTracker : MonoBehaviour
{
    [Header("Models & Data")]
    public ModelAsset poseDetector;    // pose_detection.onnx
    public ModelAsset poseLandmarker;  // pose_landmarks_detector_lite.onnx
    public TextAsset anchorsCSV;       // anchors.csv

    [Header("Scene refs")]
    public RawImage cameraView;        // 全画面のカメラ表示
    public RectTransform coreDot;      // 胸に出す赤い丸

    [Header("Tuning")]
    public float scoreThreshold = 0.75f;
    public bool flipX = false;         // 左右が逆なら切り替え
    public bool flipY = true;          // 上下が逆なら切り替え

    const int k_NumAnchors = 2254;
    const int detectorInputSize = 224;
    const int landmarkerInputSize = 256;
    const int kLeftShoulder = 11;      // BlazePoseの関節番号
    const int kRightShoulder = 12;

    float[,] m_Anchors;
    Worker m_DetectorWorker, m_LandmarkerWorker;
    Tensor<float> m_DetectorInput, m_LandmarkerInput;
    WebCamTexture m_Webcam;
    Awaitable m_Loop;
    float m_TexW, m_TexH;

    public async void Start()
    {
        m_Anchors = BlazeUtils.LoadAnchors(anchorsCSV.text, k_NumAnchors);

        // 検出器：最もスコアの高い1人だけ選ぶ後処理を足してコンパイル
        var det = ModelLoader.Load(poseDetector);
        var graph = new FunctionalGraph();
        var input = graph.AddInput(det, 0);
        var outs = Functional.Forward(det, input);
        var picked = BlazeUtils.ArgMaxFiltering(outs[0], outs[1]);
        det = graph.Compile(picked.Item1, picked.Item2, picked.Item3);
        m_DetectorWorker = new Worker(det, BackendType.GPUCompute);

        // 関節推定器：そのまま
        m_LandmarkerWorker = new Worker(ModelLoader.Load(poseLandmarker), BackendType.GPUCompute);

        m_DetectorInput = new Tensor<float>(new TensorShape(1, detectorInputSize, detectorInputSize, 3));
        m_LandmarkerInput = new Tensor<float>(new TensorShape(1, landmarkerInputSize, landmarkerInputSize, 3));

        // カメラ起動
        m_Webcam = new WebCamTexture();
        m_Webcam.Play();
        while (m_Webcam.width <= 16) await Awaitable.NextFrameAsync();
        if (cameraView) cameraView.texture = m_Webcam;

        // 毎フレーム検出ループ
        while (true)
        {
            try { m_Loop = Detect(m_Webcam); await m_Loop; }
            catch (OperationCanceledException) { break; }
        }

        m_DetectorWorker.Dispose(); m_LandmarkerWorker.Dispose();
        m_DetectorInput.Dispose();  m_LandmarkerInput.Dispose();
    }

    async Awaitable Detect(Texture tex)
    {
        m_TexW = tex.width; m_TexH = tex.height;
        var size = Mathf.Max(tex.width, tex.height);

        // カメラ画像 → 224x224 の入力テンソルへ（アフィン変換でGPU転写）
        var scale = size / (float)detectorInputSize;
        var M = BlazeUtils.mul(
            BlazeUtils.TranslationMatrix(0.5f * (new Vector2(tex.width, tex.height) + new Vector2(-size, size))),
            BlazeUtils.ScaleMatrix(new Vector2(scale, -scale)));
        BlazeUtils.SampleImageAffine(tex, m_DetectorInput, M);
        m_DetectorWorker.Schedule(m_DetectorInput);

        var idxAw   = (m_DetectorWorker.PeekOutput(0) as Tensor<int>).ReadbackAndCloneAsync();
        var scoreAw = (m_DetectorWorker.PeekOutput(1) as Tensor<float>).ReadbackAndCloneAsync();
        var boxAw   = (m_DetectorWorker.PeekOutput(2) as Tensor<float>).ReadbackAndCloneAsync();
        using var outIdx = await idxAw;
        using var outScore = await scoreAw;
        using var outBox = await boxAw;

        if (outScore[0] < scoreThreshold) { if (coreDot) coreDot.gameObject.SetActive(false); return; }

        // 検出器の荒い体キーポイントから、体を切り出す2つ目のアフィン変換を作る
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
        using var lm = await lmAw; // (1,195) = 39点 x (x,y,z,vis,presence)

        // 左肩(11)・右肩(12)を画像ピクセル座標に戻して、中点＝胸の中心
        var lp = BlazeUtils.mul(M2, new float2(lm[5 * kLeftShoulder + 0],  lm[5 * kLeftShoulder + 1]));
        var rp = BlazeUtils.mul(M2, new float2(lm[5 * kRightShoulder + 0], lm[5 * kRightShoulder + 1]));
        var chest = 0.5f * (lp + rp);

        // 0〜1に正規化 → 画面上の赤い丸を動かす
        float u = chest.x / m_TexW;
        float v = chest.y / m_TexH;
        if (flipX) u = 1f - u;
        if (flipY) v = 1f - v;

        if (coreDot)
        {
            coreDot.gameObject.SetActive(true);
            var rect = (coreDot.parent as RectTransform).rect;
            coreDot.anchoredPosition = new Vector2((u - 0.5f) * rect.width, (v - 0.5f) * rect.height);
        }
    }

    void OnDestroy() { m_Loop.Cancel(); }
}