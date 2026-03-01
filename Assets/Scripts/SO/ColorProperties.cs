using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "BoxProperties", menuName = "ScriptableObjects/BoxProperties")]
    public class ColorProperties : ScriptableObject
    {
        [SerializeField] private BoxColor _boxColor;
        [SerializeField] private Material _boxColorMaterial;
        [SerializeField] private Material _cubeColorMaterial;
        [SerializeField] private float _adjustBrightness;

        public BoxColor BoxColor => _boxColor;
        public Material BoxColorMaterial => _boxColorMaterial;
        public Material CubeColorMaterial => _cubeColorMaterial;
        public float AdjustBrightness => _adjustBrightness;
    }
}