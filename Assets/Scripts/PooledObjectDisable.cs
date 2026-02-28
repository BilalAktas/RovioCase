using UnityEngine;

namespace Core
{
    public class PooledObjectDisable : MonoBehaviour
    {
        [SerializeField] private float _disableTime;
        [SerializeField] private string _pooledName;
        
        private void OnEnable()
        {
            Invoke(nameof(Deposit), _disableTime);
        }
        
        private void Deposit() => ObjectPool.Instance.Deposit(gameObject, _pooledName);
    }
}