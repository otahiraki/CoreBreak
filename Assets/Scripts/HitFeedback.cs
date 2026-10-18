using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HitFeedback : MonoBehaviour
{
    [Header("赤フラッシュ")]
    public Image flashImage;            // DamageFlash
    public float flashAlpha = 0.5f;     // 被弾時の赤さ
    public float flashFadeTime = 0.3f;  // 消えるまで

    [Header("シェイク（任意）")]
    public RectTransform shakeTarget;   // 揺らす対象（RawImage等。空なら揺れなし）
    public float shakeAmount = 20f;
    public float shakeTime = 0.15f;

    Vector2 m_ShakeHome;
    Coroutine m_Flash, m_Shake;

    void Awake()
    {
        if (shakeTarget != null) m_ShakeHome = shakeTarget.anchoredPosition;
        if (flashImage != null) SetAlpha(0f);
    }

    public void Play()
    {
        if (flashImage != null)
        {
            if (m_Flash != null) StopCoroutine(m_Flash);
            m_Flash = StartCoroutine(FlashRoutine());
        }
        if (shakeTarget != null)
        {
            if (m_Shake != null) StopCoroutine(m_Shake);
            m_Shake = StartCoroutine(ShakeRoutine());
        }
    }

    IEnumerator FlashRoutine()
    {
        SetAlpha(flashAlpha);
        float t = 0f;
        while (t < flashFadeTime)
        {
            SetAlpha(Mathf.Lerp(flashAlpha, 0f, t / flashFadeTime));
            t += Time.unscaledDeltaTime;   // 時間停止中でも動くように
            yield return null;
        }
        SetAlpha(0f);
    }

    IEnumerator ShakeRoutine()
    {
        float t = 0f;
        while (t < shakeTime)
        {
            shakeTarget.anchoredPosition = m_ShakeHome + Random.insideUnitCircle * shakeAmount;
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        shakeTarget.anchoredPosition = m_ShakeHome;
    }

    void SetAlpha(float a)
    {
        var c = flashImage.color; c.a = a; flashImage.color = c;
    }
}