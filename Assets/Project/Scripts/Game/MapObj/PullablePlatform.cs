using UnityEngine;

public class PullablePlatform : MonoBehaviour
{
    private float pullLimit;
    private Vector3 startPos;

    public void Initialize(float limit)
    {
        pullLimit = limit;
        startPos = transform.position;

        var rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0;
        rb.linearDamping = 5f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionY;
    }

    private void FixedUpdate()
    {
        if (Mathf.Abs(transform.position.x - startPos.x) > pullLimit)
        {
            Vector3 pos = transform.position;
            pos.x = startPos.x + Mathf.Sign(pos.x - startPos.x) * pullLimit;
            transform.position = pos;
        }
    }
}
