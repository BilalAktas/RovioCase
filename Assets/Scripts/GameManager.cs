using UnityEngine;

namespace Core
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameObject _levelCompleted;
        [SerializeField] private GameObject _levelFailed;

        private int _boxAmount;
        
        private void Start()
        {
            EventBus.Subscribe<OnLevelFailedEvent>(OnLevelFail);
            EventBus.Subscribe<OnBoxFilledEvent>(OnBoxFilled);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnLevelFailedEvent>(OnLevelFail);
            EventBus.Unsubscribe<OnBoxFilledEvent>(OnBoxFilled);
        }

        private void OnBoxFilled(OnBoxFilledEvent data)
        {
            _boxAmount++;
            if (_boxAmount >= 9)
            {
                _levelCompleted.SetActive(true);
            }
        }

        private void OnLevelFail(OnLevelFailedEvent data)
        {
            Time.timeScale = 0;
            _levelFailed.SetActive(true);
        }
    }
}