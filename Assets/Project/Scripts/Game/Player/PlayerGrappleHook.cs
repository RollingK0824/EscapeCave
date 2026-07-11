using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 진자운동(스윙) 물리와, 거리가 짧을 때 로프가 아래로 휘어지는 시각적 효과를 담당합니다.
/// 로프가 지형 모서리에 걸리면 그 지점을 새 회전축(pivot)으로 삼아 감기는
/// "모서리 꺾임(Corner Wrap)"을 지원합니다.
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

    /// <summary>
    /// 해제된 로프가 순간적으로 사라지지 않고, 걸려 있던 모양 그대로
    /// 캐릭터 위치로 당겨져 돌아오는 모션을 짧게 재생합니다.
    /// </summary>
    private IEnumerator RetractRopeVisual(List<Vector2> releasedPivots)
    {
        float elapsed = 0f;
        while (elapsed < retractDuration)
        {
            float t = elapsed / retractDuration;
            Vector2 playerPoint = rb.position;

            ropeVisual.positionCount = releasedPivots.Count + 1;
            for (int i = 0; i < releasedPivots.Count; i++)
            {
                ropeVisual.SetPosition(i, Vector2.Lerp(releasedPivots[i], playerPoint, t));
            }
            ropeVisual.SetPosition(releasedPivots.Count, playerPoint);

            elapsed += Time.deltaTime;
            yield return null;
        }

        ropeVisual.enabled = false;
        retractRoutine = null;
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
    if (movement != null) movement.MovementLocked = true;  // 추가
    return;
}

        UpdateSwing();
    }
    private void OnReelComplete()
    {
        isAutoReeling = false;
        reelInInput = false;

        if (holdOnFullReel)
        {
            isHolding = true;
            // rb.linearVelocity를 매 FixedUpdate마다 0으로 되돌리는 것만으로는
            // 물리 엔진이 그 사이 프레임에 적용하는 중력만큼 위치가 계속 미세하게
            // 아래로 밀리는 것을 막지 못합니다(속도는 다시 0이 되어도 이동한 거리는
            // 남기 때문). 중력 자체를 꺼서 완전히 정지 상태를 유지합니다.
            if (!gravityDisabledForHold)
            {
                originalGravityScale = rb.gravityScale;
                rb.gravityScale = 0f;
                gravityDisabledForHold = true;
            }
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity *= launchVelocityMultiplier;
        Release();
    }

    private void RestoreGravityAfterHold()
    {
        if (gravityDisabledForHold)
        {
            rb.gravityScale = originalGravityScale;
            gravityDisabledForHold = false;
        }
    }

    /// <summary>
    /// 로프 경로 상에 지형 모서리가 걸리는지(감김) / 걸려있던 모서리를 다 돌아나왔는지(풀림)를
    /// 매 프레임 검사해서 ropePivots 리스트를 갱신합니다.
    /// </summary>
    private void UpdateRopeWrap()
    {
        if (ropePivots.Count == 0) return;

        // 1) 감기 판정: 현재 활성 회전축 <-> 플레이어 사이에 지형이 끼어들었는가
        if (ropePivots.Count < maxWrapPoints)
        {
            Vector2 activePivot = ropePivots[ropePivots.Count - 1];
            RaycastHit2D wrapHit = Physics2D.Linecast(activePivot, rb.position, groundLayer);
            // activePivot 자체가 지형 표면에 딱 붙어 있을 수 있어(예: 훅 앵커),
            // 그 경우 Linecast가 부동소수점 오차로 시작점 바로 그 지형을 다시 맞혀
            // 거리 0에 가까운 가짜 pivot을 만들어버립니다. wrapSkin보다 먼 지점만
            // 실제 새 모서리로 인정합니다.
            if (wrapHit.collider != null && Vector2.Distance(wrapHit.point, activePivot) > wrapSkin)
            {
                Vector2 newPivot = wrapHit.point + wrapHit.normal * wrapSkin;
                ropePivots.Add(newPivot);
                return; // 이번 프레임은 새 pivot 기준으로 다음 프레임부터 계산
            }
        }

        // 2) 풀림 판정: 한 단계 이전 pivot과 플레이어 사이가 뚫려있으면 모서리를 다 돌아나온 것
        if (ropePivots.Count > 1)
        {
            Vector2 prevPivot = ropePivots[ropePivots.Count - 2];
            RaycastHit2D unwrapCheck = Physics2D.Linecast(prevPivot, rb.position, groundLayer);
            if (unwrapCheck.collider == null)
            {
                ropePivots.RemoveAt(ropePivots.Count - 1);
            }
        }

    }

    /// <summary>
    /// 이미 모서리에 감겨 고정된 구간들(첫 pivot ~ 마지막 이전 pivot)의 총 길이.
    /// 이 길이만큼은 "감긴 채 고정"되어 마지막 구간(활성 pivot~플레이어)이 쓸 수 있는
    /// 로프 길이에서 빠지게 됩니다.
    /// </summary>
    private float GetFixedSegmentsLength()
    {
        float length = 0f;
        for (int i = 0; i < ropePivots.Count - 1; i++)
        {
            length += Vector2.Distance(ropePivots[i], ropePivots[i + 1]);
        }
        return length;
    }

    /// <summary>
    /// start(원 안쪽)에서 end(원 바깥)로 이어지는 경로가 center를 중심으로 한
    /// radius 원과 만나는 지점을 반환합니다. start를 지나는 이동 방향을 그대로 유지한 채
    /// 원 경계에서 멈추게 되어, pivot 방향으로 곧장 투영하는 것보다 자연스럽습니다
    /// (걷는 도중 갑자기 위로 튀어 오르는 문제를 방지).
    /// </summary>
    private Vector2 GetPathCircleIntersection(Vector2 start, Vector2 end, Vector2 center, float radius)
    {
        Vector2 d = end - start;
        Vector2 f = start - center;

        float a = Vector2.Dot(d, d);
        if (a < 0.000001f)
            return start;

        float b = 2f * Vector2.Dot(f, d);
        float c = Vector2.Dot(f, f) - radius * radius;

        float discriminant = b * b - 4f * a * c;
        if (discriminant < 0f)
            return center + (end - center).normalized * radius;

        discriminant = Mathf.Sqrt(discriminant);
        float t1 = (-b - discriminant) / (2f * a);
        float t2 = (-b + discriminant) / (2f * a);

        // start가 원 안쪽(또는 경계)에 있다는 전제 하에, 둘 중 양수인 근이
        // 경로가 원을 빠져나가는 지점입니다.
        float t = Mathf.Clamp01(Mathf.Max(t1, t2));
        return start + d * t;
    }

    private void UpdateSwing()
    {
        // 0. 모서리 감김/풀림 갱신
        UpdateRopeWrap();

        Vector2 activePivot = ActivePivot;
        float fixedLength = GetFixedSegmentsLength();
        // 이미 감긴 고정 구간 길이를 뺀, 마지막 구간이 실제로 쓸 수 있는 로프 길이
        float segmentAllowance = Mathf.Max(minRopeLength, ropeLength - fixedLength);

        // 1. 지면 체크 (PlayerJump 컴포넌트의 IsGrounded 활용)
        bool isGrounded = jump != null && jump.IsGrounded;

        // 2. 지면 상태에 따라 이동 잠금 실시간 스위칭
        if (spriteRenderer != null && animator != null && !animator.enabled)
        {
            if (isGrounded)
            {

                if (hookGroundSprite != null) spriteRenderer.sprite = hookGroundSprite;
            }
            else
            {

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

            Vector2 dir = (activePivot - rb.position).normalized;

            rb.AddForce(dir * reelForce, ForceMode2D.Force);

            ropeLength -= currentReelSpeed * Time.fixedDeltaTime;
            ropeLength = Mathf.Max(minRopeLength, ropeLength);
            segmentAllowance = Mathf.Max(minRopeLength, ropeLength - fixedLength);

            if (Vector2.Distance(rb.position, activePivot) <= minRopeLength + 0.15f)
            {
                OnReelComplete();
                return;
            }
        }
        // 자동 감기 중에는 수동 감기 입력을 무시해 힘이 중복 적용되지 않도록 합니다.
        else if (allowReel && reelInInput)
        {
            Vector2 dir = (activePivot - rb.position).normalized;

            rb.AddForce(dir * reelForce, ForceMode2D.Force);

            ropeLength -= reelSpeed * Time.fixedDeltaTime;
            ropeLength = Mathf.Max(minRopeLength, ropeLength);

            segmentAllowance = Mathf.Max(minRopeLength, ropeLength - fixedLength);

            if (Vector2.Distance(rb.position, activePivot) <= minRopeLength + 0.15f)
            {
                OnReelComplete();
                return;
            }
        }
        if (allowReel && reelOutInput)
        {
            ropeLength += reelSpeed * Time.fixedDeltaTime;
            ropeLength = Mathf.Min(maxRopeLength, ropeLength);

            segmentAllowance = Mathf.Max(minRopeLength, ropeLength - fixedLength);
        }
        // --- 공중 스윙 조작 ---
        if (!isGrounded)
        {
            Vector2 toPlayer = rb.position - activePivot;
            float distToPivot = toPlayer.magnitude;

            // 줄이 팽팽하게 걸려 있을 때만 스윙 입력을 받습니다.
            // 줄이 느슨한 상태(자유낙하)에서 접선 방향 힘을 주면
            // 반동 없이 이동키만으로 공중을 날아다니게 되기 때문입니다.
            bool isRopeTaut = distToPivot >= segmentAllowance - tautTolerance;
            if (isRopeTaut && distToPivot > 0.0001f)
            {
                Vector2 radialDir = toPlayer / distToPivot;
                Vector2 tangentDir = new Vector2(-radialDir.y, radialDir.x);

                float moveX = movement != null ? movement.MoveInput.x : 0f;
                if (moveX != 0f)
                {
                    // 수평 입력을 접선에 투영: 줄이 수평에 가까워질수록 입력 효과가
                    // 자연스럽게 0으로 수렴해, 진자 한계를 넘어 감아 올라가는 것을 막습니다.
                    float tangentInput = Vector2.Dot(Vector2.right * moveX, tangentDir);
                    rb.linearVelocity += tangentDir * tangentInput * swingControlForce * Time.fixedDeltaTime;
                }
            }
        }

        // --- 거리 구속 구간 ---
        Vector2 newPos = rb.position + rb.linearVelocity * Time.fixedDeltaTime;
        float distFromPivot = Vector2.Distance(newPos, activePivot);

        // 오직 줄 길이를 초과해서 팽팽해질 때만 위치를 강제 구속
        if (distFromPivot > segmentAllowance)
        {
            // 지면에서 활성 pivot 쪽으로 곧장 당기면(원의 중심 방향으로 투영) 걷던 속도가
            // 그대로 원 경계로 순간이동하면서 종종 지면보다 위(공중)로 튀어 오릅니다.
            // 대신 "이번 프레임에 실제로 이동하려던 경로(rb.position -> newPos)"를 따라가다가
            // 원 경계와 만나는 지점에서 멈추게 하면, 걷는 방향 그대로 멈출 뿐 튕겨 나가지 않습니다.
            Vector2 targetPos = GetPathCircleIntersection(rb.position, newPos, activePivot, segmentAllowance);

            // 🔒 순간이동(rb.position 강제 대입)은 물리 충돌 처리를 건너뛰기 때문에,
            // 현재 위치 -> 목표 위치 사이에 플랫폼/지형이 있으면 그대로 뚫고 지나가버립니다.
            // 이동 경로 상에 지형이 있는지 미리 검사해서, 있으면 표면 앞에서 멈추도록 보정합니다.
            Vector2 moveDelta = targetPos - rb.position;
            float moveDist = moveDelta.magnitude;

            if (moveDist > 0.0001f)
            {
                RaycastHit2D groundHit = Physics2D.Raycast(rb.position, moveDelta.normalized, moveDist, groundLayer);
                if (groundHit.collider != null)
                {
                    targetPos = groundHit.point + groundHit.normal * surfaceSkin;

                    // 벽으로 파고드는 속도 성분을 여기서 지우지 않으면, 다음 프레임에도 같은
                    // 속도로 같은 지점에 다시 튕겨 나와 "벽에 끼여 공중에 뜬 것처럼" 멈춰버립니다.
                    // 아래의 radial 속도 제거는 로프 pivot 기준이라 벽 방향과 다를 수 있으므로,
                    // 여기서 벽의 실제 normal 기준으로 따로 죽여줍니다.
                    float intoWallSpeed = Vector2.Dot(rb.linearVelocity, -groundHit.normal);
                    if (intoWallSpeed > 0)
                    {
                        rb.linearVelocity += groundHit.normal * intoWallSpeed;
                    }
                }
            }

            rb.position = targetPos;

            Vector2 constrainedDir = (targetPos - activePivot).normalized;
            float radialSpeed = Vector2.Dot(rb.linearVelocity, constrainedDir);

            if (radialSpeed > 0)
            {
                rb.linearVelocity -= constrainedDir * radialSpeed;
            }
        }
        if (movement != null && Mathf.Abs(rb.linearVelocity.x) > flipThreshold)
            movement.SetFacing(rb.linearVelocity.x > 0);
        // 선 그리기 및 스프라이트 갱신

    }
    private void LateUpdate()
    {
        if (IsHooking)
        {
            UpdateRopeVisual();
        }
    }
    private void UpdateRopeVisual()
    {
        if (ropeVisual == null || ropePivots.Count == 0) return;

        //  캐릭터가 바라보는 방향에 따라 오프셋 좌우 반전
        Vector2 currentOffset = visualOffset;
        if (!movement.IsFacingRight)
        {
            currentOffset.x = -currentOffset.x;
        }

        //  실제 선이 그려질 캐릭터 쪽 끝점 (배꼽이 아니라 '입' 위치)
        Vector2 playerVisualPoint = rb.position + currentOffset;

        Vector2 activePivot = ActivePivot;
        float fixedLength = GetFixedSegmentsLength();
        float segmentAllowance = Mathf.Max(minRopeLength, ropeLength - fixedLength);

        // 거리를 계산할 때도 입 위치를 기준으로 계산 (마지막 활성 pivot ~ 입 위치)
        float currentDist = Vector2.Distance(activePivot, playerVisualPoint);
        float slack = Mathf.Max(0, segmentAllowance - currentDist); // 여유 길이 계산

        // 1. 줄이 팽팽할 때 (고정된 pivot들을 지나는 직선들 + 마지막 직선)
        if (slack <= 0.01f)
        {
            ropeVisual.positionCount = ropePivots.Count + 1;
            for (int i = 0; i < ropePivots.Count; i++)
            {
                ropeVisual.SetPosition(i, ropePivots[i]);
            }
            ropeVisual.SetPosition(ropePivots.Count, playerVisualPoint); // rb.position 대신 입 위치 사용
        }
        // 2. 줄에 여유가 있을 때 (고정 pivot 구간은 직선, 마지막 구간만 베지에 곡선으로 축 늘어짐)
        else
        {
            int fixedPointCount = ropePivots.Count - 1; // 활성 pivot 이전까지의 고정 pivot 개수
            ropeVisual.positionCount = fixedPointCount + curveResolution + 1;

            for (int i = 0; i < fixedPointCount; i++)
            {
                ropeVisual.SetPosition(i, ropePivots[i]);
            }

            Vector2 startPoint = activePivot;
            Vector2 endPoint = playerVisualPoint; // 여기도 입 위치 사용

            Vector2 midPoint = (startPoint + endPoint) / 2f;
            // 여유 길이(slack)에 비례해서 중간 지점을 아래로 당김
            Vector2 controlPoint = new Vector2(midPoint.x, midPoint.y - (slack * sagMultiplier));

            for (int i = 0; i <= curveResolution; i++)
            {
                float t = i / (float)curveResolution;
                Vector2 curvePoint = CalculateQuadraticBezierPoint(t, startPoint, controlPoint, endPoint);
                ropeVisual.SetPosition(fixedPointCount + i, curvePoint);
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
