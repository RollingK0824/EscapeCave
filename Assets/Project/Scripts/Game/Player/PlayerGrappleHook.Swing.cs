using UnityEngine;

/// <summary>
/// 진자운동(스윙) 물리와 모서리 꺾임(Corner Wrap) 로직.
/// </summary>
public partial class PlayerGrappleHook
{
    private void UpdateSwing()
    {
        // 0. 모서리 감김/풀림 갱신
        UpdateRopeWrap();

        Vector2 activePivot = ActivePivot;
        float fixedLength = GetFixedSegmentsLength();
        // 이미 감긴 고정 구간 길이를 뺀, 마지막 구간이 실제로 쓸 수 있는 로프 길이
        float segmentAllowance = Mathf.Max(minRopeLength, ropeLength - fixedLength);

        bool isGrounded = jump != null && jump.IsGrounded;
        UpdateHookSpriteAndLock(isGrounded);

        if (UpdateReel(activePivot, fixedLength, ref segmentAllowance))
            return; // 이번 프레임에 로프를 다 감아 훅 상태가 전환됨

        if (!isGrounded)
            ApplySwingControl(activePivot, segmentAllowance);

        ConstrainToRopeLength(activePivot, segmentAllowance);

        if (movement != null && Mathf.Abs(rb.linearVelocity.x) > flipThreshold)
            movement.SetFacing(rb.linearVelocity.x > 0);
    }

    /// <summary>
    /// 지면 상태에 따라 훅 스프라이트와 이동 잠금을 실시간으로 스위칭합니다.
    /// </summary>
    private void UpdateHookSpriteAndLock(bool isGrounded)
    {
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

        if (movement != null) movement.MovementLocked = !isGrounded;
    }

    /// <summary>
    /// 공중에서 좌우 이동 입력을 접선 방향 힘으로 변환해 스윙을 조작합니다.
    /// 줄이 느슨한 상태(자유낙하)에서 접선 방향 힘을 주면 반동 없이 이동키만으로
    /// 공중을 날아다니게 되므로, 줄이 팽팽하게 걸려 있을 때만 입력을 받습니다.
    /// </summary>
    private void ApplySwingControl(Vector2 activePivot, float segmentAllowance)
    {
        Vector2 toPlayer = rb.position - activePivot;
        float distToPivot = toPlayer.magnitude;

        bool isRopeTaut = distToPivot >= segmentAllowance - tautTolerance;
        if (!isRopeTaut || distToPivot <= MinVectorMagnitude) return;

        Vector2 radialDir = toPlayer / distToPivot;
        Vector2 tangentDir = new Vector2(-radialDir.y, radialDir.x);

        float moveX = movement != null ? movement.MoveInput.x : 0f;
        if (moveX == 0f) return;

        // 수평 입력을 접선에 투영: 줄이 수평에 가까워질수록 입력 효과가
        // 자연스럽게 0으로 수렴해, 진자 한계를 넘어 감아 올라가는 것을 막습니다.
        float tangentInput = Vector2.Dot(Vector2.right * moveX, tangentDir);
        rb.linearVelocity += tangentDir * tangentInput * swingControlForce * Time.fixedDeltaTime;
    }

    /// <summary>
    /// 로프 길이를 초과해서 팽팽해질 때만 플레이어 위치를 원 경계로 구속합니다.
    /// </summary>
    private void ConstrainToRopeLength(Vector2 activePivot, float segmentAllowance)
    {
        Vector2 newPos = rb.position + rb.linearVelocity * Time.fixedDeltaTime;
        float distFromPivot = Vector2.Distance(newPos, activePivot);
        if (distFromPivot <= segmentAllowance) return;

        // 지면에서 활성 pivot 쪽으로 곧장 당기면(원의 중심 방향으로 투영) 걷던 속도가
        // 그대로 원 경계로 순간이동하면서 종종 지면보다 위(공중)로 튀어 오릅니다.
        // 대신 "이번 프레임에 실제로 이동하려던 경로(rb.position -> newPos)"를 따라가다가
        // 원 경계와 만나는 지점에서 멈추게 하면, 걷는 방향 그대로 멈출 뿐 튕겨 나가지 않습니다.
        Vector2 targetPos = GetPathCircleIntersection(rb.position, newPos, activePivot, segmentAllowance);

        // 순간이동(rb.position 강제 대입)은 물리 충돌 처리를 건너뛰기 때문에,
        // 현재 위치 -> 목표 위치 사이에 플랫폼/지형이 있으면 그대로 뚫고 지나가버립니다.
        // 이동 경로 상에 지형이 있는지 미리 검사해서, 있으면 표면 앞에서 멈추도록 보정합니다.
        Vector2 moveDelta = targetPos - rb.position;
        float moveDist = moveDelta.magnitude;

        if (moveDist > MinVectorMagnitude)
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
        if (a < QuadraticSolverEpsilon)
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
}
