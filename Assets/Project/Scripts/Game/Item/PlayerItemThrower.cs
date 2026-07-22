using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 마우스 조준으로 소리폭탄 등 투사체형 아이템을 포물선으로 던지는 컴포넌트.
/// Player GameObject에 부착합니다. InventoryManager가 SonicBomb 아이템 사용 시 BeginAim을 호출합니다.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class PlayerItemThrower : MonoBehaviour
{
    [Header("조준 기준점 (비우면 자신의 Transform 사용)")]
    [SerializeField] private Transform _throwOrigin;

    [Header("포물선 미리보기")]
    [SerializeField] private int _trajectoryPointCount = 60;
    [SerializeField] private float _trajectoryTimeStep = 0.08f;
    [SerializeField] private float _dashesPerUnit = 3f;
    [SerializeField] private LayerMask _groundLayer;

    private LineRenderer _trajectoryLine;
    private Texture2D _dashTexture;
    private ItemData _pendingItem;
    private System.Action _onThrown;
    private float _projectileGravityScale = 1f;

    public bool IsAiming { get; private set; }

    private void Awake()
    {
        _trajectoryLine = GetComponent<LineRenderer>();
        if (_throwOrigin == null)
        {
            _throwOrigin = transform;
        }
        _trajectoryLine.enabled = false;
        _trajectoryLine.textureMode = LineTextureMode.Tile;

        _dashTexture = new Texture2D(2, 1, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };
        _dashTexture.SetPixels(new[] { Color.white, new Color(1f, 1f, 1f, 0f) });
        _dashTexture.Apply();

        Material dashMaterial = new Material(Shader.Find("Sprites/Default"));
        dashMaterial.mainTexture = _dashTexture;
        _trajectoryLine.material = dashMaterial;
    }

    /// <summary>
    /// 조준 모드를 시작합니다. 실제로 던져지면 onThrown 콜백으로 소비 처리를 위임합니다.
    /// </summary>
    public void BeginAim(ItemData item, System.Action onThrown)
    {
        if (item == null || item.projectilePrefab == null)
        {
            Debug.LogWarning("PlayerItemThrower: item 또는 projectilePrefab이 비어있어 조준을 시작할 수 없습니다.");
            return;
        }

        _pendingItem = item;
        _onThrown = onThrown;
        IsAiming = true;

        Rigidbody2D prefabRb = item.projectilePrefab.GetComponent<Rigidbody2D>();
        _projectileGravityScale = prefabRb != null ? prefabRb.gravityScale : 1f;

        _trajectoryLine.enabled = true;
    }

    public void CancelAim()
    {
        IsAiming = false;
        _pendingItem = null;
        _onThrown = null;
        _trajectoryLine.enabled = false;
    }

    private void Update()
    {
        if (!IsAiming) return;

        if (Mouse.current == null || Camera.main == null)
        {
            CancelAim();
            return;
        }

        Vector2 velocity = ComputeThrowVelocity();
        DrawTrajectory(velocity);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Throw(velocity);
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelAim();
        }
    }

    private Vector2 ComputeThrowVelocity()
    {
        Vector2 origin = _throwOrigin.position;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(
            new Vector3(mouseScreen.x, mouseScreen.y, -Camera.main.transform.position.z));

        Vector2 direction = (Vector2)mouseWorld - origin;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.right;
        }

        return direction.normalized * _pendingItem.throwPower;
    }

    private void DrawTrajectory(Vector2 velocity)
    {
        Vector2 origin = _throwOrigin.position;
        Vector2 gravity = Physics2D.gravity * _projectileGravityScale;

        _trajectoryLine.positionCount = _trajectoryPointCount;
        _trajectoryLine.SetPosition(0, origin);
        Vector2 previousPoint = origin;
        float totalLength = 0f;
        int pointCount = 1;

        for (int i = 1; i < _trajectoryPointCount; i++)
        {
            float t = i * _trajectoryTimeStep;
            Vector2 point = origin + velocity * t + 0.5f * gravity * t * t;

            RaycastHit2D hit = Physics2D.Linecast(previousPoint, point, _groundLayer);
            if (hit.collider != null)
            {
                point = hit.point;
            }

            _trajectoryLine.SetPosition(pointCount, point);
            totalLength += Vector2.Distance(previousPoint, point);
            pointCount++;
            previousPoint = point;

            if (hit.collider != null)
            {
                break;
            }
        }

        _trajectoryLine.positionCount = pointCount;
        _trajectoryLine.material.mainTextureScale = new Vector2(totalLength * _dashesPerUnit, 1f);
    }

    private void Throw(Vector2 velocity)
    {
        ItemData item = _pendingItem;
        System.Action onThrown = _onThrown;

        GameObject projectileObj = Managers.PoolManager.Instance != null
            ? Managers.PoolManager.Instance.Pop(item.projectilePrefab, _throwOrigin.position, Quaternion.identity)
            : Instantiate(item.projectilePrefab, _throwOrigin.position, Quaternion.identity);

        SonicBombProjectile projectile = projectileObj.GetComponent<SonicBombProjectile>();
        if (projectile != null)
        {
            projectile.Launch(item.projectilePrefab, velocity, item.fuseTime, item.soundIntensity, item.soundSpeed, item.lureDuration, item.pingInterval, item.impactDelay, item.spinSpeed);
        }

        CancelAim();
        onThrown?.Invoke();
    }
}
