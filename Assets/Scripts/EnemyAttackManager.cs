using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackManager : MonoBehaviour
{
    [Header("Refs")]
    public GameObject bulletPrefab; // 弾Prefab
    public RectTransform spawnArea;  // Canvas
    public RectTransform core;       // CoreDot

    [Header("Hit radii")]
    public float bulletRadius = 25f;
    public float coreRadius = 35f;

    [Header("Attacks")]
    public RainAttack rain = new RainAttack();

    List<AttackBase> m_Attacks;
    int m_Last = -1;

    void Start()
    {
        m_Attacks = new List<AttackBase> { rain }; // ★後でここに攻撃を足す
        StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        while (true)
        {
            yield return null;   // ← 追加：毎ループ最低1フレーム進める(暴走フリーズ防止)
            int i = PickNext();
            m_Last = i;
            Debug.Log("Attack start: " + m_Attacks[i].GetType().Name);
            yield return StartCoroutine(m_Attacks[i].Run(this)); // ★終わるまで待つ
        }
    }

    int PickNext()
    {
        if (m_Attacks.Count <= 1) return 0;
        int i;
        do { i = Random.Range(0, m_Attacks.Count); } while (i == m_Last); // 直前と同じは避ける
        return i;
    }
}