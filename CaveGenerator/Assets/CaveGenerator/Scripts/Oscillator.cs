using UnityEngine;

namespace ProceduralCave
{
    // Used for property animations.
    // Provides a value between -1 and +1.
    public class Oscillator
    {
        // Higher values -> smaller increments -> slower animation!
        private float minIncr;
        private float maxIncr;

        private const float PI2 = Mathf.PI / 2f;
        private float value = Mathf.Infinity;
        private float incr;

        public Oscillator(float minIncr, float maxIncr)
        {
            this.minIncr = minIncr;
            this.maxIncr = maxIncr;
        }

        public bool MoveNext()
        {
            if (value < PI2)
            {
                value += incr;
                return true;
            }
            
            Reset();
            return false;
        }

        public void Reset()
        {
            value = -PI2;
            incr = Mathf.PI / Random.Range(minIncr, maxIncr);
        }

        public float Current
        {
            get { return (Mathf.Sin(value) + 1f) / 2f; }
        }
    }
}
