using UnityEngine;

namespace Core
{
    public class BenchArea
    {
        public Box CurrentBox { get; private set; }
        public GameObject Visual { get; private set; }

        public BenchArea(GameObject visual)
        {
            Visual = visual;
        }
        
        public void SetBox(Box box) => CurrentBox = box;
        
    }
}