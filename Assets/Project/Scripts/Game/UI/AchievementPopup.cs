using System.Collections;
using System.Collections.Generic;
using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스팀 업적 알림처럼 화면 오른쪽 아래에 업적 팝업을 띄운다.
/// - 씬을 이동해도 파괴되지 않는다 (SingletonBase의 DontDestroyOnLoad)
/// - 전용 오버레이 캔버스를 최상단 정렬 순서로 생성하므로 항상 다른 UI/연출보다 앞에 그려진다
/// - 노출 시간(기본 5초)이 지나면 자동으로 사라지며, 연속 호출은 큐에 쌓여 하나씩 재생된다
///
/// 사용법: AchievementPopup.Notify("첫 번째 탈출", 아이콘스프라이트);
/// 씬에 프리팹을 배치하지 않아도 호출 시점에 자동으로 생성된다.
/// </summary>
[DisallowMultipleComponent]
public class AchievementPopup : SingletonBase<AchievementPopup>
{
    private struct AchievementEntry
    {
        public string Title;
        public string Name;
        public Sprite Icon;
    }

    [Header("비주얼 (비워두면 기본 스타일로 자동 생성)")]
    [SerializeField] private Sprite _backgroundSprite;  // SteamUI_0 같은 배경 패널 스프라이트
    [SerializeField] private Sprite _defaultIcon;       // 업적별 아이콘이 없을 때 쓰는 기본 아이콘
    [SerializeField] private TMP_FontAsset _font;       // 비워두면 TMP 기본 폰트 사용

    [Header("레이아웃 (1920x1080 기준 픽셀)")]
    [SerializeField] private Vector2 _panelSize = new Vector2(420f, 118f);
    [SerializeField] private Vector2 _screenMargin = new Vector2(28f, 28f);
    [SerializeField] private float _padding = 14f;
    [SerializeField] private float _iconSize = 90f;

    [Header("연출 시간")]
    [SerializeField] private float _displayDuration = 5f;    // 완전히 보이는 상태로 머무는 시간
    [SerializeField] private float _slideDuration = 0.35f;   // 올라오고 내려가는 데 걸리는 시간
    [SerializeField] private float _gapBetweenPopups = 0.2f; // 연속 팝업 사이의 간격

    [Header("문구")]
    [SerializeField] private string _defaultTitle = "ACHIEVEMENT UNLOCKED";

    [Header("사운드 (선택)")]
    [SerializeField] private SoundDataSO _achievementSfx;

    // 다른 모든 캔버스보다 앞에 그리기 위한 정렬 순서 (Canvas.sortingOrder의 상한값)
    private const int TOPMOST_SORTING_ORDER = 32767;

    private readonly Queue<AchievementEntry> _queue = new Queue<AchievementEntry>();

    private RectTransform _panelRect;
    private CanvasGroup _panelGroup;
    private Image _iconImage;
    private TMP_Text _titleText;
    private TMP_Text _nameText;

    private Coroutine _playCoroutine;
    private Vector2 _shownPosition;
    private Vector2 _hiddenPosition;
    private bool _isBuilt;

    private static bool _isQuitting;

    public bool IsShowing => _playCoroutine != null;

    protected override void Awake()
    {
        // 캔버스는 반드시 최상위 오브젝트여야 DontDestroyOnLoad가 적용된다
        transform.SetParent(null);

        base.Awake();

        // 중복 인스턴스는 base.Awake()에서 파괴되므로 UI를 만들지 않는다
        if (Instance != this)
        {
            return;
        }

        BuildUI();
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    /// <summary>
    /// 업적 팝업을 띄운다. 씬 어디서든 호출할 수 있으며, 인스턴스가 없으면 자동으로 생성된다.
    /// </summary>
    /// <param name="achievementName">크게 표시할 업적 이름</param>
    /// <param name="icon">업적 아이콘 (없으면 기본 아이콘 사용)</param>
    /// <param name="title">위쪽 작은 라벨 (없으면 기본 문구 사용)</param>
    public static void Notify(string achievementName, Sprite icon = null, string title = null)
    {
        if (_isQuitting)
        {
            return;
        }

        AchievementPopup popup = Instance;
        if (popup != null)
        {
            popup.Show(achievementName, icon, title);
        }
    }

    /// <summary>
    /// 업적 팝업을 큐에 넣고 재생을 시작한다. 이미 재생 중이면 순서대로 이어서 표시된다.
    /// </summary>
    public void Show(string achievementName, Sprite icon = null, string title = null)
    {
        if (string.IsNullOrEmpty(achievementName))
        {
            return;
        }

        // 다른 스크립트의 Awake에서 이 인스턴스를 먼저 찾아 호출하는 경우를 대비해 UI 생성을 보장한다
        BuildUI();

        _queue.Enqueue(new AchievementEntry
        {
            Title = string.IsNullOrEmpty(title) ? _defaultTitle : title,
            Name = achievementName,
            Icon = icon != null ? icon : _defaultIcon
        });

        if (_playCoroutine == null)
        {
            _playCoroutine = StartCoroutine(CoPlayQueue());
        }
    }

    /// <summary>
    /// 재생 중인 팝업과 대기 중인 큐를 모두 즉시 정리한다.
    /// </summary>
    public void ClearAll()
    {
        _queue.Clear();

        if (_playCoroutine != null)
        {
            StopCoroutine(_playCoroutine);
            _playCoroutine = null;
        }

        if (_panelRect != null)
        {
            _panelGroup.alpha = 0f;
            _panelRect.anchoredPosition = _hiddenPosition;
            _panelRect.gameObject.SetActive(false);
        }
    }

    private IEnumerator CoPlayQueue()
    {
        while (_queue.Count > 0)
        {
            AchievementEntry entry = _queue.Dequeue();
            ApplyEntry(entry);
            PlayAchievementSfx();

            _panelRect.gameObject.SetActive(true);

            // 아래에서 위로 슬라이드 + 페이드 인
            yield return CoSlide(_hiddenPosition, _shownPosition, 0f, 1f);

            // 일시정지(timeScale = 0) 중에도 정상적으로 흐르도록 Realtime 사용
            yield return new WaitForSecondsRealtime(_displayDuration);

            // 다시 아래로 슬라이드 + 페이드 아웃
            yield return CoSlide(_shownPosition, _hiddenPosition, 1f, 0f);

            _panelRect.gameObject.SetActive(false);

            if (_queue.Count > 0)
            {
                yield return new WaitForSecondsRealtime(_gapBetweenPopups);
            }
        }

        _playCoroutine = null;
    }

    private IEnumerator CoSlide(Vector2 from, Vector2 to, float fromAlpha, float toAlpha)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, _slideDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f); // Cubic Ease-Out

