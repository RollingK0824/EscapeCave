using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    // 로프를 다 감았다고 판정하는 여유 거리 (activePivot과의 거리가 minRopeLength + 이 값 이하면 완료)
    private const float ReelCompleteTolerance = 0.15f;
    // 로프 시각 효과에서 "팽팽함"으로 취급할 여유 길이(slack) 임계값
    private const float TautSlackThreshold = 0.01f;
    // 방향 벡터 정규화 전에 0 나눗셈을 막기 위한 최소 크기
    private const float MinVectorMagnitude = 0.0001f;
    // 원-직선 교차 공식에서 이차항 계수가 0에 가까운지 판정하는 허용 오차
    private const float QuadraticSolverEpsilon = 0.000001f;

    [Header("Rope & Visuals")]
    [SerializeField] private LineRenderer ropeVisual;
    [SerializeField] private Sprite hookSprite;
    [SerializeField] private Sprite hookGroundSprite;
    [SerializeField] private float minRopeLength = 0.5f;
    [SerializeField] private float maxRopeLength = 10f;
    private bool reelOutInput;
    private bool reelInInput;
    [SerializeField, Tooltip("휨을 표현할 관절 개수")] private int curveResolution = 10;
    [SerializeField, Tooltip("아래로 늘어지는 정도")] private float sagMultiplier = 1.5f;
    [SerializeField] private Vector2 visualOffset = new Vector2(0.3f, 0.2f);
    [Header("Swing")]
    [SerializeField] private float swingControlForce = 15f;
    [SerializeField, Tooltip("스윙 입력을 허용하는 줄 팽팽함 판정 여유 거리")] private float tautTolerance = 0.15f;

    [Header("Release Visual")]
    [SerializeField, Tooltip("해제 시 로프가 캐릭터로 되돌아가는 데 걸리는 시간")] private float retractDuration = 0.15f;

    [Header("Hook Attach Feel")]
    [SerializeField, Tooltip("훅이 걸리는 순간 걸린 방향(x+/x-)으로 주는 수평 임펄스")] private float hookHorizontalImpulse = 3f;
    [SerializeField, Tooltip("훅이 걸리는 순간 살짝 튀어오르는 수직 임펄스")] private float hookVerticalImpulse = 2f;

    [Header("Hold (완전히 감아올렸을 때)")]
    [SerializeField, Tooltip("체크하면 로프를 끝까지 감아올렸을 때 발사되지 않고 그 자리에 매달려 정지합니다")]
    private bool holdOnFullReel = true;

    [Header("Collision")]
    [Tooltip("로프 구속으로 인한 순간이동이 플랫폼/지형을 뚫고 지나가지 않도록 검사할 레이어 (Ground)")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField, Tooltip("지형에 막혔을 때 표면으로부터 띄우는 여유 거리")] private float surfaceSkin = 0.05f;

    [Header("Corner Wrap (모서리 꺾임)")]
    [SerializeField, Tooltip("한 번에 감길 수 있는 최대 꺾임(pivot) 개수 - 안전장치")] private int maxWrapPoints = 8;
    [SerializeField, Tooltip("모서리에 새 pivot을 찍을 때 표면에서 띄우는 여유 거리")] private float wrapSkin = 0.02f;

    [Header("Reel (로프 감기/풀기)")]
    [SerializeField] private bool allowReel = true;
    [SerializeField] private float reelSpeed = 3f;
    [SerializeField] private float reelAcceleration = 15f;
    [SerializeField] private float launchVelocityMultiplier = 1.2f;
    [SerializeField] private float reelForce = 35f;      // 당기는 힘
    [SerializeField] private float maxReelSpeed = 25f;   // 최대 감기 속도
    [SerializeField] private float flipThreshold = 1f;

    private Rigidbody2D rb;
    private PlayerMovement movement;
    private PlayerJump jump;

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Sprite originalSprite;

    private float currentReelSpeed;
    private float originalGravityScale;
    private bool gravityDisabledForHold;

    // ropePivots[0] = 최초 훅이 걸린 고정 앵커 지점
    // ropePivots[마지막] = 현재 스윙/구속의 기준이 되는 활성 회전축
    // (모서리에 걸릴 때마다 새 pivot이 뒤에 추가되고, 풀리면 다시 제거됨)
    private readonly List<Vector2> ropePivots = new List<Vector2>();
    private float ropeLength; // 전체 로프 길이 예산 (리엘로 감소, 고정 구간 길이는 여기서 차감됨)
    private bool isAutoReeling;
    private Coroutine retractRoutine;
    private bool isHolding;

    public bool IsHooking { get; private set; }
    public bool IsHolding => isHolding;

    private Vector2 ActivePivot => ropePivots[ropePivots.Count - 1];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        jump = GetComponent<PlayerJump>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (jump != null)
            jump.OnLanded += HandleLanded;
    }
    private void HandleLanded()
    {
        if (IsHooking)
            Release();
    }
    private void OnDestroy()
    {
        if (jump != null)
            jump.OnLanded -= HandleLanded;
    }
    public void StartAutoReel()
    {
        if (IsHooking)
        {
            isAutoReeling = true;
            currentReelSpeed = reelSpeed;
        }
    }
    public void SetReelInput(bool isPressed)
    {
        reelInInput = isPressed;
    }

    public void StartHook(Vector3 point)
    {
        isAutoReeling = false;
        isHolding = false;
        RestoreGravityAfterHold();
        bool wasHooking = IsHooking;

        // 재훅 시 이전 해제의 되돌아오는 모션이 뒤늦게 새 로프를 꺼버리지 않도록 정리
        if (retractRoutine != null)
        {
            StopCoroutine(retractRoutine);
            retractRoutine = null;
        }

        ropePivots.Clear();
        ropePivots.Add(point);
        ropeLength = Vector2.Distance(rb.position, point);
        ropeLength = Mathf.Clamp(ropeLength, minRopeLength, maxRopeLength);
        IsHooking = true;

        // 훅이 걸리는 순간 걸린 방향으로 살짝 당겨지는 손맛 + 작은 점프감을 줍니다.
        float dirX = Mathf.Sign(point.x - rb.position.x);
        rb.linearVelocity += new Vector2(dirX * hookHorizontalImpulse, hookVerticalImpulse);

        if (ropeVisual != null) ropeVisual.enabled = true;
        if (spriteRenderer != null && hookSprite != null)
        {
            // 이미 훅 중일 때 재훅하면 originalSprite가 훅 스프라이트로 덮어씌워지므로,
            // 훅 상태가 아니었을 때만 원본 스프라이트를 저장합니다.
            if (!wasHooking)
                originalSprite = spriteRenderer.sprite;
            spriteRenderer.sprite = hookSprite;
        }
        if (animator != null) animator.enabled = false;
        if (movement != null) movement.MovementLocked = true;
        if (jump != null) jump.JumpPhysicsLocked = true;
    }

    public void Release()
    {
        if (!IsHooking) return;

        IsHooking = false;
        isHolding = false;
        isAutoReeling = false;
        RestoreGravityAfterHold();

        if (ropeVisual != null && ropePivots.Count > 0)
        {
            if (retractRoutine != null) StopCoroutine(retractRoutine);
            retractRoutine = StartCoroutine(RetractRopeVisual(new List<Vector2>(ropePivots)));
        }
        ropePivots.Clear();

        if (animator != null) animator.enabled = true;
        if (spriteRenderer != null && originalSprite != null)
        {
            spriteRenderer.sprite = originalSprite;
        }

        if (movement != null) movement.MovementLocked = false;
        if (jump != null) jump.JumpPhysicsLocked = false;
    }

    public void SetReelOutInput(bool isPressed)
    {
        reelOutInput = isPressed;
    }

    private void FixedUpdate()
    {
        if (!IsHooking) return;

        if (isHolding)
        {
            rb.linearVelocity = Vector2.zero;
            if (movement != null) movement.MovementLocked = true;
            return;
        }

        UpdateSwing();
    }

    private void RestoreGravityAfterHold()
    {
        if (gravityDisabledForHold)
        {
            rb.gravityScale = originalGravityScale;
            gravityDisabledForHold = false;
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
