using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 좌우 이동, 스프라이트 방향 전환(Flip)을 전담하는 컴포넌트.
/// 다른 컴포넌트(공격, 갈고리 등)는 IsFacingRight를 참조하거나
/// RequestFlip / FaceTowards를 호출해서 방향을 바꿉니다.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerGrappleHook grapple;

    private Vector2 moveInput;
    private bool isFacingRight = true;

    /// <summary>외부에서 이동을 잠글 때 사용 (공격/갈고리 중 등).</summary>
    public bool MovementLocked { get; set; } = false;

    public bool IsFacingRight => isFacingRight;
    public Vector2 MoveInput => moveInput;

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

        animator.SetBool("IsWalking", moveInput.x != 0);
    }

    private void FixedUpdate()
    {
        if (MovementLocked) return;

        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }

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
