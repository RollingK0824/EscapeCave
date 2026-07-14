using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    private MoveDirection direction;
    private float moveRange;
    private float speed;
    
    private Vector3 startPos;
    private float progress = 0f;
    private bool movingForward = true;

    public void Initialize(MoveDirection dir, float range, float spd)
    {
        direction = dir;
        moveRange = range;
        speed = spd > 0 ? spd : 2f;
        startPos = transform.position;
    }

    private void FixedUpdate()
    {
        if (moveRange <= 0) return;

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

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.MovePosition(newPos);
        }
        else
        {
            transform.position = newPos;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
        }
    }
}
