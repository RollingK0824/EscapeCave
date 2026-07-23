using UnityEngine;

namespace Audio
{
    [CreateAssetMenu(fileName = "StageThemeData", menuName = "Audio/Stage Theme Data")]
    public class StageThemeData : ScriptableObject
    {
        public string themeName;
        
        [Tooltip("이 테마에서 재생할 배경음악(BGM)")]
        public AudioClip themeBgm;

        [Tooltip("이 곡만의 볼륨 배율. 마스터 볼륨에 곱해진다. 곡이 유독 크면 낮춘다")]
        [Range(0f, 1f)]
        public float volumeScale = 1f;
    }
}
