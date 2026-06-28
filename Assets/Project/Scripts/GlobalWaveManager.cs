using UnityEngine;
using UnityEngine.InputSystem;

public class GlobalWaveManager : MonoBehaviour
{
    [Header("Wave Global Setting")]
    public float waveSpeed = 8.0f;
    public float maxRadius = 15.0f;

    private const int MAX_WAVE_COUNT = 5;

    private int waveCentersID;
    private int waveRadiiID;

    private Vector4[] waveCenters = new Vector4[MAX_WAVE_COUNT];
    private float[] waveRadii = new float[MAX_WAVE_COUNT];
    private int currentSlotIndex = 0;

    private void Awake()
    {
        waveCentersID = Shader.PropertyToID("_WaveCenters");
        waveRadiiID = Shader.PropertyToID("_WaveRadii");
    }

    private void Start()
    {
        for (int i = 0; i < MAX_WAVE_COUNT;++i)
        {
            waveRadii[i] = -1.0f;
            waveCenters[i] = Vector4.zero;
        }

        UpdateGlobalShaderArrays();
    }

    void Update()
    {
        // 1. 마우스 클릭 시 새로운 파동 발생 (이벤트)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TriggerGlobalWave();
        }

        // 2. 매 프레임 파동 반지름 업데이트
        bool needUpdate = false;
        for (int i = 0; i < MAX_WAVE_COUNT; i++)
        {
            if (waveRadii[i] >= 0.0f)
            {
                waveRadii[i] += waveSpeed * Time.deltaTime;
                needUpdate = true;

                if (waveRadii[i] >= maxRadius)
                {
                    waveRadii[i] = -1.0f; // 최대 반경 도달 시 끔
                }
            }
        }

        // 3. 변경 사항이 있다면 글로벌 전송
        if (needUpdate)
        {
            UpdateGlobalShaderArrays();
        }
    }

    private void TriggerGlobalWave()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0f));
        Vector3 targetPos2D = new Vector3(mouseWorldPos.x, mouseWorldPos.y, 0f);

        // 순환 버퍼 구조로 데이터 덮어쓰기
        waveCenters[currentSlotIndex] = new Vector4(targetPos2D.x, targetPos2D.y, 0f, 1.0f);
        waveRadii[currentSlotIndex] = 0.0f;

        currentSlotIndex = (currentSlotIndex + 1) % MAX_WAVE_COUNT;

        UpdateGlobalShaderArrays();
    }

    private void UpdateGlobalShaderArrays()
    {
        // ★ 핵심 변경점: 머티리얼을 지정하지 않고 'Shader' 클래스를 통해 전역(Global) 공간으로 배열을 밀어 넣습니다.
        // 이 순간 GPU 내 메모리 맵에 데이터가 할당되며, 이를 구독하는 모든 후처리 쉐이더가 값을 읽어갑니다.
        Shader.SetGlobalVectorArray(waveCentersID, waveCenters);
        Shader.SetGlobalFloatArray(waveRadiiID, waveRadii);
    }
}

