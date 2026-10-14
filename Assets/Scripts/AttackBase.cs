using System.Collections;

public abstract class AttackBase
{
    // 攻撃を再生する。終わったらコルーチンが終了する。
    public abstract IEnumerator Run(EnemyAttackManager m);
}