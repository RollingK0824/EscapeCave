using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로프 LineRenderer 시각 효과(팽팽함/처짐 곡선, 해제 시 되감기 모션)를 담당합니다.
/// </summary>
public partial class PlayerGrappleHook
{
    /// <summary>
    /// 해제된 로프가 순간적으로 사라지지 않고, 걸려 있던 모양 그대로
    /// 캐릭터 위치로 당겨져 돌아오는 모션을 짧게 재생합니다.
    /// </summary>
    private IEnumerator RetractRopeVisual(List<Vector2> releasedPivots)
    {
        float elapsed = 0f;
        while (elapsed < _retractDuration)
        {
            float t = elapsed / _retractDuration;
            Vector2 playerPoint = _rb.position;

            _ropeVisual.positionCount = releasedPivots.Count + 1;
            for (int i = 0; i < releasedPivots.Count; i++)
            {
                _ropeVisual.SetPosition(i, Vector2.Lerp(releasedPivots[i], playerPoint, t));
            }
            _ropeVisual.SetPosition(releasedPivots.Count, playerPoint);

            elapsed += Time.deltaTime;
            yield return null;
        }

        _ropeVisual.enabled = false;
        _retractRoutine = null;
    }

    private void UpdateRopeVisual()
    {
        if (_ropeVisual == null || _ropePivots.Count == 0) return;

        // 캐릭터가 바라보는 방향에 따라 오프셋 좌우 반전
        Vector2 currentOffset = _visualOffset;
        if (!_movement.IsFacingRight)
        {
            currentOffset.x = -currentOffset.x;
        }

        // 실제 선이 그려질 캐릭터 쪽 끝점 (배꼽이 아니라 '입' 위치)
        Vector2 playerVisualPoint = _rb.position + currentOffset;

        Vector2 activePivot = ActivePivot;
        float fixedLength = GetFixedSegmentsLength();
        float segmentAllowance = Mathf.Max(_minRopeLength, _ropeLength - fixedLength);

        // 거리를 계산할 때도 입 위치를 기준으로 계산 (마지막 활성 pivot ~ 입 위치)
        float currentDist = Vector2.Distance(activePivot, playerVisualPoint);
        float slack = Mathf.Max(0, segmentAllowance - currentDist); // 여유 길이 계산

        // 1. 줄이 팽팽할 때 (고정된 pivot들을 지나는 직선들 + 마지막 직선)
        if (slack <= TAUT_SLACK_THRESHOLD)
        {
            _ropeVisual.positionCount = _ropePivots.Count + 1;
            for (int i = 0; i < _ropePivots.Count; i++)
            {
                _ropeVisual.SetPosition(i, _ropePivots[i]);
            }
            _ropeVisual.SetPosition(_ropePivots.Count, playerVisualPoint); // _rb.position 대신 입 위치 사용
        }
        // 2. 줄에 여유가 있을 때 (고정 pivot 구간은 직선, 마지막 구간만 베지에 곡선으로 축 늘어짐)
        else
        {
            int fixedPointCount = _ropePivots.Count - 1; // 활성 pivot 이전까지의 고정 pivot 개수
            _ropeVisual.positionCount = fixedPointCount + _curveResolution + 1;

            for (int i = 0; i < fixedPointCount; i++)
            {
                _ropeVisual.SetPosition(i, _ropePivots[i]);
            }

            Vector2 startPoint = activePivot;
            Vector2 endPoint = playerVisualPoint; // 여기도 입 위치 사용

            Vector2 midPoint = (startPoint + endPoint) / 2f;
            // 여유 길이(slack)에 비례해서 중간 지점을 아래로 당김
            Vector2 controlPoint = new Vector2(midPoint.x, midPoint.y - (slack * _sagMultiplier));

            for (int i = 0; i <= _curveResolution; i++)
            {
                float t = i / (float)_curveResolution;
                Vector2 curvePoint = CalculateQuadraticBezierPoint(t, startPoint, controlPoint, endPoint);
                _ropeVisual.SetPosition(fixedPointCount + i, curvePoint);
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
