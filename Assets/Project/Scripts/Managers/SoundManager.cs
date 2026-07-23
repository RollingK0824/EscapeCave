using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Audio;

namespace Managers
{
    public class SoundManager : SingletonBase<SoundManager>
    {
        [Header("BGM Settings")]
        [SerializeField] private AudioSource[] _bgmSources;
        [SerializeField] private float _crossfadeDuration = 1.0f;

        [Header("SFX Pooling")]
        [SerializeField] private GameObject _soundPlayerPrefab;

        [Header("Preload Sounds")]
        [SerializeField] private List<SoundDataSO> _preloadSounds = new List<SoundDataSO>();

        private Dictionary<string, SoundDataSO> _soundDataDict = new Dictionary<string, SoundDataSO>();
        private int _activeBgmIndex = 0;
        private Coroutine _crossfadeCoroutine;
        private GameObject _fallbackPrefab;

        protected override void Awake()
        {
            base.Awake();

            InitializeBGMSources();
            InitializeFallbackSFXPrefab();
            PreloadRegisteredSounds();
        }

        private void InitializeBGMSources()
        {
            // 인스펙터에 지정되지 않았다면 동적으로 크로스페이드용 AudioSource 2개 자동 생성
            if (_bgmSources == null || _bgmSources.Length < 2)
            {
                _bgmSources = new AudioSource[2];
                for (int i = 0; i < 2; i++)
                {
                    GameObject bgmObj = new GameObject($"BGM_Source_{i}");
                    bgmObj.transform.SetParent(transform);
                    _bgmSources[i] = bgmObj.AddComponent<AudioSource>();
                    _bgmSources[i].playOnAwake = false;
                    _bgmSources[i].loop = true;
                }
            }
        }

        private void InitializeFallbackSFXPrefab()
        {
            if (_soundPlayerPrefab == null)
            {
                // 프리팹이 지정되지 않았다면 런타임에 기본 폴백용 SoundPlayer 프리팹 임시 생성
                _fallbackPrefab = new GameObject("Default_SoundPlayer_Prefab");
                _fallbackPrefab.transform.SetParent(transform);
                _fallbackPrefab.AddComponent<AudioSource>();
                _fallbackPrefab.AddComponent<SoundPlayer>();
                _fallbackPrefab.SetActive(false);
                
                _soundPlayerPrefab = _fallbackPrefab;
            }
        }

        private void PreloadRegisteredSounds()
        {
            foreach (var data in _preloadSounds)
            {
                if (data != null && !_soundDataDict.ContainsKey(data.name))
                {
                    _soundDataDict.Add(data.name, data);
                }
            }
        }

        /// <summary>
        /// 무한 스크롤 테마 변경 시 BGM을 부드럽게 크로스페이드하며 전환
        /// </summary>
        public void ChangeThemeBGM(StageThemeData themeData)
        {
            if (themeData == null)
            {
                Debug.LogWarning("[SoundManager] StageThemeData가 null입니다.");
                return;
            }

            PlayBGM(themeData.themeBgm);
        }

        /// <summary>
        /// BGM 오디오 클립 재생 (크로스페이드 적용)
        /// </summary>
        public void PlayBGM(AudioClip clip)
        {
            if (clip == null)
            {
                StopBGM();
                return;
            }

            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
            }

            int nextBgmIndex = 1 - _activeBgmIndex;
            AudioSource currentSource = _bgmSources[_activeBgmIndex];
            AudioSource nextSource = _bgmSources[nextBgmIndex];

            // 동일한 BGM이 이미 활발히 재생 중인 경우는 무시
            if (currentSource.clip == clip && currentSource.isPlaying)
            {
                return;
            }

            nextSource.clip = clip;
            nextSource.Play();

            _crossfadeCoroutine = StartCoroutine(CoCrossfade(currentSource, nextSource));
            _activeBgmIndex = nextBgmIndex;
        }

        /// <summary>
        /// BGM 재생 중지
        /// </summary>
        public void StopBGM()
        {
            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
                _crossfadeCoroutine = null;
            }

            foreach (var source in _bgmSources)
            {
                if (source != null)
                {
                    source.Stop();
                    source.clip = null;
                }
            }
        }

        private IEnumerator CoCrossfade(AudioSource fadeOutSource, AudioSource fadeInSource)
        {
            float timer = 0f;
            float startFadeOutVol = fadeOutSource.volume;
            float targetFadeInVol = 1.0f; // 옵션 설정 등 볼륨 마스터 값을 확장해 연동 가능

            fadeInSource.volume = 0f;

            while (timer < _crossfadeDuration)
            {
                timer += Time.deltaTime;
                float percent = timer / _crossfadeDuration;

                fadeOutSource.volume = Mathf.Lerp(startFadeOutVol, 0f, percent);
                fadeInSource.volume = Mathf.Lerp(0f, targetFadeInVol, percent);

                yield return null;
            }

            fadeOutSource.Stop();
            fadeOutSource.clip = null;
            fadeInSource.volume = targetFadeInVol;

            // 더 이상 사용되지 않는 배경음 에셋을 정리하기 위해 호출
            Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// 사전 로딩된 이름을 키로 사용하여 효과음 재생 (2D)
        /// </summary>
        public void PlaySFX(string soundName, Vector3? position = null)
        {
            if (_soundDataDict.TryGetValue(soundName, out var data))
            {
                PlaySFX(data, position);
            }
            else
            {
                Debug.LogWarning($"[SoundManager] '{soundName}' 이름으로 사전 등록된 SoundDataSO가 없습니다.");
            }
        }

        /// <summary>
        /// SoundDataSO ScriptableObject 직접 참조를 통한 효과음 재생 (3D 위치값 대응)
        /// </summary>
        public void PlaySFX(SoundDataSO data, Vector3? position = null)
        {
            if (data == null) return;

            if (PoolManager.Instance == null)
            {
                Debug.LogWarning("[SoundManager] PoolManager.Instance가 존재하지 않아 사운드를 재생할 수 없습니다.");
                return;
            }

            // PoolManager에서 빈 사운드 플레이어 오브젝트 대여
            GameObject playerObj = PoolManager.Instance.Pop(_soundPlayerPrefab, Vector3.zero, Quaternion.identity);
            if (playerObj != null)
            {
                SoundPlayer player = playerObj.GetComponent<SoundPlayer>();
                if (player != null)
                {
                    int poolKey = _soundPlayerPrefab.GetInstanceID();
                    player.Play(data, poolKey, position);
                }
                else
                {
                    Debug.LogError("[SoundManager] 팝업된 오브젝트에 SoundPlayer 컴포넌트가 존재하지 않습니다.");
                }
            }

            // SoundDataSO의 enableEcho 옵션에 따른 EchoManager 자동 연동
            TriggerEchoFromSoundData(data, position);
        }

        private void TriggerEchoFromSoundData(SoundDataSO data, Vector3? position)
        {
            if (data == null || !data.enableEcho || !position.HasValue) return;
            if (EchoManager.Instance == null) return;

            float intensity;
            float speed;
            float fadeSpeed;

            if (data.useAutoEchoParams)
            {
                // 최소 범위 8m 보장 + Volume 기반 최대 20m까지 확장
                intensity = (data.volume * 12f) + 8f;
                speed = (data.pitch * 10f) + 10f;
                fadeSpeed = 1.2f;
            }
            else
            {
                intensity = data.customIntensity;
                speed = data.customSpeed;
                fadeSpeed = data.customFadeSpeed;
            }

            EchoManager.Instance.TriggerSound(position.Value, intensity, speed, fadeSpeed);
        }
    }
}
