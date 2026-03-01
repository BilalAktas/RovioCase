using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    [System.Serializable]
    public class PooledObject
    {
        public string Name;
        public GameObject Prefab;
        public int SpawnAmount;
        public int ExpandAmount;
        public Queue<GameObject> ObjectPooled = new();
    }
    
    public class ObjectPool : Singleton<ObjectPool>
    {
        [SerializeField] private PooledObject[] _pooledObjects;

        private void Awake()
        {
            Init();
        }
        
        private void Init()
        {
            foreach (var pooledObject in _pooledObjects)
            {
                for (var i = 0; i < pooledObject.SpawnAmount; i++)
                {
                    var clone = Instantiate(pooledObject.Prefab, transform);
                    clone.SetActive(false);
                    pooledObject.ObjectPooled.Enqueue(clone);
                }
            }
        }
        
        public GameObject GetFromPool(string name)
        {
            foreach (var pooledObject in _pooledObjects)
            {
                if (pooledObject.Name != name)
                    continue;

                if (pooledObject.ObjectPooled.Count > 0)
                    return pooledObject.ObjectPooled.Dequeue();


                for (var i = 0; i < pooledObject.ExpandAmount; i++)
                {
                    var clone = Instantiate(pooledObject.Prefab, transform);
                    clone.SetActive(false);
                    pooledObject.ObjectPooled.Enqueue(clone);
                }

                return pooledObject.ObjectPooled.Dequeue();
            }

            return null;
        }
        
        public void Deposit(GameObject _object, string poolName)
        {
            foreach (var pooledObject in _pooledObjects)
            {
                if (pooledObject.Name != poolName)
                    continue;

                _object.SetActive(false);
                _object.transform.SetParent(transform);
                pooledObject.ObjectPooled.Enqueue(_object);
            }
        }
    }
}