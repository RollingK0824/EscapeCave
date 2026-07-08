using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Managers
{
    public class InventoryManager : SingletonBase<InventoryManager>
    {
        [SerializeField] private ItemData[] slots = new ItemData[3];
        [SerializeField] private Image[] slotIcons = new Image[3];

        [Header("Player Reference")]
        [SerializeField] private PlayerMovement player;

        protected override void Awake()
        {
            base.Awake();

            // 인스펙터에서 안 넣어줬으면 씬에서 자동으로 찾는다.
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerMovement>();
            }

            for (int i = 0; i < slotIcons.Length; i++)
            {
                if (slotIcons[i] != null)
                    slotIcons[i].enabled = false;
            }
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.digit1Key.wasPressedThisFrame) UseItem(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) UseItem(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) UseItem(2);
        }

        public bool AddItem(ItemData item)
        {
            if (item == null) return false;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    slots[i] = item;
                    UpdateSlotUI(i);
                    Debug.Log($"{i}번 슬롯에 {item.name} 추가됨");
                    return true;
                }
            }

            Debug.Log("인벤토리가 가득 찼습니다.");
            return false;
        }

        public void UseItem(int index)
        {
            if (index < 0 || index >= slots.Length) return;

            ItemData item = slots[index];
            if (item == null)
            {
                Debug.Log($"{index}번 슬롯은 비어있습니다.");
                return;
            }

            Debug.Log($"{index}번 슬롯 아이템 사용: {item.name}");

            ApplyItemEffect(item);

            slots[index] = null;
            UpdateSlotUI(index);
        }

        private void ApplyItemEffect(ItemData item)
        {
            if (player == null)
            {
                Debug.LogWarning("InventoryManager: player 참조가 없어 아이템 효과를 적용할 수 없습니다.");
                return;
            }

            switch (item.type)
            {
                case ItemType.Flight:
                    player.StartFlight(item.duration);
                    break;

                case ItemType.None:
                default:
                    break;
            }
        }

        private void UpdateSlotUI(int index)
        {
            if (slotIcons[index] == null) return;

            if (slots[index] != null)
            {
                slotIcons[index].sprite = slots[index].icon;
                slotIcons[index].enabled = true;
            }
            else
            {
                slotIcons[index].sprite = null;
                slotIcons[index].enabled = false;
            }
        }
    }
}
