using UnityEngine;

public class Bullet : MonoBehaviour
{
    [HideInInspector] public RectTransform core;
    [HideInInspector] public Vector2 direction = Vector2.down;
    [HideInInspector] public float speed = 400f;
    [HideInInspector] public float bulletRadius = 25f;
    [HideInInspector] public float coreRadius = 35f;
    [HideInInspector] public int damage = 1;

    RectTransform m_Rt, m_Canvas;
    bool m_Hit;

    void Awake()
    {
        m_Rt = (RectTransform)transform;
        m_Canvas = transform.parent as RectTransform;
    }

    void Update()
    {
        m_Rt.anchoredPosition += direction.normalized * speed * Time.deltaTime;
        if (core != null && !m_Hit)
        {
            if (Vector2.Distance(m_Rt.anchoredPosition, core.anchoredPosition) < bulletRadius + coreRadius)
            {
                m_Hit = true;
                if (GameManager.Instance != null) GameManager.Instance.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }
        }
        if (m_Canvas != null)
        {
            var half = m_Canvas.rect.size * 0.5f;
            var p = m_Rt.anchoredPosition;
            if (p.x < -half.x - 100f || p.x > half.x + 100f || p.y < -half.y - 100f || p.y > half.y + 100f)
                Destroy(gameObject);
        }
    }

    // 画面上の全弾を消す（フェーズ切替時に呼ぶ）
    public static void ClearAll()
    {
        var bullets = FindObjectsByType<Bullet>(FindObjectsSortMode.None);
        foreach (var b in bullets) Destroy(b.gameObject);
    }
}