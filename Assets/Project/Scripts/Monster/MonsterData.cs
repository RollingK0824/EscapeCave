using UnityEngine;

public enum HitReaction
{
    ReturnToChase,  // 물고기
    Stun,   // 거미
    Die,    // 박쥐
};

[CreateAssetMenu(fileName = "MonsterData", menuName = "Monster/MonsterData")]

public class MonsterData : ScriptableObject
{
    [Header("기본 스탯")]
    public float moveSpeed = 3f;
    public float detectionRange = 10f;
    public int maxHp = 3;

    [Header("공격")]
    public float attackRange = 1.5f;
    public float hoverDuration = 1f;
    public float chargeSpeed = 10f;
    public float chargeDuration = 0.5f;

    [Header("피격")]
    public bool isInvincible = false;   // 야광 꽃게, 곰 피격 x
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.2f;
    public HitReaction hitReaction;

    [Header("행동")]
    public float stunDuration = 2f;
    public float wakeUpDuration = 0.5f;

    [Header("물리")]
    public float garavityScale = 1f;    // 박쥐 중력 0
    public bool useGravity = true;
}
