using Managers;
using UnityEngine;

/// <summary>
/// 해금 트리에서 목표 노드(기본값: 마지막 Flight 노드)를 해금하면 업적 팝업을 띄운다.
///
/// DataManager.TryUnlock이 실제로 성공한 순간에만 OnUnlockChanged가 발생하고,
/// 해금 여부는 PlayerPrefs에 저장되므로 같은 노드로 팝업이 두 번 뜨지 않는다.
/// 씬에 배치하지 않아도 게임 시작 시 자동으로 생성되어 동작한다.
/// </summary>
[DisallowMultipleComponent]
public class UnlockAchievementNotifier : SingletonBase<UnlockAchievementNotifier>
{
    [Header("업적으로 알릴 해금 노드")]
    [SerializeField] private UnlockNodeData _targetNode;         // 지정하면 이 노드와 직접 대조
    [SerializeField] private string _targetNodeId = "Final";     // 노드를 비워뒀을 때 대조할 nodeId

    [Header("팝업 문구 (비워두면 자동)")]
    [SerializeField] private string _achievementName = string.Empty;  // 비우면 노드의 displayName 사용
    [SerializeField] private string _achievementTitle = string.Empty; // 비우면 팝업의 기본 라벨 사용

    private UIManager _uiManager;

    // 씬에 배치하지 않아도 해금 이벤트를 놓치지 않도록 첫 씬 로드 직후 자동 생성한다
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        UnlockAchievementNotifier notifier = Instance;
        if (notifier == null)
        {
            Debug.LogWarning("[UnlockAchievementNotifier] 인스턴스를 생성하지 못했습니다.");
        }
    }

    private void OnEnable()
    {
        // 중복 인스턴스는 base.Awake()에서 파괴되므로 구독하지 않는다
        if (Instance != this)
        {
            return;
        }

        _uiManager = UIManager.Instance;
        if (_uiManager != null)
        {
            _uiManager.OnUnlockChanged += HandleUnlockChanged;
        }
    }

    private void OnDisable()
    {
        // 종료 시 싱글턴이 되살아나지 않도록 Instance 대신 캐시해 둔 참조를 사용한다
        if (_uiManager != null)
        {
            _uiManager.OnUnlockChanged -= HandleUnlockChanged;
            _uiManager = null;
        }
    }

    private void HandleUnlockChanged(UnlockNodeData node)
    {
        if (node == null || !IsTargetNode(node))
        {
            return;
        }

        AchievementPopup.Notify(GetAchievementName(node), node.unlockedIcon, GetAchievementTitle());
    }

    private bool IsTargetNode(UnlockNodeData node)
    {
        if (_targetNode != null)
        {
            return node == _targetNode;
        }

        if (string.IsNullOrEmpty(_targetNodeId))
        {
            return false;
        }

        return node.nodeId == _targetNodeId;
    }

    private string GetAchievementName(UnlockNodeData node)
    {
        if (!string.IsNullOrEmpty(_achievementName))
        {
            return _achievementName;
        }

        if (!string.IsNullOrEmpty(node.displayName))
        {
            // 프로젝트 UI 문구가 모두 대문자이므로 노드 이름도 대문자로 맞춘다
            return node.displayName.ToUpperInvariant();
        }

        return node.nodeId;
    }

    private string GetAchievementTitle()
    {
        if (string.IsNullOrEmpty(_achievementTitle))
        {
            return null;
        }

        return _achievementTitle;
    }
}
