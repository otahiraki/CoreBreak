using System.Collections;
using UnityEngine;

[System.Serializable]
public class RainAttack : AttackBase
{
    public float duration = 5f;
    public float interval = 0.1f;
    public float bulletSpeed = 400f;

    public override IEnumerator Run(EnemyAttackManager m)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // 避けフェーズでなくなったら（BREAK）即座に弾生成を止める
            if (GameManager.Instance != null && !GameManager.Instance.IsDodgePhase)
                yield break;

            var half = m.spawnArea.rect.size * 0.5f;
            float x = Random.Range(-half.x, half.x);
            Vector2 pos = new Vector2(x, half.y);
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

            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
    }
}