using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Managers;

public class ScoreBoardUI : MonoBehaviour
{
    [System.Serializable]
    public class ScoreRowUI
    {
        public TMP_Text rankText;
        public TMP_Text nicknameText;
        public TMP_Text distanceText;
    }

    [Header("Top 10 리더보드 UI 행 목록 (1위 ~ 10위)")]
    [SerializeField] private List<ScoreRowUI> _scoreRows = new List<ScoreRowUI>();

    [Header("타이틀 씬 이름")]
    [SerializeField] private string _titleSceneName = "Title Screen";

    private void OnEnable()
    {
        RefreshLeaderboard();
    }

    public void RefreshLeaderboard()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.FetchOnlineLeaderboard(
                onSuccess: (onlineScores) => {
                    UpdateUI(onlineScores);
                },
                onError: (err) => {
                    Debug.LogWarning($"[ScoreBoardUI] Supabase 로드 실패, 로컬 점수 로드: {err}");
                    UpdateUIWithLocalScores();
                }
            );
        }
    }

    private void UpdateUI(List<ScoreEntry> scores)
    {
        for (int i = 0; i < _scoreRows.Count; i++)
        {
            if (i < scores.Count)
            {
                ScoreEntry entry = scores[i];
                if (_scoreRows[i].rankText != null) _scoreRows[i].rankText.text = GetRankString(i + 1);
                if (_scoreRows[i].nicknameText != null) _scoreRows[i].nicknameText.text = entry.nickname;
                if (_scoreRows[i].distanceText != null) _scoreRows[i].distanceText.text = $"{entry.score:F1}M";
            }
            else
            {
                if (_scoreRows[i].rankText != null) _scoreRows[i].rankText.text = GetRankString(i + 1);
                if (_scoreRows[i].nicknameText != null) _scoreRows[i].nicknameText.text = "---";
                if (_scoreRows[i].distanceText != null) _scoreRows[i].distanceText.text = "0.0M";
            }
        }
    }

    private void UpdateUIWithLocalScores()
    {
        if (UIManager.Instance == null) return;

        IReadOnlyList<float> localScores = UIManager.Instance.TopScores;
        for (int i = 0; i < _scoreRows.Count; i++)
        {
            if (i < localScores.Count)
            {
                if (_scoreRows[i].rankText != null) _scoreRows[i].rankText.text = GetRankString(i + 1);
                if (_scoreRows[i].nicknameText != null) _scoreRows[i].nicknameText.text = "Local";
                if (_scoreRows[i].distanceText != null) _scoreRows[i].distanceText.text = $"{localScores[i]:F1}M";
            }
            else
            {
                if (_scoreRows[i].rankText != null) _scoreRows[i].rankText.text = GetRankString(i + 1);
                if (_scoreRows[i].nicknameText != null) _scoreRows[i].nicknameText.text = "---";
                if (_scoreRows[i].distanceText != null) _scoreRows[i].distanceText.text = "0.0M";
            }
        }
    }

    private string GetRankString(int rank)
    {
        switch (rank)
        {
            case 1: return "1ST";
            case 2: return "2ND";
            case 3: return "3RD";
            default: return $"{rank}TH";
        }
    }

    public void OnClickTitle()
    {
        SceneManager.LoadScene(_titleSceneName);
    }
}
