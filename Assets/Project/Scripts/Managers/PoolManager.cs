using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

namespace Managers
{
    /// <summary>
    /// 게임 내 모든 동적 생성 오브젝트의 메모리 풀링을 전담하는 매니저
    /// </summary>
    public class PoolManager : SingletonBase<PoolManager>
    {
        private Dictionary<int, IObjectPool<GameObject>> poolDictionary = new Dictionary<int, IObjectPool<GameObject>>();

        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// 풀에서 오브젝트를 가져옴, 풀이 없다면 자동으로 생성
        /// </summary>
        public GameObject Pop(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;

            int key = prefab.GetInstanceID();

            if(!poolDictionary.ContainsKey(key))
            {
                RegisterNewPool(prefab, key);
            }

            GameObject obj = poolDictionary[key].Get();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            if (parent != null) obj.transform.SetParent(parent);

            return obj;
        }

        /// <summary>
        /// 사용이 끝난 오브젝트를 해당하는 풀로 반납
        /// </summary>
        public void Push(GameObject go, GameObject prefabSource)
        {
            if (go == null || prefabSource == null) return;
            int key = prefabSource.GetInstanceID();

            if(poolDictionary.ContainsKey(key))
            {
                poolDictionary[key].Release(go);
            }
            else
            {
                Destroy(go);
            }
        }

        private void RegisterNewPool(GameObject prefab, int key)
        {
            IObjectPool<GameObject> newPool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(prefab),
                actionOnGet: (go) => go.SetActive(true),
                actionOnRelease: (go) => go.SetActive(false),
                actionOnDestroy: (go) => Destroy(go),
                collectionCheck: true,
                defaultCapacity: 10,
                maxSize: 50
            );

            poolDictionary.Add(key, newPool);
        }






    }
}
