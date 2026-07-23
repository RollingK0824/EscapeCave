using System.Collections;
using Managers;
using UnityEngine;

namespace Game.Echo
{
    /// <summary>
    /// 타이틀 씬 전용 잔물결 에코 파동 링 (Title Echo Wave Ring).
    /// 물방울 충돌 지점에서 발생하여 바깥쪽으로 부드럽게 팽창(Expand)하면서
    /// 알파 투명도가 감쇄(Fade Out)하여 몽환적인 동심원 물결을 연출합니다.
    /// </summary>
    public class TitleEchoWaveRing : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private float _currentRadius = 0f;
        private float _maxRadius = 12f;
        private float _expandSpeed = 6f;
        private float _duration = 2.0f;
        private Color _baseColor = Color.white;
        private float _elapsedTime = 0f;
        private GameObject _originPrefab;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// 파동 링 파라미터 초기화 및 설정
        /// </summary>
        public void SetupRing(float maxRadius, float expandSpeed, float duration, GameObject originPrefab = null)
        {
            _maxRadius = maxRadius;
            _expandSpeed = expandSpeed;
            _duration = Mathf.Max(0.1f, duration);
            _originPrefab = originPrefab;

            _currentRadius = 0f;
            _elapsedTime = 0f;
            transform.localScale = Vector3.zero;

            if (_spriteRenderer != null)
            {
                _baseColor = _spriteRenderer.color;
                _baseColor.a = 1f;
                _spriteRenderer.color = _baseColor;
            }
        }

        private void Update()
        {
            _elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsedTime / _duration);

            // 1. 반지름 부근까지 부드럽게 팽창 (EaseOutQuad)
            float easeOutT = progress * (2f - progress);
            _currentRadius = Mathf.Lerp(0f, _maxRadius, easeOutT);
            transform.localScale = new Vector3(_currentRadius, _currentRadius, 1f);

            // 2. 시간이 지남에 따라 알파값 감쇄 (Fade Out)
            if (_spriteRenderer != null)
            {
                float alpha = Mathf.SmoothStep(1f, 0f, progress);
                Color c = _baseColor;
                c.a = alpha;
                _spriteRenderer.color = c;
            }

            // 3. 수명 종료 시 반납
            if (progress >= 1f)
            {
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            if (PoolManager.Instance != null && _originPrefab != null)
            {
                PoolManager.Instance.Push(gameObject, _originPrefab);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
