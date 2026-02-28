using TMPro;
using UnityEngine;

namespace Core
{
    public class LevelManager : Singleton<LevelManager>
    {
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private LevelDesignData[] _levelDesignData;
        public LevelDesignData CurrentLevelDesignData => _levelDesignData[SaveLoadManager.GetLevel() - 1];

        private void Start()
        {
            SetLevelText();
        }

        private void SetLevelText() => _levelText.text = $"LEVEL {SaveLoadManager.GetLevel()}";
    }
}