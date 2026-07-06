using UnityEngine;

/// <summary>
/// 마리오 스타일 가변 점프 물리 + 콜라이더 충돌 기반 접지 판정을 전담하는 컴포넌트.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerJump : MonoBehaviour
{
    [Header("Jump (Mario Style)")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 5f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private bool isGrounded; // 인스펙터 실시간 확인용

    private int groundContactCount = 0;
    private bool isJumpPressed;

    private Rigidbody2D rb;
    private Animator animator;

    /// <summary>외부에서 점프/낙하 물리를 잠글 때 사용 (갈고리로 스윙 중일 때 등).</summary>
    public bool JumpPhysicsLocked { get; set; } = false;

    public bool IsGrounded => isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        isGrounded = groundContactCount > 0;
        animator.SetBool("IsGrounded", isGrounded);
    }

    private void FixedUpdate()
    {
        if (JumpPhysicsLocked) return;

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

    #region Ground Collision
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

    #region Jump Actions
    public void StartJump()
    {
        if (!isGrounded || JumpPhysicsLocked) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        isJumpPressed = true;
        animator.SetTrigger("Jump");
        animator.SetBool("IsJump", isJumpPressed);
    }

    public void CancelJump()
    {
        isJumpPressed = false;
        animator.SetBool("IsJump", isJumpPressed);
    }
    #endregion
}
