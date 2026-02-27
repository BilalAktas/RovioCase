using UnityEngine;

namespace Core
{
    public class BoxSelecter : MonoBehaviour
    {
        private Camera _camera;
        private void Start() => _camera = Camera.main;

        private void Update()
        {
            // if(Input.GetMouseButtonDown(0))
            //     HandleStartDrag(Input.mousePosition);
            
            if (Input.touchCount <= 0) return;
            
            var input = Input.GetTouch(0);
            switch (input.phase)
            {
                case TouchPhase.Began:
                    HandleStartDrag(input.position);
                    break;
            }
        }
   
        private void HandleStartDrag(Vector3 pos)
        {
            var ray = _camera.ScreenPointToRay(pos);
            if (Physics.Raycast(ray, out var hit))
            {
                if (hit.transform.TryGetComponent(out IClickable clickable))
                {
                    if (!clickable.Clickable()) return;
                    clickable.Select();
                }
            }
        }
    }
}
