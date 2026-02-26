using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Splines;

namespace Core
{
    public class Box : MonoBehaviour, IClickable
    {
        private BoxProperties _boxProperties;
        private BoxGridNode _node;
        private bool _clickableStatus;

        [Header("MoveOnContainer")]
        private SplineContainer _splineContainer;
        [SerializeField] private float _speed;
        private float _timer;
        private bool _startedMoving;
        
        public void SetProperties(BoxProperties properties, BoxGridNode node)
        {
            _boxProperties = properties;
            _node = node;
            foreach (var renderer in GetComponentsInChildren<Renderer>())
                renderer.material = _boxProperties.ColorMaterial;

            _splineContainer = FindFirstObjectByType<SplineContainer>();
        }

        public bool Clickable()
        {
            return _clickableStatus;
        }

        public void Select()
        {
            _node.SetBox(null);
            _node = null;
            
            var e = new OnBoxMovedFromBoxAreaEvent()
            {
                Box = this
            };
            EventBus.Raise(e);
            JumpOnContainer();
        }

        public void SetClickableStatus(bool state, bool gameStart)
        {
            _clickableStatus = state;
            var scale = state ? Vector3.one : Vector3.one / 2;
            if (!gameStart) transform.DOScale(scale, .25f).SetEase(Ease.Linear);
            else transform.localScale = scale;
        }

        private void JumpOnContainer()
        {
            transform.DOComplete();
            
            Vector3 pos = _splineContainer.EvaluatePosition(0);
            var midPoint = (transform.position + pos) / 2f + Vector3.up * 5f;

            var poses = new List<Vector3>()
            {
                midPoint,
                pos
            };
            
            transform.DOScale(new Vector3(1.2f, 0.8f, 1.2f), .02f)
                .SetLoops(2, LoopType.Yoyo).OnComplete(() =>
                {
                    transform.DOPath(poses.ToArray(), .5f, PathType.CatmullRom).SetEase(Ease.InOutSine).OnComplete(() =>
                    {
                        _startedMoving = true;
                    });
                });
        }
        
        private void Update()
        {
            if (!_startedMoving) return;
            
            _timer += _speed * Time.deltaTime;

            transform.position = _splineContainer.EvaluatePosition(_timer);
            transform.forward = _splineContainer.EvaluateTangent(_timer);

            if (_timer >= 1)
            {
                _startedMoving = false;
                var e = new OnBoxEndMoveEvent()
                {
                    Box = this
                };
                EventBus.Raise(e);
            }
        }

        public void MoveToBench(BenchArea area)
        {
            transform.DOComplete();
            var midPoint = (transform.position + area.Visual.transform.position) / 2f + Vector3.up * 5f;

            var poses = new List<Vector3>()
            {
                midPoint,
                area.Visual.transform.position
            };
            
            transform.DOPath(poses.ToArray(), .5f, PathType.CatmullRom).SetEase(Ease.InOutSine).OnComplete(() =>
            {
               
            });
        }
    }
}