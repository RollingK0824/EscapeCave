using System;
using System.Collections.Generic;

namespace Managers
{
    public class UIManager : SingletonBase<UIManager>
    {
        public event Action<int> OnTotalGoldChanged;
        public event Action<int> OnCurrentGoldChanged;
        public event Action<float> OnCurrentScoreChanged;
        public event Action<float> OnBestScoreChanged;
        public event Action<IReadOnlyList<float>> OnTopScoresChanged;
        public event Action<UnlockNodeData> OnUnlockChanged;

        public int TotalGold => DataManager.Instance.TotalGold;
        public int CurrentGold => DataManager.Instance.CurrentGold;
        public float CurrentScore => DataManager.Instance.CurrentScore;
        public float BestScore => DataManager.Instance.BestScore;
        public IReadOnlyList<float> TopScores => DataManager.Instance.TopScores;

        protected override void Awake()
        {
            base.Awake();

            DataManager.Instance.OnTotalGoldChanged += HandleTotalGoldChanged;
            DataManager.Instance.OnCurrentGoldChanged += HandleCurrentGoldChanged;
            DataManager.Instance.OnCurrentScoreChanged += HandleCurrentScoreChanged;
            DataManager.Instance.OnBestScoreChanged += HandleBestScoreChanged;
            DataManager.Instance.OnTopScoresChanged += HandleTopScoresChanged;
            DataManager.Instance.OnUnlockChanged += HandleUnlockChanged;
        }

        private void HandleTotalGoldChanged(int totalGold) => OnTotalGoldChanged?.Invoke(totalGold);
        private void HandleCurrentGoldChanged(int currentGold) => OnCurrentGoldChanged?.Invoke(currentGold);
        private void HandleCurrentScoreChanged(float currentScore) => OnCurrentScoreChanged?.Invoke(currentScore);
        private void HandleBestScoreChanged(float bestScore) => OnBestScoreChanged?.Invoke(bestScore);
        private void HandleTopScoresChanged(IReadOnlyList<float> scores) => OnTopScoresChanged?.Invoke(scores);
        private void HandleUnlockChanged(UnlockNodeData node) => OnUnlockChanged?.Invoke(node);

        public void StartNewSession() => DataManager.Instance.StartNewSession();
        public void AddCurrentGold(int amount) => DataManager.Instance.AddCurrentGold(amount);
        public void UpdateCurrentScore(float score) => DataManager.Instance.UpdateCurrentScore(score);
        public void EndSession() => DataManager.Instance.EndSession();

        public bool IsUnlocked(UnlockNodeData node) => DataManager.Instance.IsUnlocked(node);
        public bool CanUnlock(UnlockNodeData node) => DataManager.Instance.CanUnlock(node);
        public bool TryUnlock(UnlockNodeData node) => DataManager.Instance.TryUnlock(node);
        public bool HasEnoughGold(int amount) => DataManager.Instance.HasEnoughGold(amount);
    }
}
