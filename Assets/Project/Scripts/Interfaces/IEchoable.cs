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
    /// 소리를 실제로 발생시키는 함수
    /// </summary>
    void Echo();
}
