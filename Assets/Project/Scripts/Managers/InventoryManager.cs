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

        private PlayerItemThrower _itemThrower;
        private PlayerShield _shield;
        private PlayerInvincibility _invincibility;

        protected override void Awake()
        {
            base.Awake();

            // 인스펙터에서 안 넣어줬으면 씬에서 자동으로 찾는다.
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerMovement>();
            }

            if (player != null)
            {
                _itemThrower = player.GetComponent<PlayerItemThrower>();
                _shield = player.GetComponent<PlayerShield>();
                _invincibility = player.GetComponent<PlayerInvincibility>();
            }

            for (int i = 0; i < slotIcons.Length; i++)
            {
                UpdateSlotUI(i);
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

            if (item.type == ItemType.SonicBomb)
            {
                // 조준/투척이 완료된 뒤에 콜백으로 슬롯을 소비한다 (조준 중 취소하면 소비되지 않음).
                if (_itemThrower == null)
                {
                    Debug.LogWarning("InventoryManager: PlayerItemThrower 참조가 없어 소리폭탄을 사용할 수 없습니다.");
                    return;
                }

                if (_itemThrower.IsAiming) return;

                _itemThrower.BeginAim(item, () => ConsumeSlot(index));
                return;
            }

            ApplyItemEffect(item);
            ConsumeSlot(index);
        }

        private void ConsumeSlot(int index)
        {
            slots[index] = null;
            UpdateSlotUI(index);
        }
        public bool HasItem(ItemData item)
        {
            if(item==null) return false;

            foreach ( ItemData slotItem in slots)
            {
                if (slotItem == item)
                {
                    return true;
                }
            }
            return false;
        }

        public bool ConsumeItem(ItemData item)
        {
            if (item == null) return false;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == item)
                {
                    slots[i] = null;
                    UpdateSlotUI(i);
                    Debug.Log($"{item.name} 아이템이 소비되었습니다.");
                    return true;
                }
            }

            Debug.Log($"{item.name} 아이템이 인벤토리에 없습니다.");
            return false;
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

                case ItemType.Shield:
                    if (_shield == null)
                    {
                        Debug.LogWarning("InventoryManager: PlayerShield 참조가 없어 쉴드를 적용할 수 없습니다.");
                        break;
                    }
                    _shield.ActivateShield();
                    break;

                case ItemType.Invincibility:
                    if (_invincibility == null)
                    {
                        Debug.LogWarning("InventoryManager: PlayerInvincibility 참조가 없어 무적을 적용할 수 없습니다.");
                        break;
                    }
                    _invincibility.StartInvincibility(item.duration);
                    break;

                case ItemType.None:
                case ItemType.SonicBomb:
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
