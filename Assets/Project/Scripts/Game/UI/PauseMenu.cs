using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("타이틀 씬 이름")]
    [SerializeField] private string titleSceneName;

    private bool isPaused = false;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
                OnClickResume();
            else
                OnClickOpenMenu();
        }
    }

    public void OnClickOpenMenu()
    {
        isPaused = true;
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void OnClickResume()
    {
        isPaused = false;
        pauseMenuPanel.SetActive(false);
        optionsPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OnClickOptions()
    {
        optionsPanel.SetActive(true);
    }

    public void OnClickCloseOptions()
    {
        optionsPanel.SetActive(false);
    }

    public void OnClickBackToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }

    public void OnClickQuit()
    {
        Time.timeScale = 1f;
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}