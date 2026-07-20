using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialSkip : MonoBehaviour
{
    [Header("씬 이름 (Build Settings에 등록된 이름과 정확히 일치해야 함)")]
    [SerializeField] private string _nextSceneName = "ProtoType";

    public void OnClickSkip()
    {
        SceneManager.LoadScene(_nextSceneName);
    }
}
