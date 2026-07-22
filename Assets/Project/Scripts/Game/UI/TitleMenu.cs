using UnityEngine;
using UnityEngine.SceneManagement;
using Managers;

public class TitleMenu : MonoBehaviour
{
    [Header("게임 씬 이름 (Build Settings에 등록된 이름과 정확히 일치해야 함)")]
    [SerializeField] private string _gameSceneName;

    [Header("해금 트리 씬 이름 (Build Settings에 등록된 이름과 정확히 일치해야 함)")]
    [SerializeField] private string _unlockTreeSceneName;

    [Header("Score Board 씬 이름 (Build Settings에 등록된 이름과 정확히 일치해야 함)")]
    [SerializeField] private string _scoreBoardScene;

    [Header("옵션 패널")]
    [SerializeField] private GameObject _optionsPanel;

    public void OnClickStart()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.StartNewSession();
        }
        SceneManager.LoadScene(_gameSceneName);
    }

    public void OnClickUnlockTree()
    {
        SceneManager.LoadScene(_unlockTreeSceneName);
    }

    public void OnClickScoreBoard()
    {
        SceneManager.LoadScene(_scoreBoardScene);
    }

    public void OnClickOptions()
    {
        _optionsPanel.SetActive(true);
    }

    public void OnClickCloseOptions()
    {
        _optionsPanel.SetActive(false);
    }

    public void OnClickQuit()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
