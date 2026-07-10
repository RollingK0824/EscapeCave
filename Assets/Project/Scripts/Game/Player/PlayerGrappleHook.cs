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

    // ropePivots[0] = 최초 훅이 걸린 고정 앵커 지점
    // ropePivots[마지막] = 현재 스윙/구속의 기준이 되는 활성 회전축
    // (모서리에 걸릴 때마다 새 pivot이 뒤에 추가되고, 풀리면 다시 제거됨)
    private readonly List<Vector2> ropePivots = new List<Vector2>();
    private float ropeLength; // 전체 로프 길이 예산 (리엘로 감소, 고정 구간 길이는 여기서 차감됨)
    private bool isAutoReeling;

    private Vector2 lastSwingDirection;


    public bool IsHooking { get; private set; }

    private Vector2 ActivePivot => ropePivots[ropePivots.Count - 1];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        jump = GetComponent<PlayerJump>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

     
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

        ropePivots.Clear();
        ropePivots.Add(point);
        ropeLength = Vector2.Distance(rb.position, point);
        ropeLength = Mathf.Clamp(ropeLength, minRopeLength, maxRopeLength);
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
        ropePivots.Clear();
        if (ropeVisual != null) ropeVisual.enabled = false;

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
        UpdateSwing();
    }
    private void OnReelComplete()
    {
        isAutoReeling = false;


        rb.linearVelocity *= launchVelocityMultiplier;

        Release();
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
            if (wrapHit.collider != null)
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

    private void UpdateSwing()
    {
        // 0. 모서리 감김/풀림 갱신
        UpdateRopeWrap();

        Vector2 activePivot = ActivePivot;
        float fixedLength = GetFixedSegmentsLength();
        // 이미 감긴 고정 구간 길이를 뺀, 마지막 구간이 실제로 쓸 수 있는 로프 길이
        float segmentAllowance = Mathf.Max(minRopeLength, ropeLength - fixedLength);
        // 지면에서 걸어가다가 최대 로프 길이를 넘으면 갈고리 해제
        
    
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
        if (allowReel && reelInInput)
        {
            Vector2 dir = (activePivot - rb.position).normalized;

            rb.AddForce(dir * reelForce, ForceMode2D.Force);

            ropeLength -= reelSpeed * Time.fixedDeltaTime;
            ropeLength = Mathf.Max(minRopeLength, ropeLength);

            segmentAllowance = Mathf.Max(minRopeLength, ropeLength - fixedLength);
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
        Vector2 toNewPos = newPos - activePivot;
        float dist = toNewPos.magnitude;

        // 오직 줄 길이를 초과해서 팽팽해질 때만 위치를 강제 구속
        if (dist > segmentAllowance)
        {
            Vector2 constrainedDir = toNewPos.normalized;
            Vector2 targetPos = activePivot + constrainedDir * segmentAllowance;

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
                }
            }

            rb.position = targetPos;

            float radialSpeed = Vector2.Dot(rb.linearVelocity, constrainedDir);

            if (radialSpeed > 0)
            {
                rb.linearVelocity -= constrainedDir * radialSpeed;
            }
        }
        if (Mathf.Abs(rb.linearVelocity.x) > 1f)
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

        // 🔥 캐릭터가 바라보는 방향에 따라 오프셋 좌우 반전
        Vector2 currentOffset = visualOffset;
        if (!movement.IsFacingRight)
        {
            currentOffset.x = -currentOffset.x;
        }

        // 🔥 실제 선이 그려질 캐릭터 쪽 끝점 (배꼽이 아니라 '입' 위치)
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