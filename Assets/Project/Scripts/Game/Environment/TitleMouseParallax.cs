using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TitleMouseParallax : MonoBehaviour
{
    [System.Serializable]
    public class LayerData
    {
        [Tooltip("패럴랙스 이동을 적용할 레이어/오브젝트 Transform")]
        public Transform layerTransform;

        [Tooltip("마우스 이동 시 적용할 패럴랙스 이동 강도 (X, Y 월드 좌표 기준)")]
        public Vector2 moveIntensity = new Vector2(0.8f, 0.5f);

        [Tooltip("마우스 이동 방향과 반대로 움직일지 여부 (원경은 반대, 근경은 정방향 추천)")]
        public bool invertDirection = false;

        [Header("스케일 설정")]
        [Tooltip("카메라 뷰포트에 맞춘 자동 스케일 조정 적용 여부 (배경=true, 개구리/캐릭터/특수오브젝트=false)")]
        public bool autoScaleToCamera = true;

        [Tooltip("수동 지정 스케일 사용 여부 (체크 시 아래 Custom Scale 값 적용, 해제 시 에디터 Transform 스케일 유지)")]
        public bool useCustomScale = false;

        [Tooltip("useCustomScale이 체크되어 있을 때 적용할 스케일 Vector3")]
        public Vector3 customScale = Vector3.one;
    }

    [Header("카메라 설정")]
    [SerializeField] private Camera _targetCamera;
    [SerializeField] private bool _autoScaleToCamera = true;
    [SerializeField, Tooltip("마우스 이동 시 배경 테두리가 잘리지 않도록 여유 스케일 비율 (1.15 = 15% 여유)")]
    private float _extraScaleMargin = 1.15f;

    [Header("패럴랙스 모션 설정")]
    [SerializeField, Tooltip("마우스 이동 보정 속도 (Smooth Time, 낮을수록 반응이 빠름)")]
    private float _smoothTime = 0.15f;

    [Header("레이어 설정 (비어있을 경우 자식 오브젝트 자동 등록)")]
    [SerializeField] private List<LayerData> _layers = new List<LayerData>();

    private Vector2 _normalizedMousePos;
    private Dictionary<Transform, Vector3> _initialPositions = new Dictionary<Transform, Vector3>();

    private void Awake()
    {
        if (_targetCamera == null)
        {
            _targetCamera = Camera.main;
        }

        if (_layers == null || _layers.Count == 0)
        {
            AutoSetupChildLayers();
        }

        CacheInitialPositions();
    }

    private void Start()
    {
        if (_autoScaleToCamera && _targetCamera != null)
        {
            ScaleLayersToFitCamera();
        }
    }

    private void Update()
    {
        UpdateNormalizedMousePosition();
        ApplyParallaxMovement();
    }

    /// <summary>
    /// 화면 중앙 기준 마우스 위치 (-1 ~ 1 범위) 계산
    /// </summary>
    private void UpdateNormalizedMousePosition()
    {
        Vector3 rawMousePos;

        if (Mouse.current != null)
        {
            rawMousePos = Mouse.current.position.ReadValue();
        }
        else
        {
            rawMousePos = Input.mousePosition;
        }

        float halfWidth = Screen.width * 0.5f;
        float halfHeight = Screen.height * 0.5f;

        if (halfWidth > 0f && halfHeight > 0f)
        {
            float normX = Mathf.Clamp((rawMousePos.x - halfWidth) / halfWidth, -1f, 1f);
            float normY = Mathf.Clamp((rawMousePos.y - halfHeight) / halfHeight, -1f, 1f);
            _normalizedMousePos = new Vector2(normX, normY);
        }
    }

    /// <summary>
    /// 마우스 위치에 따른 각 레이어 패럴랙스 이동 적용
    /// </summary>
    private void ApplyParallaxMovement()
    {
        foreach (var layer in _layers)
        {
            if (layer == null || layer.layerTransform == null) continue;

            if (!_initialPositions.TryGetValue(layer.layerTransform, out Vector3 initialPos))
            {
                initialPos = layer.layerTransform.localPosition;
            }

            float dirMultiplier = layer.invertDirection ? -1f : 1f;

            Vector3 targetOffset = new Vector3(
                _normalizedMousePos.x * layer.moveIntensity.x * dirMultiplier,
                _normalizedMousePos.y * layer.moveIntensity.y * dirMultiplier,
                0f
            );

            Vector3 targetPosition = initialPos + targetOffset;

            layer.layerTransform.localPosition = Vector3.Lerp(
                layer.layerTransform.localPosition,
                targetPosition,
                Time.deltaTime / Mathf.Max(0.01f, _smoothTime)
            );
        }
    }

    /// <summary>
    /// 자식 Transform을 순회하여 자동 레이어 등록
    /// </summary>
    private void AutoSetupChildLayers()
    {
        _layers = new List<LayerData>();
        int childCount = transform.childCount;

        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            string childName = child.name.ToLower();

            float depthFactor = (float)(i + 1) / Mathf.Max(1, childCount);


            LayerData data = new LayerData
            {
                layerTransform = child,
                moveIntensity = new Vector2(0.3f * depthFactor, 0.2f * depthFactor),
                invertDirection = false,
                autoScaleToCamera = true,
                useCustomScale = false,
                customScale = child.localScale
            };

            _layers.Add(data);
        }
    }

    /// <summary>
    /// 레이어들의 초기 Local Position 캐싱
    /// </summary>
    private void CacheInitialPositions()
    {
        _initialPositions.Clear();
        foreach (var layer in _layers)
        {
            if (layer != null && layer.layerTransform != null && !_initialPositions.ContainsKey(layer.layerTransform))
            {
                _initialPositions.Add(layer.layerTransform, layer.layerTransform.localPosition);
            }
        }
    }

    /// <summary>
    /// 레이어별 설정에 따른 스케일 지정/자동 조정
    /// </summary>
    private void ScaleLayersToFitCamera()
    {
        if (_targetCamera == null || !_targetCamera.orthographic) return;

        float worldScreenHeight = _targetCamera.orthographicSize * 2.0f;
        float worldScreenWidth = worldScreenHeight * _targetCamera.aspect;

        foreach (var layer in _layers)
        {
            if (layer == null || layer.layerTransform == null) continue;

            // 1. 수동 Custom Scale 사용 시
            if (layer.useCustomScale)
            {
                layer.layerTransform.localScale = layer.customScale;
                continue;
            }

            // 2. autoScaleToCamera가 false이면 사용자가 에디터에서 설정한 기존 스케일 그대로 유지
            if (!layer.autoScaleToCamera)
            {
                continue;
            }

            // 3. autoScaleToCamera가 true인 배경 레이어에 한해서만 카메라 자동 핏팅 계산
            SpriteRenderer sr = layer.layerTransform.GetComponentInChildren<SpriteRenderer>();
            if (sr == null || sr.sprite == null) continue;

            Vector2 spriteSize = sr.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f) continue;

            float scaleY = (worldScreenHeight / spriteSize.y) * _extraScaleMargin;
            float scaleX = (worldScreenWidth / spriteSize.x) * _extraScaleMargin;

            float finalScale = Mathf.Max(scaleX, scaleY);

            layer.layerTransform.localScale = new Vector3(finalScale, finalScale, 1f);
        }
    }

    private void Reset()
    {
        _targetCamera = Camera.main;
        AutoSetupChildLayers();
    }
}