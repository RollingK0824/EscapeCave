using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Managers;

[RequireComponent(typeof(Button))]
public class UnlockNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private UnlockNodeData _nodeData;
    public UnlockNodeData NodeData => _nodeData;
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _costText;
    [SerializeField] private NodeTooltip _tooltip;

    [SerializeField] private Sprite _lockedIcon;
    [SerializeField] private ConfirmPopup _confirmPopup;
    [SerializeField] private TreeScrollController _scrollController;
    private Button _button;
    private RectTransform _rectTransform;
    private Color _iconDefaultColor;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(HandleClick);
        _rectTransform = transform as RectTransform;

        if (_icon != null)
        {
            _iconDefaultColor = _icon.color;
        }
    }
    private void OnEnable()
    {
        RefreshVisual();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_icon != null)
        {
            _icon.color = new Color(_iconDefaultColor.r * 0.5f, _iconDefaultColor.g * 0.5f, _iconDefaultColor.b * 0.5f, _iconDefaultColor.a);
        }

        if (_tooltip != null && _nodeData != null)
        {
            _tooltip.Show(_rectTransform, _nodeData.description);
        }
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (_icon != null)
        {
            _icon.color = _iconDefaultColor;
        }

        if (_tooltip != null)
        {
            _tooltip.Hide();
        }
    }
    private void HandleClick()
    {
        if (UIManager.Instance.CanUnlock(_nodeData))
        {
            _confirmPopup.Show($"UNLOCK '{_nodeData.displayName}' FOR {_nodeData.cost} GOLD?", () =>
            {
                if (UIManager.Instance.TryUnlock(_nodeData))
                {
                    RefreshVisual();
                    _scrollController.ScrollToFrontier();
                }
            });
            return;
        }

        if (UIManager.Instance.IsUnlocked(_nodeData)) return;

        if (!HasMetPrerequisites())
        {
            _confirmPopup.Show("PREREQUISITES NOT MET", () => { });
            return;
        }

        if (!UIManager.Instance.HasEnoughGold(_nodeData.cost))
        {
            _confirmPopup.Show($"NOT ENOUGH GOLD (HAVE {UIManager.Instance.TotalGold} / NEED {_nodeData.cost})", () => { });
        }
    }
    private bool HasMetPrerequisites()
    {
        foreach (UnlockNodeData prereq in _nodeData.prerequisites)
        {
            if (!UIManager.Instance.IsUnlocked(prereq)) return false;
        }

        return true;
    }
    private void RefreshVisual()
    {
        if (_nodeData == null || _icon == null) return;

        bool isUnlocked = UIManager.Instance.IsUnlocked(_nodeData);

        _icon.sprite = isUnlocked ? _nodeData.unlockedIcon : _lockedIcon;

        if (_nameText != null)
        {
            _nameText.text = _nodeData.displayName;
        }

        if (_costText != null)
        {
            _costText.text = isUnlocked ? string.Empty : $"{_nodeData.cost} G";
        }
    }
}