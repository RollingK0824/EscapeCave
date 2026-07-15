using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "SoundDataSO", menuName = "Scriptable Objects/Sound/SoundDataSO")]
public class SoundDataSO : ScriptableObject
{
    [Header("Audio Settings")]
    public AudioClip[] audioClips; // 무작위 재생을 위해 여러 개 등록 가능
    public AudioMixerGroup mixerGroup;

    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    [Range(0f, 0.5f)] public float pitchRandomness = 0f; // 타격감 등을 위한 무작위 피치 범위

    public bool loop = false;
    public bool playOnAwake = false;

    [Header("Loading Option")]
    public bool isGlobalPreload = false; // 게임 시작 시 필수 로딩 여부

    public AudioClip GetClip()
    {
        if (audioClips == null || audioClips.Length == 0) return null;
        return audioClips[Random.Range(0, audioClips.Length)];
    }
}