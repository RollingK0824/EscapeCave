using UnityEngine;

namespace Managers
{
    public class EchoManager : SingletonBase<EchoManager>
    {
        [Header("핵심 렌더링 에셋")]
        [SerializeField] private GameObject _wavePrefab;
        [SerializeField] private Material _postProMaterial;
        [SerializeField] private RenderTexture _echoWaveRT;

        [Header("레이어 환경 설정")]
        [SerializeField] private string _echoWaveMaskLayerName = "EchoWaveMask";

        private int _echoWaveMaskLayer;

        protected override void Awake()
        {
            base.Awake();

            _echoWaveMaskLayer = LayerMask.NameToLayer(_echoWaveMaskLayerName);
            if(_echoWaveMaskLayer == -1)
            {
                Debug.LogError($"[EchoManager] '{_echoWaveMaskLayerName}' 레이어가 프로젝트 세팅에 존재하지 않습니다.");
            }
        }

        private void Start()
        {
            if(_postProMaterial != null && _echoWaveRT != null)
            {
                _postProMaterial.SetTexture("_MaskTex", _echoWaveRT);
            }
            else
            {
                Debug.LogError("[EchoManager] 필수 에셋(Material 또는 Render Texture)이 인스펙터에 할당되지 않았습니다.");
            }
        }
        
        public void TriggerSound(Vector3 worldPosition, float intensity, float speed)
        {
            if (_wavePrefab == null || PoolManager.Instance == null) return;

            GameObject waveObj = PoolManager.Instance.Pop(_wavePrefab,worldPosition,Quaternion.identity);
            SetLayerRecursive(waveObj, _echoWaveMaskLayer);
            waveObj.layer = _echoWaveMaskLayer;

            EchoWaveObject waveComp = waveObj.GetComponent<EchoWaveObject>();
            if(waveComp != null)
            {
                waveComp.SetupWave(intensity, speed, _wavePrefab);
            }
        }
        
        private void SetLayerRecursive(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach(Transform child in obj.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }
    }
}
