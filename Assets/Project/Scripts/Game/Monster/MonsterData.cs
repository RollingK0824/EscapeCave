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

public enum HitReaction
{
    ReturnToChase,  // 물고기
    Stun,   // 쥐
    Die,    // 박쥐
}

[CreateAssetMenu(fileName = "MonsterData", menuName = "Monster/MonsterData")]
public class MonsterData : ScriptableObject
{
    [Header("기본")]
    public string MonsterName;
    public MonsterType MonsterType;
    public bool IsInvincible = false;

    [Header("이동")]
    public float MoveSpeed = 3f;
    public float DetectionRange = 5f;
    public float PatrolSpeed;
    public float PatrolRange;

    [Header("공격")]
    public float AttackRange = 1.5f;
    public float PauseDuration = 1f;
    public float ChargeSpeed = 10f;
    public float ChargeDuration = 0.5f;

    [Header("피격")]
    public float KnockbackForce = 5f;
    public float KnockbackDuration = 0.2f;
    //public HitReaction hitReaction;

    [Header("행동")]
    public float StunDuration = 2f;
    public float wakeUpDuration = 0.5f;
    public float AggroResetTime = 3f;

    [Header("물리")]
    public float GravityScale = 1f;
    public bool UseGravity = true;
}