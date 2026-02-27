using DG.Tweening;
using UnityEngine;

namespace Core
{
    public class Cube : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private TrailRenderer _trail;
        [SerializeField] private GameObject _particle;
        
        public ProductGridNode CurrentNode { get; private set; }
        public BoxProperties Properties { get; private set; }
        
        public void SetProperties(BoxProperties properties)
        {
            Properties = properties;
            GetComponent<MeshRenderer>().sharedMaterial = Properties.ColorMaterial;
            var c = Properties.ColorMaterial.color;
            Color.RGBToHSV(c, out float h, out float s, out float v);
            v += .25f;
            v = Mathf.Clamp01(v);

            var co = Color.HSVToRGB(h, s, v);
            co.a = 1f;
            
            _trail.material.SetColor("_BaseColor", co);
            
            _trail.emitting = false;
        }

        public void SetNode(ProductGridNode node)
        {
            CurrentNode = node;
        }
        
        public void OnSelectedByBox(Transform slot)
        {
            _trail.emitting = true;
            CurrentNode.SetCube(null);
            CurrentNode = null;
            GetComponent<Collider>().enabled = false;
            
            transform.DOScale(.6f, 0.05f).OnComplete(() =>
            {
                transform.DOScale(.5f, 0.05f);
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
                    var targetPos = slot.position;
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
                    transform.SetParent(slot);
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.identity;

                    var clone = Instantiate(_particle);
                    clone.transform.position = transform.position;
            
                    clone.GetComponent<ParticleSystemRenderer>().material.color =
                        Properties.ColorMaterial.color;
                    
                    clone.GetComponent<ParticleSystem>().Play();
                    
                    _trail.emitting = false;
                    _audioSource.Play();
                    GetComponentInParent<Box>().CheckFill();
                    
                });
        }
    }
}