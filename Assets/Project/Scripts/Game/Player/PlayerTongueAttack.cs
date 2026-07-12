using System.Collections;
using System.Collections.Generic;
using Managers;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
public class PlayerTongueAttack : MonoBehaviour
{
    [Header("Tongue Attack")]
    [SerializeField] private float _attackDamageRange = 5f;   // 적/아이템 판정 사거리
    [SerializeField] private float _attackDuration = 0.2f;
    [SerializeField] private float _attackDamage = 10f;
    [SerializeField] private LineRenderer _tongueVisual;
    [SerializeField] private Vector3 _mouthOffset = new Vector3(0.3f, 0.2f, 0f);
    [SerializeField, Tooltip("혀끝 원(Tongue Tip)이 연결 안 됐을 때 대신 쓰는 판정 반경")]
    private float _hitRadius = 0.5f;
    [SerializeField, Tooltip("혀끝 원 오브젝트의 CircleCollider2D (비주얼 겸 히트박스). 이 radius가 실제 판정 크기가 됩니다.")]
    private CircleCollider2D _tongueTip;

    [Header("Grapple Detection")]
    [Tooltip("IHookable 컴포넌트가 없어도 이 레이어에 속하면 자동으로 갈고리가 걸립니다.")]
    [SerializeField] private LayerMask _grappleableLayer;
    [SerializeField] private float _grappleRange = 8f;         // 갈고리 판정 사거리 (공격보다 길게)

    [Header("Auto-aim")]
    [SerializeField, Tooltip("마우스 방향 기준 좌우로 탐색할 총 각도")]
    private float _aimConeAngle = 30f;
    [SerializeField, Tooltip("부채꼴 안에서 벽/갈고리를 탐색할 레이 개수")]
    private int _aimRayCount = 9;

    private bool _isAttacking;
    public bool IsAttacking => _isAttacking;

    private Animator _animator;
    private PlayerMovement _movement;
    private PlayerGrappleHook _grappleHook;
    private PlayerSoundEmitter _soundEmitter;

    private enum TargetType { None, Enemy, Item, Wall }

    private struct AimResult
    {
        public TargetType type;
        public Vector3 point;
    }

    // 조준 보정(FindAimTarget)과 최종 판정(TongueRoutine) 두 곳 모두
    // 이 순서 하나만 참조하도록 만들어, "적 > 아이템 > 벽" 우선순위 규칙이
    // 서로 다른 두 곳에 따로따로 구현되어 어긋나는 일을 막습니다.
    private static readonly TargetType[] _priorityOrder = { TargetType.Enemy, TargetType.Item, TargetType.Wall };

    // 부채꼴 안 후보들을 비교할 때 쓰는 가중치 (카테고리 우선순위가 아니라,
    // 같은 카테고리 안에서 각도/거리로 미세 조정하는 용도)
    private const float ANGLE_SCORE_WEIGHT = 10f;
    private const float DISTANCE_SCORE_WEIGHT = 5f;
    // 공격자 자신의 콜라이더 등, 거리가 0에 가까운 후보를 걸러내는 최소 거리
    private const float MIN_TARGET_DISTANCE = 0.01f;
    // 혀끝이 입에서 이만큼(+혀끝 반경) 이상 나아간 뒤부터 이동 중 접촉 판정을 시작
    // (플레이어가 서 있는 발밑 지형에 뻗자마자 붙어버리는 것 방지)
    private const float MIN_TIP_TRAVEL = 0.1f;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _movement = GetComponent<PlayerMovement>();
        _grappleHook = GetComponent<PlayerGrappleHook>();
        _soundEmitter = GetComponent<PlayerSoundEmitter>();

        if (_tongueVisual == null)
        {
            Debug.LogError($"{nameof(PlayerTongueAttack)}: tongueVisual이 인스펙터에 연결되지 않았습니다.", this);
        }

