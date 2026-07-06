using UnityEngine;
using Managers;

public class Test : MonoBehaviour, IGrabbable
{
    private SpriteRenderer sr;

    public Transform GrabTransform => transform;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void OnGrabbed()
    {
        Debug.Log("잡힘");
    }

    public void OnCollected()
    {
        Debug.Log(sr.sprite.name);

        if (InventoryManager.Instance.AddItem(sr.sprite))
        {
            Destroy(gameObject);
        }
    }
}