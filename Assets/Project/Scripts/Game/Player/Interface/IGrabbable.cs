using UnityEngine;

/// <summary>
/// 혀 공격에 그랩되어 플레이어 쪽으로 끌려오는 대상이 구현하는 인터페이스.
/// 기존 CompareTag("Item") 분기를 대체합니다.
/// </summary>
public interface IGrabbable
{
    /// <summary>
    /// 그랩되는 동안 위치를 옮길 기준 Transform.
    /// 보통 자기 자신의 transform을 반환하면 됩니다.
    /// </summary>
    Transform GrabTransform { get; }

    /// <summary>
    /// 혀에 잡히는 순간 호출됩니다. (물리 끄기, 콜라이더 비활성화 등)
    /// </summary>
    void OnGrabbed();

    /// <summary>
    /// 플레이어에게 완전히 도달하여 습득 처리가 끝났을 때 호출됩니다.
    /// (인벤토리 추가, 파괴 등은 호출부에서 처리하고, 여기서는 아이템 자체의 후처리만)
    /// </summary>
    void OnCollected();
}
