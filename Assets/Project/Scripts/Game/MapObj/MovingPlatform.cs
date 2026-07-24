using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    private MoveDirection direction;
    private float moveRange;
    private float speed;

    private Vector3 startPos;
    private float progress = 0f;
    private bool movingForward = true;

    [Header("Detection Settings")]
    [Tooltip("발판 위/아래 감지 여유 공간 (Y축 확장 크기)")]
    [SerializeField] private float detectionMarginY = 0.6f;

    [Tooltip("발판을 따라 움직일 대상 레이어 (비트 플래그 다중 선택)")]
    [SerializeField] private LayerMask riderLayerMask;

    [Tooltip("isTrigger여도 발판 탑승을 허용할 레이어 (예: Item)")]
    [SerializeField] private LayerMask allowTriggerRiderMask;

    private BoxCollider2D platformCollider;

    private void Awake()
    {
        if (riderLayerMask == 0)
        {
            riderLayerMask = LayerMask.GetMask("Player", "Monster", "Item", "MapObject");
        }
        if (allowTriggerRiderMask == 0)
        {
            allowTriggerRiderMask = LayerMask.GetMask("Item");
        }
    }

    public void Initialize(MoveDirection dir, float range, float spd)
    {
        direction = dir;
        moveRange = range;
        speed = spd > 0 ? spd : 2f;
        startPos = transform.position;

        platformCollider = GetComponentInChildren<BoxCollider2D>();
    }

    private void FixedUpdate()
    {
        if (moveRange <= 0) return;

        Vector3 prevPos = transform.position;
        
        if (movingForward)
        {
            progress += speed * Time.fixedDeltaTime;
            if (progress >= moveRange)
            {
                progress = moveRange;
                movingForward = false;
            }
        }
        else
        {
            progress -= speed * Time.fixedDeltaTime;
            if (progress <= 0)
            {
                progress = 0;
                movingForward = true;
            }
        }

        Vector3 newPos = startPos;
        if (direction == MoveDirection.Horizontal)
        {
            newPos.x += progress;
        }
        else
        {
            newPos.y += progress;
        }

        transform.position = newPos;
        Vector3 delta = newPos - prevPos;

        if (delta != Vector3.zero)
        {
            MovePassengers(delta);
        }
    }

    private void MovePassengers(Vector3 delta)
    {
        Vector2 boxCenter = transform.position;
        Vector2 boxSize = Vector2.one;
        if (platformCollider != null)
        {
            boxCenter = platformCollider.bounds.center;
            boxSize = new Vector2(platformCollider.bounds.size.x, platformCollider.bounds.size.y + detectionMarginY);
        }

        Collider2D[] passengers = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, riderLayerMask);
        for (int i = 0; i < passengers.Length; i++)
        {
            Collider2D col = passengers[i];

            if (col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;

            // isTrigger 객체 중 allowTriggerRiderMask(예: Item)에 해당하지 않는 트리거만 제외
            if (col.isTrigger && ((1 << col.gameObject.layer) & allowTriggerRiderMask) == 0) continue;

            Rigidbody2D passengerRb = col.attachedRigidbody;

            if (passengerRb != null && passengerRb.bodyType != RigidbodyType2D.Static)
            {
                passengerRb.position += (Vector2)delta;
            }
            else
            {
                col.transform.position += delta;
            }
        }
    }
}
