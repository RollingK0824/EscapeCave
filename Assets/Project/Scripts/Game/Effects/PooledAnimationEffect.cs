using Managers;
using UnityEngine;

/// <summary>
/// 지정된 애니메이션 클립 길이만큼 재생한 뒤 스스로 PoolManager에 반납되는
/// 1회성 스프라이트 애니메이션 이펙트. PooledParticleEffect의 Animator 버전입니다.
/// </summary>
public class PooledAnimationEffect : MonoBehaviour
{
    [SerializeField, Tooltip("재생 길이를 여기서 읽어옵니다. 실제 재생되는 클립과 같은 걸 연결해두세요.")]
    private AnimationClip _clip;

    private GameObject _originPrefab;
    private float _elapsed;
    private bool _isPlaying;

    /// <summary>Pop으로 위치를 옮긴 직후 호출해서 반납 타이머를 시작합니다.</summary>
    public void Play(GameObject originPrefab)
    {
        _originPrefab = originPrefab;
        _elapsed = 0f;
        _isPlaying = true;
    }

    private void Update()
    {
        if (!_isPlaying) return;

        _elapsed += Time.deltaTime;
        float duration = _clip != null ? _clip.length : 0.5f;
        if (_elapsed < duration) return;

        _isPlaying = false;
        if (PoolManager.Instance != null && _originPrefab != null)
        {
            PoolManager.Instance.Push(gameObject, _originPrefab);
        }
    }
}