            _panelRect.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            _panelGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);

            yield return null;
        }

        _panelRect.anchoredPosition = to;
        _panelGroup.alpha = toAlpha;
    }

    private void ApplyEntry(AchievementEntry entry)
    {
        _titleText.text = entry.Title;
        _nameText.text = entry.Name;

        if (entry.Icon != null)
        {
            _iconImage.sprite = entry.Icon;
            _iconImage.color = Color.white;
        }
        else
        {
            // 아이콘이 없으면 스팀 배경과 어울리는 어두운 자리 표시자를 그린다
            _iconImage.sprite = null;
            _iconImage.color = new Color32(0x0E, 0x14, 0x1B, 0xFF);
        }
    }

    private void PlayAchievementSfx()
    {
        if (_achievementSfx == null || SoundManager.Instance == null)
        {
            return;
        }

        SoundManager.Instance.PlaySFX(_achievementSfx);
    }

    #region UI 생성

    private void BuildUI()
    {
        if (_isBuilt)
        {
            return;
        }

        _isBuilt = true;

        // Canvas는 RectTransform을 요구하므로, 일반 Transform 오브젝트에 붙였을 경우를 대비해 먼저 보장한다
        if (gameObject.GetComponent<RectTransform>() == null)
        {
            gameObject.AddComponent<RectTransform>();
        }

        Canvas canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = TOPMOST_SORTING_ORDER;

        CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        // 프로젝트의 InGameUI 캔버스와 동일한 스케일 기준 사용
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        // GraphicRaycaster를 붙이지 않아 팝업이 클릭/터치 입력을 가로채지 않는다
        _panelRect = CreateChild("AchievementPanel", transform);
        _panelRect.anchorMin = new Vector2(1f, 0f);
        _panelRect.anchorMax = new Vector2(1f, 0f);
        _panelRect.pivot = new Vector2(1f, 0f);
        _panelRect.sizeDelta = _panelSize;

        _shownPosition = new Vector2(-_screenMargin.x, _screenMargin.y);
        _hiddenPosition = new Vector2(-_screenMargin.x, -_panelSize.y - 10f);
        _panelRect.anchoredPosition = _hiddenPosition;

        Image background = _panelRect.gameObject.AddComponent<Image>();
        if (_backgroundSprite != null)
        {
            background.sprite = _backgroundSprite;
            background.type = Image.Type.Simple;
            background.color = Color.white;
        }
        else
        {
            background.color = new Color32(0x1B, 0x28, 0x38, 0xF2);
        }

        _panelGroup = _panelRect.gameObject.AddComponent<CanvasGroup>();
        _panelGroup.alpha = 0f;
        _panelGroup.interactable = false;
        _panelGroup.blocksRaycasts = false;

        float iconSize = Mathf.Clamp(_iconSize, 1f, _panelSize.y);

        RectTransform iconRect = CreateChild("Icon", _panelRect);
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        iconRect.anchoredPosition = new Vector2(_padding, 0f);

        _iconImage = iconRect.gameObject.AddComponent<Image>();
        _iconImage.preserveAspect = true;
        _iconImage.color = new Color32(0x0E, 0x14, 0x1B, 0xFF);

        RectTransform textArea = CreateChild("TextArea", _panelRect);
        SetStretch(textArea, (_padding * 2f) + iconSize, _padding, _padding, _padding);

        RectTransform titleRect = CreateChild("Title", textArea);
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 28f);
        titleRect.anchoredPosition = Vector2.zero;

        _titleText = CreateText(titleRect, 20f, new Color32(0x8F, 0xB2, 0xD9, 0xFF));
        _titleText.text = _defaultTitle;

        RectTransform nameRect = CreateChild("Name", textArea);
        SetStretch(nameRect, 0f, 0f, 32f, 0f);

        _nameText = CreateText(nameRect, 28f, Color.white);
        _nameText.fontStyle = FontStyles.Bold;
        _nameText.text = string.Empty;

        _panelRect.gameObject.SetActive(false);
    }

    private TMP_Text CreateText(RectTransform parent, float fontSize, Color color)
    {
        TextMeshProUGUI text = parent.gameObject.AddComponent<TextMeshProUGUI>();

        if (_font != null)
        {
            text.font = _font;
        }

        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;

        return text;
    }

    private static RectTransform CreateChild(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        return obj.GetComponent<RectTransform>();
    }

    private static void SetStretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    #endregion

    [ContextMenu("테스트 팝업 띄우기")]
    private void ShowTestPopup()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[AchievementPopup] 플레이 모드에서만 테스트 팝업을 띄울 수 있습니다.");
            return;
        }

        Show("FIRST ESCAPE");
    }
}
