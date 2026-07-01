using Managers;
using UnityEngine;
using UnityEngine.InputSystem;

public class RenderTargetEcholocationController : MonoBehaviour
{
    public float intensity;
    public float speed;
    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(Camera.main.transform.position.z)));
            worldPos.z = 0f;

            EchoManager.Instance.TriggerSound(worldPos, intensity, speed);
        }
    }
}