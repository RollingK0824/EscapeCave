using JetBrains.Annotations;
using Managers;
using UnityEngine;

public class DistanceTracker : MonoBehaviour
{
    [Header("추적 대상")]
    [SerializeField]
    private Transform _player;

    [Header("거리 보정 설정")]
    [Tooltip("1 Unity Unit 당 몇 Meter(m)로 계산할 것인지")]
    private float _unitToMeterScale = 1.0f;

    private float _startX;
    private float _maxDistance;

    private void Start()
    {
        if (_player != null)
        {
            _startX = _player.position.x;
        }
    }

    private void Update()
    {
        if (_player == null) return;

        float currentRawDiatance = Mathf.Max(0f, _player.position.x - _startX);
        float currentMeter = currentRawDiatance * _unitToMeterScale;

        if (currentMeter > _maxDistance)
        {
            _maxDistance = currentMeter;

            UIManager.Instance.UpdateCurrentScore(_maxDistance);
        }
    }

    public void ResetTracker()
    {
        if (_player != null)
        {
            _startX = _player.position.x;
        }

        _maxDistance = 0f;
    }
}
