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
        if (!_allowReel) return false;

        if (_isAutoReeling)
        {
            _currentReelSpeed += _reelAcceleration * Time.fixedDeltaTime;
            _currentReelSpeed = Mathf.Min(_currentReelSpeed, _maxReelSpeed);

            ReelTowardPivot(activePivot, _currentReelSpeed, _maxReelSpeed, fixedLength, ref segmentAllowance);

            if (Vector2.Distance(_rb.position, activePivot) <= _minRopeLength + REEL_COMPLETE_TOLERANCE)
            {
                OnReelComplete();
                return true;
            }
        }
        // 자동 감기 중에는 수동 감기 입력을 무시해 힘이 중복 적용되지 않도록 합니다.
        else if (_reelInInput)
        {
            ReelTowardPivot(activePivot, _reelSpeed, _manualPullMaxSpeed, fixedLength, ref segmentAllowance);

            if (Vector2.Distance(_rb.position, activePivot) <= _minRopeLength + REEL_COMPLETE_TOLERANCE)
            {
                OnReelComplete();
                return true;
            }
        }

        // 감기(자동/수동) 중에는 풀기 입력을 무시해, 당기는 힘과 로프 늘리기가
        // 같은 프레임에 동시 적용되어 상쇄되는 것을 막습니다.
        else if (_reelOutInput)
        {
            _ropeLength += _reelSpeed * Time.fixedDeltaTime;
            _ropeLength = Mathf.Min(_maxRopeLength, _ropeLength);
            segmentAllowance = Mathf.Max(_minRopeLength, _ropeLength - fixedLength);
        }

        return false;
    }

    /// <summary>
    /// activePivot 방향으로 당기는 힘을 가하고, 그만큼 로프 길이 예산을 줄입니다.
    /// </summary>
    private void ReelTowardPivot(Vector2 activePivot, float speed, float maxPullSpeed, float fixedLength, ref float segmentAllowance)
    {
        Vector2 dir = (activePivot - _rb.position).normalized;

        // pivot 방향 속도 성분이 이미 상한(maxPullSpeed)에 도달했으면 더 이상 힘을 주지 않는다.
        // 자동 감기(X)는 상한이 높아(_maxReelSpeed) 지금처럼 고속으로 빨려가는 손맛이 유지되고,
        // 수동 감기(W)는 낮은 상한(_manualPullMaxSpeed)이라 일정 속도로 차분하게 감긴다.
        float radialSpeed = Vector2.Dot(_rb.linearVelocity, dir);
        if (radialSpeed < maxPullSpeed)
        {
            _rb.AddForce(dir * _reelForce, ForceMode2D.Force);
        }

        // 스윙 접선 속도 + 당기는 힘이 같은 방향(예: 스윙 궤적이 위로 꺾이는 지점)에서
        // 겹치면 순간적으로 튀어오르는 현상이 있어서, radial 성분뿐 아니라
        // 최종 속도 크기 자체도 상한으로 눌러준다.
        if (_rb.linearVelocity.magnitude > maxPullSpeed)
        {
            _rb.linearVelocity = _rb.linearVelocity.normalized * maxPullSpeed;
        }

        _ropeLength -= speed * Time.fixedDeltaTime;
        _ropeLength = Mathf.Max(_minRopeLength, _ropeLength);
        segmentAllowance = Mathf.Max(_minRopeLength, _ropeLength - fixedLength);
    }

    private void OnReelComplete()
    {
        _isAutoReeling = false;
        _reelInInput = false;

        if (_holdOnFullReel)
        {
            _isHolding = true;
            // _rb.linearVelocity를 매 FixedUpdate마다 0으로 되돌리는 것만으로는
            // 물리 엔진이 그 사이 프레임에 적용하는 중력만큼 위치가 계속 미세하게
            // 아래로 밀리는 것을 막지 못합니다(속도는 다시 0이 되어도 이동한 거리는
            // 남기 때문). 중력 자체를 꺼서 완전히 정지 상태를 유지합니다.
            if (!_gravityDisabledForHold)
            {
                _originalGravityScale = _rb.gravityScale;
                _rb.gravityScale = 0f;
                _gravityDisabledForHold = true;
            }
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        _rb.linearVelocity *= _launchVelocityMultiplier;
        Release();
    }
}
