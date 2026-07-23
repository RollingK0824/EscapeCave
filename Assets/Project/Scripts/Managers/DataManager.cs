using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Managers
{
    [Serializable]
    public class HighScoreData
    {
        public List<float> records = new List<float>();
    }

    public class DataManager : SingletonBase<DataManager>
    {
        protected override void Awake()
        {
            base.Awake();

            LoadGold();
            LoadGameRecord();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        [SerializeField] private string _gameSceneName = "ProtoType";

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 씬이 로드될 때 이전 세션 데이터가 남아있지 않도록 시작
            StartNewSession();
        }

        #region Total Gold

        private const string GOLD_KEY = "Data_Gold";
        private const int DEFAULT_GOLD = 5;

        [SerializeField] private int _totalGold;
        public int TotalGold => _totalGold;

        public event Action<int> OnTotalGoldChanged;

        public void AddTotalGold(int amount)
        {
            if (amount <= 0) return;

            _totalGold += amount;
            SaveGold();
            OnTotalGoldChanged?.Invoke(_totalGold);
            Debug.Log($"총 골드 {amount} 획득, 현재 보유 골드: {_totalGold}");
        }

        public bool HasEnoughGold(int amount)
        {
            return _totalGold >= amount;
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0) return false;

            if (!HasEnoughGold(amount))
            {
                Debug.Log($"골드 부족: 필요 {amount}, 보유 {_totalGold}");
                return false;
            }

            _totalGold -= amount;
            SaveGold();
            OnTotalGoldChanged?.Invoke(_totalGold);
            Debug.Log($"골드 {amount} 소비, 현재 보유 골드: {_totalGold}");
            return true;
        }

        private void LoadGold()
        {
            _totalGold = PlayerPrefs.GetInt(GOLD_KEY, DEFAULT_GOLD);
        }

        private void SaveGold()
        {
            PlayerPrefs.SetInt(GOLD_KEY, _totalGold);
            PlayerPrefs.Save();
        }

        #endregion

        #region In-Game Session Data (Transient)

        [SerializeField] private int _currentGold;
        public int CurrentGold => _currentGold;

        [SerializeField] private float _currentScore;
        public float CurrentScore => _currentScore;

        public event Action<int> OnCurrentGoldChanged;
        public event Action<float> OnCurrentScoreChanged;

        /// <summary> 게임 시작/씬 진입 시 런타임 세션 데이터(획득 골드, 진행 점수/거리) 초기화 </summary>
        public void StartNewSession()
        {
            _currentGold = 0;
            _currentScore = 0f;

            OnCurrentGoldChanged?.Invoke(_currentGold);
            OnCurrentScoreChanged?.Invoke(_currentScore);
        }

        /// <summary> 인게임 게임플레이 중 골드 획득 </summary>
        public void AddCurrentGold(int amount)
        {
            if (amount <= 0) return;

            _currentGold += amount;
            OnCurrentGoldChanged?.Invoke(_currentGold);
        }

        /// <summary> 인게임 게임플레이 중 진행 점수/거리(M) 갱신 </summary>
        public void UpdateCurrentScore(float score)
        {
            if (score <= _currentScore) return;

            _currentScore = score;
            OnCurrentScoreChanged?.Invoke(_currentScore);
        }

        /// <summary> 게임 종료/정산 시 획득 골드를 총 골드에 반영하고 Top 10 기록 갱신 및 세션 초기화 </summary>
        public int EndSession()
        {
            if (_currentGold > 0)
            {
                AddTotalGold(_currentGold);
            }

            int rank = TryAddScore(_currentScore);

            // Supabase 온라인 DB에도 기록 등록
            if (_currentScore > 0f && SupabaseManager.Instance != null)
            {
                string userNickname = PlayerPrefs.GetString("Data_UserNickname", string.Empty);
                SupabaseManager.Instance.PostScore(userNickname, _currentScore);
            }

            // 정산 후 세션 데이터 초기화 (Retry 시 이전 데이터가 남지 않도록)
            _currentGold = 0;
            _currentScore = 0f;

            OnCurrentGoldChanged?.Invoke(_currentGold);
            OnCurrentScoreChanged?.Invoke(_currentScore);

            return rank;
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

        #region Game Record & Top 10 Leaderboard

        private const string TOP10_RECORDS_KEY = "Data_Top10Records";
        private const int MAX_RECORD_COUNT = 10;

        private List<float> _topScores = new List<float>();
        public IReadOnlyList<float> TopScores => _topScores.AsReadOnly();

        // 1위 최고 기록
        public float BestScore => _topScores.Count > 0 ? _topScores[0] : 0f;

        public event Action<IReadOnlyList<float>> OnTopScoresChanged;
        public event Action<float> OnBestScoreChanged;

        public int TryAddScore(float score)
        {
            if (score <= 0f) return -1;

            float previousBest = BestScore;

            // 10개가 채워져 있고, 최하위 기록보다 낮거나 같으면 갱신 안 함
            if (_topScores.Count >= MAX_RECORD_COUNT && score <= _topScores[_topScores.Count - 1])
            {
                return -1;
            }

            _topScores.Add(score);
            _topScores.Sort((a, b) => b.CompareTo(a)); // 내림차순 정렬

            if (_topScores.Count > MAX_RECORD_COUNT)
            {
                _topScores.RemoveRange(MAX_RECORD_COUNT, _topScores.Count - MAX_RECORD_COUNT);
            }

            int rank = _topScores.IndexOf(score) + 1;

            SaveTopRecords();
            OnTopScoresChanged?.Invoke(_topScores.AsReadOnly());

            if (BestScore > previousBest)
            {
                OnBestScoreChanged?.Invoke(BestScore);
            }

            return rank;
        }

        private void LoadGameRecord()
        {
            string json = PlayerPrefs.GetString(TOP10_RECORDS_KEY, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                HighScoreData data = JsonUtility.FromJson<HighScoreData>(json);
                _topScores = data.records ?? new List<float>();
            }
            else
            {
                _topScores = new List<float>();
            }
        }

        private void SaveTopRecords()
        {
            HighScoreData data = new HighScoreData { records = _topScores };
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(TOP10_RECORDS_KEY, json);
            PlayerPrefs.Save();
        }

        #endregion
    }
}
