using UnityEngine;

namespace Core
{
    /// <summary>
    /// Automatically disables and returns the GameObject to the object pool after a specified time.
    /// </summary>
    public class PooledObjectDisable : MonoBehaviour
    {
        [SerializeField] private float _disableTime;
        [SerializeField] private string _pooledName;

        /// <summary>
        /// Called when the object is enabled. Starts a timer to return the object to the pool.
        /// </summary>
        private void OnEnable()
        {
            Invoke(nameof(Deposit), _disableTime);
        }

        /// <summary>
        /// Returns the object to the object pool.
        /// </summary>
        private void Deposit() => ObjectPool.Instance.Deposit(gameObject, _pooledName);
    }
}