using System;

namespace Managers
{
    public class UIManager : SingletonBase<UIManager>
    {
        public event Action<int> OnGoldChanged;
        public event Action<UnlockNodeData> OnUnlockChanged;
        public event Action<float> OnBestDepthChanged;

        public int CurrentGold => DataManager.Instance.CurrentGold;
        public float BestDepth => DataManager.Instance.BestDepth;

        protected override void Awake()
        {
            base.Awake();

            DataManager.Instance.OnGoldChanged += HandleGoldChanged;
            DataManager.Instance.OnUnlockChanged += HandleUnlockChanged;
            DataManager.Instance.OnBestDepthChanged += HandleBestDepthChanged;
        }

        private void HandleGoldChanged(int gold) => OnGoldChanged?.Invoke(gold);
        private void HandleUnlockChanged(UnlockNodeData node) => OnUnlockChanged?.Invoke(node);
        private void HandleBestDepthChanged(float depth) => OnBestDepthChanged?.Invoke(depth);

        public bool IsUnlocked(UnlockNodeData node) => DataManager.Instance.IsUnlocked(node);

        public bool CanUnlock(UnlockNodeData node) => DataManager.Instance.CanUnlock(node);

        public bool TryUnlock(UnlockNodeData node) => DataManager.Instance.TryUnlock(node);

        public bool HasEnoughGold(int amount) => DataManager.Instance.HasEnoughGold(amount);
    }
}
