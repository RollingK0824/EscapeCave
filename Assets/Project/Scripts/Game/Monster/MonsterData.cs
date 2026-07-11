using UnityEngine;

public enum MonsterType
{
    Base,
    Bat,
    Mouse,
    Fish,
    Crab,
    Bear
}

[Unity.Behavior.BlackboardEnum]
public enum HitReaction
{
    None,   // 야광 꽃게 (공격 불가, 반응 없음)
    Die,    // 박쥐
    Stun,   // 쥐
}

[System.Flags]
public enum TriggerResponse
{
    None = 0,
    Sound = 1 << 0,
    Vibration = 1 << 1,
}

[CreateAssetMenu(fileName = "MonsterData", menuName = "Monster/MonsterData")]
public class MonsterData : ScriptableObject
{
    [Header("기본")]
    public string MonsterName;
    public MonsterType MonsterType;

    [Header("감지")]
    public TriggerResponse TriggerResponse = TriggerResponse.Sound;
    public float DetectionRange = 5f;

    [Header("이동")]
    public float MoveSpeed = 3f;
    public float PatrolSpeed;
    public float PatrolRange;

    [Header("공격")]
    public bool CanAttack = true;
    public int AttackDamage = 1;
    public float AttackRange = 1.5f;
    public float AttackRangeExitBuffer = 0.3f;
    public float PauseDuration = 1f;
    public float ChargeSpeed = 10f;
    public float ChargeDuration = 0.5f;

    [Header("피격")]
    public bool IsAttackable = true;
    public bool IsInvincible = false;
    public HitReaction HitReaction = HitReaction.Stun;
    public float KnockbackForce = 5f;
    public float KnockbackDuration = 0.2f;

    [Header("행동")]
    public float StunDuration = 2f;
    public float NoticeDuration = 0.5f;
    public float AggroResetTime = 3f;

    [Header("물리")]
    public float GravityScale = 1f;
    public bool UseGravity = true;
    public bool FliesFreely = false;

    [Header("추격 난이도")]
    public float SpeedRampRate = 0f;
    public float MaxMoveSpeed = 0f;
}