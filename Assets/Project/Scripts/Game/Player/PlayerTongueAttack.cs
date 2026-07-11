using System.Collections;
using System.Collections.Generic;
using Managers;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
public class PlayerTongueAttack : MonoBehaviour
{
    [Header("Tongue Attack")]
    [SerializeField] private float attackDamageRange = 5f;   // 적/아이템 판정 사거리
    [SerializeField] private float attackDuration = 0.2f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private LineRenderer tongueVisual;
    [SerializeField] private Vector3 mouthOffset = new Vector3(0.3f, 0.2f, 0f);
    [SerializeField] private float hitRadius = 0.5f;

    [Header("Grapple Detection")]
    [Tooltip("IHookable 컴포넌트가 없어도 이 레이어에 속하면 자동으로 갈고리가 걸립니다.")]
    [SerializeField] private LayerMask grappleableLayer;
    [SerializeField] private float grappleRange = 8f;         // 갈고리 판정 사거리 (공격보다 길게)

    [Header("Auto-aim")]
    [SerializeField, Tooltip("마우스 방향 기준 좌우로 탐색할 총 각도")]
    private float aimConeAngle = 30f;
    [SerializeField, Tooltip("부채꼴 안에서 벽/갈고리를 탐색할 레이 개수")]
    private int aimRayCount = 9;

    private bool isAttacking;
    public bool IsAttacking => isAttacking;

    private Animator animator;
    private PlayerMovement movement;
    private PlayerGrappleHook grappleHook;
    private PlayerSoundEmitter soundEmitter;

    private enum TargetType { None, Enemy, Item, Wall }

    private struct AimResult
    {
        public TargetType type;
        public Vector3 point;
    }

    // 조준 보정(FindAimTarget)과 최종 판정(TongueRoutine) 두 곳 모두
    // 이 순서 하나만 참조하도록 만들어, "적 > 아이템 > 벽" 우선순위 규칙이
    // 서로 다른 두 곳에 따로따로 구현되어 어긋나는 일을 막습니다.
    private static readonly TargetType[] PriorityOrder = { TargetType.Enemy, TargetType.Item, TargetType.Wall };

