using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;   // ゲームの司令塔

    public int maxHP = 10;
    int m_HP;
    bool m_GameOver;

    void Awake()
    {
        Instance = this;
        m_HP = maxHP;
        Time.timeScale = 1f;   // 再生のたびに時間を通常へ(前回0のまま固まるの防止)
    }

    public void TakeDamage(int amount)
    {
        if (m_GameOver) return;
        m_HP -= amount;
        Debug.Log("HP: " + m_HP + " / " + maxHP);
        if (m_HP <= 0)
        {
            m_HP = 0;
            m_GameOver = true;
            Debug.Log("GAME OVER");
            Time.timeScale = 0f;   // ゲーム停止
        }
    }
}