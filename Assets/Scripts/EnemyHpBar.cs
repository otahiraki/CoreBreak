using UnityEngine;
using UnityEngine.UI;

public class EnemyHpBar : MonoBehaviour
{
    public Image fill;

    void Update()
    {
        if (fill == null || GameManager.Instance == null) return;
        fill.fillAmount = GameManager.Instance.EnemyHpRatio;
    }
}