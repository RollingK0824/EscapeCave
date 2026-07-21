using UnityEngine;
using Managers;

public class CoinPickup : MonoBehaviour, IGrabbable
{
    [SerializeField] private int _goldAmount = 1;

    public Transform GrabTransform => transform;

    public void OnGrabbed()
    {
    }

    public void OnCollected()
    {
        DataManager.Instance.AddCurrentGold(_goldAmount);
        Destroy(gameObject);
    }
}
