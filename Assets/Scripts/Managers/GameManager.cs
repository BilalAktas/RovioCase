using DG.Tweening;
using UnityEngine;

namespace Core
{
    public enum GameState
    {
        Idle,
        Play
    }
    
    public class GameManager : MonoBehaviour
    {
        public static GameState GameState;
        [SerializeField] private GameObject _levelCompleted;
        [SerializeField] private GameObject _levelFailed;
        private int _boxAmount;
        
        private void Start()
        {
            Application.targetFrameRate = 60; // TEST BUILD.
            
            EventBus.Subscribe<OnLevelSpawnedEvent>(OnLevelSpawned);
            EventBus.Subscribe<OnLevelFailedEvent>(OnLevelFail);
            EventBus.Subscribe<OnBoxFilledEvent>(OnBoxFilled);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnLevelSpawnedEvent>(OnLevelSpawned);
            EventBus.Unsubscribe<OnLevelFailedEvent>(OnLevelFail);
            EventBus.Unsubscribe<OnBoxFilledEvent>(OnBoxFilled);
        }

        private void OnLevelSpawned(OnLevelSpawnedEvent data)
        {
            _levelCompleted.SetActive(false);
            _levelFailed.SetActive(false);
            _boxAmount = 0;
            GameState = GameState.Play;
        }

        private void OnBoxFilled(OnBoxFilledEvent data)
        {
            if (GameState == GameState.Idle) return;
            
            _boxAmount++;
            if (_boxAmount >= LevelManager.Instance.CurrentLevelDesignData.GetBoxAmount())
            {
                DOTween.CompleteAll();
                SaveLoadManager.IncreaseLevel();
                GameState = GameState.Idle;
                _levelCompleted.SetActive(true);
            }
        }

        private void OnLevelFail(OnLevelFailedEvent data)
        {
            if (GameState == GameState.Idle) return;

            DOTween.CompleteAll();
            GameState = GameState.Idle;
            _levelFailed.SetActive(true);
        }
    }
}