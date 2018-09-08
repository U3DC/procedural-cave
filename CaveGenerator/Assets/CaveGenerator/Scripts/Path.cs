using UnityEngine;

namespace ProceduralCave
{
    // Random path generator.
    // Could be replaced with any Vector3 list, perhaps based on some level design.

    public class Path
    {
        private bool curvatureEnabled = true;

        private float maxHorizontalCurve = 1f;
        private float hCurveMinIncr = 50f;
        private float hCurveMaxIncr = 100f;

        private float maxVerticalCurve = 0.1f;
        private float vCurveMinIncr = 50f;
        private float vCurveMaxIncr = 100f;

        private float hCurve;
        private float hCurvePrev;
        private float hCurveNext;
        private Oscillator hCurveOsc;

        private float vCurve;
        private float vCurvePrev;
        private float vCurveNext;
        private Oscillator vCurveOsc;

        private Vector3 position;
        private Vector3 direction;
        private Vector3 step;

        public Path(float ringWidth)
        {
            position = new Vector3(0f, 0f, ringWidth * -2f);
            direction = Vector3.forward;
            step = direction * ringWidth;

            if (curvatureEnabled)
            {
                hCurveOsc = new Oscillator(hCurveMinIncr, hCurveMaxIncr);
                vCurveOsc = new Oscillator(vCurveMinIncr, vCurveMaxIncr);
            }
        }

        public Vector3 GetNextPosition()
        {
            if (curvatureEnabled)
            {
                if (!hCurveOsc.MoveNext())
                {
                    hCurvePrev = hCurveNext;
                    hCurveNext = Random.Range(-maxHorizontalCurve, maxHorizontalCurve);
                }
                hCurve = Mathf.Lerp(hCurvePrev, hCurveNext, hCurveOsc.Current);

                if (!vCurveOsc.MoveNext())
                {
                    vCurvePrev = vCurveNext;
                    vCurveNext = Random.Range(-maxVerticalCurve, maxVerticalCurve);
                }
                vCurve = Mathf.Lerp(vCurvePrev, vCurveNext, vCurveOsc.Current);

                Vector3 pXZ = new Vector3(position.x, 0f, position.z);
                Vector3 next = pXZ + Quaternion.Euler(0f, hCurve, 0f) * direction * step.z;

                direction = (next - pXZ).normalized;
                position = new Vector3(next.x, position.y + vCurve, next.z);
            }
            else
            {
                position += step;
            }

            //Debug.DrawRay(position, direction, Color.white, 100f);
            return position;
        }
    }
}
