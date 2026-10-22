using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using UnityEngine.Sprites;

namespace CardCreator
{
    //Builds card outlines from shape settings
    public static class CardShapeUtility
    {

        //card outline with rounded edges
        public static List<Vector2> BuildOutline(CardShapeSettings pShape)
        {
            List<Vector2> lPoints = BuildCorners(pShape);

            if (SupportsRounding(pShape))
                lPoints = RoundCorners(lPoints, pShape.cornerRadius, pShape.cornerSegments);

            return lPoints;
        }

        //Outline is built before rounding, it's used as starting point when converting from preset topolygon
        public static List<Vector2> BuildCorners(CardShapeSettings pShape)
        {
            List<Vector2> lPoints;

            //every shape is built normalized then scaled to card size
            if (pShape.source == CardShapeSource.Polygon && pShape.points.Count >= 3)
                lPoints = new List<Vector2>(pShape.points);
            else if (pShape.source == CardShapeSource.Sprite && pShape.sprite != null)
                lPoints = GetSpriteOutline(pShape.sprite);
            else
                lPoints = BuildPreset(pShape);

            //Debug.Log(lPoints[0]);

            for (int i = 0; i < lPoints.Count; i++)
                lPoints[i] = Vector2.Scale(lPoints[i], pShape.size);

            RemoveDuplicates(lPoints);


            //Debug.Log(lPoints[0]);

            if (PolygonTriangulator.GetSignedArea(lPoints) < 0f)
                lPoints.Reverse();

            return lPoints;
        }

        public static bool SupportsRounding(CardShapeSettings pShape)
        {
            switch (pShape.source)
            {
                case CardShapeSource.Polygon:
                    return true;
                case CardShapeSource.Sprite:
                    return false;
                default:
                    return pShape.preset != CardShapePreset.Ellipse;
            }
        }

        //Replaces every corner by a circular arc (radius shrinks on corners edges too short)
        public static List<Vector2> RoundCorners(List<Vector2> pPoints, float pRadius, int pSegments)
        {
            if (pRadius <= 0f)
                return pPoints;

            List<Vector2> lResult = new List<Vector2>();
            int lCount = pPoints.Count;

            for (int i = 0; i < lCount; i++)
            {
                Vector2 lCorner = pPoints[i];
                Vector2 lToPrevious = pPoints[(i + lCount - 1) % lCount] - lCorner;
                Vector2 lToNext = pPoints[(i + 1) % lCount] - lCorner;
                float lHalfAngle = Vector2.Angle(lToPrevious, lToNext) * 0.5f * Mathf.Deg2Rad;

                //spikes/flat corners kept
                if (lHalfAngle < 0.01f || lHalfAngle > Mathf.PI * 0.5f - 0.01f)
                {
                    lResult.Add(lCorner);
                    continue;
                }

                //Dist between the corner and the points where the arc touches both edges
                float lTangentLength = Mathf.Min(pRadius / Mathf.Tan(lHalfAngle), lToPrevious.magnitude * 0.5f, lToNext.magnitude * 0.5f);
                float lRadius = lTangentLength * Mathf.Tan(lHalfAngle);

                Vector2 lBisector = (lToPrevious.normalized + lToNext.normalized).normalized;

                //Debug.Log(lBisector);
                Vector2 lCenter = lCorner + lBisector * (lRadius / Mathf.Sin(lHalfAngle));

                Vector2 lStart = lCorner + lToPrevious.normalized * lTangentLength - lCenter;
                Vector2 lEnd = lCorner + lToNext.normalized * lTangentLength - lCenter;

                float lStartAngle = Mathf.Atan2(lStart.y, lStart.x);
                float lSweep = Mathf.DeltaAngle(lStartAngle * Mathf.Rad2Deg, Mathf.Atan2(lEnd.y, lEnd.x) * Mathf.Rad2Deg) * Mathf.Deg2Rad;

                for (int j = 0; j <= pSegments; j++)
                {
                    float lAngle = lStartAngle + lSweep * j / pSegments;
                    lResult.Add(lCenter + new Vector2(Mathf.Cos(lAngle), Mathf.Sin(lAngle)) * lRadius);
                }
            }

            //Debug.Log(lResult.Count);

            RemoveDuplicates(lResult);

            //Debug.Log(lResult.Count);
            return lResult;
        }

