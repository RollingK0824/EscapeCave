using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverMenu : MonoBehaviour
{
    [Header("씬 이름 (Build Settings에 등록된 이름과 정확히 일치해야 함)")]
    [SerializeField] private string gameSceneName;
    [SerializeField] private string titleSceneName;

    public void OnClickRetry()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnClickBackToTitle()
    {
        SceneManager.LoadScene(titleSceneName);
    }
}