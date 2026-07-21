using TMPro;
using UnityEngine;
using Managers;

public class InGameGoldUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _goldText;

    private void Reset()
    {
        _goldText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnCurrentGoldChanged += UpdateGoldUI;
            UpdateGoldUI(UIManager.Instance.CurrentGold);
        }
    }

    private void OnDisable()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnCurrentGoldChanged -= UpdateGoldUI;
        }
    }

    private void UpdateGoldUI(int gold)
    {
        if (_goldText != null)
        {
            _goldText.text = $"{gold} G";
        }
    }
}
