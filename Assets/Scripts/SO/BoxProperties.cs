using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "BoxProperties", menuName = "ScriptableObjects/BoxProperties")]
    public class BoxProperties : ScriptableObject
    {
        [SerializeField] private float _speed;
        [SerializeField] private float _turnAngle;
        [SerializeField] private LayerMask _cubeLayer;
        
        public float Speed => _speed;
        public float TurnAngle => _turnAngle;
        public LayerMask CubeLayer => _cubeLayer;
    }
}