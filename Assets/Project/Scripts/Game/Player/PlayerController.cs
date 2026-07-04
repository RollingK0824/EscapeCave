using Managers;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
public class PlayerController : MonoBehaviour, IEchoable
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    private Vector2 moveInput;
    private bool isFacingRight = true;

    [Header("Jump (Mario Style)")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 5f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private bool isGrounded; // 인스펙터 실시간 확인용

    private int groundContactCount = 0; // 추가: 콜라이더 충돌 카운트로 바닥 판정

    private bool isJumpPressed;

    [Header("Tongue Attack")]
    [SerializeField] private float maxAttackRange = 5f;
    [SerializeField] private float attackDuration = 0.2f;
    [SerializeField] private LineRenderer tongueVisual;
    [SerializeField] private Vector3 mouthOffset = new Vector3(0.3f, 0.2f, 0f); // 캐릭터 중심 기준 입 위치 보정값
    private bool isAttacking;

    private Rigidbody2D rb;
    private Animator animator; // 추가: 애니메이터 연동용
    private PlayerControls controls;

    [SerializeField] private float _soundIntensity;


    public float SoundIntensity => _soundIntensity;

    [SerializeField] private float _soundSpeed;
    public float SoundSpeed => _soundSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>(); // 추가
        // 만약 모델/애니메이터가 자식 오브젝트에 있다면 아래 줄로 교체하세요:
        // animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        // OnEnable에서 인풋 시스템 객체와 이벤트를 안전하게 초기화 (NullReferenceException 방지)
        if (controls == null)
        {
            controls = new PlayerControls();

            controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

            controls.Player.Jump.started += ctx => StartJump();
            controls.Player.Jump.canceled += ctx => CancelJump();

            controls.Player.Attack.performed += ctx => Attack();
            controls.Player.Cry.performed += ctx => Cry();
        }

        controls.Enable();
    }

    private void OnDisable()
    {
        if (controls != null)
        {
            controls.Disable();
        }
    }

    private void Update()
    {
        isGrounded = groundContactCount > 0;
        animator.SetBool("IsGrounded", isGrounded);
        if (isGrounded && isJumpPressed && rb.linearVelocity.y <= 0f)
        {
            isJumpPressed = false;
            animator.SetBool("IsJump", false);
        }
        if (!isAttacking && moveInput.x != 0)
        {
            CheckMovementFlip();
        }

        animator.SetBool("IsWalking", moveInput.x != 0);
    }

    private void FixedUpdate()
    {
        // 좌우 이동 물리 적용
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        // 마리오 스타일 가변 점프 물리 로직
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !isJumpPressed)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    #region Ground Collision (추가: OverlapCircle 대신 콜라이더 충돌로 바닥 판정)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            groundContactCount++;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            groundContactCount = Mathf.Max(0, groundContactCount - 1);
        }
    }
    #endregion

    #region Flip Logic (방향 전환)
    private void CheckMovementFlip()
    {
        if ((moveInput.x > 0 && !isFacingRight) || (moveInput.x < 0 && isFacingRight))
        {
            Flip();
        }
    }

    private void CheckAttackFlip(Vector3 mouseWorldPos)
    {
        if (mouseWorldPos.x > transform.position.x && !isFacingRight)
        {
            Flip();
        }
        else if (mouseWorldPos.x < transform.position.x && isFacingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }
    #endregion

    #region Jump Logic
    private void StartJump()
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isJumpPressed = true;
            animator.SetTrigger("Jump"); // 추가
            animator.SetBool("IsJump", isJumpPressed); // 추가
        }
    }

    private void CancelJump()
    {
        isJumpPressed = false;
        animator.SetBool("IsJump", isJumpPressed); // 추가
    }
    #endregion

    #region Attack Logic
    private void Attack()
    {
        if (isAttacking) return;

        animator.SetTrigger("Attack"); // 추가

        // 마우스의 현재 월드 좌표 계산
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        mouseScreenPos.z = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0;

        // 공격할 때 마우스 방향을 강제로 바라보게 처리
        CheckAttackFlip(mouseWorldPos);

        Vector3 originPos = transform.position;
        Vector3 attackDirection = (mouseWorldPos - originPos).normalized;
        float distanceToMouse = Vector3.Distance(originPos, mouseWorldPos);

        // 범위 바깥을 클릭해도 사거리 최대치까지 조준되도록 보정
        Vector3 targetPos;
        if (distanceToMouse > maxAttackRange)
        {
            targetPos = originPos + attackDirection * maxAttackRange;
        }
        else
        {
            targetPos = mouseWorldPos;
        }
        Echo();
        StartCoroutine(TongueRoutine(targetPos));
    }
    #region Cry Logic
    private void Cry()
    {
        if (isAttacking) return; // 공격 중이면 울음 방지 (필요 없으면 제거)

        animator.SetTrigger("Cry");
        Debug.Log("Cry called");
        Echo();
    }
    #endregion
    private IEnumerator TongueRoutine(Vector3 targetPosition)
    {
        isAttacking = true;
        tongueVisual.enabled = true;

        Vector3 flippedMouthOffset = new Vector3(
            isFacingRight ? mouthOffset.x : -mouthOffset.x,
            mouthOffset.y,
            mouthOffset.z
        );

        Vector3 originPos = transform.position + flippedMouthOffset;

        // ===== 2. 혓바닥이 늘어나는 구간 =====
        // 이 while문은 "attackDuration 시간 동안" 매 프레임 실행됨
        // 예: attackDuration이 0.1초면, 0.1초 동안 프레임마다 조금씩 혀를 늘림
        float elapsedTime = 0f;

        while (elapsedTime < attackDuration)
        {
            Vector3 currentTip = Vector3.Lerp(originPos, targetPosition, elapsedTime / attackDuration);
            tongueVisual.SetPosition(0, transform.position + flippedMouthOffset);
            tongueVisual.SetPosition(1, currentTip);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 최종 도달 지점에서 판정
        List<Transform> grabbedItems = new List<Transform>();
        Collider2D[] hits = Physics2D.OverlapCircleAll(targetPosition, 0.5f);
        Debug.Log($"판정 지점: {targetPosition}, 감지된 개수: {hits.Length}");
        foreach (var hit in hits)
        {
            Debug.Log($"감지됨: {hit.name}, 태그: {hit.tag}");

            if (hit.CompareTag("Enemy"))
            {
                Debug.Log($"{hit.name} 타격 성공!");
                // hit.GetComponent<EnemyHealth>()?.TakeDamage(damage);
            }
            else if (hit.CompareTag("Item"))
            {
                Debug.Log($"{hit.name} 그랩!");
                var rb = hit.attachedRigidbody;
                if (rb != null) rb.simulated = false;
                hit.enabled = false;
                grabbedItems.Add(hit.transform);
            }
        }

        // --- 되돌아오는 구간: 그랩된 아이템도 같이 따라옴 ---
        elapsedTime = 0f;
        while (elapsedTime < attackDuration)
        {
            Vector3 currentTip = Vector3.Lerp(targetPosition, transform.position, elapsedTime / attackDuration);
            tongueVisual.SetPosition(0, transform.position + flippedMouthOffset);
            tongueVisual.SetPosition(1, currentTip);

            foreach (var item in grabbedItems)
            {
                if (item != null)
                    item.position = currentTip;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // --- 도착 처리: 습득 ---
        foreach (var item in grabbedItems)
        {
            if (item != null)
            {
                var sr = item.GetComponent<SpriteRenderer>();
                Sprite icon = sr != null ? sr.sprite : null;

                bool added = Managers.InventoryManager.Instance.AddItem(icon);

                if (added)
                {
                    Debug.Log($"{item.name} 습득 완료");
                    Destroy(item.gameObject);
                }
            }
        }
        tongueVisual.enabled = false; 
        isAttacking = false;
    }

    #endregion

        // TODO: 특수공격(Cry), 사망(Die) 처리 함수가 생기면 아래처럼 트리거를 호출하세요.

        // animator.SetTrigger("Die");

    private void OnDrawGizmosSelected()
    {
        // 에디터 뷰 조절용 사거리 시각화 (빨간색 원)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxAttackRange);
    }

    public void Echo()
    {
        EchoManager.Instance.TriggerSound(transform.position, SoundIntensity, SoundSpeed);
    }
}