using UnityEngine;

/// <summary>
/// 갈고리(그래플 훅)로 걸 수 있는 대상이 구현하는 인터페이스.
/// IGrabbable과 반대로, 이 대상이 아니라 플레이어가 대상 쪽으로 끌려갑니다.
/// </summary>
public interface IHookable
{
    /// <summary>
    /// 갈고리가 실제로 고정되는 지점 (로프의 최종 앵커 좌표).
    /// </summary>
    Vector3 HookPoint { get; }

    /// <summary>
    /// 지금 이 대상에 갈고리를 걸 수 있는 상태인지 여부.
    /// (예: 파괴된 오브젝트, 비활성화된 훅 포인트 등을 걸러내는 용도)
    /// </summary>
    bool CanHook { get; }
}
