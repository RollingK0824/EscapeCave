using UnityEngine;
using UnityEngine.SceneManagement;

public class UnlockTreeMenu : MonoBehaviour
{
    [Header("타이틀 씬 이름 (Build Settings에 등록된 이름과 정확히 일치해야 함)")]
    [SerializeField] private string titleSceneName;

    public void OnClickBack()
    {
        SceneManager.LoadScene(titleSceneName);
    }
}