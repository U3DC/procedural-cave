using g3;
using UnityEngine;

namespace ProceduralCave
{
    // Elliptical shape generator for the cave's cross section.

    public class CrossSection
    {
        // Nominal ellipse size, distorted size might be smaller.
        private float width = 16f;
        private float height = 9f;

        private bool animationEnabled = true;

        // Distortion / deformation of the ellipse.
        //
        private float distrStrength = 1f;
        private float distrMargin = 0.1f;
        private float distrMinIncr = 20f;
        private float distrMaxIncr = 40f;

        // Noise / jagged edges.
        // 
        // Range in degrees.
        private float ceilRange = 120f;
        private float ceilMinStrength = 0f;
        private float ceilMaxStrength = 0.5f;
        // Range in degrees.
        private float floorRange = 120f;
        private float floorMinStrength = 0f;
        private float floorMaxStrength = 0.2f;
        private float noiseMinIncr = 100f;
        private float noiseMaxIncr = 200f;

        private float cNoise;
        private float cNoisePrev;
        private float cNoiseNext;
        private Oscillator cNoiseOsc;

        private float fNoise;
        private float fNoisePrev;
        private float fNoiseNext;
        private Oscillator fNoiseOsc;

        private Vector2[] ellipse;
        private Attractor[] attractors;
        private float noiseScale;

        public CrossSection(int nPoints)
        {
            ellipse = new Vector2[nPoints];

            float w = width / 2f;
            float h = height / 2f;
            float arc = 360f / (nPoints - 1);

            for (int i = 0; i < nPoints; i++)
            {
                float t = (360f - i * arc) * Mathf.Deg2Rad;
                ellipse[i] = new Vector3(w * Mathf.Cos(t), h * Mathf.Sin(t));
            }

            if (animationEnabled)
            {
                // Noise should scale with size.
                // noiseScale = 1 for a 16 x 9 ellipse.
                noiseScale = (width * height) / 144f;

                w += width * distrMargin;
                h += height * distrMargin;
                attractors = new Attractor[] {
                    new Attractor(-w,  w,   h,  0f, distrMinIncr, distrMaxIncr),
                    new Attractor(-w,  w,   0f, -h, distrMinIncr, distrMaxIncr),
                    new Attractor(-w,  0f, -h,   h, distrMinIncr, distrMaxIncr),
                    new Attractor(0f,  w,  -h,   h, distrMinIncr, distrMaxIncr),
                    new Attractor(-w,  w,  -h,   h, distrMinIncr, distrMaxIncr)
                };

                cNoiseOsc = new Oscillator(noiseMinIncr, noiseMaxIncr);
                fNoiseOsc = new Oscillator(noiseMinIncr, noiseMaxIncr);
            }
        }

        public bool Update()
        {
            if (animationEnabled)
            {
                foreach (Attractor v in attractors)
                {
                    v.Update();
                }

                if (!cNoiseOsc.MoveNext())
                {
                    cNoisePrev = cNoiseNext;
                    cNoiseNext = Random.Range(ceilMinStrength, ceilMaxStrength);
                }
                cNoise = Mathf.Lerp(cNoisePrev, cNoiseNext, cNoiseOsc.Current);

                if (!fNoiseOsc.MoveNext())
                {
                    fNoisePrev = fNoiseNext;
                    fNoiseNext = Random.Range(floorMinStrength, floorMaxStrength);
                }
                fNoise = Mathf.Lerp(fNoisePrev, fNoiseNext, fNoiseOsc.Current);
            }

            return animationEnabled;
        }

        public Polygon2d GetPolygon(bool update = true)
        {
            if (update)
                Update();

            Polygon2d poly = new Polygon2d();

            for (int i = 0; i < ellipse.Length; i++)
            {
                Vector2 p = ellipse[i];

                if (animationEnabled)
                {
                    foreach (Attractor v in attractors)
                    {
                        p = v.Apply(p);
                    }
                    p = ellipse[i] + (p - ellipse[i]) * distrStrength;

                    float aC = Mathf.Max(0f, ceilRange - Vector2.Angle(Vector2.up, p)) / ceilRange;
                    p.y += (Random.value - 0.5f) * aC * cNoise * noiseScale;

                    float aF = Mathf.Max(0f, floorRange - Vector2.Angle(Vector2.down, p)) / floorRange;
                    p.y += (Random.value - 0.5f) * aF * fNoise * noiseScale;
                }

                poly.AppendVertex(p);
            }
            //Draw(poly);
            return poly;
        }

        private void Draw(Polygon2d poly)
        {
            for (int i = 1; i < poly.VertexCount; i++)
            {
                Debug.DrawLine(new Vector3((float)poly[i - 1].x, 
                                           (float)poly[i - 1].y, 
                                           0f), 
                               new Vector3((float)poly[i].x, 
                                           (float)poly[i].y, 
                                           0f), 
                               Color.cyan);
            }
        }
    }

    // Distorts the ellipse.

    public class Attractor
    {
        private Rect bounds;
        private Vector2 pos;
        private Vector2 posPrev;
        private Vector2 posNext;
        private Oscillator osc;

        public Attractor(float x1, float x2,
                         float y1, float y2,
                         float minIncr, float maxIncr)
        {
            osc = new Oscillator(minIncr, maxIncr);
            bounds = new Rect(x1, y1, x2 - x1, y2 - y1);
            posNext = bounds.center;
        }

        public void Update()
        {
            if (!osc.MoveNext())
            {
                posPrev = posNext;
                posNext.x = Random.Range(bounds.xMin, bounds.xMax);
                posNext.y = Random.Range(bounds.yMin, bounds.yMax);
            }
            pos = Vector3.Lerp(posPrev, posNext, osc.Current);
            //Draw();
        }

        public Vector2 Apply(Vector2 p)
        {
            float d = 1f / (Vector2.Distance(pos, p) + 1f);
            return Vector2.Lerp(p, pos, d);
        }

        private void Draw(bool drawBounds = false)
        {
            float s = 0.1f;
            Vector2 p1 = new Vector2(pos.x - s, pos.y);
            Vector2 p2 = new Vector2(pos.x + s, pos.y);
            Vector2 p3 = new Vector2(pos.x, pos.y - s);
            Vector2 p4 = new Vector2(pos.x, pos.y + s);

            Debug.DrawLine(p1, p2, Color.magenta);
            Debug.DrawLine(p3, p4, Color.magenta);

            if (drawBounds)
            {
                p1 = new Vector2(bounds.xMin, bounds.yMin);
                p2 = new Vector2(bounds.xMax, bounds.yMin);
                p3 = new Vector2(bounds.xMax, bounds.yMax);
                p4 = new Vector2(bounds.xMin, bounds.yMax);

                Debug.DrawLine(p1, p2, Color.blue);
                Debug.DrawLine(p2, p3, Color.blue);
                Debug.DrawLine(p3, p4, Color.blue);
                Debug.DrawLine(p4, p1, Color.blue);
            }
        }
    }
}
