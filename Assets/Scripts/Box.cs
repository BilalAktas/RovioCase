using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Splines;

namespace Core
{
    public class Box : MonoBehaviour, IClickable
    {
        [SerializeField] private AudioSource _pickSource;
        [SerializeField] private AudioSource _filledSource;
        
        private BoxProperties _boxProperties;
        private BoxGridNode _node;
        private bool _clickableStatus;
        [SerializeField] private Canvas _canvas;
        private RectTransform _rectTransform;

        private int _maxCubeAmount;
        private int _cubeAmount;
        private int _rCubeAmount;
        [SerializeField] private TextMeshProUGUI _cubeAmountText;
        
        [Header("MoveOnContainer")]
        private SplineContainer _splineContainer;
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

        private bool _filled;
        private bool _fillCalled;
        private Sequence _collectSequence;


        public void SetProperties(BoxProperties properties, BoxGridNode node, int maxCubeAmount)
        {
            _boxProperties = properties;
            _node = node;
            foreach (var renderer in GetComponentsInChildren<Renderer>())
                renderer.material = _boxProperties.ColorMaterial;

            _splineContainer = FindFirstObjectByType<SplineContainer>();
            foreach (var slot in _cubeSlots)
                _cubes[slot] = null;

            ClearAllLockedColumns();

            _canvas.worldCamera = Camera.main;
            _rectTransform = _canvas.GetComponent<RectTransform>();

            _maxCubeAmount = maxCubeAmount;
               _cubeAmountText.text = $"{_cubeAmount}/{_maxCubeAmount}";
               
            EventBus.Subscribe<OnLevelSpawnedEvent>(OnLevelSpawned);

            transform.DOComplete();
            transform.localScale = Vector3.one;
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnLevelSpawnedEvent>(OnLevelSpawned);
        }

        private void OnLevelSpawned(OnLevelSpawnedEvent data)
        {
            _cubeAmount = 0;
            _rCubeAmount = 0;
            _timer = 0;
            _filled = false;
            _fillCalled = false;
            _collectSequence.Complete();
            foreach (var slot in _cubeSlots)
            {
                var c = _cubes[slot];
                if (c)
                {
                    _cubes[slot] = null;
                    Destroy(c.gameObject);    
                }
            }
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
            
            if (!state)
            {
                var c = _cubeAmountText.color;
                c.a = .35f;
                _cubeAmountText.color = c;
                
                foreach (var renderer in GetComponentsInChildren<Renderer>())
                {
                    renderer.material = _boxProperties.ColorMaterial;
                    
                    Color.RGBToHSV(renderer.material.color, out float h, out float s, out float v);
                    v -= .75f;
                    v = Mathf.Clamp01(v);

                    var co = Color.HSVToRGB(h, s, v);
                    co.a = 1f;

                    renderer.material.color = co;
                }
            }
            else
            {
                var c = _cubeAmountText.color;
                c.a = 1f;
                _cubeAmountText.color = c;
                foreach (var renderer in GetComponentsInChildren<Renderer>())
                {
                    renderer.material = _boxProperties.ColorMaterial;
                }
            }
        }

        private void JumpOnContainer()
        {
            _pickSource.Play();
            
            _timer = 0;
            transform.DOComplete();

            Vector3 pos = _splineContainer.EvaluatePosition(0f);
            pos += Vector3.up * .85f;
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
            if (GameManager.GameState == GameState.Idle) return;
            if (!_startedMoving) return;

            Move();
            CollectCube();
        }
     
        private void Move()
        {
            _timer += _speed * Time.deltaTime;

            transform.position = _splineContainer.EvaluatePosition(_timer);
            transform.position += Vector3.up;
            
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

                    switch (GetBoxDirection())
                    {
                        case ProductDepthDirection.Up: _rectTransform.localRotation = Quaternion.Euler(50f, 90, 0);break;
                        case ProductDepthDirection.Right: _rectTransform.localRotation = Quaternion.Euler(50f, 0, 0);break;
                        case ProductDepthDirection.Down: _rectTransform.localRotation = Quaternion.Euler(50f, 270, 0);break;
                        case ProductDepthDirection.Left: _rectTransform.localRotation = Quaternion.Euler(50f, 180, 0);break;
                    }
                }

                _prevDir = dir;
            }


            if (_timer >= 1)
            {
                _startedMoving = false;
                if (!_filled)
                {
                    var e = new OnBoxEndMoveEvent()
                    {
                        Box = this
                    };
                    EventBus.Raise(e);    
                }
            }
        }

        private void CollectCube()
        {
            if (_filled) return;
            
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
                                        _rCubeAmount++;
                                        if (_rCubeAmount >= _maxCubeAmount)
                                        {
                                            _filled = true;    
                                        }
                                        
                                        _lockedColumns[GetBoxDirection()].Add(index);
                                        _lastCube.OnSelectedByBox(slot);
                                        _cubes[slot] = _lastCube;
                                        
                                       
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        
        private void OnCollectCube()
        {
            //transform.DOComplete();
            _collectSequence.Complete();
            _collectSequence = DOTween.Sequence();

            _collectSequence.Append(transform.GetChild(0).DOScale(new Vector3(1.15f, 0.65f, 1.1f), 0.08f)
                .SetEase(Ease.OutQuad));
            
            _collectSequence.Append(transform.GetChild(0).DOScale(Vector3.one, 0.3f)
                .SetEase(Ease.OutElastic));
            
            _collectSequence.OnComplete(() =>
            {
                if (!_filled) return;
                if (_fillCalled) return;

                _filledSource.Play();
                
                _fillCalled = true;
                transform.DOComplete();

                DOVirtual.DelayedCall(.1f, () =>
                {
                    var sequence = DOTween.Sequence();
                
                    sequence.Append(transform.GetChild(0).DOScale(new Vector3(1.5f, 1.5f, 1.5f), .2f)
                        .SetEase(Ease.Linear));
            
                    // sequence.Append(transform.GetChild(0).DOScale(Vector3.one, 0.3f)
                    //     .SetEase(Ease.OutElastic));

                    sequence.Append(
                        transform.GetChild(0).DORotate(new Vector3(0, 360f * 3, 0), 0.5f, RotateMode.FastBeyond360)
                            .SetEase(Ease.InOutSine)
                    );

                    sequence.Append(transform.DOScale(Vector3.zero, .5f)).SetEase(Ease.Linear);
                    //
                    sequence.OnComplete(() =>
                    {
                        _startedMoving = false;
                        this.enabled = false;
                        EventBus.Raise(new OnBoxFilledEvent());
                    });
                });
            });

        }
        public void CheckFill()
        {
            _cubeAmount++;
            _cubeAmountText.text = $"{_cubeAmount}/{_maxCubeAmount}";
            OnCollectCube();
        }

        private ProductDepthDirection GetBoxDirection()
        {
            if (transform.localEulerAngles.y < -85f)
                return ProductDepthDirection.Up;
            else if (transform.localEulerAngles.y > 355f && transform.localEulerAngles.y < 362f)
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