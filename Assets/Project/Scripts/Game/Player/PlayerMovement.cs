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
    [SerializeField] private float _moveSpeed = 8f;

    [Header("Flight")]
    [SerializeField] private float _flightMoveSpeed = 6f; // 비행 중 상하좌우 이동 속도

    [Header("Step Climb")]
    [SerializeField, Tooltip("이 높이 이하의 수직 단차는 자동으로 타고 오릅니다.")]
    private float _maxStepHeight = 0.4f;
    [SerializeField, Tooltip("정면 단차를 감지하는 레이 길이")]
    private float _stepCheckDistance = 0.3f;
    [SerializeField, Tooltip("단차를 오를 때 초당 밀어올리는 속도. 너무 크면 순간이동처럼 튀고, 너무 작으면 계단에서 밀려 못 올라감.")]
    private float _stepClimbSpeed = 6f;

    private Rigidbody2D _rb;
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private PlayerGrappleHook _grapple;
    private PlayerJump _jump;
    private Collider2D _collider;

    private Vector2 _moveInput;
    private bool _isFacingRight = true;
    private readonly ContactPoint2D[] _contactBuffer = new ContactPoint2D[8];

    private bool _isFlying;
    private float _originalGravityScale;
    private Coroutine _flightRoutine;

    /// <summary>외부에서 이동을 잠글 때 사용 (공격/갈고리 중 등).</summary>
    public bool MovementLocked { get; set; } = false;

    public bool IsFacingRight => _isFacingRight;
    public Vector2 MoveInput => _moveInput;
    public bool IsFlying => _isFlying;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _grapple = GetComponent<PlayerGrappleHook>();
        _jump = GetComponent<PlayerJump>();
        _collider = GetComponent<Collider2D>();
    }

    public void SetMoveInput(Vector2 input)
    {
        _moveInput = input;
    }

    private void Update()
    {
        if (MovementLocked || (_grapple != null && _grapple.IsHooking)) return;

        if (_moveInput.x != 0)
        {
            CheckMovementFlip();
        }

        // 비행 중에는 걷기 애니메이션이 덮어쓰지 않도록 막는다.
        _animator.SetBool("IsWalking", _moveInput.x != 0 && !_isFlying);
    }

    private void FixedUpdate()
    {
        if (MovementLocked) return;

        // ── 벽 프리징(wall-stick) 버그 수정 ──────────────────────────────
        // [현상] 공중에서 벽에 몸을 박은 채 이동키를 계속 누르고 있으면,
        //        캐릭터가 벽에 달라붙어 y축으로 전혀 떨어지지 않고 얼어붙었음.
        //
        // [원인] 아래에서 매 물리 프레임 _rb.linearVelocity.x에 이동 속도를 강제로
        //        대입하는데, 벽 방향으로 계속 밀어 넣으면 물리 엔진이 벽 접촉면에
        //        큰 수직항력(normal force)을 만들고, 그에 비례한 "마찰력"이
        //        중력을 상쇄해버림. 결과적으로 벽에 매달린 것처럼 정지.
        //
        // [해결] 속도를 대입하기 전에, 이동하려는 방향에 이미 수직 벽이 접촉해
        //        있는지 검사(IsPressingIntoWall)하고, 벽이 있으면 그 방향의
        //        x속도 성분을 0으로 만든다. 벽을 미는 힘이 사라지면 마찰력도
        //        사라지므로 중력에 의해 자연스럽게 미끄러져 내려온다.
        //        (반대 방향으로 입력하면 벽 검사에 걸리지 않아 즉시 이탈 가능)
        // ────────────────────────────────────────────────────────────────
        float inputX = _moveInput.x;
        if (inputX != 0 && !_isFlying && _jump != null && _jump.IsGrounded)
        {
            TryStepUp(Mathf.Sign(inputX));
        }

        if (inputX != 0 && IsPressingIntoWall(Mathf.Sign(inputX)))
        {
            inputX = 0f;
        }

        if (_isFlying)
        {
            // 비행 중: 좌우 + 상하 자유 이동, 중력 영향 없음(StartFlight에서 gravityScale 0으로 설정)
            _rb.linearVelocity = new Vector2(inputX * _flightMoveSpeed, _moveInput.y * _flightMoveSpeed);
        }
        else
        {
            _rb.linearVelocity = new Vector2(inputX * _moveSpeed, _rb.linearVelocity.y);
        }
    }

    /// <summary>
    /// dirX(+1: 오른쪽, -1: 왼쪽) 방향으로 이동하려는 지금,
    /// 그 방향의 "수직 벽"에 이미 몸이 접촉해 있는지 검사합니다.
    ///
    /// 원리: 물리 엔진이 알려주는 접촉면의 normal(표면에서 수직으로 뻗는 방향)은
    /// 항상 플레이어 쪽을 향합니다. 즉 오른쪽 벽에 닿아 있으면 normal.x ≈ -1,
    /// 왼쪽 벽이면 normal.x ≈ +1, 평평한 바닥이면 normal = (0, 1)입니다.
    ///
    /// 따라서 normal.x * dirX < -0.7f 라는 조건은
    /// "이동 방향과 거의 정반대를 향하는(=가로막는) 가파른 면"만 벽으로 인정한다는 뜻:
    ///  - 오른쪽 이동(dirX=+1) 중 오른쪽 벽(normal.x≈-1) → -1 < -0.7 → 벽 ○
    ///  - 바닥(normal.x≈0)                              →  0 > -0.7 → 벽 ×
    ///  - 걸을 수 있는 완만한 경사면(|normal.x| < 0.7)   →  벽 × (정상 등반 가능)
    /// -0.7은 약 45도보다 가파른 면부터 벽으로 취급하는 기준값입니다.
    /// </summary>
    private bool IsPressingIntoWall(float dirX)
    {
        // 현재 이 Rigidbody에 닿아 있는 모든 접촉점을 버퍼에 받아온다 (할당 없음)
        int count = _rb.GetContacts(_contactBuffer);
        for (int i = 0; i < count; i++)
        {
            if (_contactBuffer[i].normal.x * dirX < -0.7f)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// dirX 방향 정면에 <see cref="_maxStepHeight"/> 이하의 수직 단차가 있으면
    /// 캐릭터를 그 위로 밀어 올려 계단식 지형을 자동으로 타고 오르게 합니다.
    /// (발 높이 레이는 막혀있고, 단차 높이 레이는 뚫려있고, 그 지점 아래에 바닥이 있을 때만 동작)
    /// </summary>
    private bool TryStepUp(float dirX)
    {
        if (_collider == null || _jump == null) return false;

        Bounds bounds = _collider.bounds;
        Vector2 dir = new Vector2(dirX, 0f);
        Vector2 footOrigin = new Vector2(bounds.center.x, bounds.min.y + 0.05f);

        RaycastHit2D lowHit = Physics2D.Raycast(footOrigin, dir, _stepCheckDistance, _jump.GroundLayer);
        if (lowHit.collider == null) return false;

        Vector2 upperOrigin = footOrigin + Vector2.up * _maxStepHeight;
        RaycastHit2D upperHit = Physics2D.Raycast(upperOrigin, dir, _stepCheckDistance, _jump.GroundLayer);
        if (upperHit.collider != null) return false;

        Vector2 downOrigin = upperOrigin + dir * _stepCheckDistance;
        RaycastHit2D downHit = Physics2D.Raycast(downOrigin, Vector2.down, _maxStepHeight + 0.1f, _jump.GroundLayer);
        if (downHit.collider == null) return false;

        // 한 번에 순간이동시키지 않고, 목표 높이까지 초당 _stepClimbSpeed만큼만 밀어올린다.
        // (즉시 스냅하면 중력이 같은 프레임에 다시 끌어내리면서 위아래로 튀는 현상이 생김)
        float targetY = _rb.position.y + (downHit.point.y - bounds.min.y + 0.02f);
        float newY = Mathf.MoveTowards(_rb.position.y, targetY, _stepClimbSpeed * Time.fixedDeltaTime);
        _rb.position = new Vector2(_rb.position.x, newY);

        // 밀어올리는 동안 잔여 낙하 속도가 남아있으면 중력과 상쇄되어 진동하므로 제거
        if (_rb.linearVelocity.y < 0f)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
        }

        return true;
    }

    #region Flight Logic
    /// <summary>
    /// duration초 동안 비행 상태로 전환합니다. (중력 해제 + 상하좌우 자유 이동 + 비행 애니메이션)
    /// 인벤토리 등 외부에서 아이템 사용 시 호출합니다.
    /// </summary>
public void StartFlight(float duration)
{
    // 1. 기존 코루틴이 있다면 멈추기 전에 상태를 확실하게 복구
    if (_flightRoutine != null)
    {
        StopCoroutine(_flightRoutine);
        
        // 상태 초기화 (애니메이션과 중력값 원복)
        _rb.gravityScale = _originalGravityScale;
        _animator.SetBool("IsFlying", false);
        _isFlying = false;
    }

    // 2. 새로운 코루틴 시작
    _flightRoutine = StartCoroutine(FlightRoutine(duration));
}
    private IEnumerator FlightRoutine(float duration)
    {
        // 1. 이미 비행 중이라면 루틴을 새로 시작하지 않음 (선택 사항)
        if (_isFlying) yield break;

        _isFlying = true;
        _originalGravityScale = _rb.gravityScale;
        _rb.gravityScale = 0f;
        _animator.SetBool("IsFlying", true);

        yield return new WaitForSeconds(duration);

        // 2. 루틴 종료 후 상태 복구
        _rb.gravityScale = _originalGravityScale;
        _animator.SetBool("IsFlying", false);
        _isFlying = false;

        _flightRoutine = null;
    }
    #endregion

    #region Flip Logic
    private void CheckMovementFlip()
    {
        if ((_moveInput.x > 0 && !_isFacingRight) || (_moveInput.x < 0 && _isFacingRight))
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
        if (worldTarget.x > transform.position.x && !_isFacingRight)
        {
            Flip();
        }
        else if (worldTarget.x < transform.position.x && _isFacingRight)
        {
            Flip();
        }
    }

    public void Flip()
    {
        _isFacingRight = !_isFacingRight;

        if (_spriteRenderer != null)
        {
            // 원본 스프라이트가 오른쪽을 보고 있다고 가정:
            // 오른쪽을 볼 때(true) flipX = false, 왼쪽을 볼 때(false) flipX = true
            _spriteRenderer.flipX = !_isFacingRight;
        }
    }
    public void SetFacing(bool faceRight)
    {
        if (faceRight != _isFacingRight)
        {
            Flip();
        }
    }
    #endregion
}
