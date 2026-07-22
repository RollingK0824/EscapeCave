using UnityEngine;

[RequireComponent(typeof(Camera))]
public class WaterReflectionCamera : MonoBehaviour
{
    private static readonly int SurfaceWorldYId = Shader.PropertyToID("_SurfaceWorldY");

    [SerializeField] private Camera _sourceCamera;
    [SerializeField] private Transform _waterSurface;
    [SerializeField] private Material _waterMaterial;

    private Camera _mirrorCamera;

    private void Awake()
    {
        _mirrorCamera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (_sourceCamera == null)
        {
            _sourceCamera = Camera.main;
        }

        if (_sourceCamera == null)
        {
            return;
        }

        // 미러 카메라는 메인 카메라와 완전히 같은 시점을 가져야 반사 대상(캐릭터 등)을 실제로 담을 수 있다.
        // 물 표면 기준 대칭 계산은 WaterSurface 쉐이더의 프래그먼트 단계에서 처리한다.
        transform.SetPositionAndRotation(_sourceCamera.transform.position, _sourceCamera.transform.rotation);

        _mirrorCamera.orthographic = _sourceCamera.orthographic;
        _mirrorCamera.orthographicSize = _sourceCamera.orthographicSize;
        _mirrorCamera.aspect = _sourceCamera.aspect;
        _mirrorCamera.nearClipPlane = _sourceCamera.nearClipPlane;
        _mirrorCamera.farClipPlane = _sourceCamera.farClipPlane;

        if (_waterSurface != null && _waterMaterial != null)
        {
            _waterMaterial.SetFloat(SurfaceWorldYId, _waterSurface.position.y);
        }
    }
}
