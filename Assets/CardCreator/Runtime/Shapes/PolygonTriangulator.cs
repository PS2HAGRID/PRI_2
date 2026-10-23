using System.Collections.Generic;
using UnityEngine;

namespace CardCreator
{
    //polygon helpers, polygons are non-convex and simple
    public static class PolygonTriangulator
    {
        //positive when polygon is counter-clockwise
        public static float GetSignedArea(IList<Vector2> pPoints)
        {
            float lArea = 0f;

            for (int i = 0; i < pPoints.Count; i++)
            {
                Vector2 lA = pPoints[i];
                Vector2 lB = pPoints[(i + 1) % pPoints.Count];
                lArea += lA.x * lB.y - lB.x * lA.y;
            }

            return lArea * 0.5f;
        }

        //Ear clipping triangulation
        //Returns counter-clockwise triangles that index into pPoints
        public static List<int> Triangulate(IList<Vector2> pPoints)
        {
            List<int> lTriangles = new List<int>();

            if (pPoints.Count < 3)
                return lTriangles;

            List<int> lRemaining = new List<int>(pPoints.Count);
            for (int i = 0; i < pPoints.Count; i++)
                lRemaining.Add(i);

            //Debug.Log(GetSignedArea(pPoints));

            if (GetSignedArea(pPoints) < 0f)
                lRemaining.Reverse();

            int lIndex = 0;
            int lFailures = 0;

            while (lRemaining.Count > 3)
            {
                int lCount = lRemaining.Count;
                int lPrevious = lRemaining[(lIndex + lCount - 1) % lCount];
                int lCurrent = lRemaining[lIndex];
                int lNext = lRemaining[(lIndex + 1) % lCount];

                Vector2 lIn = pPoints[lCurrent] - pPoints[lPrevious];
                Vector2 lOut = pPoints[lNext] - pPoints[lCurrent];
                float lCross = Cross(lIn, lOut);

                if (Mathf.Abs(lCross) <= 1e-6f * lIn.magnitude * lOut.magnitude)
                {
                    //pruning of collinear or duplicated vertices
                    lRemaining.RemoveAt(lIndex);
                    lFailures = 0;
                }
                else if (lCross > 0f && !ContainsAnyVertex(pPoints, lRemaining, lPrevious, lCurrent, lNext))
                {
                    lTriangles.Add(lPrevious);
                    lTriangles.Add(lCurrent);
                    lTriangles.Add(lNext);
                    lRemaining.RemoveAt(lIndex);
                    lFailures = 0;
                }
                else
                {
                    lIndex++;

                    //Self intersect check
                    if (++lFailures > lCount)
                        break;
                }

                lIndex %= lRemaining.Count;

                //Debug.Log(lIndex);
            }

            if (lRemaining.Count == 3)
            {
                lTriangles.Add(lRemaining[0]);
                lTriangles.Add(lRemaining[1]);
                lTriangles.Add(lRemaining[2]);
            }

            return lTriangles;
        }

        //Even/odd point in polygon test
        public static bool ContainsPoint(IList<Vector2> pPolygon, Vector2 pPoint)
        {
            bool lIsInside = false;

            for (int i = 0, j = pPolygon.Count - 1; i < pPolygon.Count; j = i++)
            {
                Vector2 lA = pPolygon[i];
                Vector2 lB = pPolygon[j];

                if ((lA.y > pPoint.y) != (lB.y > pPoint.y) &&
                    pPoint.x < (lB.x - lA.x) * (pPoint.y - lA.y) / (lB.y - lA.y) + lA.x) 
                    lIsInside = !lIsInside; //Odd/even intersect test, odd = intersect, even = no intersect
            }

            return lIsInside;
        }

        public static bool IsSelfIntersecting(IList<Vector2> pPolygon)
        {
            int lCount = pPolygon.Count;

            for (int i = 0; i < lCount; i++)
            {
                //Neighbouring edges share a vertex so only non-neighbouring edges are tested
                for (int j = i + 2; j < lCount; j++)
                {
                    if (i == 0 && j == lCount - 1)
                        continue;

                    if (SegmentsIntersect(pPolygon[i], pPolygon[(i + 1) % lCount], pPolygon[j], pPolygon[(j + 1) % lCount]))
                        return true;
                }
            }

            return false;
        }

        private static bool ContainsAnyVertex(IList<Vector2> pPoints, List<int> pRemaining, int pA, int pB, int pC)
        {
            Vector2 lA = pPoints[pA];
            Vector2 lB = pPoints[pB];
            Vector2 lC = pPoints[pC];

            foreach (int lIndex in pRemaining)
            {
                if (lIndex == pA || lIndex == pB || lIndex == pC)
                    continue;

                Vector2 lPoint = pPoints[lIndex];

                //Debug.Log(lPoint);

                //vertex sitting on corner is ignored
                if (lPoint == lA || lPoint == lB || lPoint == lC)
                    continue;

                if (Cross(lB - lA, lPoint - lA) >= 0f && Cross(lC - lB, lPoint - lB) >= 0f && Cross(lA - lC, lPoint - lC) >= 0f)
                    return true;
            }

            return false;
        }

        private static bool SegmentsIntersect(Vector2 pA, Vector2 pB, Vector2 pC, Vector2 pD)
        {
            float lD1 = Cross(pB - pA, pC - pA);
            float lD2 = Cross(pB - pA, pD - pA);
            float lD3 = Cross(pD - pC, pA - pC);
            float lD4 = Cross(pD - pC, pB - pC);

            bool lResult = lD1 * lD2 < 0f && lD3 * lD4 < 0f;

            //Debug.Log(lResult);

            return lResult;
        }

        private static float Cross(Vector2 pA, Vector2 pB)
        {
            return pA.x * pB.y - pA.y * pB.x;
        }
    }
}
