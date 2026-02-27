using DG.Tweening;
using UnityEngine;

namespace Core
{
    public class Cube : MonoBehaviour
    {
        public ProductGridNode CurrentNode { get; private set; }
        public BoxProperties Properties;

        public void SetProperties(BoxProperties properties)
        {
            Properties = properties;
            GetComponent<MeshRenderer>().sharedMaterial = Properties.ColorMaterial;
        }

        public void SetNode(ProductGridNode node)
        {
            CurrentNode = node;
        }
        
        public void OnSelectedByBox(Transform slot)
        {
            CurrentNode.SetCube(null);
            CurrentNode = null;
            GetComponent<Collider>().enabled = false;
            transform.localScale = new Vector3(.5f, 1, .25f);
          
            var duration = 0.5f;
            var elapsed = 0f;
            var height = 1f;

            var startPos = transform.position;

            DOTween.To(() => elapsed, x => elapsed = x, duration, duration)
                .OnUpdate(() =>
                {
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
                });
        }
    }
}