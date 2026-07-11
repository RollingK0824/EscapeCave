using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 좌우 이동, 스프라이트 방향 전환(Flip), 비행 능력을 전담하는 컴포넌트.
/// 다른 컴포넌트(공격, 갈고리 등)는 IsFacingRight를 참조하거나
/// RequestFlip / FaceTowards를 호출해서 방향을 바꿉니다.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;

    [Header("Flight")]
    [SerializeField] private float flightMoveSpeed = 6f; // 비행 중 상하좌우 이동 속도

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerGrappleHook grapple;

    private Vector2 moveInput;
    private bool isFacingRight = true;

    private bool isFlying;
    private float originalGravityScale;
    private Coroutine flightRoutine;

    /// <summary>외부에서 이동을 잠글 때 사용 (공격/갈고리 중 등).</summary>
    public bool MovementLocked { get; set; } = false;

    public bool IsFacingRight => isFacingRight;
    public Vector2 MoveInput => moveInput;
    public bool IsFlying => isFlying;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        grapple = GetComponent<PlayerGrappleHook>();
    }

    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    private void Update()
    {
        if (!grapple.IsHooking && moveInput.x != 0 && !MovementLocked)
        {
            CheckMovementFlip();
        }

        // 비행 중에는 걷기 애니메이션이 덮어쓰지 않도록 막는다.
        //animator.SetBool("IsWalking", moveInput.x != 0 && !isFlying);
    }

    private void FixedUpdate()
    {
        if (MovementLocked) return;

        if (isFlying)
        {
            // 비행 중: 좌우 + 상하 자유 이동, 중력 영향 없음(StartFlight에서 gravityScale 0으로 설정)
            rb.linearVelocity = new Vector2(moveInput.x * flightMoveSpeed, moveInput.y * flightMoveSpeed);
        }
        else
        {
            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        }
    }

    #region Flight Logic
    /// <summary>
    /// duration초 동안 비행 상태로 전환합니다. (중력 해제 + 상하좌우 자유 이동 + 비행 애니메이션)
    /// 인벤토리 등 외부에서 아이템 사용 시 호출합니다.
    /// </summary>
public void StartFlight(float duration)
{
    // 1. 기존 코루틴이 있다면 멈추기 전에 상태를 확실하게 복구
    if (flightRoutine != null)
    {
        StopCoroutine(flightRoutine);
        
        // 상태 초기화 (애니메이션과 중력값 원복)
        rb.gravityScale = originalGravityScale;
        animator.SetBool("IsFlying", false);
        isFlying = false;
    }

    // 2. 새로운 코루틴 시작
    flightRoutine = StartCoroutine(FlightRoutine(duration));
}
    private IEnumerator FlightRoutine(float duration)
    {
        // 1. 이미 비행 중이라면 루틴을 새로 시작하지 않음 (선택 사항)
        if (isFlying) yield break;

        isFlying = true;
        originalGravityScale = rb.gravityScale;
        rb.gravityScale = 0f;
        animator.SetBool("IsFlying", true);

        yield return new WaitForSeconds(duration);

        // 2. 루틴 종료 후 상태 복구
        rb.gravityScale = originalGravityScale;
        animator.SetBool("IsFlying", false);
        isFlying = false;

        flightRoutine = null;
    }
    #endregion

    #region Flip Logic
    private void CheckMovementFlip()
    {
        if ((moveInput.x > 0 && !isFacingRight) || (moveInput.x < 0 && isFacingRight))
        {
            Flip();
        }
    }

    /// <summary>
    /// 월드 좌표 target 방향을 바라보도록 필요 시 Flip 합니다.
    /// 마우스 조준, 갈고리 방향 등 외부 컴포넌트가 호출할 수 있습니다.
    /// </summary>
    public void FaceTowards(Vector3 worldTarget)
    {
        if (worldTarget.x > transform.position.x && !isFacingRight)
        {
            Flip();
        }
        else if (worldTarget.x < transform.position.x && isFacingRight)
        {
            Flip();
        }
    }

    public void Flip()
    {
        isFacingRight = !isFacingRight;

        if (spriteRenderer != null)
        {
            // 원본 스프라이트가 오른쪽을 보고 있다고 가정:
            // 오른쪽을 볼 때(true) flipX = false, 왼쪽을 볼 때(false) flipX = true
            spriteRenderer.flipX = !isFacingRight;
        }
    }
    public void SetFacing(bool faceRight)
    {
        if (faceRight != isFacingRight)
        {
            Flip();
        }
    }
    #endregion
}
