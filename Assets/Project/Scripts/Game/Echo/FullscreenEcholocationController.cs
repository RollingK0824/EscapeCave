using UnityEngine;
using UnityEngine.InputSystem;

public class FullscreenEcholocationController : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera; // 메인 카메라 할당 필수

    [Header("Echolocation Settings")]
    public float expandSpeed = 15f;    // 월드 좌표 기준 파동 속도
    public float maxRadius = 30f;      // 월드 좌표 기준 최대 반경

    [Header("Visual Settings")]
    public float waveWidth = 3f;       // 월드 좌표 기준 파동 두께
    public Color baseDarkness = Color.black;
    [ColorUsage(true, true)] public Color waveHighlight = Color.cyan;

    private float currentWorldRadius = 0f;
    private bool isPinging = false;
    private Vector3 currentPingWorldPos;

    // 셰이더 ID 캐싱
    private int pingCenterUVID = Shader.PropertyToID("_PingCenterUV");
    private int pingRadiusUVID = Shader.PropertyToID("_PingRadiusUV");
    private int pingWidthUVID = Shader.PropertyToID("_PingWidthUV");
    private int baseColorID = Shader.PropertyToID("_BaseColor");
    private int waveColorID = Shader.PropertyToID("_WaveColor");

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // 초기 암전 상태 색상 설정
        Shader.SetGlobalColor(baseColorID, baseDarkness);
        Shader.SetGlobalColor(waveColorID, waveHighlight);
        Shader.SetGlobalFloat(pingRadiusUVID, 0f);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            // 예시: 카메라의 현재 중심점을 파동 시작점으로 테스트
            TriggerPing(mainCamera.transform.position);
        }

        if (isPinging)
        {
            currentWorldRadius += expandSpeed * Time.deltaTime;

            UpdateShaderParameters();

            if (currentWorldRadius > maxRadius)
            {
                isPinging = false;
                currentWorldRadius = 0f;
                Shader.SetGlobalFloat(pingRadiusUVID, 0f); // 완전히 닫힘 (암전)
            }
        }
    }

    public void TriggerPing(Vector3 worldPosition)
    {
        currentPingWorldPos = worldPosition;
        currentWorldRadius = 0f;
        isPinging = true;
    }

    private void UpdateShaderParameters()
    {
        // 1. 월드 좌표를 화면 UV 좌표 (0.0 ~ 1.0)로 변환
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(currentPingWorldPos);

        // 2. 화면 비율 (가로 / 세로)
        float aspectRatio = (float)Screen.width / Screen.height;

        // X, Y는 위치, Z는 화면 비율을 셰이더로 전달
        Vector4 centerUV = new Vector4(viewportPos.x, viewportPos.y, aspectRatio, 0);
        Shader.SetGlobalVector(pingCenterUVID, centerUV);

        // 3. 월드 반경을 UV 반경으로 변환 (2D 직교 카메라 기준)
        // OrthographicSize는 세로 절반의 크기를 나타냄 (월드 단위)
        float cameraWorldHeight = mainCamera.orthographicSize * 2f;

        // UV 좌표계에서 높이는 1.0이므로, 비율을 계산
        float radiusUV = currentWorldRadius / cameraWorldHeight;
        float widthUV = waveWidth / cameraWorldHeight;

        Shader.SetGlobalFloat(pingRadiusUVID, radiusUV);
        Shader.SetGlobalFloat(pingWidthUVID, widthUV);
    }
}