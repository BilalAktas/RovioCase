using DG.Tweening;
using UnityEngine;

namespace Core
{
    public class Cube : MonoBehaviour
    {
        public ProductGridNode CurrentNode { get; private set; }
        public ColorProperties Properties { get; private set; }
        
        [SerializeField] private TrailRenderer _trail;
        private Vector3 _defaultScale;
        private MaterialPropertyBlock _propertyBlock;
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _albedoColorId = Shader.PropertyToID("_AlbedoColor");

        private Collider _collider;
        private MeshRenderer _renderer;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _renderer = GetComponent<MeshRenderer>();
            _defaultScale = transform.localScale;
        }

        public void SetProperties(ColorProperties properties)
        {
            _collider.enabled = true;
            transform.localScale = _defaultScale;
            transform.rotation = Quaternion.identity;
            
            Properties = properties;

            _propertyBlock = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(_albedoColorId, Properties.CubeColorMaterialColor);
            _renderer.SetPropertyBlock(_propertyBlock);
            
             var newColor = Helpers.AdjustBrightness(Properties.CubeColorMaterialColor, .25f);
             newColor.a = 1f;
            
             _propertyBlock = new MaterialPropertyBlock();
            _trail.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(_baseColorId, newColor);
            _trail.SetPropertyBlock(_propertyBlock);
            
            _trail.emitting = false;
 
        }

        public void SetNode(ProductGridNode node)
        {
            CurrentNode = node;
        }
        
        public void OnSelectedByBox(Transform box)
        {
            _trail.emitting = true;
            CurrentNode.SetCube(null);
            CurrentNode = null;
            _collider.enabled = false;
            
            transform.DOScale(_defaultScale + new Vector3(.1f, .1f,.1f), 0.05f).OnComplete(() =>
            {
                transform.DOScale(_defaultScale, 0.05f);
            });
            
            var duration = 0.5f;
            var elapsed = 0f;
            var height = 5f;

            var startPos = transform.position;

            DOTween.To(() => elapsed, x => elapsed = x, duration, duration)
                .SetEase(Ease.InCubic).OnUpdate(() =>
                {
                    transform.Rotate(Vector3.up * 720 * Time.deltaTime);
                    
                    var t = elapsed / duration;
                    var targetPos = box.position;
                    var flatPos = Vector3.Lerp(startPos, targetPos, t);
                    var arc = 4 * height * t * (1 - t);

                    transform.position = new Vector3(
                        flatPos.x,
                        flatPos.y + arc,
                        flatPos.z
                    );
                })
                .OnComplete(() =>
                {
                    transform.SetParent(box.GetChild(0));

                    var clone = ObjectPool.Instance.GetFromPool("CubeCollectParticle");
                    if (clone.TryGetComponent(out PooledParticle particle))
                    {
                        clone.SetActive(true);
                        particle.PlayAt(transform.position,  Properties.CubeColorMaterialColor);
                    }
                    
                    _trail.emitting = false;
                    GetComponentInParent<Box>().OnCubeMoveEnd(this);
                });
        }
    }
}