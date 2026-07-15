using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class UnlockManager : SingletonBase<UnlockManager>
    {
        private readonly HashSet<string> _unlockedIds = new HashSet<string>();

        public bool IsUnlocked(UnlockNodeData node)
        {
            if (node == null) return false;

            if (_unlockedIds.Contains(node.nodeId)) return true;

            if (PlayerPrefs.GetInt(SaveKey(node), 0) == 1)
            {
                _unlockedIds.Add(node.nodeId);
                return true;
            }

            return false;
        }

        public bool CanUnlock(UnlockNodeData node)
        {
            if (node == null || IsUnlocked(node)) return false;

            foreach (UnlockNodeData prereq in node.prerequisites)
            {
                if (!IsUnlocked(prereq)) return false;
            }

            return GoldManager.Instance.HasEnoughGold(node.cost);
        }


        public bool TryUnlock(UnlockNodeData node)
        {
            if (!CanUnlock(node)) return false;

            GoldManager.Instance.TrySpendGold(node.cost);
            _unlockedIds.Add(node.nodeId);

            PlayerPrefs.SetInt(SaveKey(node), 1);
            PlayerPrefs.Save();

            return true;
        }

        private string SaveKey(UnlockNodeData node)
        {
            return $"UnlockNode_{node.nodeId}";
        }
    }
}