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
            CreateBench();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OnBoxEndMoveEvent>(OnBoxEndMove);
        }

        private void CreateBench()
        {
            var pos = new Vector3(-6, 0, 0);
            for (var i = 0; i < 5; i++)
            {
                var clone = Instantiate(_benchAreaVisualPrefab, transform);
                clone.transform.localPosition = pos;

                var bench = new BenchArea(clone);
                _areas.Add(bench);
                
                pos.x += 3f;
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