        //largest physics shape of the sprite normalized to the sprite rect (falls back to the full rect if sprite has no generated physics shape)
        public static List<Vector2> GetSpriteOutline(Sprite pSprite)
        {
            List<Vector2> lBest = new List<Vector2>();
            List<Vector2> lPath = new List<Vector2>();
            float lBestArea = 0f;

            for (int i = 0; i < pSprite.GetPhysicsShapeCount(); i++)
            {
                pSprite.GetPhysicsShape(i, lPath);
                float lArea = Mathf.Abs(PolygonTriangulator.GetSignedArea(lPath));

                if (lArea > lBestArea)
                {
                    lBestArea = lArea;
                    lBest = new List<Vector2>(lPath);
                }
            }

            if (lBest.Count < 3)
                return BuildRect();

            //Physics shape points are in local space, relative to the PIVOT (0,0 = top left)
            Vector2 lSize = pSprite.rect.size / pSprite.pixelsPerUnit;
            Vector2 lCenter = (pSprite.rect.size * 0.5f - pSprite.pivot) / pSprite.pixelsPerUnit;

            for (int i = 0; i < lBest.Count; i++)
                lBest[i] = (lBest[i] - lCenter) / lSize;

            //Debug.Log(lBest.Count);
            //Debug.Log(lBest[0]);

            return lBest;
        }

        //Part of the sprite texture covered by the full sprite rect
        public static Rect GetSpriteUVRect(Sprite pSprite)
        {
            if (pSprite == null)
                return new Rect(0f, 0f, 1f, 1f);

            //tight sprites are trimmed nad transparent padding is added back
            Vector4 lOuterUV = DataUtility.GetOuterUV(pSprite);
            Vector4 lPadding = DataUtility.GetPadding(pSprite);
            Vector2 lTrimmedSize = pSprite.rect.size - new Vector2(lPadding.x + lPadding.z, lPadding.y + lPadding.w);
            Vector2 lUVPerPixel = new Vector2((lOuterUV.z - lOuterUV.x) / lTrimmedSize.x, (lOuterUV.w - lOuterUV.y) / lTrimmedSize.y);

            return Rect.MinMaxRect(
                lOuterUV.x - lPadding.x * lUVPerPixel.x,
                lOuterUV.y - lPadding.y * lUVPerPixel.y,
                lOuterUV.z + lPadding.z * lUVPerPixel.x,
                lOuterUV.w + lPadding.w * lUVPerPixel.y);
        }

        //Maps a normalized point of the card (-0.5 to 0.5) unto a uv rect
        public static Vector2 MapUV(Vector2 pNormalizedPoint, Rect pUVRect)
        {
            return pUVRect.min + Vector2.Scale(pNormalizedPoint + Vector2.one * 0.5f, pUVRect.size);
        }

        public static List<Vector2> Normalize(List<Vector2> pPoints, Vector2 pSize)
        {
            List<Vector2> lResult = new List<Vector2>(pPoints.Count);

            foreach (Vector2 lPoint in pPoints)
                lResult.Add(lPoint / pSize);

            return lResult;
        }

        private static List<Vector2> BuildPreset(CardShapeSettings pShape)
        {
            switch (pShape.preset)
            {
                case CardShapePreset.Ellipse:
                    return BuildRegularPolygon(pShape.ellipseSegments, 0f);
                case CardShapePreset.RegularPolygon:
                    return BuildRegularPolygon(pShape.polygonSides, 90f + pShape.polygonRotation);
                default:
                    return BuildRect();
            }
        }

        private static List<Vector2> BuildRect() //Guess what this one does (wrong it builds a square :p )
        {
            return new List<Vector2>
            {
                new Vector2(-0.5f, -0.5f),
                new Vector2(0.5f, -0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-0.5f, 0.5f)
            };
        }

        //Regular polygon stretched to fill normalized card
        private static List<Vector2> BuildRegularPolygon(int pSides, float pStartAngle)
        {
            List<Vector2> lPoints = new List<Vector2>(pSides);
            Vector2 lMin = Vector2.positiveInfinity;
            Vector2 lMax = Vector2.negativeInfinity;

            for (int i = 0; i < pSides; i++)
            {
                float lAngle = pStartAngle * Mathf.Deg2Rad + i * Mathf.PI * 2f / pSides;
                Vector2 lPoint = new Vector2(Mathf.Cos(lAngle), Mathf.Sin(lAngle));

                lPoints.Add(lPoint);
                lMin = Vector2.Min(lMin, lPoint);
                lMax = Vector2.Max(lMax, lPoint);
            }

            Vector2 lCenter = (lMin + lMax) * 0.5f;
            Vector2 lSize = lMax - lMin;

            for (int i = 0; i < pSides; i++)
                lPoints[i] = (lPoints[i] - lCenter) / lSize;

            return lPoints;
        }

        private static void RemoveDuplicates(List<Vector2> pPoints)
        {
            for (int i = pPoints.Count - 1; i >= 0 && pPoints.Count > 1; i--)
            {
                if ((pPoints[i] - pPoints[(i + 1) % pPoints.Count]).sqrMagnitude < 0.0000000001f)
                    pPoints.RemoveAt(i);
                //Debug.Log("removed duplicate");
            }
        }
    }
}
