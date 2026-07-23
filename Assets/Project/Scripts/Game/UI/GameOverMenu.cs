using System.Collections.Generic;
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

    [Header("신기록 달성 알림 UI (TextMeshPro)")]
    [SerializeField] private TMP_Text _newRecordText;

    [Header("씬 이름 (Build Settings에 등록된 이름과 정확히 일치해야 함)")]
    [SerializeField] private string gameSceneName;
    [SerializeField] private string titleSceneName;

    private Coroutine _animationCoroutine;

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

            // 2. [정산 실행] 획득 골드를 TotalGold에 더하고 세션 초기화 (TotalGold 정산 및 로컬 순위 반환)
            int localRank = UIManager.Instance.EndSession();

            // 3. [정산 후] 최신 정산이 반영된 TotalGold를 UI에 표시
            if (_totalGoldText != null)
            {
                _totalGoldText.text = $"{UIManager.Instance.TotalGold} G";
            }

            // 기본적으로 알림 비활성화
            if (_newRecordText != null)
            {
                _newRecordText.gameObject.SetActive(false);
            }

            // 4. [온라인 랭킹 연동을 통한 실시간 순위 계산 및 연출]
            if (_newRecordText != null && sessionScore > 0f)
            {
                UIManager.Instance.FetchOnlineLeaderboard(
                    onSuccess: (onlineScores) => {
                        int onlineRank = GetOnlineRank(onlineScores, sessionScore);
                        if (onlineRank >= 1 && onlineRank <= 10)
                        {
                            _newRecordText.gameObject.SetActive(true);
                            _newRecordText.text = $"NEW RECORD! GLOBAL {GetRankString(onlineRank)}!!";
                            StartNewRecordAnimation();
                        }
                        else
                        {
                            _newRecordText.gameObject.SetActive(false);
                        }
                    },
                    onError: (err) => {
                        Debug.LogWarning($"[GameOverMenu] 온라인 랭킹 조회 실패, 로컬 랭킹 대체 적용: {err}");
                        if (localRank >= 1 && localRank <= 10)
                        {
                            _newRecordText.gameObject.SetActive(true);
                            _newRecordText.text = $"NEW RECORD! LOCAL {GetRankString(localRank)}!!";
                            StartNewRecordAnimation();
                        }
                        else
                        {
                            _newRecordText.gameObject.SetActive(false);
                        }
                    }
                );
            }
        }
    }

    private void OnDisable()
    {
        StopNewRecordAnimation();
    }

    private void StartNewRecordAnimation()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }
        _animationCoroutine = StartCoroutine(PlayNewRecordEffects());
    }

    private void StopNewRecordAnimation()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }
    }

    private System.Collections.IEnumerator PlayNewRecordEffects()
    {
        if (_newRecordText == null) yield break;

        // 초기 크기를 0으로 리셋
        _newRecordText.transform.localScale = Vector3.zero;
        Color originalColor = _newRecordText.color;

        // 씬 페이드인 및 정산 속도를 고려해 0.2초 딜레이 후 노출 시작
        yield return new WaitForSeconds(0.2f);

        // 1. Pop Up 효과 (0.25초 동안 크기 0 -> 1.3)
        float duration = 0.25f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(0f, 1.3f, t);
            _newRecordText.transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        // 2. Settle 효과 (0.1초 동안 크기 1.3 -> 1.0)
        duration = 0.1f;
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1.3f, 1.0f, t);
            _newRecordText.transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        _newRecordText.transform.localScale = Vector3.one;

        // 3. Pulse & Alpha breathing 효과 (무한 루프)
        float pulseSpeed = 2f; 
        while (true)
        {
            float wave = Mathf.Sin(Time.time * pulseSpeed);
            
            // 크기 변화: 0.95 ~ 1.05
            float scale = 1.0f + (wave * 0.05f);
            _newRecordText.transform.localScale = new Vector3(scale, scale, 1f);

            // 알파(투명도) 변화: 0.75 ~ 1.0
            float alpha = 0.875f + (wave * 0.125f);
            Color nextColor = originalColor;
            nextColor.a = alpha;
            _newRecordText.color = nextColor;

            yield return null;
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

    private int GetOnlineRank(List<ScoreEntry> onlineScores, float currentScore)
    {
        if (currentScore <= 0f) return -1;

        // 1. 내 디바이스 ID를 기반으로 이미 등록된 랭킹 정보가 있는지 먼저 확인
        string myDeviceId = "";
        if (SupabaseManager.Instance != null)
        {
            myDeviceId = SupabaseManager.Instance.DeviceId;
        }

        int existingRank = -1;
        for (int i = 0; i < onlineScores.Count; i++)
        {
            // 만약 서버에서 넘어온 데이터에 이미 내 기록이 업로드되어 매치된다면 그 순위를 신뢰하여 반환
            if (!string.IsNullOrEmpty(myDeviceId) && 
                onlineScores[i].user_id == myDeviceId && 
                Mathf.Approximately(onlineScores[i].score, currentScore))
            {
                existingRank = i + 1;
                break;
            }
        }

        if (existingRank != -1)
        {
            return existingRank;
        }

        // 2. 만약 아직 내 기록이 서버 리스트에 반영되기 전이라면 가상으로 대입해 순위 계산
        List<float> scores = new List<float>();
        foreach (var entry in onlineScores)
        {
            scores.Add(entry.score);
        }

        scores.Add(currentScore);
        scores.Sort((a, b) => b.CompareTo(a)); // 내림차순 정렬

        int index = scores.IndexOf(currentScore);
        if (index >= 0 && index < 10)
        {
            return index + 1;
        }

        return -1;
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