using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum Phase { Dodge, Attack }   // 避け / 攻撃 の2フェーズ

    [Header("Player")]
    public int maxHP = 5;
    public HitFeedback hitFeedback;  
    public int CurrentHP => m_HP;

    [Header("Enemy")]
    public int enemyMaxHP = 10;
    int m_EnemyHP;
    public float EnemyHpRatio => (enemyMaxHP <= 0) ? 0f : (float)m_EnemyHP / enemyMaxHP;

    [Header("Break Gauge")]
    public float maxGauge = 10f;
    public float gaugeDrainPerSecond = 2f;

    [Header("Attack")]
    public int attackPower = 1;   // 1回のSpaceで与えるダメージ

    [Header("Attack Phase")]
    public float attackDuration = 3f;      // 攻撃フェーズの長さ（今は空の時間）

    int m_HP;
    float m_Gauge;
    bool m_GameOver;
    bool m_Win;
    Phase m_Phase;

    // 外に見せる情報（読むだけ）
    public Phase CurrentPhase => m_Phase;
    public bool IsDodgePhase => m_Phase == Phase.Dodge;
    public float GaugeRatio => (maxGauge <= 0f) ? 0f : m_Gauge / maxGauge;

    void Awake()
    {
        Instance = this;
        m_HP = maxHP;
        m_EnemyHP = enemyMaxHP;
        m_Gauge = maxGauge;
        m_Phase = Phase.Dodge;
        Time.timeScale = 1f;
    }

        void Update()
    {
        if (m_GameOver || m_Win) return;

        if (m_Phase == Phase.Dodge)
        {
            ReduceGauge(gaugeDrainPerSecond * Time.deltaTime);
        }
        else // 攻撃フェーズ
        {
            if (Input.GetKeyDown(KeyCode.Space))
                DamageEnemy(attackPower);
        }
    }

    public void ReduceGauge(float amount)
    {
        if (m_GameOver || m_Phase != Phase.Dodge) return;
        m_Gauge -= amount;
        if (m_Gauge <= 0f)
        {
            m_Gauge = 0f;
            StartCoroutine(AttackPhase());     // BREAK → 攻撃フェーズへ
        }
    }

        IEnumerator AttackPhase()
    {
        m_Phase = Phase.Attack;
        Bullet.ClearAll();                 // ← 追加：画面上の弾を全部消す
        Debug.Log("BREAK! -> ATTACK PHASE");

        yield return new WaitForSeconds(attackDuration);

        m_Gauge = maxGauge;
        m_Phase = Phase.Dodge;
        Debug.Log("back to DODGE");
    }

    public void TakeDamage(int amount)
    {
        if (m_GameOver) return;
        if (hitFeedback != null) hitFeedback.Play();
        m_HP -= amount;
        Debug.Log("HP: " + m_HP + " / " + maxHP);
        if (m_HP <= 0) { m_HP = 0; m_GameOver = true; Debug.Log("GAME OVER"); Time.timeScale = 0f; }
    }

        public void DamageEnemy(int amount)
    {
        if (m_Phase != Phase.Attack || m_GameOver || m_Win) return;
        m_EnemyHP -= amount;
        Debug.Log("Enemy HP: " + m_EnemyHP + " / " + enemyMaxHP);
        if (m_EnemyHP <= 0)
        {
            m_EnemyHP = 0;
            m_Win = true;
            Debug.Log("WIN!");
            Time.timeScale = 0f;
        }
    }
}