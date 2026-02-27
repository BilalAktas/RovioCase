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

        [Header("MoveOnContainer")] private SplineContainer _splineContainer;
        [SerializeField] private float _speed;
        private float _timer;
        private bool _startedMoving;
        private Vector3 _prevDir = Vector3.forward;
        private float _cooldown;
        
        [Header("Ray")] 
        private Cube _lastCube;
        [SerializeField] private Transform[] _cubeSlots;
        private readonly Dictionary<Transform, Cube> _cubes = new();
        private Cube _depthCube;
        private Dictionary<ProductDepthDirection, HashSet<int>> _lockedColumns = new();
        [SerializeField] private LayerMask _cubeLayer;

        public void SetProperties(BoxProperties properties, BoxGridNode node)
        {
            _boxProperties = properties;
            _node = node;
            foreach (var renderer in GetComponentsInChildren<Renderer>())
                renderer.material = _boxProperties.ColorMaterial;

            _splineContainer = FindFirstObjectByType<SplineContainer>();
            foreach (var slot in _cubeSlots)
                _cubes[slot] = null;

            ClearAllLockedColumns();
        }

        private void ClearAllLockedColumns()
        {
            _lockedColumns[ProductDepthDirection.Up] = new HashSet<int>();
            _lockedColumns[ProductDepthDirection.Right] = new HashSet<int>();
            _lockedColumns[ProductDepthDirection.Down] = new HashSet<int>();
            _lockedColumns[ProductDepthDirection.Left] = new HashSet<int>();
        }

        public bool Clickable()
        {
            return _clickableStatus;
        }

        public void Select()
        {
            if (_startedMoving) return;
            
            _node?.SetBox(null);
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
            _timer = 0;
            transform.DOComplete();

            Vector3 pos = _splineContainer.EvaluatePosition(0);
            pos += Vector3.up * .1f;
            var midPoint = (transform.position + pos) / 2f + Vector3.up * 5f;

            var poses = new List<Vector3>()
            {
                midPoint,
                pos
            };

            transform.DOScale(new Vector3(1.2f, 0.8f, 1.2f), .02f)
                .SetLoops(2, LoopType.Yoyo).OnComplete(() =>
                {
                    transform.DOPath(poses.ToArray(), .5f, PathType.CatmullRom).SetEase(Ease.InOutSine)
                        .OnComplete(() => { _startedMoving = true; });
                });
        }

        private void Update()
        {
            if (!_startedMoving) return;

            Move();
            CollectCube();
        }
     
        private void Move()
        {
            _timer += _speed * Time.deltaTime;

            transform.position = _splineContainer.EvaluatePosition(_timer);
            transform.position += Vector3.up * .1f;
            
            Vector3 tan = _splineContainer.EvaluateTangent(_timer);
            if (tan.sqrMagnitude > 1e-6f)
            {
                var dir = tan.normalized;
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                
                _cooldown -= Time.deltaTime;
                var angle = Vector3.Angle(_prevDir, dir);

                if (_cooldown <= 0f && angle > 45f)
                {
                    _cooldown = 0.2f;

                    _lastCube = null;
                    ClearAllLockedColumns();
                    EventBus.Raise(new OnReCalculateDepthEvent());
                }

                _prevDir = dir;
            }


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

        private void CollectCube()
        {
            if (Physics.Raycast(transform.position, transform.right, out var hit, _cubeLayer))
            {
                if (hit.transform.TryGetComponent(out Cube cube))
                {
                    if (_lastCube != null && cube != _lastCube)
                    {
                        if (!ProductAreaManager.IsLastColumn(GetBoxDirection(), cube.CurrentNode))
                        {
                            EventBus.Raise(new OnReCalculateDepthEvent());
                        }
                    }

                    if (cube != _lastCube)
                    {
                        if (ProductAreaManager.IsFirstColumnToGet(GetBoxDirection(), cube))
                        {
                            _lastCube = cube;
                            
                            foreach (var slot in _cubeSlots)
                            {
                                if (_cubes[slot] == null)
                                {
                                    var index = ProductAreaManager.GetDepthColumnIndex(GetBoxDirection(),
                                        _lastCube.CurrentNode);
                                 
                                    //Debug.Log($"lockedCheck {GetBoxDirection()} -- {index} -- {cube.name} -- {_lockedColumns[GetBoxDirection()].Contains(index)}");
                                    
                                    if (!_lockedColumns[GetBoxDirection()].Contains(index))
                                    {
                                        if (cube.Properties.BoxColor != _boxProperties.BoxColor) return;
                                        
                                        //Debug.Log($"add locked {index} - {GetBoxDirection()} - {cube.name}");
                                        _lockedColumns[GetBoxDirection()].Add(index);
                                        _lastCube.OnSelectedByBox(slot);
                                        _cubes[slot] = _lastCube;
                                        CheckAllSlots();
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void CheckAllSlots()
        {
            foreach (var slot in _cubeSlots)
            {
                if (_cubes[slot] == null)
                {
                    return;
                }
            }

            this.enabled = false;
            _startedMoving = false;
            transform.DORotate(Vector3.one, .5f, RotateMode.FastBeyond360).SetEase(Ease.Linear);
            transform.DOMove(transform.position + Vector3.up * 2f, .5f).OnComplete(() =>
            {
                transform.DOScale(Vector3.zero, .5f).OnComplete(() => { EventBus.Raise(new OnBoxFilledEvent()); });
            });
        }

        private ProductDepthDirection GetBoxDirection()
        {
            if (transform.localEulerAngles.y < -85f)
                return ProductDepthDirection.Up;
            else if (transform.localEulerAngles.y > -2f && transform.localEulerAngles.y < 2f)
                return ProductDepthDirection.Right;
            else if (transform.localEulerAngles.y > 88f && transform.localEulerAngles.y < 92f)
                return ProductDepthDirection.Down;
            else if (transform.localEulerAngles.y > 177 && transform.localEulerAngles.y < 182f)
                return ProductDepthDirection.Left;

            return ProductDepthDirection.Up;
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
                ClearAllLockedColumns();
            });
        }
    }
}