using UnityEngine;

public enum HitReaction
{
    ReturnToChase,  // 물고기
    Stun,   // 거미
    Die,    // 박쥐
}

[CreateAssetMenu(fileName = "MonsterData", menuName = "Monster/MonsterData")]
public class MonsterData : ScriptableObject
{
    [Header("기본 스탯")]
    public float moveSpeed = 3f;
    public float detectionRange = 10f;

    [Header("공격")]
    public float attackRange = 1.5f;
    public float pauseDuration = 1f;
    public float chargeSpeed = 10f;
    public float chargeDuration = 0.5f;

    [Header("피격")]
    public bool isInvincible = false;
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.2f;
    public HitReaction hitReaction;

    [Header("행동")]
    public float stunDuration = 2f;
    public float wakeUpDuration = 0.5f;

    [Header("물리")]
    public float garavityScale = 1f;
    public bool useGravity = true;
}
