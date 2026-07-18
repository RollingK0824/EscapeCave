using TMPro;
using UnityEngine;
using Managers;

public class GoldDisplayUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _goldText;

    private void OnEnable()
    {
        UIManager.Instance.OnGoldChanged += HandleGoldChanged;
        HandleGoldChanged(UIManager.Instance.CurrentGold);
    }

    private void OnDisable()
    {
        UIManager.Instance.OnGoldChanged -= HandleGoldChanged;
    }

    private void HandleGoldChanged(int gold)
    {
        _goldText.text = $"{gold} G";
    }
}
