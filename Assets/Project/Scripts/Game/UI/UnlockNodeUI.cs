using UnityEngine;
using UnityEngine.UI;
using Managers;

[RequireComponent(typeof(Button))]
public class UnlockNodeUI : MonoBehaviour
{
    [SerializeField] private UnlockNodeData _nodeData;
    [SerializeField] private Image _icon;

    [SerializeField] private Sprite _lockedIcon;
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(HandleClick);
    }
    private void OnEnable()
    {
        RefreshVisual();
    }
    private void HandleClick()
    {
        if (UnlockManager.Instance.TryUnlock(_nodeData))
        {
            RefreshVisual();
        }
    }
  private void RefreshVisual()
{
    if (_nodeData == null || _icon == null) return;

    bool isUnlocked = UnlockManager.Instance.IsUnlocked(_nodeData);

    if (isUnlocked)
    {
        _icon.sprite = _nodeData.requiredItem != null ? _nodeData.requiredItem.icon : null;
    }
    else
    {
        _icon.sprite = _lockedIcon;
    }
}
}
