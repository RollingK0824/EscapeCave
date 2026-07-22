using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// 오프닝 동영상 컷신을 풀스크린으로 재생하고, 끝나면(또는 스킵하면) 다음 씬으로 전환합니다.
/// VideoPlayer의 Render Mode/Target(RawImage 또는 Camera Far Plane)은 에디터에서 설정합니다.
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public class VideoCutscenePlayer : MonoBehaviour
{
    [SerializeField] private string _nextSceneName = "ProtoType";
    [SerializeField] private bool _allowSkip = true;
    [SerializeField] private KeyCode _skipKey = KeyCode.Escape;

    private VideoPlayer _videoPlayer;
    private bool _isTransitioning;

    private void Awake()
    {
        _videoPlayer = GetComponent<VideoPlayer>();
        _videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void Start()
    {
        _videoPlayer.Play();
    }

    private void Update()
    {
        if (_allowSkip && Input.GetKeyDown(_skipKey))
        {
            LoadNextScene();
        }
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (_isTransitioning)
        {
            return;
        }

        _isTransitioning = true;
        _videoPlayer.loopPointReached -= OnVideoFinished;
        SceneManager.LoadScene(_nextSceneName);
    }

    private void OnDestroy()
    {
        _videoPlayer.loopPointReached -= OnVideoFinished;
    }
}
