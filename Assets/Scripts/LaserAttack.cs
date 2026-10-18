using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class LaserAttack : AttackBase
{
    [Header("Timing")]
    public float warnTime = 0.5f;       // 予告が縮んで消えるまで
    public float fireTime = 0.2f;       // 発射＝判定の長さ
    public int laserCount = 2;          // 1回で撃つ本数
    public float betweenDelay = 0.4f;   // 次の1本までの間

    [Header("Thickness")]
    public float beamThickness = 50f;   // 本命ビームの太さ（予告もここから始まる）

    [Header("Colors")]
    public Color warnColor = new Color(1f, 0.3f, 0.3f, 0.35f); // 薄い赤（予告）
    public Color beamColor = new Color(1f, 0.2f, 0.2f, 1f);    // 濃い赤（本命）

    public override IEnumerator Run(EnemyAttackManager m)
    {
        for (int n = 0; n < laserCount; n++)
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsDodgePhase)
                yield break;
            yield return FireOne(m);
            yield return new WaitForSeconds(betweenDelay);
        }
    }

    IEnumerator FireOne(EnemyAttackManager m)
    {
        bool horizontal = Random.value < 0.5f;
        var area = m.spawnArea.rect;

        var go = Object.Instantiate(m.laserPrefab, m.spawnArea);
        var rt = (RectTransform)go.transform;
        rt.localScale = Vector3.one;
        var img = go.GetComponent<Image>();

        float linePos;     // 横ならy、縦ならx の位置
        float longSide;    // 帯の長い方（横レーザー=幅、縦=高さ）
        if (horizontal)
        {
            float halfH = area.height * 0.5f;
            linePos = Random.Range(-halfH, halfH);
            rt.anchoredPosition = new Vector2(0f, linePos);
            longSide = area.width;
        }
        else
        {
            float halfW = area.width * 0.5f;
            linePos = Random.Range(-halfW, halfW);
            rt.anchoredPosition = new Vector2(linePos, 0f);
            longSide = area.height;
        }

        // ① 予告：薄い色、太さ beamThickness → 0 に縮んで消える
        img.color = warnColor;
        float t = 0f;
        while (t < warnTime)
        {
            float th = Mathf.Lerp(beamThickness, 0f, t / warnTime);  // だんだん細く
            rt.sizeDelta = horizontal ? new Vector2(longSide, th) : new Vector2(th, longSide);
            t += Time.deltaTime;
            yield return null;
        }

        // 予告中にブレイクしたらキャンセル
        if (GameManager.Instance != null && !GameManager.Instance.IsDodgePhase)
        {
            Object.Destroy(go);
            yield break;
        }

        // ② 発射：消えた瞬間に本命ビーム（太さを戻す・濃い色）この間だけ判定
        rt.sizeDelta = horizontal ? new Vector2(longSide, beamThickness) : new Vector2(beamThickness, longSide);
        img.color = beamColor;

        bool hit = false;
        t = 0f;
        while (t < fireTime)
        {
            if (!hit && m.core != null)
            {
                float d = horizontal
                    ? Mathf.Abs(m.core.anchoredPosition.y - linePos)
                    : Mathf.Abs(m.core.anchoredPosition.x - linePos);
                if (d < beamThickness * 0.5f + m.coreRadius)
                {
                    hit = true;
                    if (GameManager.Instance != null) GameManager.Instance.TakeDamage(1);
                }
            }
            t += Time.deltaTime;
            yield return null;
        }

        Object.Destroy(go);
    }
}