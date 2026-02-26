using UnityEngine;

namespace Core
{
    public class GameManager : MonoBehaviour
    {
        private void Start()
        {
            EventBus.Subscribe<OnLevelFailedEvent>(OnLevelFail);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnLevelFailedEvent>(OnLevelFail);
        }

        private void OnLevelFail(OnLevelFailedEvent data)
        {
            Debug.Log("Fail");
        }
    }
}