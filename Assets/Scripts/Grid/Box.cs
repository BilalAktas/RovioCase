using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Splines;

namespace Core
{
    public class Box : MonoBehaviour, IClickable
    {
        [Header("Grid")]
        private BoxGridNode _node;
        [Header("Sfx")]
        [SerializeField] private AudioSource _pickSource;
        [SerializeField] private AudioSource _filledSource;
        [Header("Properties")]
        [SerializeField] private BoxProperties _boxProperties;
        private ColorProperties _colorProperties;
        private MaterialPropertyBlock _propertyBlock;
        private static readonly int _baseColorId = Shader.PropertyToID("_AlbedoColor");
        private Renderer[] _renderers;
        [Header("CollectCube")]
        [SerializeField] private TextMeshProUGUI _cubeAmountText;
        [SerializeField] private Canvas _canvas;
        private int _maxCubeAmount;
        private int _cubeAmount;
        private int _rCubeAmount;
        private RectTransform _rectTransform;
        [Header("MoveOnContainer")]
        private bool _clickableStatus;
        private SplineContainer _splineContainer;
        private float _timer;
        private bool _startedMoving;
        private Vector3 _prevDir = Vector3.forward;
        private float _cooldown;
        private Cube _lastCube;
        private Dictionary<ProductDepthDirection, HashSet<int>> _lockedColumns = new();
        private bool _filled;
        private bool _fillCalled;
        private Sequence _collectSequence;
        private Camera _cam;
        private ProductDepthDirection _currentDir;
        
        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
        }

        public void SetProperties(ColorProperties properties, BoxGridNode node, int maxCubeAmount)
        {
            _colorProperties = properties;
            _node = node;

            _propertyBlock = new MaterialPropertyBlock();

            foreach (var renderer in _renderers)
            {
                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(_baseColorId, _colorProperties.BoxColorMaterialColor);
                renderer.SetPropertyBlock(_propertyBlock);
            }
            
            _splineContainer = FindFirstObjectByType<SplineContainer>();
            ClearAllLockedColumns();

            _cam = Camera.main;
            _canvas.worldCamera = _cam;
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
            
            _propertyBlock = new MaterialPropertyBlock();
            if (!state)
            {
                var c = _cubeAmountText.color;
                c.a = .35f;
                _cubeAmountText.color = c;
                
                foreach (var renderer in _renderers)
                {
                    var newColor = Helpers.AdjustBrightness(_colorProperties.BoxColorMaterialColor, _colorProperties.AdjustBrightness);
                    newColor.a = 1f;
                    
                    renderer.GetPropertyBlock(_propertyBlock);
                    _propertyBlock.SetColor(_baseColorId, newColor);
                    renderer.SetPropertyBlock(_propertyBlock);
                }
            }
            else
            {
                var c = _cubeAmountText.color;
                c.a = 1f;
                _cubeAmountText.color = c;
                
                foreach (var renderer in _renderers)
                {
                    var newColor = Helpers.AdjustBrightness(_colorProperties.BoxColorMaterialColor, 0);
                    newColor.a = 1f;
                    
                    renderer.GetPropertyBlock(_propertyBlock);
                    _propertyBlock.SetColor(_baseColorId, newColor);
                    renderer.SetPropertyBlock(_propertyBlock);
                }
            }
        }

