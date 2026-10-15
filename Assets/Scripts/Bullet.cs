using UnityEngine;

public class Bullet : MonoBehaviour
{
    [HideInInspector] public RectTransform core;
    [HideInInspector] public Vector2 direction = Vector2.down;
    [HideInInspector] public float speed = 400f;
    [HideInInspector] public float bulletRadius = 25f;
    [HideInInspector] public float coreRadius = 35f;
    [HideInInspector] public int damage = 1;   // この弾のダメージ

    RectTransform m_Rt;
    RectTransform m_Canvas;
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
                Destroy(gameObject);   // 当たった弾は消す
                return;
            }
        }

        if (m_Canvas != null)
        {
            var half = m_Canvas.rect.size * 0.5f;
            var p = m_Rt.anchoredPosition;
            if (p.x < -half.x - 100f || p.x > half.x + 100f ||
                p.y < -half.y - 100f || p.y > half.y + 100f)
                Destroy(gameObject);
        }
    }
}