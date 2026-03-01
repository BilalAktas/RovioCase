using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "BoxProperties", menuName = "ScriptableObjects/BoxProperties")]
    public class ColorProperties : ScriptableObject
    {
        [SerializeField] private BoxColor _boxColor;
        [SerializeField] private Material _colorMaterial;

        public BoxColor BoxColor => _boxColor;
        public Material ColorMaterial => _colorMaterial;
    }
}