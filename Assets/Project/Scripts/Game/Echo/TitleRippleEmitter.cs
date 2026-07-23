using System.Collections;
using Managers;
using UnityEngine;

namespace Game.Echo
{
    /// <summary>
    /// 물방울 충돌 지점에서 여러 개의 동심원 에코 파동 링(TitleEchoWaveRing)을
    /// 시간차를 두고 연달아 생성하여 화면 전체로 잔물결이 퍼져나가는 연출을 관리하는 방출기.
    /// </summary>
    public class TitleRippleEmitter : MonoBehaviour
    {
        public static TitleRippleEmitter ActiveEmitter { get; private set; }

        [Header("파동 링 프리팹 (EchoWaveMask 레이어 지정)")]
        [SerializeField] private GameObject _waveRingPrefab;

        [Header("잔물결 퍼짐 설정")]
        [SerializeField, Tooltip("한 번 낙하 시 발생할 동심원 링의 개수")]
        private int _ringCount = 4;

        [SerializeField, Tooltip("동심원 링 간 생성 시간차 (초 단위)")]
        private float _ringInterval = 0.2f;

        [SerializeField, Tooltip("파동의 최대 확장 반지름")]
        private float _maxRadius = 14.0f;

        [SerializeField, Tooltip("파동 팽창 속도")]
        private float _expandSpeed = 6.0f;

        [SerializeField, Tooltip("각 파동 링의 존속 시간 (Fade Out 완료 시간)")]
        private float _ringDuration = 2.5f;

        [Header("레이어 설정")]
        [SerializeField] private string _echoWaveMaskLayerName = "EchoWaveMask";

        private int _echoWaveMaskLayer;

        private void OnEnable()
        {
            ActiveEmitter = this;
        }

        private void OnDisable()
        {
            if (ActiveEmitter == this)
            {
                ActiveEmitter = null;
            }
        }

        private void Awake()
        {
            _echoWaveMaskLayer = LayerMask.NameToLayer(_echoWaveMaskLayerName);
            if (_echoWaveMaskLayer == -1)
            {
                _echoWaveMaskLayer = LayerMask.NameToLayer("Default");
            }
        }

        /// <summary>
        /// 지정 위치에서 잔물결 에코 파동 시리즈 발생 (외부 호출용)
        /// </summary>
        public void EmitRipple(Vector3 worldPosition)
        {
            if (_waveRingPrefab == null)
            {
                Debug.LogWarning($"[{nameof(TitleRippleEmitter)}] Wave Ring Prefab이 할당되지 않았습니다.", this);
                return;
            }

            StartCoroutine(CoEmitRipplesRoutine(worldPosition));
        }

        private IEnumerator CoEmitRipplesRoutine(Vector3 originPos)
        {
            for (int i = 0; i < _ringCount; i++)
            {
                GameObject ringObj = null;

                if (PoolManager.Instance != null)
                {
                    ringObj = PoolManager.Instance.Pop(_waveRingPrefab, originPos, Quaternion.identity);
                }
                else
                {
                    ringObj = Instantiate(_waveRingPrefab, originPos, Quaternion.identity);
                }

                if (ringObj != null)
                {
                    // EchoWaveMask 레이어 적용
                    SetLayerRecursive(ringObj, _echoWaveMaskLayer);

                    if (ringObj.TryGetComponent<TitleEchoWaveRing>(out var ringComp))
                    {
                        // 링 순서에 따라 약간의 반지름 / 지속시간 미세 변주
                        float currentMaxRadius = _maxRadius * (1.0f - i * 0.08f);
                        ringComp.SetupRing(currentMaxRadius, _expandSpeed, _ringDuration, _waveRingPrefab);
                    }
                }

                yield return new WaitForSeconds(_ringInterval);
            }
        }

        private void SetLayerRecursive(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }
    }
}
