using UnityEngine;
using System.Collections;
using Managers;

namespace Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundPlayer : MonoBehaviour
    {
        private AudioSource _audioSource;
        private int _poolKey;
        private Coroutine _playCoroutine;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        public void Play(SoundDataSO data, int poolKey, Vector3? position = null)
        {
            _poolKey = poolKey;

            if (_playCoroutine != null)
            {
                StopCoroutine(_playCoroutine);
            }

            // 3D 공간 오디오와 2D 오디오 분리 설정
            if (position.HasValue)
            {
                transform.position = position.Value;
                _audioSource.spatialBlend = 1.0f; // 3D
            }
            else
            {
                _audioSource.spatialBlend = 0.0f; // 2D
            }

            // 사운드 특성 데이터 설정
            _audioSource.clip = data.GetClip();
            if (_audioSource.clip == null)
            {
                ReturnToPool();
                return;
            }

            _audioSource.volume = data.volume;
            // 피치 변동성(Randomness) 처리로 기계적인 중복 사운드 느낌 해소
            _audioSource.pitch = data.pitch + Random.Range(-data.pitchRandomness, data.pitchRandomness);
            _audioSource.loop = data.loop;
            _audioSource.outputAudioMixerGroup = data.mixerGroup;

            _audioSource.Play();

            // 루프 사운드가 아닌 경우 사운드 길이만큼 대기한 후 자동으로 풀에 반환
            if (!data.loop)
            {
                float duration = _audioSource.clip.length / Mathf.Max(0.01f, Mathf.Abs(_audioSource.pitch));
                _playCoroutine = StartCoroutine(CoReturnToPoolAfterPlay(duration));
            }
        }

        private IEnumerator CoReturnToPoolAfterPlay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnToPool();
        }

        public void StopAndReturn()
        {
            if (_playCoroutine != null)
            {
                StopCoroutine(_playCoroutine);
                _playCoroutine = null;
            }
            _audioSource.Stop();
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            _audioSource.clip = null; // 오디오 클립 참조 해제하여 GC 대상이 되도록 처리
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Push(gameObject, _poolKey);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
