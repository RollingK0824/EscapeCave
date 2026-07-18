using System;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class DataManager : SingletonBase<DataManager>
    {
        protected override void Awake()
        {
            base.Awake();

            LoadGold();
            LoadGameRecord();
        }

        #region Gold

        private const string GOLD_KEY = "Data_Gold";
        private const int DEFAULT_GOLD = 5;

        [SerializeField] private int _currentGold;
        public int CurrentGold => _currentGold;

        public event Action<int> OnGoldChanged;

        public void AddGold(int amount)
        {
            if (amount <= 0) return;

            _currentGold += amount;
            SaveGold();
            OnGoldChanged?.Invoke(_currentGold);
            Debug.Log($"골드 {amount} 획득, 현재 골드: {_currentGold}");
        }

        public bool HasEnoughGold(int amount)
        {
            return _currentGold >= amount;
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0) return false;

            if (!HasEnoughGold(amount))
            {
                Debug.Log($"골드 부족: 필요 {amount}, 보유 {_currentGold}");
                return false;
            }

            _currentGold -= amount;
            SaveGold();
            OnGoldChanged?.Invoke(_currentGold);
            Debug.Log($"골드 {amount} 소비, 현재 골드: {_currentGold}");
            return true;
        }

        private void LoadGold()
        {
            _currentGold = PlayerPrefs.GetInt(GOLD_KEY, DEFAULT_GOLD);
        }

        private void SaveGold()
        {
            PlayerPrefs.SetInt(GOLD_KEY, _currentGold);
            PlayerPrefs.Save();
        }

        #endregion

        #region Unlock

        private readonly HashSet<string> _unlockedIds = new HashSet<string>();

        public event Action<UnlockNodeData> OnUnlockChanged;

        public bool IsUnlocked(UnlockNodeData node)
        {
            if (node == null) return false;

            if (_unlockedIds.Contains(node.nodeId)) return true;

            if (PlayerPrefs.GetInt(UnlockKey(node), 0) == 1)
            {
                _unlockedIds.Add(node.nodeId);
                return true;
            }

            return false;
        }

        public bool CanUnlock(UnlockNodeData node)
        {
            if (node == null || IsUnlocked(node)) return false;

            foreach (UnlockNodeData prereq in node.prerequisites)
            {
                if (!IsUnlocked(prereq)) return false;
            }

            return HasEnoughGold(node.cost);
        }

        public bool TryUnlock(UnlockNodeData node)
        {
            if (!CanUnlock(node)) return false;

            TrySpendGold(node.cost);
            _unlockedIds.Add(node.nodeId);

            PlayerPrefs.SetInt(UnlockKey(node), 1);
            PlayerPrefs.Save();

            OnUnlockChanged?.Invoke(node);
            return true;
        }

        private string UnlockKey(UnlockNodeData node)
        {
            return $"UnlockNode_{node.nodeId}";
        }

        #endregion

        #region Game Record

        private const string BEST_DEPTH_KEY = "Data_BestDepth";

        private float _bestDepth;
        public float BestDepth => _bestDepth;

        public event Action<float> OnBestDepthChanged;

        public bool TryUpdateBestDepth(float depth)
        {
            if (depth <= _bestDepth) return false;

            _bestDepth = depth;
            PlayerPrefs.SetFloat(BEST_DEPTH_KEY, _bestDepth);
            PlayerPrefs.Save();

            OnBestDepthChanged?.Invoke(_bestDepth);
            return true;
        }

        private void LoadGameRecord()
        {
            _bestDepth = PlayerPrefs.GetFloat(BEST_DEPTH_KEY, 0f);
        }

        #endregion
    }
}
