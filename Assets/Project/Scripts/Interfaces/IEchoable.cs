using UnityEngine;

public interface IEchoable
{
    /// <summary>
    /// 소리를 낼 때의 강도(범위)
    /// </summary>
    float SoundIntensity { get; }
    /// <summary>
    /// 소리를 내는 전파 속도
    /// </summary>
    float SoundSpeed { get; }
    /// <summary>
    /// 에코 투명도 감소(페이드) 속도 (기본값 -1일 경우 intensity/speed 비례 자동 계산)
    /// </summary>
    float FadeSpeed => -1f;

    /// <summary>
    /// 소리를 실제로 발생시키는 함수
    /// </summary>
    void Echo();
}