        private void JumpOnContainer()
        {
            _pickSource.Play();
            
            _timer = 0;
            transform.DOComplete();

            Vector3 pos = _splineContainer.EvaluatePosition(0f);
            pos += Vector3.up * 3f;
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
            _timer += _boxProperties.Speed * Time.deltaTime;

            transform.position = _splineContainer.EvaluatePosition(_timer);
            transform.position += Vector3.up * 3f;
            
            Vector3 tan = _splineContainer.EvaluateTangent(_timer);
            if (tan.sqrMagnitude > 1e-6f)
            {
                var dir = tan.normalized;
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                
                _cooldown -= Time.deltaTime;
                var angle = Vector3.Angle(_prevDir, dir);

                _rectTransform.rotation = _cam.transform.rotation;
                
                if (_cooldown <= 0f && angle > _boxProperties.TurnAngle)
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
            var currentDir = GetBoxDirection();
            
            if (Physics.Raycast(transform.position, transform.right, out var hit, Mathf.Infinity,_boxProperties.CubeLayer))
            {
                if (hit.transform.TryGetComponent(out Cube cube))
                {
                    if (_lastCube != null && cube != _lastCube)
                    {
                        if (!ProductAreaManager.IsLastColumn(currentDir, cube.CurrentNode))
                        {
                            EventBus.Raise(new OnReCalculateDepthEvent());
                        }
                    }

                    if (cube != _lastCube)
                    {
                        //Debug.Log($"lockedCheck {currentDir} -- {cube.name}  - {cube.Properties.BoxColor}");
                        if (ProductAreaManager.IsFirstColumnToGet(currentDir, cube))
                        {
                            _lastCube = cube;
                            
                            var index = ProductAreaManager.GetDepthColumnIndex(currentDir,
                                _lastCube.CurrentNode);
                                 
                            //Debug.Log($"lockedCheck {currentDir} -- {index} -- {cube.name} -- {_lockedColumns[currentDir].Contains(index)} - {cube.Properties.BoxColor}");
                            
                            if (!_lockedColumns[currentDir].Contains(index))
                            {
                                if (cube.Properties.BoxColor != _colorProperties.BoxColor) return;
                                
                                //Debug.Log($"add locked {index} - {currentDir} - {cube.name}");
                                _rCubeAmount++;
                                if (_rCubeAmount >= _maxCubeAmount)
                                {
                                    _filled = true;    
                                }
                                        
                                _lockedColumns[currentDir].Add(index);
                                _lastCube.OnSelectedByBox(transform);
                                       
                            }
                        }
                    }
                }
            }
        }
        
        
        private void OnCollectCubeEffect()
        {
            //transform.DOComplete();
            _collectSequence.Complete();
            _collectSequence = DOTween.Sequence();

            _collectSequence.Append(transform.GetChild(0).DOScale(new Vector3(1.15f, 1f, 1.1f), 0.08f)
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
        
        public void OnCubeMoveEnd(Cube cube)
        {
            _cubeAmount++;

            var i = _cubeAmount - 1;
            var layer = i / (5 * 5);
            var indexInLayer = i % (5 * 5);
            var zIndex = indexInLayer / 5;
            var xIndex = indexInLayer % 5;

            var newPos = new Vector3(-.8f, -.5f, 1f) + new Vector3(
                xIndex * .4f,
                layer * .65f,
                zIndex * -.5f
            );

            cube.transform.localPosition = newPos;
            
            EventBus.Raise(new OnCollectCubeEvent(){Cube = cube});
            _cubeAmountText.text = $"{_cubeAmount}/{_maxCubeAmount}";
            OnCollectCubeEffect();
        }

        
        private ProductDepthDirection GetBoxDirection()
        {
            //Debug.Log($"dirY {transform.localEulerAngles.y}");
            if (transform.localEulerAngles.y > 250 && transform.localEulerAngles.y < 300)
                _currentDir = ProductDepthDirection.Up;
            else if (transform.localEulerAngles.y > 300 && transform.localEulerAngles.y < 320f)
                _currentDir = ProductDepthDirection.Right;
            else if (transform.localEulerAngles.y > 70f && transform.localEulerAngles.y < 130f)
                _currentDir = ProductDepthDirection.Down;
            else if (transform.localEulerAngles.y > 140 && transform.localEulerAngles.y < 185f)
                _currentDir = ProductDepthDirection.Left;

            return _currentDir;
        }

        public void MoveToBench(BenchArea area)
        {
            transform.DOComplete();
            var midPoint = (transform.position + area.Visual.transform.position) / 2f + Vector3.up * 5f;

            var poses = new List<Vector3>()
            {
                midPoint,
                area.Visual.transform.position + Vector3.up * 1f
            };

            transform.DOPath(poses.ToArray(), .5f, PathType.CatmullRom).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                ClearAllLockedColumns();
            });
        }
    }
}