using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 마리오 스타일 가변 점프 물리 + 콜라이더 충돌 기반 접지 판정을 전담하는 컴포넌트.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerJump : MonoBehaviour
{
    [Header("Jump (Mario Style)")]
    [SerializeField] private float _jumpForce = 12f;
    [SerializeField] private float _fallMultiplier = 2.5f;
    [SerializeField] private float _lowJumpMultiplier = 5f;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField, Tooltip("이 값보다 수직에 가까운(위를 향하는) 접촉면만 바닥으로 인정합니다")]
    private float _groundNormalMinY = 0.7f; // 약 45도 이내만 바닥으로 인정 (벽 모서리 오탐 방지)
    [SerializeField] private bool _isGrounded; // 인스펙터 실시간 확인용

    // 콜라이더별로 "지금 이 접촉이 바닥으로 인정되는가"를 추적합니다.
    // 벽/바닥이 하나의 콜라이더(예: Composite Collider)로 이어져 있으면 Enter/Exit가
    // 다시 호출되지 않은 채 접촉면(normal)만 바뀔 수 있어(모서리를 타고 미끄러지는 경우),
    // 카운터 증감 방식으로는 벽에 박았을 때 바닥 판정이 그대로 남아버립니다.
    // Stay에서 매 물리 프레임 갱신해 이 문제를 없앱니다.
    private readonly Dictionary<Collider2D, bool> _groundContacts = new Dictionary<Collider2D, bool>();
    private bool _isJumpPressed;
    public event System.Action OnLanded;
    private Rigidbody2D _rb;
    private Animator _animator;

    /// <summary>외부에서 점프/낙하 물리를 잠글 때 사용 (갈고리로 스윙 중일 때 등).</summary>
    public bool JumpPhysicsLocked { get; set; } = false;

    public bool IsGrounded => _isGrounded;
    private bool _wasGrounded;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        _wasGrounded = _isGrounded;
        _isGrounded = IsAnyContactGround();

        if (!_wasGrounded && _isGrounded)
        {
            // 착지 시점: 점프 입력을 강제로 해제하여 다음 점프를 대기하게 함
            _isJumpPressed = false;
            _animator.SetBool("IsJump", false);
            OnLanded?.Invoke();
        }

        _animator.SetBool("IsGrounded", _isGrounded);
    }

    private void FixedUpdate()
    {
        if (JumpPhysicsLocked) return;

        // 마리오 스타일 가변 점프 물리 로직
        if (_rb.linearVelocity.y < 0)
        {
            _rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (_fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (_rb.linearVelocity.y > 0 && !_isJumpPressed)
        {
            _rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (_lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    #region Ground Collision
    private void OnCollisionEnter2D(Collision2D collision)
    {
        UpdateGroundContact(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        UpdateGroundContact(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & _groundLayer) == 0)
            return;

        _groundContacts.Remove(collision.collider);
    }

    /// <summary>
    /// 콜라이더 하나가 이번 물리 프레임에 "바닥 접촉"으로 인정되는지 다시 계산해 기록합니다.
    /// 벽과 바닥이 이어진 콜라이더를 타고 미끄러질 때 접촉면이 벽 쪽(수평 normal)으로
    /// 바뀌어도 여기서 즉시 false로 갱신되므로, 벽에 박았을 때 바닥 판정이 남지 않습니다.
    /// </summary>
    private void UpdateGroundContact(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & _groundLayer) == 0)
            return;

        bool isGroundContact = false;
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > _groundNormalMinY)
            {
                isGroundContact = true;
                break;
            }
        }

        _groundContacts[collision.collider] = isGroundContact;
    }

    private bool IsAnyContactGround()
    {
        foreach (bool isGroundContact in _groundContacts.Values)
        {
            if (isGroundContact) return true;
        }
        return false;
    }
    #endregion

    #region Jump Actions
    public void StartJump()
    {
        if (!_isGrounded || JumpPhysicsLocked) return;

        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpForce);
        _isJumpPressed = true;
        _animator.SetTrigger("Jump");
        _animator.SetBool("IsJump", _isJumpPressed);
    }

    public void CancelJump()
    {
        _isJumpPressed = false;
        _animator.SetBool("IsJump", _isJumpPressed);
    }
    #endregion
}
