using UnityEngine;
using Managers;

public class EchoWaveObject : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private float _currentRadius = 0f;
    private float _maxRadius = 5f;
    private float _expansionSpeed = 8f;
    private float _fadeSpeed = 2f;
    private Color _waveColor = Color.white;

    private GameObject _originPrefab;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetupWave(float intensity, float speed, GameObject source)
    {
        _maxRadius = intensity;
        _expansionSpeed = speed;
        _fadeSpeed = speed / intensity;
        _originPrefab = source;

        transform.localScale = Vector3.zero;
        _currentRadius = 0f;
        _waveColor = Color.white;

        if(_spriteRenderer != null)
        {
            _spriteRenderer.color = _waveColor;
        }
    }

    private void Update()
    {
        if(_currentRadius < _maxRadius)
        {
            _currentRadius += _expansionSpeed * Time.deltaTime;
            transform.localScale = new Vector3(_currentRadius, _currentRadius, 1f);
        }

        _waveColor.a -= _fadeSpeed * Time.deltaTime;
        if(_spriteRenderer != null)
        {
            _spriteRenderer.color = _waveColor;
        }

        if(_waveColor.a <= 0f)
        {
            if(PoolManager.Instance != null && _originPrefab != null)
            {
                PoolManager.Instance.Push(gameObject, _originPrefab);
            }
        }
    }
}
