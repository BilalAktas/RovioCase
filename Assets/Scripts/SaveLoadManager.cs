using UnityEngine;

namespace Core
{
    public static class SaveLoadManager
    {
        public static void IncreaseLevel() => PlayerPrefs.SetInt("Level", GetLevel() + 1);
        public static int GetLevel() => PlayerPrefs.GetInt("Level", 1);
    }
}