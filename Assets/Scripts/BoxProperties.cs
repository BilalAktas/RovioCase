using Core;
using UnityEngine;

[CreateAssetMenu(fileName = "BoxProperties", menuName = "ScriptableObjects/BoxProperties")]
public class BoxProperties : ScriptableObject
{
    [SerializeField] private BoxColor _boxColor;
    [SerializeField] private Material _colorMaterial;

    public BoxColor BoxColor => _boxColor;
    public Material ColorMaterial => _colorMaterial;
}