        // 혀끝 원은 공격 중에만 보이도록 시작 시 꺼둡니다.
        SetTongueTipActive(false);
    }

    private void Update()
    {
        if (_isAttacking)
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            _movement.FaceTowards(mouseWorldPos);
        }
    }

    public void Attack()
    {
        if (_isAttacking) return;
        if (_movement.IsFlying) return; // 비행 중 공격 차단
        if (_tongueVisual == null) return; // 인스펙터 연결 누락 시 NRE 방지

        _animator.SetTrigger("Attack");

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        _movement.FaceTowards(mouseWorldPos);

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
            float dist = Mathf.Min(Vector3.Distance(originPos, mouseWorldPos), _attackDamageRange);
            targetPos = originPos + aimDir * dist;
        }

        _soundEmitter?.Echo();
        StartCoroutine(TongueRoutine(targetPos));
    }

    /// <summary>
    /// 마우스 방향 부채꼴 안에서 우선순위(_priorityOrder: 적 > 아이템 > 벽)에 따라
    /// 혀가 실제로 뻗어나갈 '조준 지점'을 찾습니다.
    /// 정확히 그 방향이 아니어도 부채꼴 범위 안이면 자동으로 보정됩니다.
    /// </summary>
    private AimResult FindAimTarget(Vector3 origin, Vector3 aimDir)
    {
        float halfAngle = _aimConeAngle * 0.5f;
        Collider2D[] nearby = Physics2D.OverlapCircleAll(origin, _attackDamageRange);

        foreach (TargetType type in _priorityOrder)
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
            if (dist < MIN_TARGET_DISTANCE || dist > _attackDamageRange) continue;

            float angle = Vector3.Angle(aimDir, toCol);
            if (angle > halfAngle) continue;

            bool matches = type == TargetType.Enemy
                ? col.GetComponent<IDamageable>() != null
                : col.GetComponent<IGrabbable>() != null;
            if (!matches) continue;

            float angleScore = 1f - (angle / halfAngle);
            float distScore = 1f - (dist / _attackDamageRange);
            float score = angleScore * ANGLE_SCORE_WEIGHT + distScore * DISTANCE_SCORE_WEIGHT;

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

        for (int i = 0; i < _aimRayCount; i++)
        {
            float t = _aimRayCount == 1 ? 0f : (float)i / (_aimRayCount - 1);
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 dir = Quaternion.Euler(0, 0, angle) * aimDir;

            RaycastHit2D hit = Physics2D.Raycast(origin, dir, _grappleRange, _grappleableLayer);
            if (hit.collider == null) continue;

            float angleScore = 1f - (Mathf.Abs(angle) / halfAngle);
            float distScore = 1f - (hit.distance / _grappleRange);
            float score = angleScore * ANGLE_SCORE_WEIGHT + distScore * DISTANCE_SCORE_WEIGHT;

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
        _isAttacking = true;
        _tongueVisual.enabled = true;
        SetTongueTipActive(true);

        Vector3 flippedMouthOffset = new Vector3(
            _movement.IsFacingRight ? _mouthOffset.x : -_mouthOffset.x,
            _mouthOffset.y,
            _mouthOffset.z);

        Vector3 startTipPos = transform.position + flippedMouthOffset;
        // 뻗는 도중 혀끝 원에 뭔가 닿으면 목표까지 안 가고 그 지점에서 판정합니다
        // (개구리 혀처럼 "처음 닿은 것"에 붙는 동작).
        Vector3 judgePosition = targetPosition;

        // ===== 뻗는 구간 =====
        float elapsedTime = 0f;
        while (elapsedTime < _attackDuration)
        {
            Vector3 currentTip = Vector3.Lerp(transform.position + flippedMouthOffset, targetPosition, elapsedTime / _attackDuration);
            _tongueVisual.SetPosition(0, transform.position + flippedMouthOffset);
            _tongueVisual.SetPosition(1, currentTip);
            MoveTongueTip(currentTip);

            // 입 바로 앞(발밑 지형 등)에 뻗자마자 붙지 않도록, 최소 진행 거리 이후부터 검사
            if (Vector3.Distance(startTipPos, currentTip) > GetTipRadius() + MIN_TIP_TRAVEL
                && TipTouchesAnything(currentTip))
            {
                judgePosition = currentTip;
                break;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // ===== 최종 판정: 닿은(또는 도달한) 지점 주변에서 우선순위(적 > 아이템 > 벽) 적용 =====
        Collider2D[] hits = Physics2D.OverlapCircleAll(judgePosition, GetTipRadius());

        List<IDamageable> damageTargets = new List<IDamageable>();
        List<IGrabbable> grabTargets = new List<IGrabbable>();
        IHookable hookTarget = null;

        foreach (var hit in hits)
        {
            // 플레이어 자신과 그 자식(혀끝 원 콜라이더 포함)은 판정에서 제외
            if (hit.transform.IsChildOf(transform)) continue;

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
                else if (((1 << hit.gameObject.layer) & _grappleableLayer) != 0)
                {
                    Vector3 point = hit.ClosestPoint(judgePosition);
                    hookTarget = new StaticHookPoint(point);
                }
            }
        }

        // 우선순위 판정: FindAimTarget과 동일한 _priorityOrder를 그대로 따라가며,
        // 앞 카테고리에 후보가 없으면 다음 카테고리로 넘어갑니다.
        foreach (TargetType type in _priorityOrder)
        {
            switch (type)
            {
                case TargetType.Enemy:
                {
                    if (damageTargets.Count == 0) continue;

                    foreach (var damageable in damageTargets)
                    {
                        damageable.TakeDamage(_attackDamage);
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

                    _tongueVisual.enabled = false;
                    SetTongueTipActive(false);
                    _grappleHook.StartHook(hookTarget.HookPoint);
                    _isAttacking = false;
                    yield break;
                }
            }
        }

        // 아무것도 안 걸렸으면 그냥 되돌아오기
        yield return ReturnTongue(flippedMouthOffset, null);
    }

    private IEnumerator ReturnTongue(Vector3 flippedMouthOffset, List<Transform> grabbedItems)
    {
        Vector3 startTip = _tongueVisual.GetPosition(1);
        float elapsedTime = 0f;

        while (elapsedTime < _attackDuration)
        {
            Vector3 currentTip = Vector3.Lerp(startTip, transform.position + flippedMouthOffset, elapsedTime / _attackDuration);
            _tongueVisual.SetPosition(0, transform.position + flippedMouthOffset);
            _tongueVisual.SetPosition(1, currentTip);
            MoveTongueTip(currentTip);

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

        SetTongueTipActive(false);
        _tongueVisual.enabled = false;
        _isAttacking = false;
    }

    /// <summary>혀끝 판정 반경. 혀끝 콜라이더가 연결돼 있으면 그 radius(스케일 반영)를, 없으면 _hitRadius를 사용합니다.</summary>
    private float GetTipRadius()
    {
        if (_tongueTip != null)
        {
            return _tongueTip.radius * Mathf.Abs(_tongueTip.transform.lossyScale.x);
        }
        return _hitRadius;
    }

    private void MoveTongueTip(Vector3 position)
    {
        if (_tongueTip != null)
        {
            _tongueTip.transform.position = position;
        }
    }

    private void SetTongueTipActive(bool isActive)
    {
        if (_tongueTip != null)
        {
            _tongueTip.gameObject.SetActive(isActive);
        }
    }

    /// <summary>
    /// 혀끝 원 범위 안에 판정 대상(적/아이템/훅 대상/갈고리 레이어)이 하나라도 있는지 검사합니다.
    /// 뻗는 도중 매 프레임 호출되어, 처음 닿은 지점에서 혀를 멈추게 하는 용도입니다.
    /// </summary>
    private bool TipTouchesAnything(Vector3 tipPosition)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(tipPosition, GetTipRadius());
        foreach (var hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue;

            if (hit.GetComponent<IDamageable>() != null) return true;
            if (hit.GetComponent<IGrabbable>() != null) return true;
            if (hit.TryGetComponent<IHookable>(out var hookable) && hookable.CanHook) return true;
            if (((1 << hit.gameObject.layer) & _grappleableLayer) != 0) return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackDamageRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _grappleRange);
    }

    private class StaticHookPoint : IHookable
    {
        private readonly Vector3 _point;
        public StaticHookPoint(Vector3 point) => _point = point;
        public Vector3 HookPoint => _point;
        public bool CanHook => true;
    }
}