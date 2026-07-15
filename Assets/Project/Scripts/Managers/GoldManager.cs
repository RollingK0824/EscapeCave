using UnityEngine;

namespace Managers
{
    public class GoldManager : SingletonBase<GoldManager>
    {
       
        [SerializeField] private int _currentGold = 5;
        public int CurrentGold => _currentGold;

        public void AddGold(int amount)
        {
            if (amount <= 0) return;

            _currentGold += amount;
            Debug.Log($"골드 {amount} 획득, 현재 골드: {_currentGold}");
        }
        public bool HasEnoughGold(int amount)
        {
            return _currentGold >= amount;
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0) return false;

            if (!HasEnoughGold(amount))
            {
                Debug.Log($"골드 부족: 필요 {amount}, 보유 {_currentGold}");
                return false;
            }
            _currentGold -= amount;
            Debug.Log($"골드 {amount} 소비, 현재 골드: {_currentGold}");
            return true;
        }
    }
}

