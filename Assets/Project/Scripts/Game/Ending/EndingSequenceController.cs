using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// Ending 씬에 배치. 하얀 화면에서 시작해 영상 컷신을 재생하고, 다시 하얗게 전환하면서
/// 엔딩 이미지를 드러낸 뒤 크레딧을 스크롤하고 원래 씬(UnlockTree)으로 복귀합니다.
/// </summary>
public class EndingSequenceController : MonoBehaviour
{
    [Header("White Flash")]
    [SerializeField] private CanvasGroup _whiteCanvasGroup;
    [SerializeField] private float _fadeDuration = 1f;

    [Header("Phase 1: Video Cutscene")]
    [SerializeField] private VideoPlayer _videoPlayer;
    [SerializeField] private VideoClip _videoClip;
    [SerializeField] private float _videoFallbackDuration = 10f;
    private bool _videoFinished;

    [Header("Phase 2: Ending Image Swap")]
    [SerializeField] private GameObject[] _hideOnEndingImage;
    [SerializeField] private GameObject[] _showOnEndingImage;

    [Header("Phase 3: Credits")]
    [SerializeField] private RectTransform _creditsRoot;
    [SerializeField] private Vector2 _creditsStartPos;
    [SerializeField] private Vector2 _creditsEndPos;
    [SerializeField] private float _creditsScrollDuration = 10f;

    [Header("Return")]
    [SerializeField] private string _returnSceneName = "UnlockTree";
    [SerializeField] private float _holdBeforeReturn = 2f;

    private void Awake()
    {
        if (_whiteCanvasGroup != null)
        {
            _whiteCanvasGroup.alpha = 1f;
            _whiteCanvasGroup.blocksRaycasts = true;
        }

        // 크레딧 단계 전까지는 화면 밖(시작 위치)에 미리 대기시켜둠
        if (_creditsRoot != null)
        {
            _creditsRoot.anchoredPosition = _creditsStartPos;
        }

        // 페이드가 끝난 뒤 바로 재생할 수 있도록 흰 화면이 떠 있는 동안 미리 로딩만 해둠
        if (_videoPlayer != null)
        {
            _videoPlayer.playOnAwake = false;
            _videoPlayer.Stop();

            // 클립을 다시 할당해서 내부 디코더 상태를 완전히 초기화 (Stop만으로는 재생 중간에 이전 재생분의
            // 마지막 프레임으로 튀는 경우가 있음)
            if (_videoClip != null)
            {
                _videoPlayer.clip = null;
                _videoPlayer.clip = _videoClip;
            }

            _videoPlayer.time = 0;

            // RenderTexture에 이전 재생분의 마지막 프레임이 남아있으면 초기 페이드에 비치므로 미리 비움
            // (투명하게 지우면 뒤에 있는 오브젝트가 비치므로 불투명한 검정으로 채움)
            ClearRenderTexture(_videoPlayer.targetTexture, Color.black);

            _videoPlayer.Prepare();
        }
    }

    private static void ClearRenderTexture(RenderTexture texture, Color color)
    {
        if (texture == null) return;

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = texture;
        GL.Clear(true, true, color);
        RenderTexture.active = previous;
    }

    private void Start()
    {
        StartCoroutine(EndingRoutine());
    }

    private IEnumerator EndingRoutine()
    {
        // 영상 로딩이 끝날 때까지 대기 (흰 화면이 가리고 있는 동안)
        yield return WaitUntilVideoPrepared();

        // 페이드 아웃 시작과 동시에 영상 재생 시작 -> 페이드가 걷히며 영상 시작 장면이 자연스럽게 드러남
        StartVideoPlayback();

        // 씬 진입: 하얀 화면에서 페이드 아웃
        yield return FadeRoutine(1f, 0f);

        // 1) 영상이 끝날 때까지 대기
        yield return WaitUntilVideoFinished();

        // 2) 하얗게 전환
        yield return FadeRoutine(0f, 1f);

        // 하얀 화면 뒤에서 엔딩 이미지로 교체
        SetActiveAll(_hideOnEndingImage, false);
        SetActiveAll(_showOnEndingImage, true);

        // 하얗게 전환하면서 엔딩 이미지 드러남
        yield return FadeRoutine(1f, 0f);

        // 3) 크레딧 스크롤
        yield return CreditsRoutine();

        yield return new WaitForSeconds(_holdBeforeReturn);

        yield return FadeRoutine(0f, 1f);

        SceneManager.LoadScene(_returnSceneName);
    }

    private IEnumerator WaitUntilVideoPrepared()
    {
        if (_videoPlayer == null) yield break;

        while (!_videoPlayer.isPrepared)
        {
            yield return null;
        }
    }

    private void StartVideoPlayback()
    {
        if (_videoPlayer == null) return;

        _videoFinished = false;
        _videoPlayer.loopPointReached += HandleVideoFinished;
        _videoPlayer.Play();
    }

    private IEnumerator WaitUntilVideoFinished()
    {
        if (_videoPlayer == null) yield break;

        float elapsed = 0f;
        while (!_videoFinished && elapsed < _videoFallbackDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        _videoPlayer.loopPointReached -= HandleVideoFinished;
    }

    private void HandleVideoFinished(VideoPlayer source)
    {
        _videoFinished = true;
    }

    private IEnumerator CreditsRoutine()
    {
        if (_creditsRoot == null) yield break;

        _creditsRoot.anchoredPosition = _creditsStartPos;

        float elapsed = 0f;
        while (elapsed < _creditsScrollDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _creditsScrollDuration);
            _creditsRoot.anchoredPosition = Vector2.Lerp(_creditsStartPos, _creditsEndPos, t);
            yield return null;
        }
    }

    private IEnumerator FadeRoutine(float from, float to)
    {
        if (_whiteCanvasGroup == null) yield break;

        _whiteCanvasGroup.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            _whiteCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / _fadeDuration);
            yield return null;
        }

        _whiteCanvasGroup.alpha = to;
        _whiteCanvasGroup.blocksRaycasts = to > 0.99f;
    }

    private static void SetActiveAll(GameObject[] objects, bool isActive)
    {
        if (objects == null) return;

        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                obj.SetActive(isActive);
            }
        }
    }
}
