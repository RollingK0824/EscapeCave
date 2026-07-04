using Managers;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

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

        // 타격 판정 영역 (Enemy 태그를 가진 오브젝트를 감지)
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(targetPosition, 0.5f);
        foreach (var enemy in hitEnemies)
        {
            if (enemy.CompareTag("Enemy"))
            {
                Debug.Log($"{enemy.name} 타격 성공!");
            }
        }

        // 혓바닥 시각 연출 (LineRenderer 직선 제어)
        float elapsedTime = 0f;
        while (elapsedTime < attackDuration)
        {
            tongueVisual.SetPosition(0, transform.position);
            tongueVisual.SetPosition(1, Vector3.Lerp(transform.position, targetPosition, elapsedTime / attackDuration));
            elapsedTime += Time.deltaTime;
            yield return null;
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
        EchoManager.Instance.TriggerSound(transform.position,SoundIntensity,SoundSpeed);
    }
}