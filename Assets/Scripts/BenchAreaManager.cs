using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class BenchAreaManager : MonoBehaviour
    {
        private readonly List<BenchArea> _areas = new();
        [SerializeField] private GameObject _benchAreaVisualPrefab;
        
        private void Start()
        {
            EventBus.Subscribe<OnBoxEndMoveEvent>(OnBoxEndMove);
            EventBus.Subscribe<OnBoxMovedFromBoxAreaEvent>(OnBoxMovedFromBoxArea);
            CreateBench();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnBoxEndMoveEvent>(OnBoxEndMove);
            EventBus.Unsubscribe<OnBoxMovedFromBoxAreaEvent>(OnBoxMovedFromBoxArea);
        }

        private void OnBoxMovedFromBoxArea(OnBoxMovedFromBoxAreaEvent data)
        {
            foreach (var area in _areas)
            {
                if (area.CurrentBox == data.Box)
                {
                    area.SetBox(null);
                    break;
                }
            }
        }
        
        private void CreateBench()
        {
            var pos = new Vector3(-7, 0, 0);
            for (var i = 0; i < 5; i++)
            {
                var clone = Instantiate(_benchAreaVisualPrefab, transform);
                clone.transform.localPosition = pos;

                var bench = new BenchArea(clone);
                _areas.Add(bench);
                
                pos.x += 4f;
            }
        }

        private void OnBoxEndMove(OnBoxEndMoveEvent data)
        {
            var fail = true;
            foreach (var area in _areas)
            {
                if (!area.CurrentBox)
                {
                    area.SetBox(data.Box);
                    data.Box.MoveToBench(area);
                    fail = false;
                    break;
                }
            }
            
            if(fail) EventBus.Raise(new OnLevelFailedEvent());
        }
    }
}