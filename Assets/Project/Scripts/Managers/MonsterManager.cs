using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public static class MonsterManager
    {
        public static Transform PlayerTransform { get; private set; }
        private static readonly List<MonsterController> _monsters = new List<MonsterController>();

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

        public static void RegisterMonster(MonsterController monster) => _monsters.Add(monster);

        public static void UnregisterMonster(MonsterController monster) => _monsters.Remove(monster);

        public static void NotifySound(Transform source, float lureDuration = 0f)
        {
            foreach(var monster in _monsters)
            {
                monster?.NotifySound(source, lureDuration);
            }
        }

        public static void NotifyVibration()
        {
            foreach(var monster in _monsters)
            {
                monster?.NotifyVibration();
            }
        }
    }
}
