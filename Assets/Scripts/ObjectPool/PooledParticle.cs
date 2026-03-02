using UnityEngine;

namespace Core
{
    public class PooledParticle : MonoBehaviour
    {
        private ParticleSystem _particleSystem;
        private ParticleSystemRenderer _particleSystemRenderer;

        private MaterialPropertyBlock _materialPropertyBlock;
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            _particleSystemRenderer = GetComponent<ParticleSystemRenderer>();

            _materialPropertyBlock ??= new MaterialPropertyBlock();
        }

        public void PlayAt(Vector3 worldPos, Color color)
        {
            transform.position = worldPos;
            
            _particleSystemRenderer.GetPropertyBlock(_materialPropertyBlock);
            _materialPropertyBlock.SetColor(_baseColorId, color);
            _particleSystemRenderer.SetPropertyBlock(_materialPropertyBlock);

            _particleSystem.Play(true);
        }
    }
}