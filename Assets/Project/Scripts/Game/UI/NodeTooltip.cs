using TMPro;
using UnityEngine;

public class NodeTooltip : MonoBehaviour
{
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private Vector2 _offset = new Vector2(0f, 40f);

    private RectTransform _target;

    private void Awake()
    {
        Hide();
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        _rectTransform.position = (Vector2)_target.position + _offset;
    }

    public void Show(RectTransform target, string description)
    {
        if (string.IsNullOrEmpty(description)) return;

        _target = target;
        _descriptionText.text = description;
        _rectTransform.position = (Vector2)_target.position + _offset;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        _target = null;
        gameObject.SetActive(false);
    }
}
