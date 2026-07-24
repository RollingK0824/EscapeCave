using System.Collections;
using Audio;
using Managers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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

    [Header("Phase 4: Achievement")]
    [SerializeField] private string _achievementName = "ESCAPE";
    [SerializeField] private string _achievementDescription = "The End of the Journey";
    [SerializeField] private Sprite _achievementIcon;

    [Header("Phase 5: Touch To Continue")]
    [SerializeField] private CanvasGroup _touchToContinueGroup;
    [SerializeField, Range(0f, 1f)] private float _touchTextMinAlpha = 0.6f;
    [SerializeField, Range(0f, 1f)] private float _touchTextMaxAlpha = 1f;
    [SerializeField] private float _touchTextFadeDuration = 1f;
    [SerializeField] private float _touchTextPulseDuration = 1f;

    [Header("Return")]
    [SerializeField] private string _returnSceneName = "UnlockTree";
    [SerializeField] private StageThemeData _returnBgmTheme;

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

        // 크레딧이 끝나기 전까지는 숨겨둠
        if (_touchToContinueGroup != null)
        {
            _touchToContinueGroup.alpha = 0f;
            _touchToContinueGroup.blocksRaycasts = false;
            _touchToContinueGroup.gameObject.SetActive(false);
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

        // 4) 크레딧이 끝나면 엔딩 업적 알림 표시
        NotifyEndingAchievement();

        // 5) 화면을 터치/클릭할 때까지 대기 (자동으로 넘어가지 않음)
        yield return WaitForTouchToContinueRoutine();

        Debug.Log("[EndingSequenceController] 터치 감지됨. 언락씬으로 복귀를 시작합니다.");

        yield return FadeRoutine(0f, 1f);

        // 씬 전환이 일어나면 이 오브젝트(Ending 씬 소속) 자체가 파괴되면서 여기서 실행 중인
        // 코루틴도 함께 끊긴다. 그래서 나머지(씬 로드 대기 -> BGM 복원 -> 페이드 인 -> 자기 파괴)는
        // DontDestroyOnLoad로 살아남는 오버레이 오브젝트 스스로가 처리하도록 넘긴다.
        BeginReturnToPreviousScene();
    }

    private void NotifyEndingAchievement()
    {
        AchievementPopup.Notify(_achievementName, _achievementIcon, _achievementDescription);
    }

    private IEnumerator WaitForTouchToContinueRoutine()
    {
        Coroutine pulseCoroutine = null;

        if (_touchToContinueGroup != null)
        {
            _touchToContinueGroup.gameObject.SetActive(true);
            _touchToContinueGroup.blocksRaycasts = false;
            pulseCoroutine = StartCoroutine(PulseCanvasGroupAlphaRoutine(_touchToContinueGroup));
        }

        // 크레딧을 넘기려던 마지막 입력이 그대로 넘어와 즉시 스킵되지 않도록 한 프레임 대기
        yield return null;

        while (!IsScreenTouched())
        {
            yield return null;
        }

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }

        if (_touchToContinueGroup != null)
        {
            yield return FadeCanvasGroupAlphaRoutine(_touchToContinueGroup, _touchToContinueGroup.alpha, 0f, _touchTextFadeDuration);
            _touchToContinueGroup.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// "Touch the Screen" 문구를 최소~최대 알파 사이로 계속 오가게 만들어 깜빡이듯 보이게 한다.
    /// 클릭이 감지되면 바깥의 WaitForTouchToContinueRoutine에서 이 코루틴을 멈춘다.
    /// </summary>
    private IEnumerator PulseCanvasGroupAlphaRoutine(CanvasGroup group)
    {
        yield return FadeCanvasGroupAlphaRoutine(group, 0f, _touchTextMinAlpha, _touchTextFadeDuration);

        float elapsed = 0f;
        while (true)
        {
            elapsed += Time.deltaTime;
            float pingPong = Mathf.PingPong(elapsed / Mathf.Max(0.01f, _touchTextPulseDuration), 1f);
            group.alpha = Mathf.Lerp(_touchTextMinAlpha, _touchTextMaxAlpha, pingPong);
            yield return null;
        }
    }

    private static bool IsScreenTouched()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            return true;
        }

        // 새 Input System 디바이스가 인식되지 않는 환경을 대비한 구식 Input 폴백
        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }

        return false;
    }

    private static IEnumerator FadeCanvasGroupAlphaRoutine(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        group.alpha = to;
    }

    /// <summary>
    /// 화면을 하얗게 덮은 오버레이를 만들고, 씬 로드 -> BGM 복원 -> 페이드 인 -> 자기 파괴까지
    /// 전부 그 오버레이 스스로(DontDestroyOnLoad) 처리하도록 맡긴다. Ending 씬 소속인
    /// EndingSequenceController는 씬 전환과 함께 파괴되므로 여기서 코루틴을 계속 들고 있을 수 없다.
    /// </summary>
    private void BeginReturnToPreviousScene()
    {
        CanvasGroup overlay = CreatePersistentWhiteOverlay();
        SceneReturnOverlay runner = overlay.gameObject.AddComponent<SceneReturnOverlay>();
        runner.Run(_returnSceneName, _returnBgmTheme, _fadeDuration);
    }

    /// <summary>
    /// 씬 전환용 흰색 오버레이에 붙어 씬 로드 대기 -> BGM 테마 복원 -> 페이드 인 -> 자기 파괴를
    /// 스스로 처리하는 컴포넌트. 오버레이 오브젝트가 DontDestroyOnLoad라 씬이 바뀌어도 살아남는다.
    /// </summary>
    private sealed class SceneReturnOverlay : MonoBehaviour
    {
        public void Run(string sceneName, StageThemeData bgmTheme, float fadeDuration)
        {
            StartCoroutine(RunRoutine(sceneName, bgmTheme, fadeDuration));
        }

        private IEnumerator RunRoutine(string sceneName, StageThemeData bgmTheme, float fadeDuration)
        {
            Debug.Log($"[EndingSequenceController] '{sceneName}' 씬 로드 시작");

            AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName);
            if (loadOp == null)
            {
                Debug.LogError($"[EndingSequenceController] '{sceneName}' 씬을 로드하지 못했습니다. Build Settings의 Scenes In Build 목록에 씬이 추가되어 있는지 확인하세요.");
                yield break;
            }

            while (!loadOp.isDone)
            {
                yield return null;
            }

            Debug.Log($"[EndingSequenceController] '{sceneName}' 씬 로드 완료");

            if (bgmTheme != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.ChangeThemeBGM(bgmTheme);
            }

            CanvasGroup group = GetComponent<CanvasGroup>();
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }

            group.alpha = 0f;
            group.blocksRaycasts = false;

            Destroy(gameObject);
        }
    }

    private CanvasGroup CreatePersistentWhiteOverlay()
    {
        GameObject overlayObj = new GameObject("EndingReturnFadeOverlay");
        overlayObj.transform.SetParent(null);
        DontDestroyOnLoad(overlayObj);

        Canvas canvas = overlayObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        overlayObj.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = overlayObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject imageObj = new GameObject("WhiteImage", typeof(RectTransform));
        imageObj.transform.SetParent(overlayObj.transform, false);
        RectTransform imageRect = (RectTransform)imageObj.transform;
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        Image image = imageObj.AddComponent<Image>();
        image.color = Color.white;

        CanvasGroup group = overlayObj.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.blocksRaycasts = true;

        return group;
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
