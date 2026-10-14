using System.Collections;
using UnityEngine;

[System.Serializable]
public class RainAttack : AttackBase
{
    public float duration = 5f;      // この攻撃が続く秒数
    public float interval = 0.3f;    // 何秒ごとに1発降らすか
    public float bulletSpeed = 400f; // 落下速度

    public override IEnumerator Run(EnemyAttackManager m)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // ★雨の個性：yは上端で固定、xはランダム、向きは下
            var half = m.spawnArea.rect.size * 0.5f;
            float x = Random.Range(-half.x, half.x);
            Vector2 pos = new Vector2(x, half.y);

            // 弾を1個生成（案1：生成の事務をここに直接書く）
            var go = Object.Instantiate(m.bulletPrefab, m.spawnArea);
            var rt = (RectTransform)go.transform;
            rt.localScale = Vector3.one;
            rt.anchoredPosition = pos;

            var b = go.GetComponent<Bullet>();
            b.core = m.core;
            b.direction = Vector2.down;
            b.speed = bulletSpeed;
            b.bulletRadius = m.bulletRadius;
            b.coreRadius = m.coreRadius;

            yield return new WaitForSeconds(interval); // ここでinterval秒止まる
            elapsed += interval;
        }
    }
}