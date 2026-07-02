using UnityEngine;
using UnityEngine.Tilemaps;

public class PlatformerMapGenerator : MonoBehaviour
{
    [Header("타일맵 설정")]
    public Tilemap tilemap;

    [Header("플랫폼 타일 에셋")]
    [Tooltip("에디터에서 세팅한 Rule Tile 에셋(.asset)을 여기에 넣으세요.")]
    public TileBase groundRuleTile;

    [Header("특수 발판 프리팹")]
    public GameObject movingPlatformPrefab; // 상하좌우로 움직이는 기믹 발판

    [Header("스테이지 및 플랫폼 기본 설정")]
    public int width = 300;                 // 맵 가로 총 길이
    public int height = 18;                 // 맵 세로 통로 높이
    public int minPlatformLength = 1;       // 플랫폼 최소 길이 (칸)
    public int maxPlatformLength = 4;       // 플랫폼 최대 길이 (칸)

    [Header("펄린 노이즈 (시작 지점 기준)")]
    public float startNoiseScale = 0.12f;
    [Range(0f, 1f)] public float startThreshold = 0.4f;
    public float caveWallStrength = 1.5f;

    [Header("점진적 난이도 상승 곡선 (끝 지점 기준)")]
    public bool enableDifficultyCurve = true;
    public float endNoiseScale = 0.18f;
    [Range(0f, 1f)] public float endThreshold = 0.55f;
    [Range(0f, 1f)] public float maxMovingPlatformChance = 0.3f;

    private float _seedX, _seedY;

    void Start()
    {
        GenerateMap();
    }

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        if (tilemap == null || groundRuleTile == null)
        {
            Debug.LogWarning("Tilemap이나 Rule Tile이 할당되지 않았습니다.");
            return;
        }

        // 1. 초기화 
        _seedX = Random.Range(0f, 10000f);
        _seedY = Random.Range(0f, 10000f);
        tilemap.ClearAllTiles();
        ClearChildPlatforms();

        // 2. 가로(X축) 방향으로 맵 생성 진행
        for (int x = 0; x < width; x++)
        {
            float progress = (float)x / width; // 0.0 ~ 1.0

            float currentScale = enableDifficultyCurve ? Mathf.Lerp(startNoiseScale, endNoiseScale, progress) : startNoiseScale;
            float currentThreshold = enableDifficultyCurve ? Mathf.Lerp(startThreshold, endThreshold, progress) : startThreshold;

            for (int y = 0; y < height; y++)
            {
                float pX = _seedX + (x * currentScale);
                float pY = _seedY + (y * currentScale);
                float rawNoise = Mathf.PerlinNoise(pX, pY);

                float halfHeight = height / 2f;
                float distFromCenter = Mathf.Abs(y - halfHeight) / halfHeight;
                float finalNoise = rawNoise + (distFromCenter * caveWallStrength);

                if (finalNoise > currentThreshold)
                {
                    bool isInnerSpace = distFromCenter < 0.65f;
                    float movingChance = progress * maxMovingPlatformChance;

                    // 허공이고, 확률을 뚫었다면 -> 움직이는 프리팹 발판 스폰
                    if (enableDifficultyCurve && isInnerSpace && movingPlatformPrefab != null && Random.value < movingChance)
                    {
                        if (Random.value < 0.15f)
                        {
                            Vector3 pos = new Vector3(x + 0.5f, y + 0.5f, 0f);
                            Instantiate(movingPlatformPrefab, pos, Quaternion.identity, transform);
                        }
                    }
                    else
                    {
                        // ★ Rule Tile을 활용한 가로 발판 생성 로직 ★
                        int platformLength = Random.Range(minPlatformLength, maxPlatformLength + 1);

                        if (x + platformLength > width)
                        {
                            platformLength = width - x;
                        }

                        // 복잡한 좌/중/우 판별 없이, 그냥 정해진 길이만큼 Rule Tile을 연속해서 칠해주면 
                        // Rule Tile이 알아서 좌, 중, 우 이미지를 렌더링합니다.
                        for (int i = 0; i < platformLength; i++)
                        {
                            tilemap.SetTile(new Vector3Int(x + i, y, 0), groundRuleTile);
                        }

                        x += (platformLength - 1);
                        break;
                    }
                }
            }
        }

        // 3. 시작점(스폰)과 끝점(클리어)의 길 강제 뚫기
        EnsurePath(0);
        EnsurePath(width - 6);

        // 타일맵 물리 콜라이더 강제 갱신
        tilemap.RefreshAllTiles();
    }

    private void ClearChildPlatforms()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (child != tilemap.gameObject)
            {
                DestroyImmediate(child);
            }
        }
    }

    private void EnsurePath(int startX)
    {
        int midY = height / 2;
        for (int x = startX; x < startX + 6; x++)
        {
            for (int y = midY - 2; y <= midY + 2; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), null);
            }
        }
    }
}