    // 부채꼴 안 후보들을 비교할 때 쓰는 가중치 (카테고리 우선순위가 아니라,
    // 같은 카테고리 안에서 각도/거리로 미세 조정하는 용도)
    private const float AngleScoreWeight = 10f;
    private const float DistanceScoreWeight = 5f;
    // 공격자 자신의 콜라이더 등, 거리가 0에 가까운 후보를 걸러내는 최소 거리
    private const float MinTargetDistance = 0.01f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();
        grappleHook = GetComponent<PlayerGrappleHook>();
        soundEmitter = GetComponent<PlayerSoundEmitter>();
    }

    private void Update()
    {
        if (isAttacking)
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            movement.FaceTowards(mouseWorldPos);
        }
    }

    public void Attack()
    {
        if (isAttacking) return;
        if (movement.IsFlying) return; // 비행 중 공격 차단

        animator.SetTrigger("Attack");

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        movement.FaceTowards(mouseWorldPos);

        Vector3 originPos = transform.position;
        Vector3 aimDir = (mouseWorldPos - originPos).normalized;

        AimResult aim = FindAimTarget(originPos, aimDir);

        Vector3 targetPos;
        if (aim.type != TargetType.None)
        {
            targetPos = aim.point;
        }
        else
        {
            // 아무 후보도 없으면 그냥 마우스 방향으로 공격 사거리만큼 뻗음
            float dist = Mathf.Min(Vector3.Distance(originPos, mouseWorldPos), attackDamageRange);
            targetPos = originPos + aimDir * dist;
        }

        soundEmitter?.Echo();
        StartCoroutine(TongueRoutine(targetPos));
    }

    /// <summary>
    /// 마우스 방향 부채꼴 안에서 우선순위(PriorityOrder: 적 > 아이템 > 벽)에 따라
    /// 혀가 실제로 뻗어나갈 '조준 지점'을 찾습니다.
    /// 정확히 그 방향이 아니어도 부채꼴 범위 안이면 자동으로 보정됩니다.
    /// </summary>
    private AimResult FindAimTarget(Vector3 origin, Vector3 aimDir)
    {
        float halfAngle = aimConeAngle * 0.5f;
        Collider2D[] nearby = Physics2D.OverlapCircleAll(origin, attackDamageRange);

        foreach (TargetType type in PriorityOrder)
        {
            AimResult candidate = type == TargetType.Wall
                ? FindBestWallCandidate(origin, aimDir, halfAngle)
                : FindBestOverlapCandidate(nearby, origin, aimDir, halfAngle, type);

            if (candidate.type != TargetType.None)
                return candidate;
        }

        return new AimResult { type = TargetType.None };
    }

    /// <summary>
    /// 부채꼴(halfAngle) 안에서 지정된 type(Enemy는 IDamageable, Item은 IGrabbable)을
    /// 가진 후보 중, 조준 방향에 가깝고 가까운 순으로 가장 좋은 후보 하나를 고릅니다.
    /// </summary>
    private AimResult FindBestOverlapCandidate(Collider2D[] candidates, Vector3 origin, Vector3 aimDir, float halfAngle, TargetType type)
    {
        AimResult best = new AimResult { type = TargetType.None };
        float bestScore = float.NegativeInfinity;

        foreach (var col in candidates)
        {
            Vector3 toCol = (Vector3)col.bounds.center - origin;
            float dist = toCol.magnitude;
            if (dist < MinTargetDistance || dist > attackDamageRange) continue;

            float angle = Vector3.Angle(aimDir, toCol);
            if (angle > halfAngle) continue;

            bool matches = type == TargetType.Enemy
                ? col.GetComponent<IDamageable>() != null
                : col.GetComponent<IGrabbable>() != null;
            if (!matches) continue;

            float angleScore = 1f - (angle / halfAngle);
            float distScore = 1f - (dist / attackDamageRange);
            float score = angleScore * AngleScoreWeight + distScore * DistanceScoreWeight;

            if (score > bestScore)
            {
                bestScore = score;
                best = new AimResult { type = type, point = col.ClosestPoint(origin) };
            }
        }

        return best;
    }

    /// <summary>
    /// 부채꼴 레이캐스트로 벽/갈고리 지점을 탐색합니다 (적/아이템보다 긴 사거리).
    /// </summary>
    private AimResult FindBestWallCandidate(Vector3 origin, Vector3 aimDir, float halfAngle)
    {
        AimResult best = new AimResult { type = TargetType.None };
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < aimRayCount; i++)
        {
            float t = aimRayCount == 1 ? 0f : (float)i / (aimRayCount - 1);
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 dir = Quaternion.Euler(0, 0, angle) * aimDir;

            RaycastHit2D hit = Physics2D.Raycast(origin, dir, grappleRange, grappleableLayer);
            if (hit.collider == null) continue;

            float angleScore = 1f - (Mathf.Abs(angle) / halfAngle);
            float distScore = 1f - (hit.distance / grappleRange);
            float score = angleScore * AngleScoreWeight + distScore * DistanceScoreWeight;

            if (score > bestScore)
            {
                bestScore = score;
                best = new AimResult { type = TargetType.Wall, point = hit.point };
            }
        }

        return best;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        mouseScreenPos.z = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0;
        return mouseWorldPos;
    }

    private IEnumerator TongueRoutine(Vector3 targetPosition)
    {
        isAttacking = true;
        tongueVisual.enabled = true;

        Vector3 flippedMouthOffset = new Vector3(
            movement.IsFacingRight ? mouthOffset.x : -mouthOffset.x,
            mouthOffset.y,
            mouthOffset.z);

        // ===== 뻗는 구간 =====
        float elapsedTime = 0f;
        while (elapsedTime < attackDuration)
        {
            Vector3 currentTip = Vector3.Lerp(transform.position + flippedMouthOffset, targetPosition, elapsedTime / attackDuration);
            tongueVisual.SetPosition(0, transform.position + flippedMouthOffset);
            tongueVisual.SetPosition(1, currentTip);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // ===== 최종 판정: 도달 지점 주변에서 우선순위(적 > 아이템 > 벽) 적용 =====
        Collider2D[] hits = Physics2D.OverlapCircleAll(targetPosition, hitRadius);

        List<IDamageable> damageTargets = new List<IDamageable>();
        List<IGrabbable> grabTargets = new List<IGrabbable>();
        IHookable hookTarget = null;

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                damageTargets.Add(damageable);
                continue;
            }
            if (hit.TryGetComponent<IGrabbable>(out var grabbable))
            {
                grabTargets.Add(grabbable);
                continue;
            }
            if (hookTarget == null)
            {
                if (hit.TryGetComponent<IHookable>(out var hookable) && hookable.CanHook)
                {
                    hookTarget = hookable;
                }
                else if (((1 << hit.gameObject.layer) & grappleableLayer) != 0)
                {
                    Vector3 point = hit.ClosestPoint(targetPosition);
                    hookTarget = new StaticHookPoint(point);
                }
            }
        }

        // 우선순위 판정: FindAimTarget과 동일한 PriorityOrder를 그대로 따라가며,
        // 앞 카테고리에 후보가 없으면 다음 카테고리로 넘어갑니다.
        foreach (TargetType type in PriorityOrder)
        {
            switch (type)
            {
                case TargetType.Enemy:
                {
                    if (damageTargets.Count == 0) continue;

                    foreach (var damageable in damageTargets)
                    {
                        damageable.TakeDamage(attackDamage);
                    }
                    yield return ReturnTongue(flippedMouthOffset, null);
                    yield break;
                }
                case TargetType.Item:
                {
                    if (grabTargets.Count == 0) continue;

                    List<Transform> grabbedItems = new List<Transform>();
                    foreach (var grabbable in grabTargets)
                    {
                        grabbable.OnGrabbed();
                        if (grabbable is Component comp)
                        {
                            var rb = comp.GetComponent<Rigidbody2D>();
                            if (rb != null) rb.simulated = false;
                            var col = comp.GetComponent<Collider2D>();
                            if (col != null) col.enabled = false;
                        }
                        grabbedItems.Add(grabbable.GrabTransform);
                    }
                    yield return ReturnTongue(flippedMouthOffset, grabbedItems);
                    yield break;
                }
                case TargetType.Wall:
                {
                    if (hookTarget == null) continue;

                    tongueVisual.enabled = false;
                    grappleHook.StartHook(hookTarget.HookPoint);
                    isAttacking = false;
                    yield break;
                }
            }
        }

        // 아무것도 안 걸렸으면 그냥 되돌아오기
        yield return ReturnTongue(flippedMouthOffset, null);
    }

    private IEnumerator ReturnTongue(Vector3 flippedMouthOffset, List<Transform> grabbedItems)
    {
        Vector3 startTip = tongueVisual.GetPosition(1);
        float elapsedTime = 0f;

        while (elapsedTime < attackDuration)
        {
            Vector3 currentTip = Vector3.Lerp(startTip, transform.position + flippedMouthOffset, elapsedTime / attackDuration);
            tongueVisual.SetPosition(0, transform.position + flippedMouthOffset);
            tongueVisual.SetPosition(1, currentTip);

            if (grabbedItems != null)
            {
                foreach (var item in grabbedItems)
                {
                    if (item != null) item.position = currentTip;
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (grabbedItems != null)
        {
            foreach (var item in grabbedItems)
            {
                if (item == null) continue;
                item.GetComponent<IGrabbable>()?.OnCollected();
            }
        }

        tongueVisual.enabled = false;
        isAttacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDamageRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, grappleRange);
    }

    private class StaticHookPoint : IHookable
    {
        private readonly Vector3 point;
        public StaticHookPoint(Vector3 point) => this.point = point;
        public Vector3 HookPoint => point;
        public bool CanHook => true;
    }
}