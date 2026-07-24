using UnityEngine;
using Managers;

public class EchoWaveObject : MonoBehaviour
{
    private enum WaveState
    {
        Expand,   // 1단계: 초고속 폭발 확장
        Linger,   // 2단계: 잔류 및 미세 링 진동
        Collapse  // 3단계: 역방향 수축 소멸
    }

    private SpriteRenderer _spriteRenderer;
    private float _currentRadius = 0f;
    private float _maxRadius = 5f;
    private float _expansionSpeed = 8f;
    private float _fadeSpeed = 1f;
    private Color _waveColor = Color.white;

    private WaveState _state = WaveState.Expand;
    private float _stateTimer = 0f;
    private float _expandDuration = 0.08f;  // 0.08초 폭발 확장
    private float _lingerDuration = 1.2f;   // 1.2초 잔류 및 진동
    private float _collapseDuration = 0.4f; // 0.4초 수축 소멸

    private GameObject _originPrefab;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetupWave(float intensity, float speed, GameObject source, float fadeSpeed = -1f)
    {
        _maxRadius = intensity;
        _expansionSpeed = speed;
        _fadeSpeed = (fadeSpeed > 0f) ? fadeSpeed : 1f;
        _originPrefab = source;

        // 페이드 속도에 맞춘 잔류 시간 보정 (기본 약 1.2초)
        _lingerDuration = 1.2f / _fadeSpeed;

        _state = WaveState.Expand;
        _stateTimer = 0f;
        _currentRadius = 0f;
        transform.localScale = Vector3.zero;
        _waveColor = Color.white;

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = _waveColor;
        }
    }

    private void Update()
    {
        // Time.timeScale이 0인 컷신(예: MonsterRevealTrigger) 중에도 웨이브가 멈추지 않고 재생되도록 unscaled 시간을 사용한다
        _stateTimer += Time.unscaledDeltaTime;

        switch (_state)
        {
            case WaveState.Expand:
                // 1단계: EaseOut 폭발 확장 (0.08초 만에 지정 반지름까지 팍!)
                float expandProgress = Mathf.Clamp01(_stateTimer / _expandDuration);
                float easeOutT = expandProgress * (2f - expandProgress); // EaseOutQuad
                _currentRadius = Mathf.Lerp(0f, _maxRadius, easeOutT);
                _waveColor.a = 1f;

                if (expandProgress >= 1f)
                {
                    _state = WaveState.Linger;
                    _stateTimer = 0f;
                }
                break;

            case WaveState.Linger:
                // 2단계: 최대 지름 유지 + 미세 링 진동 (Wiggle)
                float vibrate = Mathf.Sin(Time.unscaledTime * 25f) * 0.025f * _maxRadius;
                _currentRadius = _maxRadius + vibrate;
                _waveColor.a = 1f;

                if (_stateTimer >= _lingerDuration)
                {
                    _state = WaveState.Collapse;
                    _stateTimer = 0f;
                }
                break;

            case WaveState.Collapse:
                // 3단계: 원이 안쪽(중심)으로 오므라들며 수축 소멸 (Inward Collapse)
                float collapseProgress = Mathf.Clamp01(_stateTimer / _collapseDuration);
                _currentRadius = Mathf.Lerp(_maxRadius, 0f, collapseProgress);
                
                // 알파 감쇄 (수축하며 Fade Out)
                float alphaProgress = 1f - collapseProgress;
                _waveColor.a = alphaProgress * alphaProgress;

                if (collapseProgress >= 1f)
                {
                    ReturnToPool();
                    return;
                }
                break;
        }

        transform.localScale = new Vector3(_currentRadius, _currentRadius, 1f);

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = _waveColor;
        }
    }

    private void ReturnToPool()
    {
        if (PoolManager.Instance != null && _originPrefab != null)
        {
            PoolManager.Instance.Push(gameObject, _originPrefab);
        }
    }
}
