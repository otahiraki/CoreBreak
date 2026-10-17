using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackManager : MonoBehaviour
{
    [Header("Refs")]
    public GameObject bulletPrefab;
    public RectTransform spawnArea;
    public RectTransform core;

    [Header("Hit radii")]
    public float bulletRadius = 25f;
    public float coreRadius = 35f;

    [Header("Attacks")]
    public RainAttack rain = new RainAttack();

    List<AttackBase> m_Attacks;
    int m_Last = -1;

    void Start()
    {
        m_Attacks = new List<AttackBase> { rain };
        StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        while (true)
        {
            yield return null;

            // 避けフェーズになるまで待つ（攻撃フェーズ中は弾を出さない）
            while (GameManager.Instance != null && !GameManager.Instance.IsDodgePhase)
                yield return null;

            int i = PickNext();
            m_Last = i;
            Debug.Log("Attack start: " + m_Attacks[i].GetType().Name);
            yield return StartCoroutine(m_Attacks[i].Run(this));
        }
    }

    int PickNext()
    {
        if (m_Attacks.Count <= 1) return 0;
        int i;
        do { i = Random.Range(0, m_Attacks.Count); } while (i == m_Last);
        return i;
    }
}