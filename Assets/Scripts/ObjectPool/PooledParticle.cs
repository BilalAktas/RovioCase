using UnityEngine;

namespace Core
{
    public class PooledParticle : MonoBehaviour
    {
        private ParticleSystem _ps;
        private ParticleSystemRenderer _psr;

        private MaterialPropertyBlock _mpb;
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            if (_ps == null) _ps = GetComponent<ParticleSystem>();
            if (_psr == null) _psr = GetComponent<ParticleSystemRenderer>();

            _mpb ??= new MaterialPropertyBlock();
        }

        public void PlayAt(Vector3 worldPos, Color color)
        {
            transform.position = worldPos;
            
            _psr.GetPropertyBlock(_mpb);
            _mpb.SetColor(_baseColorId, color);
            _psr.SetPropertyBlock(_mpb);

            _ps.Play(true);
        }
    }
}