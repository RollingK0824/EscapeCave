using TMPro;
using UnityEngine;
using Managers;

public class TotalGoldUI : MonoBehaviour
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
            UIManager.Instance.OnTotalGoldChanged += UpdateGoldUI;
            UpdateGoldUI(UIManager.Instance.TotalGold);
        }
    }

    private void OnDisable()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnTotalGoldChanged -= UpdateGoldUI;
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
