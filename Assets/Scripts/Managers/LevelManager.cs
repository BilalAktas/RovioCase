using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core
{
    public class LevelManager : Singleton<LevelManager>
    {
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private LevelDesignData[] _levelDesignDatas;
        private LevelDesignData _currentLevelDesignData;
        public LevelDesignData CurrentLevelDesignData => _currentLevelDesignData;
        
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _retryButton;

        private void Start()
        {
            SpawnLevel();
            _nextButton.onClick.AddListener(SpawnLevel);
            _retryButton.onClick.AddListener(SpawnLevel);
        }

        private void OnDestroy()
        {
            _nextButton.onClick.RemoveAllListeners();
            _retryButton.onClick.RemoveAllListeners();
        }

        private void SetLevelText() => _levelText.text = $"LEVEL {SaveLoadManager.GetLevel()}";

        private void SpawnLevel()
        {
            var currentLevel = SaveLoadManager.GetLevel();
            var id = (currentLevel - 1) % _levelDesignDatas.Length;
            _currentLevelDesignData = _levelDesignDatas[id];
            SetLevelText();
            EventBus.Raise(new OnLevelSpawnedEvent());
        }
    }
}