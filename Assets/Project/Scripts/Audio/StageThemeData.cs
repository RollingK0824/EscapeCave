using UnityEngine;

namespace Audio
{
    [CreateAssetMenu(fileName = "StageThemeData", menuName = "Audio/Stage Theme Data")]
    public class StageThemeData : ScriptableObject
    {
        public string themeName;
        
        [Tooltip("이 테마에서 재생할 배경음악(BGM)")]
        public AudioClip themeBgm;
    }
}
