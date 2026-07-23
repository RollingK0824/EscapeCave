using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
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

    [Header("닉네임 설정 UI (선택사항 - 미할당 시 OnGUI Fallback 윈도우 제공)")]
    [SerializeField] private TMP_InputField _nicknameInputField;
    [SerializeField] private TMP_Text _currentNicknameText;
    [SerializeField] private GameObject _nicknamePanel;

    private bool _showFallbackGUI = false;
    private string _guiNicknameInput = "";

    private void Start()
    {
        // 닉네임 초기화
        string savedNickname = PlayerPrefs.GetString("Data_UserNickname", string.Empty);
        if (string.IsNullOrEmpty(savedNickname))
        {
            string deviceId = "";
            if (SupabaseManager.Instance != null)
            {
                deviceId = SupabaseManager.Instance.DeviceId;
            }
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = System.Guid.NewGuid().ToString();
            }
            savedNickname = $"User_{deviceId.Substring(0, 4)}";
            PlayerPrefs.SetString("Data_UserNickname", savedNickname);
            PlayerPrefs.Save();
        }

        // 씬 내에서 바인딩되지 않은 UI 컴포넌트 자동 탐색
        AutoFindUIComponents();

        // UI 갱신
        UpdateNicknameUI(savedNickname);

        // 여전히 인풋필드를 찾지 못했다면 런타임 OnGUI Fallback UI 활성화
        if (_nicknameInputField == null)
        {
            _showFallbackGUI = true;
            _guiNicknameInput = savedNickname;
        }
    }

    private void AutoFindUIComponents()
    {
        if (_nicknameInputField == null)
        {
            _nicknameInputField = FindObjectOfType<TMP_InputField>();
        }
        if (_currentNicknameText == null)
        {
            TMP_Text[] texts = FindObjectsOfType<TMP_Text>();
            foreach (var t in texts)
            {
                if (t.name.ToLower().Contains("nickname") || t.name.ToLower().Contains("user"))
                {
                    _currentNicknameText = t;
                    break;
                }
            }
        }
        if (_nicknamePanel == null)
        {
            var panels = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var p in panels)
            {
                if (p.name.ToLower().Contains("nickname") && p.name.ToLower().Contains("panel"))
                {
                    _nicknamePanel = p;
                    break;
                }
            }
        }
    }

    private void UpdateNicknameUI(string nickname)
    {
        if (_currentNicknameText != null)
        {
            _currentNicknameText.text = $"Nickname: {nickname}";
        }
        if (_nicknameInputField != null)
        {
            _nicknameInputField.text = nickname;
        }
    }

    public void OnClickOpenNicknamePanel()
    {
        if (_nicknamePanel != null)
        {
            _nicknamePanel.SetActive(true);
        }
        
        string savedNickname = PlayerPrefs.GetString("Data_UserNickname", string.Empty);
        if (_nicknameInputField != null)
        {
            _nicknameInputField.text = savedNickname;
        }
    }

    public void OnClickCloseNicknamePanel()
    {
        if (_nicknamePanel != null)
        {
            _nicknamePanel.SetActive(false);
        }
    }

    public void OnClickSaveNickname()
    {
        if (_nicknameInputField == null) return;

        string newNickname = _nicknameInputField.text.Trim();
        SaveAndSyncNickname(newNickname);
    }

    private void SaveAndSyncNickname(string newNickname)
    {
        if (string.IsNullOrEmpty(newNickname))
        {
            Debug.LogWarning("[TitleMenu] Nickname cannot be empty.");
            return;
        }

        if (newNickname.Length < 2 || newNickname.Length > 12)
        {
            Debug.LogWarning("[TitleMenu] Nickname must be between 2 and 12 characters.");
            return;
        }

        // 로컬 저장
        PlayerPrefs.SetString("Data_UserNickname", newNickname);
        PlayerPrefs.Save();

        UpdateNicknameUI(newNickname);
        _guiNicknameInput = newNickname;

        // Supabase DB 동기화
        if (SupabaseManager.Instance != null)
        {
            SupabaseManager.Instance.UpdateNickname(newNickname,
                onSuccess: () => Debug.Log($"✅ [TitleMenu] Supabase nickname synced successfully: {newNickname}"),
                onError: (err) => Debug.LogError($"❌ [TitleMenu] Supabase nickname sync failed: {err}")
            );
        }
    }

    private void OnGUI()
    {
        if (!_showFallbackGUI) return;

        // 화면 좌측 하단에 닉네임 변경 GUI 창 표시 (UI 미지정 시 Fallback - 크기 확장)
        int width = 320;
        int height = 100;
        int x = 20;
        int y = Screen.height - height - 20;

        GUI.Box(new Rect(x, y, width, height), "Quick Nickname Setup (Fallback)");
        
        string currentNickname = PlayerPrefs.GetString("Data_UserNickname", "N/A");
        GUI.Label(new Rect(x + 10, y + 25, width - 20, 20), $"Current Nickname: {currentNickname}");
        
        _guiNicknameInput = GUI.TextField(new Rect(x + 10, y + 55, width - 110, 26), _guiNicknameInput);
        
        if (GUI.Button(new Rect(x + width - 90, y + 55, 80, 26), "Apply"))
        {
            SaveAndSyncNickname(_guiNicknameInput.Trim());
        }
    }

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
