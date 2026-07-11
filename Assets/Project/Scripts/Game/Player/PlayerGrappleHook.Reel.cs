using UnityEngine;

/// <summary>
/// 로프 감기(자동/수동)와 풀기, 완전히 감아올렸을 때의 처리(Hold/Launch)를 담당합니다.
/// </summary>
public partial class PlayerGrappleHook
{
    /// <summary>
    /// 자동/수동 감기 및 풀기 입력을 처리합니다.
    /// 로프를 다 감아 훅 상태가 전환되었으면(Hold 또는 Launch) true를 반환합니다.
    /// </summary>
    private bool UpdateReel(Vector2 activePivot, float fixedLength, ref float segmentAllowance)
    {
        if (!allowReel) return false;

        if (isAutoReeling)
        {
            currentReelSpeed += reelAcceleration * Time.fixedDeltaTime;
            currentReelSpeed = Mathf.Min(currentReelSpeed, maxReelSpeed);

            ReelTowardPivot(activePivot, currentReelSpeed, fixedLength, ref segmentAllowance);

            if (Vector2.Distance(rb.position, activePivot) <= minRopeLength + ReelCompleteTolerance)
            {
                OnReelComplete();
                return true;
            }
        }
        // 자동 감기 중에는 수동 감기 입력을 무시해 힘이 중복 적용되지 않도록 합니다.
        else if (reelInInput)
        {
            ReelTowardPivot(activePivot, reelSpeed, fixedLength, ref segmentAllowance);

            if (Vector2.Distance(rb.position, activePivot) <= minRopeLength + ReelCompleteTolerance)
            {
                OnReelComplete();
                return true;
            }
        }

        if (reelOutInput)
        {
            ropeLength += reelSpeed * Time.fixedDeltaTime;
            ropeLength = Mathf.Min(maxRopeLength, ropeLength);
            segmentAllowance = Mathf.Max(minRopeLength, ropeLength - fixedLength);
        }

        return false;
    }

    /// <summary>
    /// activePivot 방향으로 당기는 힘을 가하고, 그만큼 로프 길이 예산을 줄입니다.
    /// </summary>
    private void ReelTowardPivot(Vector2 activePivot, float speed, float fixedLength, ref float segmentAllowance)
    {
        Vector2 dir = (activePivot - rb.position).normalized;
        rb.AddForce(dir * reelForce, ForceMode2D.Force);

        ropeLength -= speed * Time.fixedDeltaTime;
        ropeLength = Mathf.Max(minRopeLength, ropeLength);
        segmentAllowance = Mathf.Max(minRopeLength, ropeLength - fixedLength);
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
}
