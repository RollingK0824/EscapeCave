using UnityEngine;

public class BossCameraFollow : MonoBehaviour
{
    private Transform followCamera;
    private float halfHeight;

    private void Start()
    {
        followCamera = Camera.main.transform;
        Vector3 size = GetComponent<SpriteRenderer>().bounds.size;
        halfHeight = size.y * 0.25f;
    }
    private void FixedUpdate()
    {
        float cameraY = followCamera.position.y + halfHeight;
        transform.position = new Vector2(transform.position.x,cameraY);
    }
}
