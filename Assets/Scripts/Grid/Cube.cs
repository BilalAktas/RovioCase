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
        private MaterialPropertyBlock _trailPropertyBlock;
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        
        public void SetProperties(ColorProperties properties)
        {
            Properties = properties;
            
            GetComponent<MeshRenderer>().sharedMaterial = Properties.ColorMaterial;
            var newColor = Helpers.AdjustBrightness(Properties.ColorMaterial.color, .25f);
            newColor.a = 1f;
            
            _trailPropertyBlock = new MaterialPropertyBlock();
            _trail.GetPropertyBlock(_trailPropertyBlock);
            _trailPropertyBlock.SetColor(_baseColorId, newColor);
            _trail.SetPropertyBlock(_trailPropertyBlock);
            
            _trail.emitting = false;
            _defaultScale = transform.localScale;
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
            GetComponent<Collider>().enabled = false;
            
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
                    transform.SetParent(box);
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.identity;

                    var clone = ObjectPool.Instance.GetFromPool("CubeCollectParticle");
                    if (clone.TryGetComponent(out PooledParticle particle))
                    {
                        clone.SetActive(true);
                        particle.PlayAt(transform.position,  Properties.ColorMaterial.color);
                    }
                    
                    _trail.emitting = false;
                    GetComponentInParent<Box>().OnCubeMoveEnd(this);
                });
        }
    }
}