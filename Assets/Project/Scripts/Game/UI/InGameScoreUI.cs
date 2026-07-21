using TMPro;
using UnityEngine;
using Managers;

public class InGameScoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _scoreText;

    private void Reset()
    {
        _scoreText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnCurrentScoreChanged += UpdateScoreUI;
            UpdateScoreUI(UIManager.Instance.CurrentScore);
        }
    }

    private void OnDisable()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnCurrentScoreChanged -= UpdateScoreUI;
        }
    }

    private void UpdateScoreUI(float score)
    {
        if (_scoreText != null)
        {
            _scoreText.text = $"{score:F1} m";
        }
    }
}
