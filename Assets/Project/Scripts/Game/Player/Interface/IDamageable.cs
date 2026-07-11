/// <summary>
/// 혀 공격 등으로 데미지를 받을 수 있는 대상이 구현하는 인터페이스.
/// 기존 CompareTag("Enemy") 분기를 대체합니다.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 데미지를 적용합니다.
    /// </summary>
    /// <param name="damage">적용할 데미지 양</param>
    void TakeDamage(float damage);
}
