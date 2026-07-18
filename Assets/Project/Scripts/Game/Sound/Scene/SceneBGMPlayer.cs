using UnityEngine;
using Audio;
using Managers;

public class SceneBGMPlayer : MonoBehaviour
{
    [SerializeField] private StageThemeData _theme;

    private void Start()
    {
        SoundManager.Instance.ChangeThemeBGM(_theme);
    }
}
