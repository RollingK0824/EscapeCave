using System.Collections;
using System.Collections.Generic;
using Managers;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 혀를 뻗어 공격/그랩/갈고리 대상을 판정하는 컴포넌트.
/// 태그 대신 IDamageable / IGrabbable / IHookable 인터페이스로 판정합니다.
/// 갈고리 대상이 감지되면 실제 스윙 물리는 PlayerGrappleHook에 위임합니다.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerTongueAttack : MonoBehaviour
{
    [Header("Tongue Attack")]
    [SerializeField] private float maxAttackRange = 5f;
    [SerializeField] private float attackDuration = 0.2f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private LineRenderer tongueVisual;
    [SerializeField] private Vector3 mouthOffset = new Vector3(0.3f, 0.2f, 0f);
    [SerializeField] private float hitRadius = 0.5f;

    [Header("Grapple Detection")]
    [Tooltip("IHookable 컴포넌트가 없어도 이 레이어에 속하면 자동으로 갈고리가 걸립니다 (벽/플랫폼 전체용).")]
    [SerializeField] private LayerMask grappleableLayer;

    private bool isAttacking;
    public bool IsAttacking => isAttacking;

    private Animator animator;
    private PlayerMovement movement;
    private PlayerGrappleHook grappleHook;
    private PlayerSoundEmitter soundEmitter;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();
        grappleHook = GetComponent<PlayerGrappleHook>();
        soundEmitter = GetComponent<PlayerSoundEmitter>();
    }

    private void Update()
    {
        // 공격 중일 때는 마우스 방향을 계속 바라보게 처리
        if (isAttacking)
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            movement.FaceTowards(mouseWorldPos);
        }
    }

    public void Attack()
    {
        if (isAttacking) return;

        animator.SetTrigger("Attack");

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        movement.FaceTowards(mouseWorldPos);

        Vector3 originPos = transform.position;
        Vector3 attackDirection = (mouseWorldPos - originPos).normalized;
        float distanceToMouse = Vector3.Distance(originPos, mouseWorldPos);

        Vector3 targetPos = distanceToMouse > maxAttackRange
              ? originPos + attackDirection * maxAttackRange
              : mouseWorldPos;

        float distanceToTarget = Vector3.Distance(originPos, targetPos);
        RaycastHit2D hit = Physics2D.Raycast(originPos, attackDirection, distanceToTarget, grappleableLayer);

        if (hit.collider != null)
        {
            // 벽에 맞았다면, 혓바닥이 벽을 뚫고 들어가지 않도록 타겟 위치를 '벽의 표면'으로 수정합니다.
            targetPos = hit.point;
        }

        soundEmitter?.Echo();
        Debug.Log($"PlayerTongueAttack) position : ({transform.position})");
        StartCoroutine(TongueRoutine(targetPos));
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

        // ===== 판정 =====
        Collider2D[] hits = Physics2D.OverlapCircleAll(targetPosition, hitRadius);

        // 1단계: 갈고리 대상부터 먼저 "확정"만 한다 (다른 판정과 절대 섞지 않음)
        IHookable hookTarget = null;
        foreach (var hit in hits)
        {
            // 1순위: 특수 갈고리 포인트(IHookable)가 명시적으로 붙어있는 경우
            if (hit.TryGetComponent<IHookable>(out var hookable) && hookable.CanHook)
            {
                hookTarget = hookable;
                break;
            }

            // 2순위: 일반 벽/플랫폼 - 레이어만으로 판정 (개별 스크립트 불필요)
            if (((1 << hit.gameObject.layer) & grappleableLayer) != 0)
            {
                Vector3 point = hit.ClosestPoint(targetPosition);
                hookTarget = new StaticHookPoint(point);
                break;
            }
        }

        // ===== 갈고리에 걸렸으면: 그랩/공격 판정은 아예 실행하지 않고 스윙으로 위임 후 종료 =====
        // (여기서 바로 종료해야, 같은 판정 범위 안에 있던 아이템이
        //  콜라이더만 꺼진 채 방치되는 버그가 발생하지 않음)
        if (hookTarget != null)
        {
            tongueVisual.enabled = false;
            grappleHook.StartHook(hookTarget.HookPoint);
            isAttacking = false;
            yield break;
        }

        // 2단계: 갈고리 대상이 없을 때만 공격/그랩 판정 진행
        List<Transform> grabbedItems = new List<Transform>();
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(attackDamage);
            }
            else if (hit.TryGetComponent<IGrabbable>(out var grabbable))
            {
                grabbable.OnGrabbed();
                var rb = hit.attachedRigidbody;
                if (rb != null) rb.simulated = false;
                hit.enabled = false;
                grabbedItems.Add(grabbable.GrabTransform);
            }
        }

        // ===== 되돌아오는 구간: 그랩된 아이템도 같이 따라옴 =====
        elapsedTime = 0f;
        while (elapsedTime < attackDuration)
        {
            Vector3 currentTip = Vector3.Lerp(targetPosition, transform.position + flippedMouthOffset, elapsedTime / attackDuration);
            tongueVisual.SetPosition(0, transform.position + flippedMouthOffset);
            tongueVisual.SetPosition(1, currentTip);

            foreach (var item in grabbedItems)
            {
                if (item != null) item.position = currentTip;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // ===== 도착 처리: 습득 =====
        foreach (var item in grabbedItems)
        {
            if (item == null) continue;

            item.GetComponent<IGrabbable>()?.OnCollected();
        }
        tongueVisual.enabled = false;
        isAttacking = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxAttackRange);
    }

    /// <summary>
    /// 레이어만으로 감지된 갈고리 지점(개별 스크립트가 없는 일반 벽/플랫폼)을
    /// IHookable로 다루기 위한 어댑터. 특수 갈고리 포인트(IHookable을 직접 구현한
    /// 오브젝트)와 동일한 방식으로 grappleHook.StartHook()에 넘길 수 있게 해줍니다.
    /// </summary>
    private class StaticHookPoint : IHookable
    {
        private readonly Vector3 point;
        public StaticHookPoint(Vector3 point) => this.point = point;
        public Vector3 HookPoint => point;
        public bool CanHook => true;
    }
}