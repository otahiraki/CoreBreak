using UnityEngine;
using UnityEngine.UI;

public class GaugeBar : MonoBehaviour
{
    public Image fill;   // Filledにした黄緑のImageをここに入れる

    void Update()
    {
        if (fill == null || GameManager.Instance == null) return;
        fill.fillAmount = GameManager.Instance.GaugeRatio;  // 値を映すだけ
    }
}