using System.Collections.Generic;
using UnityEngine;

namespace CardCreator
{

    //Builds the flat card body used by the world render modes, submesh 01 is backface
    public static class CardMeshBuilder
    {
        public static Mesh Build(List<Vector2> pOutline, Vector2 pSize, Rect pFrontUV, Rect pBackUV, bool pIsDoubleSided)
        {
            List<int> lTriangles = PolygonTriangulator.Triangulate(pOutline);
            int lCount = pOutline.Count;
            int lVertexCount = pIsDoubleSided ? lCount * 2 : lCount;

            Vector3[] lVertices = new Vector3[lVertexCount];
            Vector3[] lNormals = new Vector3[lVertexCount];
            Vector2[] lUVs = new Vector2[lVertexCount];
            Color32[] lColors = new Color32[lVertexCount];

            for (int i = 0; i < lCount; i++)
            {
                Vector2 lNormalized = pOutline[i] / pSize;

                //Debug.Log(pOutline[0]);

                lVertices[i] = pOutline[i];
                lNormals[i] = Vector3.back;
                lUVs[i] = CardShapeUtility.MapUV(lNormalized, pFrontUV);
                lColors[i] = Color.white;

                if (!pIsDoubleSided)
                    continue;

                lVertices[lCount + i] = pOutline[i];
                lNormals[lCount + i] = Vector3.forward;
                lColors[lCount + i] = Color.white;
                //Mirrored so the back sprite looks correct once the card is flipped
                lUVs[lCount + i] = CardShapeUtility.MapUV(new Vector2(-lNormalized.x, lNormalized.y), pBackUV);
            }

            //Debug.Log(pOutline[0]);

            //The triangulation is CC since triangulator returns CC triangles, this also means the front triangles are always reversed
            int[] lFront = new int[lTriangles.Count];
            int[] lBack = new int[pIsDoubleSided ? lTriangles.Count : 0];

            for (int i = 0; i < lTriangles.Count; i += 3)
            {
                lFront[i] = lTriangles[i];          //Clockwise wind
                lFront[i + 1] = lTriangles[i + 2];
                lFront[i + 2] = lTriangles[i + 1];

                //Debug.Log(lFront[i + 2]);

                if (!pIsDoubleSided)
                    continue;

                lBack[i] = lCount + lTriangles[i];
                lBack[i + 1] = lCount + lTriangles[i + 1];
                lBack[i + 2] = lCount + lTriangles[i + 2];
            }

            //PrintArray(lFront);

            Mesh lMesh = new Mesh { name = "CardBody"};
            lMesh.vertices = lVertices;
            lMesh.normals = lNormals;
            lMesh.uv = lUVs;
            //Sprite shaders multiply by the vertex color and tinting is done by the material
            lMesh.colors32 = lColors;
            lMesh.subMeshCount = pIsDoubleSided ? 2 : 1;
            lMesh.SetTriangles(lFront, 0);

            if (pIsDoubleSided)
                lMesh.SetTriangles(lBack, 1);

            lMesh.RecalculateBounds();

            return lMesh;
        }

        private static void PrintArray(int[] pArray)
        {
            for (int i = 0; i < pArray.Length; i++)
            {
                Debug.Log(pArray[i]);
                Debug.Log("___________________________________________");
            }
        }
    }
}