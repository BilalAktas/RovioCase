using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class BenchAreaManager : MonoBehaviour
    {
        private readonly List<BenchArea> _areas = new();
        
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
            foreach (Transform child in transform)
            {
                var bench = new BenchArea(child.gameObject);
                _areas.Add(bench);
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