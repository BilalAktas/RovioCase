using Lofelt.NiceVibrations;
using UnityEngine;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

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

            if (Touch.activeTouches.Count == 0) return;

            var t = Touch.activeTouches[0];
            if(t.phase == TouchPhase.Began) HandleTap(t.screenPosition);
        }
   
        private void HandleTap(Vector3 pos)
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
                    HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact);
                }
            }
        }
    }
}
