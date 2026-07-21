using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Managers;

public class GameOverMenu : MonoBehaviour
{
    [Header("결과 표시 UI (GameOver 패널의 TextMeshPro 연결)")]
    [SerializeField] private TMP_Text _scoreText;     // 이번 판 도달 거리 (m)
    [SerializeField] private TMP_Text _goldText;      // 이번 판 획득 골드 (+G)
    [SerializeField] private TMP_Text _totalGoldText; // 정산 후 보유 총 골드 (G)

    [Header("씬 이름 (Build Settings에 등록된 이름과 정확히 일치해야 함)")]
    [SerializeField] private string gameSceneName;
    [SerializeField] private string titleSceneName;

    private void OnEnable()
    {
        if (UIManager.Instance != null)
        {
            // 1. [정산 전] 이번 판 획득 골드 및 거리를 임시 보관하고 UI에 표시
            int sessionGold = UIManager.Instance.CurrentGold;
            float sessionScore = UIManager.Instance.CurrentScore;

            if (_scoreText != null)
            {
                _scoreText.text = $"{sessionScore:F1} m";
            }

            if (_goldText != null)
            {
                _goldText.text = $"+{sessionGold} G";
            }

            // 2. [정산 실행] 획득 골드를 TotalGold에 더하고 세션 초기화 (TotalGold 정산)
            UIManager.Instance.EndSession();

            // 3. [정산 후] 최신 정산이 반영된 TotalGold를 UI에 표시
            if (_totalGoldText != null)
            {
                _totalGoldText.text = $"{UIManager.Instance.TotalGold} G";
            }
        }
    }

    public void OnClickRetry()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnClickBackToTitle()
    {
        SceneManager.LoadScene(titleSceneName);
    }
}