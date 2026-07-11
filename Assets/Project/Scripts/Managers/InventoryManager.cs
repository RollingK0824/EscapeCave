using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Managers
{
    public class InventoryManager : SingletonBase<InventoryManager>
    {
        [SerializeField] private Sprite[] slots = new Sprite[3];
        [SerializeField] private Image[] slotIcons = new Image[3];

        protected override void Awake()
        {
            base.Awake();

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

        public bool AddItem(Sprite icon)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                {
                    slots[i] = icon;
                    UpdateSlotUI(i);
                    Debug.Log($"{i}번 슬롯에 아이템 추가됨");
                    return true;
                }
            }

            Debug.Log("인벤토리가 가득 찼습니다.");
            return false;
        }

        public void UseItem(int index)
        {
            if (index < 0 || index >= slots.Length) return;
            if (slots[index] == null)
            {
                Debug.Log($"{index}번 슬롯은 비어있습니다.");
                return;
            }

            Debug.Log($"{index}번 슬롯 아이템 사용");
            slots[index] = null;
            UpdateSlotUI(index);
        }

        private void UpdateSlotUI(int index)
        {
            if (slotIcons[index] == null) return;

            if (slots[index] != null)
            {
                slotIcons[index].sprite = slots[index];
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