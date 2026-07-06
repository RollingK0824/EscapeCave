using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 진자운동(스윙) 물리와, 거리가 짧을 때 로프가 아래로 휘어지는 시각적 효과를 담당합니다.
/// (모서리 꺾임 기능 제거 버전)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerGrappleHook : MonoBehaviour
{
    [Header("Rope & Visuals")]
    [SerializeField] private LineRenderer ropeVisual;
    [SerializeField] private Sprite hookSprite;
    [SerializeField] private Sprite hookGroundSprite;
    [SerializeField] private float minRopeLength = 0.5f;
    [SerializeField] private float maxRopeLength = 10f;
    [SerializeField, Tooltip("휨을 표현할 관절 개수")] private int curveResolution = 10;
    [SerializeField, Tooltip("아래로 늘어지는 정도")] private float sagMultiplier = 1.5f;
    [SerializeField] private Vector2 visualOffset = new Vector2(0.3f, 0.2f);
    [Header("Swing")]
    [SerializeField] private float swingControlForce = 15f;

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

    private Vector2 hookPoint; // 리스트 대신 단일 앵커 포인트만 사용
    private float ropeLength;
    private bool reelInInput;
    private bool isAutoReeling;

    private Vector2 lastSwingDirection;
 
    private PlayerControls inputActions;

    public bool IsHooking { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        jump = GetComponent<PlayerJump>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        inputActions = new PlayerControls();
        inputActions.Player.Reel.performed += ctx => StartAutoReel();
        inputActions.Player.Jump.performed += ctx => Release();
    }
    private void OnEnable()
    {
        inputActions?.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions?.Player.Disable();
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

        hookPoint = point;
        ropeLength = maxRopeLength;

        IsHooking = true;
        if (ropeVisual != null) ropeVisual.enabled = true;
        if (spriteRenderer != null && hookSprite != null)
        {
            originalSprite = spriteRenderer.sprite; 
            spriteRenderer.sprite = hookSprite;    
        }
        if (animator != null) animator.enabled = false;
        movement.MovementLocked = true;
        if (jump != null) jump.JumpPhysicsLocked = true;
    }

    public void Release()
    {
        if (!IsHooking) return;

        IsHooking = false;
        if (ropeVisual != null) ropeVisual.enabled = false;

        if (animator != null) animator.enabled = true;
        if (spriteRenderer != null && originalSprite != null)
        {
            spriteRenderer.sprite = originalSprite;
        }

        if (movement != null) movement.MovementLocked = false;
        if (jump != null) jump.JumpPhysicsLocked = false;
    }

    private void FixedUpdate()
    {
        if (!IsHooking) return;
        UpdateSwing();
    }
    private void OnReelComplete()
    {
        isAutoReeling = false;

      
        rb.linearVelocity *= launchVelocityMultiplier;

        Release();
    }
    private void UpdateSwing()
    {
        // 1. 지면 체크 (PlayerJump 컴포넌트의 IsGrounded 활용)
        bool isGrounded = jump != null && jump.IsGrounded;

        // 2. 지면 상태에 따라 이동 잠금 실시간 스위칭
        if (spriteRenderer != null && animator != null && !animator.enabled)
        {
            if (isGrounded)
            {
                // 땅에 닿으면 웅크려서 버티는 스프라이트로!
                if (hookGroundSprite != null) spriteRenderer.sprite = hookGroundSprite;
            }
            else
            {
                // 공중에 뜨면 다시 붕 뜬 공격 모션 스프라이트로!
                if (hookSprite != null) spriteRenderer.sprite = hookSprite;
            }
        }
        if (isGrounded)
        {
            if (movement != null) movement.MovementLocked = false;
        }
        else
        {
            if (movement != null) movement.MovementLocked = true;
        }

        // --- 로프 자동 감기 (X키) ---
        if (allowReel && isAutoReeling)
        {
            currentReelSpeed += reelAcceleration * Time.fixedDeltaTime;
            currentReelSpeed = Mathf.Min(currentReelSpeed, maxReelSpeed);

            Vector2 dir = (hookPoint - rb.position).normalized;

            rb.AddForce(dir * reelForce, ForceMode2D.Force);

            ropeLength -= currentReelSpeed * Time.fixedDeltaTime;
            ropeLength = Mathf.Max(minRopeLength, ropeLength);

            if (Vector2.Distance(rb.position, hookPoint) <= minRopeLength + 0.15f)
            {
                OnReelComplete();
                return;
            }
        }

        // --- 공중 스윙 조작 ---
        if (!isGrounded)
        {
            Vector2 toPlayer = rb.position - hookPoint;
            Vector2 radialDir = toPlayer.normalized;
            Vector2 tangentDir = new Vector2(-radialDir.y, radialDir.x);

            float moveX = movement != null ? movement.MoveInput.x : 0f;
            if (moveX != 0f)
            {
       
                float tangentSign = Mathf.Sign(Vector2.Dot(tangentDir, Vector2.right));
                rb.linearVelocity += tangentDir * tangentSign * moveX * swingControlForce * Time.fixedDeltaTime;
            }
        }

        // --- 거리 구속 구간 ---
        Vector2 newPos = rb.position + rb.linearVelocity * Time.fixedDeltaTime;
        Vector2 toNewPos = newPos - hookPoint;
        float dist = toNewPos.magnitude;

        // 오직 줄 길이를 초과해서 팽팽해질 때만 위치를 강제 구속
        if (dist > ropeLength)
        {
            Vector2 constrainedDir = toNewPos.normalized;
            rb.position = hookPoint + constrainedDir * ropeLength;

            float radialSpeed = Vector2.Dot(rb.linearVelocity, constrainedDir);

            if (radialSpeed > 0)
            {
                rb.linearVelocity -= constrainedDir * radialSpeed;
            }
        }
        if (Mathf.Abs(rb.linearVelocity.x) > 1f)
            movement.SetFacing(rb.linearVelocity.x > 0);
        // 선 그리기 및 스프라이트 갱신
        UpdateRopeVisual();
    }
    private void UpdateRopeVisual()
    {
        if (ropeVisual == null) return;

        // 🔥 캐릭터가 바라보는 방향에 따라 오프셋 좌우 반전
        Vector2 currentOffset = visualOffset;
        if (!movement.IsFacingRight)
        {
            currentOffset.x = -currentOffset.x;
        }

        // 🔥 실제 선이 그려질 캐릭터 쪽 끝점 (배꼽이 아니라 '입' 위치)
        Vector2 playerVisualPoint = rb.position + currentOffset;

        // 거리를 계산할 때도 입 위치를 기준으로 계산
        float currentDist = Vector2.Distance(hookPoint, playerVisualPoint);
        float slack = Mathf.Max(0, ropeLength - currentDist); // 여유 길이 계산

        // 1. 줄이 팽팽할 때 (직선)
        if (slack <= 0.01f)
        {
            ropeVisual.positionCount = 2;
            ropeVisual.SetPosition(0, hookPoint);
            ropeVisual.SetPosition(1, playerVisualPoint); // rb.position 대신 입 위치 사용
        }
        // 2. 줄에 여유가 있을 때 (베지에 곡선으로 축 늘어짐)
        else
        {
            ropeVisual.positionCount = curveResolution + 1;
            Vector2 startPoint = hookPoint;
            Vector2 endPoint = playerVisualPoint; // 여기도 입 위치 사용

            Vector2 midPoint = (startPoint + endPoint) / 2f;
            // 여유 길이(slack)에 비례해서 중간 지점을 아래로 당김
            Vector2 controlPoint = new Vector2(midPoint.x, midPoint.y - (slack * sagMultiplier));

            for (int i = 0; i <= curveResolution; i++)
            {
                float t = i / (float)curveResolution;
                Vector2 curvePoint = CalculateQuadraticBezierPoint(t, startPoint, controlPoint, endPoint);
                ropeVisual.SetPosition(i, curvePoint);
            }
        }
    }


    private Vector2 CalculateQuadraticBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector2 p = uu * p0;
        p += 2 * u * t * p1;
        p += tt * p2;

        
        return p;
    }

}
