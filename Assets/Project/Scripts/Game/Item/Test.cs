using UnityEngine;
using Managers;

public class Test : MonoBehaviour, IGrabbable
{
    [SerializeField] private ItemData itemData;

    public Transform GrabTransform => transform;

    public void OnGrabbed()
    {
        Debug.Log("잡힘");
    }

    public void OnCollected()
    {
        if (itemData == null)
        {
            Debug.LogWarning($"{name}: itemData가 인스펙터에 할당되지 않았습니다.");
            return;
        }

        if (InventoryManager.Instance.AddItem(itemData))
        {
            Destroy(gameObject);
        }
    }
}
