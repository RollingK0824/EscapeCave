using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Managers;

/// <summary>
/// 진자운동(스윙) 물리와, 거리가 짧을 때 로프가 아래로 휘어지는 시각적 효과를 담당합니다.
/// 로프가 지형 모서리에 걸리면 그 지점을 새 회전축(pivot)으로 삼아 감기는
/// "모서리 꺾임(Corner Wrap)"을 지원합니다.
/// 구현은 책임별로 파일을 나눠 partial class로 관리합니다:
/// 이 파일(생명주기/공개 API), .Swing(스윙 물리), .Reel(로프 감기/풀기), .Visual(로프 시각 효과).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public partial class PlayerGrappleHook : MonoBehaviour
{
    // 로프를 다 감았다고 판정하는 여유 거리 (activePivot과의 거리가 _minRopeLength + 이 값 이하면 완료)
    private const float REEL_COMPLETE_TOLERANCE = 0.15f;
    // 로프 시각 효과에서 "팽팽함"으로 취급할 여유 길이(slack) 임계값
    private const float TAUT_SLACK_THRESHOLD = 0.01f;
    // 방향 벡터 정규화 전에 0 나눗셈을 막기 위한 최소 크기
    private const float MIN_VECTOR_MAGNITUDE = 0.0001f;
    // 원-직선 교차 공식에서 이차항 계수가 0에 가까운지 판정하는 허용 오차
    private const float QUADRATIC_SOLVER_EPSILON = 0.000001f;
    // 고속 스윙 시 모서리 감김(wrap) 검사를 프레임 사이 경로에서 추가로 몇 유닛 간격으로 할지
    private const float WRAP_SWEEP_STEP = 0.15f;
    // 위 추가 검사의 프레임당 최대 횟수 (성능 안전장치)
    private const int MAX_WRAP_SWEEP_STEPS = 8;

    [Header("Rope & Visuals")]
    [SerializeField] private LineRenderer _ropeVisual;
    [SerializeField] private Sprite _hookSprite;
    [SerializeField] private Sprite _hookGroundSprite;
    [SerializeField] private float _minRopeLength = 0.5f;
    [SerializeField] private float _maxRopeLength = 10f;
    private bool _reelOutInput;
    private bool _reelInInput;
    [SerializeField, Tooltip("휨을 표현할 관절 개수")] private int _curveResolution = 10;
    [SerializeField, Tooltip("아래로 늘어지는 정도")] private float _sagMultiplier = 1.5f;
    [SerializeField] private Vector2 _visualOffset = new Vector2(0.3f, 0.2f);
    [Header("Swing")]
    [SerializeField] private float _swingControlForce = 15f;
    [SerializeField, Tooltip("스윙 입력을 허용하는 줄 팽팽함 판정 여유 거리")] private float _tautTolerance = 0.15f;

    [Header("Release Visual")]
    [SerializeField, Tooltip("해제 시 로프가 캐릭터로 되돌아가는 데 걸리는 시간")] private float _retractDuration = 0.15f;

    [Header("Hook Attach Feel")]
    [SerializeField, Tooltip("훅이 걸리는 순간 걸린 방향(x+/x-)으로 주는 수평 임펄스")] private float _hookHorizontalImpulse = 3f;
    [SerializeField, Tooltip("훅이 걸리는 순간 살짝 튀어오르는 수직 임펄스")] private float _hookVerticalImpulse = 2f;

    [Header("Hold (완전히 감아올렸을 때)")]
    [SerializeField, Tooltip("체크하면 로프를 끝까지 감아올렸을 때 발사되지 않고 그 자리에 매달려 정지합니다")]
    private bool _holdOnFullReel = true;
    [SerializeField, Tooltip("Hold 상태에서 입력으로 매달린 채 움직일 수 있는 속도")]
    private float _holdMoveSpeed = 3f;

    [Header("Collision")]
    [Tooltip("로프 구속으로 인한 순간이동이 플랫폼/지형을 뚫고 지나가지 않도록 검사할 레이어 (Ground)")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField, Tooltip("지형에 막혔을 때 표면으로부터 띄우는 여유 거리")] private float _surfaceSkin = 0.05f;

    [Header("Corner Wrap (모서리 꺾임)")]
    [SerializeField, Tooltip("한 번에 감길 수 있는 최대 꺾임(pivot) 개수 - 안전장치")] private int _maxWrapPoints = 8;
    [SerializeField, Tooltip("모서리에 새 pivot을 찍을 때 표면에서 띄우는 여유 거리")] private float _wrapSkin = 0.02f;

    [Header("Reel (로프 감기/풀기)")]
    [SerializeField] private bool _allowReel = true;
    [SerializeField] private float _reelSpeed = 3f;
    [SerializeField] private float _reelAcceleration = 15f;
    [SerializeField] private float _launchVelocityMultiplier = 1.2f;
    [SerializeField] private float _reelForce = 35f;      // 당기는 힘
    [SerializeField] private float _maxReelSpeed = 25f;   // 최대 감기 속도 (자동 감기 X)
    [SerializeField, Tooltip("수동 감기(W) 시 pivot 방향으로 당겨지는 최대 속도 (자동 감기 X는 Max Reel Speed 사용)")]
    private float _manualPullMaxSpeed = 4f;
    [SerializeField] private float _flipThreshold = 1f;

    private Rigidbody2D _rb;
    private PlayerMovement _movement;
    private PlayerJump _jump;

    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    private Sprite _originalSprite;

    private float _currentReelSpeed;
    private float _originalGravityScale;
    private bool _gravityDisabledForHold;

    // _ropePivots[0] = 최초 훅이 걸린 고정 앵커 지점
    // _ropePivots[마지막] = 현재 스윙/구속의 기준이 되는 활성 회전축
    // (모서리에 걸릴 때마다 새 pivot이 뒤에 추가되고, 풀리면 다시 제거됨)
    private readonly List<Vector2> _ropePivots = new List<Vector2>();
    private float _ropeLength; // 전체 로프 길이 예산 (리엘로 감소, 고정 구간 길이는 여기서 차감됨)
    private bool _isAutoReeling;
    private Coroutine _retractRoutine;
    private bool _isHolding;
    private Vector2 _prevWrapCheckPos; // 지난 물리 프레임의 플레이어 위치 (고속 스윙 wrap 터널링 보완용)

    // 훅을 건 대상. 움직이는 플랫폼처럼 HookPoint가 매 프레임 바뀌는 대상이면,
    // FixedUpdate마다 _ropePivots[0](최초 앵커)을 이 값으로 다시 동기화해서
    // 로프가 훅을 건 "순간의 좌표"가 아니라 대상의 "현재 위치"를 따라가게 한다.
    private IHookable _anchorHookable;

    public bool IsHooking { get; private set; }
    public bool IsHolding => _isHolding;

    private Vector2 ActivePivot => _ropePivots[_ropePivots.Count - 1];

    /// <summary>혀(TongueTip)가 벽에 붙어있는 것처럼 보여줄 최초 훅 지점. 코너에 감겨도 바뀌지 않습니다.</summary>
    public Vector2 AnchorPoint => _ropePivots.Count > 0 ? _ropePivots[0] : (Vector2)transform.position;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _movement = GetComponent<PlayerMovement>();
        _jump = GetComponent<PlayerJump>();

        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();

        if (_jump != null)
            _jump.OnLanded += HandleLanded;
    }
    private void HandleLanded(float fallDistance)
    {
        if (IsHooking)
            Release();
    }
    private void OnDestroy()
    {
        if (_jump != null)
            _jump.OnLanded -= HandleLanded;
    }
    public void StartAutoReel()
    {
        if (IsHooking)
        {
            _isAutoReeling = true;
            _currentReelSpeed = _reelSpeed;
        }
    }
    public void SetReelInput(bool isPressed)
    {
        _reelInInput = isPressed;
    }

    public void StartHook(IHookable hookable)
    {
        Vector3 point = hookable.HookPoint;

        _isAutoReeling = false;
        _isHolding = false;
        RestoreGravityAfterHold();
        bool wasHooking = IsHooking;

        // 재훅 시 이전 해제의 되돌아오는 모션이 뒤늦게 새 로프를 꺼버리지 않도록 정리
        if (_retractRoutine != null)
        {
            StopCoroutine(_retractRoutine);
            _retractRoutine = null;
        }

        _anchorHookable = hookable;
        _ropePivots.Clear();
        _ropePivots.Add(point);
        _ropeLength = Vector2.Distance(_rb.position, point);
        _ropeLength = Mathf.Clamp(_ropeLength, _minRopeLength, _maxRopeLength);
        _prevWrapCheckPos = _rb.position;
        IsHooking = true;

        // 훅이 걸리는 순간 걸린 방향으로 살짝 당겨지는 손맛 + 작은 점프감을 줍니다.
        float dirX = Mathf.Sign(point.x - _rb.position.x);
        _rb.linearVelocity += new Vector2(dirX * _hookHorizontalImpulse, _hookVerticalImpulse);

        if (_ropeVisual != null) _ropeVisual.enabled = true;
        if (_spriteRenderer != null && _hookSprite != null)
        {
            // 이미 훅 중일 때 재훅하면 originalSprite가 훅 스프라이트로 덮어씌워지므로,
            // 훅 상태가 아니었을 때만 원본 스프라이트를 저장합니다.
            if (!wasHooking)
                _originalSprite = _spriteRenderer.sprite;
            _spriteRenderer.sprite = _hookSprite;
        }
        if (_animator != null) _animator.enabled = false;
        if (_movement != null) _movement.MovementLocked = true;
        if (_jump != null) _jump.JumpPhysicsLocked = true;
    }

    public void Release()
    {
        if (!IsHooking) return;

        IsHooking = false;
        _isHolding = false;
        _isAutoReeling = false;
        _anchorHookable = null;
        RestoreGravityAfterHold();

        if (_ropeVisual != null && _ropePivots.Count > 0)
        {
            if (_retractRoutine != null) StopCoroutine(_retractRoutine);
            _retractRoutine = StartCoroutine(RetractRopeVisual(new List<Vector2>(_ropePivots)));
        }
        _ropePivots.Clear();

        if (_animator != null) _animator.enabled = true;
        if (_spriteRenderer != null && _originalSprite != null)
        {
            _spriteRenderer.sprite = _originalSprite;
        }

        if (_movement != null) _movement.MovementLocked = false;
        if (_jump != null) _jump.JumpPhysicsLocked = false;
    }

    public void SetReelOutInput(bool isPressed)
    {
        _reelOutInput = isPressed;
    }

    private void FixedUpdate()
    {
        if (!IsHooking) return;

        SyncAnchorPivot();

        if (_isHolding)
        {
            UpdateHold();
            return;
        }

        UpdateSwing();
    }

    /// <summary>
    /// 최초 앵커(_ropePivots[0])를 훅 대상의 현재 HookPoint로 매 프레임 갱신합니다.
    /// 대상이 정적 지형이면 값이 그대로 유지되고, 움직이는 플랫폼처럼 HookPoint가
    /// 매 프레임 바뀌는 대상이면 로프가 그 이동을 그대로 따라갑니다.
    /// </summary>
    private void SyncAnchorPivot()
    {
        if (_anchorHookable == null || _ropePivots.Count == 0) return;
        _ropePivots[0] = _anchorHookable.HookPoint;
    }

    private void RestoreGravityAfterHold()
    {
        if (_gravityDisabledForHold)
        {
            _rb.gravityScale = _originalGravityScale;
            _gravityDisabledForHold = false;
        }
    }

    private void LateUpdate()
    {
        if (IsHooking)
        {
            UpdateRopeVisual();
        }
    }
}
