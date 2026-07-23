using System.Collections;
using System.Collections.Generic;
using Managers;
using UnityEngine;

public class TitleWaterDropSpawner : MonoBehaviour
{
    [Header("스폰 프리팹 설정")]
    [SerializeField, Tooltip("낙하할 물방울 프리팹 (WaterDrop 또는 TutorialWaterDrop 컴포넌트 부착 프리팹)")]
    private GameObject _waterDropPrefab;

    [Header("스폰 위치 설정")]
    [SerializeField, Tooltip("물방울이 생성될 위치 목록 (비어있으면 현재 Transform 위치 사용)")]
    private Transform[] _spawnPoints;

    [Header("스폰 간격 설정 (초 단위)")]
    [SerializeField, Tooltip("최소 스폰 간격")]
    private float _minSpawnInterval = 3.5f;

    [SerializeField, Tooltip("최대 스폰 간격")]
    private float _maxSpawnInterval = 6.0f;

    [Header("옵션 설정")]
    [SerializeField, Tooltip("씬 진입 시 첫 물방울을 빠르게 생성할지 여부")]
    private bool _spawnInitialDropQuickly = true;

    [SerializeField, Tooltip("첫 물방울 생성까지의 대기 시간")]
    private float _initialDelay = 1.0f;

    private Coroutine _spawnCoroutine;

    private void OnEnable()
    {
        StartSpawning();
    }

    private void OnDisable()
    {
        StopSpawning();
    }

    /// <summary>
    /// 물방울 자동 생성 시작
    /// </summary>
    public void StartSpawning()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
        }
        _spawnCoroutine = StartCoroutine(CoSpawnRoutine());
    }

    /// <summary>
    /// 물방울 생성 중지
    /// </summary>
    public void StopSpawning()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
    }

    /// <summary>
    /// 수동으로 물방울 1개 즉시 드롭 (테스트 / 이벤트용)
    /// </summary>
    public void SpawnSingleDrop()
    {
        if (_waterDropPrefab == null)
        {
            Debug.LogWarning($"[{nameof(TitleWaterDropSpawner)}] Water Drop Prefab이 할당되지 않았습니다.", this);
            return;
        }

        Vector3 spawnPosition = GetRandomSpawnPosition();
        Quaternion spawnRotation = Quaternion.identity;

        GameObject dropObj = Instantiate(_waterDropPrefab, spawnPosition, spawnRotation);

        if (dropObj != null)
        {
            // Rigidbody2D 속도 초기화 (이전 물리 영향 제거)
            if (dropObj.TryGetComponent<Rigidbody2D>(out var rb))
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
    }

    private IEnumerator CoSpawnRoutine()
    {
        if (_spawnInitialDropQuickly)
        {
            yield return new WaitForSeconds(_initialDelay);
            SpawnSingleDrop();
        }

        while (true)
        {
            float nextInterval = Random.Range(_minSpawnInterval, _maxSpawnInterval);
            yield return new WaitForSeconds(nextInterval);

            SpawnSingleDrop();
        }
    }

    /// <summary>
    /// 등록된 스폰 지점 중 하나를 랜덤으로 선택하여 반환
    /// </summary>
    private Vector3 GetRandomSpawnPosition()
    {
        if (_spawnPoints != null && _spawnPoints.Length > 0)
        {
            // null이 아닌 스폰 포인트들 추출
            List<Transform> validPoints = new List<Transform>();
            foreach (var pt in _spawnPoints)
            {
                if (pt != null) validPoints.Add(pt);
            }

            if (validPoints.Count > 0)
            {
                int randomIndex = Random.Range(0, validPoints.Count);
                return validPoints[randomIndex].position;
            }
        }

        return transform.position;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        if (_spawnPoints != null && _spawnPoints.Length > 0)
        {
            foreach (var pt in _spawnPoints)
            {
                if (pt != null)
                {
                    Gizmos.DrawWireSphere(pt.position, 0.25f);
                    Gizmos.DrawLine(pt.position, pt.position + Vector3.down * 1.5f);
                }
            }
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 0.25f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 1.5f);
        }
    }
#endif
}
