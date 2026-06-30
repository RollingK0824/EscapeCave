using UnityEngine;

[CreateAssetMenu(fileName = "MonsterData", menuName = "Monster/MonsterData")]
public class MonsterData : ScriptableObject
{
    [Header("기본 스탯")]
    public float moveSpeed = 3f;
    public float detectionRange = 10f;
    public int maxHp = 3;

    [Header("공격")]
    public float attackRange = 1.5f;
    public float chargeSpeed = 10f;
    public float chargeDuration = 0.5f;

    [Header("행동")]
    public float hoverDuration = 1f;
    public float stunDuration = 2f;
    public float wakeUpDuration = 0.5f;

    [Header("물리")]
    public float garavityScale = 1f;
    public bool useGravity = true;
}
