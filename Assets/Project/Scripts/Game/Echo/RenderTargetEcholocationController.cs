using UnityEngine;
using UnityEngine.InputSystem;

public class RenderTargetEcholocationController : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public GameObject pingPrefab;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(mainCamera.transform.position.z)));
            worldPos.z = 0f;

            Instantiate(pingPrefab, worldPos, Quaternion.identity);
        }
    }
}