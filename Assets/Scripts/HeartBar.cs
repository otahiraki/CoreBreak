using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartBar : MonoBehaviour
{
    public GameObject heartPrefab;   // Heartプレハブ
    public Sprite fullHeart;         // 赤ハート
    public Sprite emptyHeart;        // 割れハート
    public float heartSize = 48f;
    public float spacing = 56f;      // ハート同士の間隔

    readonly List<Image> m_Hearts = new List<Image>();
    int m_BuiltMax = -1;

    void Update()
    {
        if (GameManager.Instance == null) return;
        int max = GameManager.Instance.maxHP;
        int cur = GameManager.Instance.CurrentHP;

        if (max != m_BuiltMax) Rebuild(max);      // 最大数が変わった時だけ並べ直す

        for (int i = 0; i < m_Hearts.Count; i++)
            m_Hearts[i].sprite = (i < cur) ? fullHeart : emptyHeart;  // 満 or 空
    }

    void Rebuild(int max)
    {
        while (m_Hearts.Count < max)   // 足りなければ追加
        {
            var go = Instantiate(heartPrefab, transform);
            var rt = (RectTransform)go.transform;
            rt.localScale = Vector3.one;
            rt.sizeDelta = new Vector2(heartSize, heartSize);
            rt.anchoredPosition = new Vector2(m_Hearts.Count * spacing, 0f);
            m_Hearts.Add(go.GetComponent<Image>());
        }
        while (m_Hearts.Count > max)   // 多ければ削除
        {
            var last = m_Hearts[m_Hearts.Count - 1];
            m_Hearts.RemoveAt(m_Hearts.Count - 1);
            Destroy(last.gameObject);
        }
        m_BuiltMax = max;
    }
}