using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Managers
{
    [Serializable]
    public class ScoreEntry
    {
        public string user_id;
        public string nickname;
        public float score;
        public string created_at;
    }

    [Serializable]
    public class ScoreEntryListWrapper
    {
        public ScoreEntry[] items;
    }

    [Serializable]
    public class ScorePostPayload
    {
        public string user_id;
        public string nickname;
        public float score;
    }

    [Serializable]
    public class NicknameUpdatePayload
    {
        public string nickname;
    }

    public class SupabaseManager : SingletonBase<SupabaseManager>
    {
        [Header("Supabase API Settings")]
        [SerializeField] private string _supabaseUrl = "https://jzxxqnyhgmnetpmxpwqh.supabase.co";
        [SerializeField] private string _supabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imp6eHhxbnloZ21uZXRwbXhwd3FoIiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODQ3MDMwNDQsImV4cCI6MjEwMDI3OTA0NH0.jEg8XB_7KEffSV6GGsRJ2tdNtrN7rk5Jp7cy1Am_Tk0";

        [ContextMenu("Reset Component Settings")]
        private void ResetSettings()
        {
            _supabaseUrl = "https://jzxxqnyhgmnetpmxpwqh.supabase.co";
            _supabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imp6eHhxbnloZ21uZXRwbXhwd3FoIiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODQ3MDMwNDQsImV4cCI6MjEwMDI3OTA0NH0.jEg8XB_7KEffSV6GGsRJ2tdNtrN7rk5Jp7cy1Am_Tk0";
            Debug.Log("[Supabase] Settings reset to default!");
        }


        private string _deviceId;
        public string DeviceId => _deviceId;

        private string GetCleanUrl()
        {
            if (string.IsNullOrEmpty(_supabaseUrl)) return string.Empty;
            string clean = _supabaseUrl.Trim();
            if (clean.EndsWith("/")) clean = clean.TrimEnd('/');
            if (clean.EndsWith("/rest/v1")) clean = clean.Substring(0, clean.Length - 8);
            if (clean.EndsWith("/")) clean = clean.TrimEnd('/');
            return clean;
        }

        protected override void Awake()
        {
            base.Awake();
            InitDeviceId();
            Debug.Log($"[Supabase] Initialized with URL: '{GetCleanUrl()}'");
        }

        private void InitDeviceId()
        {
            _deviceId = PlayerPrefs.GetString("Data_UserDeviceId", string.Empty);
            if (string.IsNullOrEmpty(_deviceId))
            {
                _deviceId = Guid.NewGuid().ToString();
                PlayerPrefs.SetString("Data_UserDeviceId", _deviceId);
                PlayerPrefs.Save();
            }
        }

        /// <summary> 글로벌 Top 10 리더보드 가져오기 </summary>
        public void FetchTop10(Action<List<ScoreEntry>> onSuccess, Action<string> onError = null)
        {
            StartCoroutine(CoFetchTop10(onSuccess, onError));
        }

        #region Inspector Context Menu Tests

        [ContextMenu("Test Post Score (150.5M)")]
        private void TestPostScore()
        {
            PostScore("TestFrog", 150.5f, 
                onSuccess: () => Debug.Log("✅ [Test] Supabase 점수 등록 성공!"), 
                onError: (err) => Debug.LogError($"❌ [Test] Supabase 점수 등록 실패: {err}"));
        }

        [ContextMenu("Test Fetch Top 10")]
        private void TestFetchTop10()
        {
            FetchTop10(
                onSuccess: (list) => {
                    Debug.Log($"✅ [Test] Supabase Top 10 조회 성공! 총 {list.Count}개 항목 수신됨.");
                    for (int i = 0; i < list.Count; i++)
                    {
                        Debug.Log($"   🏆 [{i + 1}위] {list[i].nickname} - {list[i].score:F1}M (UID: {list[i].user_id})");
                    }
                },
                onError: (err) => Debug.LogError($"❌ [Test] Supabase Top 10 조회 실패: {err}")
            );
        }

        #endregion

        private IEnumerator CoFetchTop10(Action<List<ScoreEntry>> onSuccess, Action<string> onError)
        {
            string baseUrl = GetCleanUrl();
            string url = $"{baseUrl}/rest/v1/leaderboards?select=user_id,nickname,score,created_at&order=score.desc&limit=10";

            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.SetRequestHeader("apikey", _supabaseAnonKey.Trim());
                req.SetRequestHeader("Authorization", $"Bearer {_supabaseAnonKey.Trim()}");

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    string json = req.downloadHandler.text;
                    string wrappedJson = $"{{\"items\":{json}}}";
                    ScoreEntryListWrapper wrapper = JsonUtility.FromJson<ScoreEntryListWrapper>(wrappedJson);

                    List<ScoreEntry> list = new List<ScoreEntry>(wrapper.items ?? new ScoreEntry[0]);
                    onSuccess?.Invoke(list);
                }
                else
                {
                    Debug.LogError($"[Supabase] Fetch Leaderboard Error: {req.error} (Requested URL: '{url}')");
                    onError?.Invoke(req.error);
                }
            }
        }

        /// <summary> 최고 기록 DB에 저장하기 </summary>
        public void PostScore(string nickname, float score, Action onSuccess = null, Action<string> onError = null)
        {
            StartCoroutine(CoPostScore(nickname, score, onSuccess, onError));
        }

        private IEnumerator CoPostScore(string nickname, float score, Action onSuccess, Action<string> onError)
        {
            string baseUrl = GetCleanUrl();
            string url = $"{baseUrl}/rest/v1/leaderboards";

            // 닉네임이 비어있으면 기본값 지정
            string displayNickname = string.IsNullOrEmpty(nickname) ? $"User_{_deviceId.Substring(0, 4)}" : nickname;

            ScorePostPayload payload = new ScorePostPayload
            {
                user_id = _deviceId,
                nickname = displayNickname,
                score = score
            };

            string jsonBody = JsonUtility.ToJson(payload);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("apikey", _supabaseAnonKey.Trim());
                req.SetRequestHeader("Authorization", $"Bearer {_supabaseAnonKey.Trim()}");
                req.SetRequestHeader("Prefer", "return=minimal");

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success || req.responseCode == 201)
                {
                    Debug.Log($"[Supabase] 점수 전송 성공! (닉네임: {displayNickname}, 기록: {score}M)");
                    onSuccess?.Invoke();
                }
                else
                {
                    Debug.LogError($"[Supabase] Post Score Error: {req.error} (Requested URL: '{url}')");
                    onError?.Invoke(req.error);
                }
            }
        }

        /// <summary> 기존에 등록된 랭킹 데이터의 닉네임을 일괄 변경 </summary>
        public void UpdateNickname(string newNickname, Action onSuccess = null, Action<string> onError = null)
        {
            StartCoroutine(CoUpdateNickname(newNickname, onSuccess, onError));
        }

        private IEnumerator CoUpdateNickname(string newNickname, Action onSuccess, Action<string> onError)
        {
            string baseUrl = GetCleanUrl();
            string url = $"{baseUrl}/rest/v1/leaderboards?user_id=eq.{_deviceId}";

            NicknameUpdatePayload payload = new NicknameUpdatePayload { nickname = newNickname };
            string jsonBody = JsonUtility.ToJson(payload);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

            using (UnityWebRequest req = new UnityWebRequest(url, "PATCH"))
            {
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("apikey", _supabaseAnonKey.Trim());
                req.SetRequestHeader("Authorization", $"Bearer {_supabaseAnonKey.Trim()}");
                req.SetRequestHeader("Prefer", "return=minimal");

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success || req.responseCode == 200 || req.responseCode == 204)
                {
                    Debug.Log($"[Supabase] 닉네임 업데이트 성공! ({newNickname})");
                    onSuccess?.Invoke();
                }
                else
                {
                    Debug.LogError($"[Supabase] Update Nickname Error: {req.error} (StatusCode: {req.responseCode}, URL: '{url}')");
                    onError?.Invoke(req.error);
                }
            }
        }
    }
}