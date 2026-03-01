using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "BoxProperties", menuName = "ScriptableObjects/BoxProperties")]
    public class ColorProperties : ScriptableObject
    {
        [SerializeField] private BoxColor _boxColor;
        [SerializeField] private Color _boxColorMaterialColor;
        [SerializeField] private Color _cubeColorMaterialColor;
        [SerializeField] private float _adjustBrightness;

        public BoxColor BoxColor => _boxColor;
        public Color BoxColorMaterialColor => _boxColorMaterialColor;
        public Color CubeColorMaterialColor => _cubeColorMaterialColor;
        public float AdjustBrightness => _adjustBrightness;
    }
}