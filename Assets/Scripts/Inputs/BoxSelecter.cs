using UnityEngine;

namespace Core
{
    public class BoxSelecter : MonoBehaviour
    {
        [SerializeField] private float _coolDown;
        private float _currentTime;
        private bool _firstSelect;
        
        private Camera _camera;
        private void Start() => _camera = Camera.main;

        private void Update()
        {
            if (GameManager.GameState == GameState.Idle) return;
            
            if(Input.GetMouseButtonDown(0))
                HandleStartDrag(Input.mousePosition);
            
            // if (Input.touchCount <= 0) return;
            //
            // var input = Input.GetTouch(0);
            // switch (input.phase)
            // {
            //     case TouchPhase.Began:
            //         HandleStartDrag(input.position);
            //         break;
            // }
        }
   
        private void HandleStartDrag(Vector3 pos)
        {
            if (_firstSelect && Time.time - _currentTime < _coolDown) return;

            _currentTime = Time.time;
            var ray = _camera.ScreenPointToRay(pos);
            if (Physics.Raycast(ray, out var hit))
            {
                if (hit.transform.TryGetComponent(out IClickable clickable))
                {
                    if (!clickable.Clickable()) return;
                    clickable.Select();
                    _firstSelect = true;
                }
            }
        }
    }
}
