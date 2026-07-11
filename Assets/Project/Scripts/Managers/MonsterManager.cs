using UnityEngine;

namespace Managers
{
    public static class MonsterManager
    {
        public static Transform PlayerTransform { get; private set; }

        public static void RegisterPlayer(Transform playerTransform)
        {
            PlayerTransform = playerTransform;
        }

        public static void UnregisterPlayer(Transform playerTransform)
        {
            if (PlayerTransform == playerTransform)
            {
                PlayerTransform = null;
            }
        }
    }
}
