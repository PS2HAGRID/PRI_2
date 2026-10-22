using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CardCreator
{

    //main draw card class
    [RequireComponent(typeof(CanvasRenderer))]
    public class CardShapeGraphic : MaskableGraphic, ICanvasRaycastFilter
    {
        //outline normalized to rect space (-0.5/0.5)
        [SerializeField] private List<Vector2> _outline = new List<Vector2>();
        [SerializeField] private Sprite _sprite;

        private List<int> _triangles;

        public override Texture mainTexture => _sprite != null ? _sprite.texture : s_WhiteTexture;

        public Sprite sprite
        {
            get => _sprite;
            set
            {
                _sprite = value;
                SetAllDirty();
            }
        }

        public void SetOutline(List<Vector2> pOutline)
        {
            _outline = new List<Vector2>(pOutline);
            _triangles = null;
            SetVerticesDirty();
        }

        //drawing the triangles of the card, watch out for vertex winding order
        protected override void OnPopulateMesh(VertexHelper pVertexHelper)
        {
            pVertexHelper.Clear();

            if (_outline.Count < 3)
                return;

            _triangles ??= PolygonTriangulator.Triangulate(_outline);

            Rect lRect = GetPixelAdjustedRect();
            Rect lUVRect = CardShapeUtility.GetSpriteUVRect(_sprite);

            foreach (Vector2 lPoint in _outline)
                pVertexHelper.AddVert(lRect.center + Vector2.Scale(lPoint, lRect.size), color, CardShapeUtility.MapUV(lPoint, lUVRect));

            for (int i = 0; i < _triangles.Count; i += 3)
                pVertexHelper.AddTriangle(_triangles[i], _triangles[i + 1], _triangles[i + 2]);
        }

        public bool IsRaycastLocationValid(Vector2 pScreenPoint, Camera pEventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, pScreenPoint, pEventCamera, out Vector2 lLocalPoint))
                return false;

            Rect lRect = rectTransform.rect;
            return PolygonTriangulator.ContainsPoint(_outline, (lLocalPoint - lRect.center) / lRect.size);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            _triangles = null;
        }
#endif
    }
}
