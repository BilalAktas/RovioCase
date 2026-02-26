using UnityEngine;

namespace Core
{
    [System.Serializable]
    public struct BoxAreaDesignStruct
    {
        public Vector2Int GridPosition;
        public BoxColor BoxColor;
    }
    
    [CreateAssetMenu(fileName = "BoxAreaDesign", menuName = "ScriptableObjects/BoxAreaDesign")]
    public class BoxAreaDesign : ScriptableObject
    {
        [SerializeField] private BoxAreaDesignStruct[] _boxAreaDesignStruct;
        public BoxAreaDesignStruct[] BoxAreaDesignStruct => _boxAreaDesignStruct;
    }
}