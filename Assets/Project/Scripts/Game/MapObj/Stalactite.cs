using UnityEngine;
using System.Collections;

/// <summary>
/// 종유석 오브젝트.
/// 주기적으로 무작위 시간에 물방울 프리팹을 풀에서 생성(Pop)하여 떨어뜨립니다.
/// </summary>
public class Stalactite : MonoBehaviour
{
    [Header("물방울 스폰 설정")]
    [SerializeField] private GameObject _waterDropPrefab;
    [SerializeField] private Transform _dropPoint;

    [Header("낙하 주기 범위 (초)")]
    [SerializeField] private float _minSpawnInterval = 1.5f;
    [SerializeField] private float _maxSpawnInterval = 4.0f;

    private Coroutine _spawnCoroutine;

    private void Start()
    {
        if (_dropPoint == null)
        {
            _dropPoint = transform;
        }
    }

    private void OnEnable()
    {
        if (_spawnCoroutine == null)
        {
            _spawnCoroutine = StartCoroutine(CoSpawnWaterDrops());
        }
    }

    private void OnDisable()
    {
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
    }

    private IEnumerator CoSpawnWaterDrops()
    {
        while (true)
        {
            float randomInterval = Random.Range(_minSpawnInterval, _maxSpawnInterval);
            yield return new WaitForSeconds(randomInterval);

            SpawnDrop();
        }
    }

    private void SpawnDrop()
    {
        if (_waterDropPrefab == null || Managers.PoolManager.Instance == null) return;

        GameObject dropObj = Managers.PoolManager.Instance.Pop(_waterDropPrefab, _dropPoint.position, Quaternion.identity);
        if (dropObj != null)
        {
            WaterDrop waterDrop = dropObj.GetComponent<WaterDrop>();
            if (waterDrop != null)
            {
                waterDrop.poolKey = _waterDropPrefab.GetInstanceID();
            }
        }
    }
}
