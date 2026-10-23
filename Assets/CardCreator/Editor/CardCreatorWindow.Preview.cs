using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CardCreator.Editor
{
    //Left panel of the card creator, renderer with selection, move, resize and shape editing
    public partial class CardCreatorWindow
    {
        private enum PreviewDrag
        {
            None,
            Pan,
            Move,
            Resize,
            ShapePoint
        }

        private static readonly Color _SelectionColor = new Color(0.3f, 0.75f, 1f);
        private static readonly Color _ShapeColor = new Color(1f, 0.7f, 0.2f);

        [SerializeField] private string _SelectedElementId;
        [SerializeField] private float _Zoom = 1f;
        [SerializeField] private Vector2 _Pan;
        [SerializeField] private bool _showBack;
        [SerializeField] private bool _isEditingShape;

        private CardPreviewRenderer _Preview;
        private bool _isPreviewDirty;
        private GUIStyle _placeholderStyle;

        private PreviewDrag _drag;
        //Corner index when resizing, point index when editing shape
        private int _dragIndex;
        //Fixed corner when resizing, grab offset when moving
        private Vector2 _dragAnchor;
        private Vector2 _dragStartSize;
        private int _dragUndoGroup;

        private void DrawPreview(Rect pRect)
        {
            Rect lToolbarRect = new Rect(pRect.x, pRect.y, pRect.width, EditorStyles.toolbar.fixedHeight);
            Rect lViewRect = new Rect(pRect.x, lToolbarRect.yMax, pRect.width, pRect.height - lToolbarRect.height);

            DrawPreviewToolbar(lToolbarRect);

            GUI.BeginClip(lViewRect);
            Rect lView = new Rect(Vector2.zero, lViewRect.size);

            HandlePreviewInput(lView);

            if (Event.current.type == EventType.Repaint)
            {
                if (_isPreviewDirty)
                {
                    _Preview.Rebuild(_Template, _showBack);
                    _isPreviewDirty = false;
                }

                GUI.DrawTexture(lView, _Preview.Render(lView, _Pan, GetOrthographicSize(lView)), ScaleMode.StretchToFill, false);
                DrawOverlay(lView);
            }

            GUI.EndClip();
        }

        private void DrawPreviewToolbar(Rect pRect)
        {
            GUILayout.BeginArea(pRect, EditorStyles.toolbar);
            GUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            _showBack = GUILayout.Toggle(_showBack, "Show Back", EditorStyles.toolbarButton);

            if (EditorGUI.EndChangeCheck())
                _isPreviewDirty = true;

            if (_Template.shape.source == CardShapeSource.Polygon)
                _isEditingShape = GUILayout.Toggle(_isEditingShape, "Edit Shape", EditorStyles.toolbarButton);

            GUILayout.FlexibleSpace();
            GUILayout.Label(Mathf.RoundToInt(_Zoom * 100f) + "%", EditorStyles.miniLabel);

            if (GUILayout.Button("Frame", EditorStyles.toolbarButton))
                FramePreview();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void FramePreview()
        {
            _Zoom = 1f;
            _Pan = Vector2.zero;
        }

        //Quick bool so toggles lives through changes
        private bool IsEditingShape()
        {
            return _isEditingShape && _Template.shape.source == CardShapeSource.Polygon;
        }

        #region Input

        private void HandlePreviewInput(Rect pView)
        {
            Event lEvent = Event.current;
            int lControlId = GUIUtility.GetControlID(FocusType.Passive);

            switch (lEvent.GetTypeForControl(lControlId))
            {
                case EventType.ScrollWheel:
                    if (!pView.Contains(lEvent.mousePosition))
                        return;

                    //Zoom on mouse pos
                    Vector2 lBefore = ViewToCard(pView, lEvent.mousePosition);
                    _Zoom = Mathf.Clamp(_Zoom * (lEvent.delta.y > 0f ? 0.9f : 1.1f), 0.2f, 8f); //Evil
                    _Pan += lBefore - ViewToCard(pView, lEvent.mousePosition);
                    lEvent.Use();
                    Repaint();
                    break;

                case EventType.MouseDown:
                    if (!pView.Contains(lEvent.mousePosition))
                        return;

                    StartDrag(pView, lEvent);

                    if (_drag != PreviewDrag.None)
                        GUIUtility.hotControl = lControlId;

                    lEvent.Use();
                    Repaint();
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != lControlId)
                        return;

                    UpdateDrag(pView, lEvent);
                    lEvent.Use();
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl != lControlId)
                        return;

                    GUIUtility.hotControl = 0;
                    _drag = PreviewDrag.None;
                    Undo.CollapseUndoOperations(_dragUndoGroup);
                    lEvent.Use();
                    break;
            }
        }

        private void StartDrag(Rect pView, Event pEvent)
        {
            _drag = PreviewDrag.None;

            if (pEvent.button == 2 || (pEvent.button == 0 && pEvent.alt))
            {
                _drag = PreviewDrag.Pan;
                return;
            }

            //The back view is only there to look at, elements live on the front
            if (_showBack)
                return;

            Undo.IncrementCurrentGroup();
            _dragUndoGroup = Undo.GetCurrentGroup();

            if (IsEditingShape())
            {
                StartShapeDrag(pView, pEvent);
                return;
            }

            if (pEvent.button != 0)
                return;

            CardElement lSelected = _Template.FindElement(_SelectedElementId);

            if (lSelected != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (Vector2.Distance(FromCardToView(pView, GetCorner(lSelected, i)), pEvent.mousePosition) > 7f)
                        continue;

                    _drag = PreviewDrag.Resize;
                    _dragIndex = i;
                    _dragAnchor = GetCorner(lSelected, (i + 2) % 4);
                    _dragStartSize = lSelected.size;
                    return;
                }
            }

            Vector2 lMouse = ViewToCard(pView, pEvent.mousePosition);

            //Topmost element first
            for (int i = _Template.elements.Count - 1; i >= 0; i--)
            {
                CardElement lElement = _Template.elements[i];

                if (!ContainPoint(lElement, lMouse))
                    continue;

                SelectElement(lElement);
                _drag = PreviewDrag.Move;
                _dragAnchor = lElement.position - lMouse;
                return;
            }

            _SelectedElementId = null;
        }

        private void StartShapeDrag(Rect pView, Event pEvent)
        {
            List<Vector2> lPoints = _Template.shape.points;
            int lPoint = FindShapePoint(pView, pEvent.mousePosition);

            if (pEvent.button == 1)
            {
                // Polygons keeps at least 3 points
                if (lPoint >= 0 && lPoints.Count > 3)
                    ModifyTemplate("Remove Shape Point", () => lPoints.RemoveAt(lPoint));

                return;
            }

            if (pEvent.button != 0)
                return;

            if (lPoint < 0 && pEvent.clickCount == 2)
                lPoint = InsertShapePoint(pView, pEvent.mousePosition);

            if (lPoint < 0)
                return;

            _drag = PreviewDrag.ShapePoint;
            _dragIndex = lPoint;
        }

        private void UpdateDrag(Rect pView, Event pEvent)
        {
            if (_drag == PreviewDrag.Pan)
            {
                _Pan -= new Vector2(pEvent.delta.x, -pEvent.delta.y) / GetViewScale(pView);
                Repaint();
                return;
            }

            Vector2 lMouse = ViewToCard(pView, pEvent.mousePosition);
            CardElement lElement = _Template.FindElement(_SelectedElementId);

            switch (_drag)
            {
                case PreviewDrag.Move:
                    Undo.RecordObject(_Template, "Move Element");
                    lElement.position = Snap(lMouse + _dragAnchor);
                    break;

                case PreviewDrag.Resize:
                    Undo.RecordObject(_Template, "Resize Element");
                    ResizeElement(lElement, Snap(lMouse), pEvent.shift);
                    break;

                case PreviewDrag.ShapePoint:
                    Undo.RecordObject(_Template, "Move Shape Point");
                    _Template.shape.points[_dragIndex] = Snap(lMouse) / _Template.shape.size;
                    break;
            }

            OnTemplateChanged();
        }

        //Resizes around the corner opposite to the draged one, in the element's local rotated space
        private void ResizeElement(CardElement pElement, Vector2 pCorner, bool FixedRatio)
        {
            Vector2 lDiagonal = Rotate(pCorner - _dragAnchor, -pElement.rotation);
            Vector2 lSize = new Vector2(Mathf.Max(Mathf.Abs(lDiagonal.x), 0.05f), Mathf.Max(Mathf.Abs(lDiagonal.y), 0.05f));

            if (FixedRatio)
            {
                float lRatio = _dragStartSize.x / _dragStartSize.y;

                if (lSize.x / lSize.y > lRatio)
                    lSize.y = lSize.x / lRatio;
                else
                    lSize.x = lSize.y * lRatio;
            }

            Vector2 lSignedSize = new Vector2(lDiagonal.x < 0f ? -lSize.x : lSize.x, lDiagonal.y < 0f ? -lSize.y : lSize.y);
            pElement.size = lSize;
            pElement.position = _dragAnchor + Rotate(lSignedSize * 0.5f, pElement.rotation);
        }

        private void SelectElement(CardElement pElement)
        {
            _SelectedElementId = pElement.id;
            _expandedRows.Add(string.IsNullOrEmpty(pElement.contentVariableId) ? pElement.id : pElement.contentVariableId);
        }

        private int FindShapePoint(Rect pView, Vector2 pMousePosition)
        {
            List<Vector2> lPoints = _Template.shape.points;

            for (int i = 0; i < lPoints.Count; i++)
            {
                if (Vector2.Distance(FromCardToView(pView, Vector2.Scale(lPoints[i], _Template.shape.size)), pMousePosition) <= 7f)
                    return i;
            }

            return -1;
        }

        //Adds a point on the edge under the mouse, returns its index or -1 when no edge is close enough 
        private int InsertShapePoint(Rect pView, Vector2 pMousePosition)
        {
            List<Vector2> lPoints = _Template.shape.points;

            for (int i = 0; i < lPoints.Count; i++)
            {
                Vector2 lA = FromCardToView(pView, Vector2.Scale(lPoints[i], _Template.shape.size));
                Vector2 lB = FromCardToView(pView, Vector2.Scale(lPoints[(i + 1) % lPoints.Count], _Template.shape.size));

                if (HandleUtility.DistancePointLine(pMousePosition, lA, lB) > 6f)
                    continue;

                Vector2 lPoint = ViewToCard(pView, HandleUtility.ProjectPointLine(pMousePosition, lA, lB)) / _Template.shape.size;
                ModifyTemplate("Add Shape Point", () => lPoints.Insert(i + 1, lPoint));
                return i + 1;
            }

            return -1;
        }

        //small pos quant func
        private static Vector2 Snap(Vector2 pPoint)
        {
            if (!EditorGUI.actionKey)
                return pPoint;

            float snapMargin = 20f;
            return new Vector2(Mathf.Round(pPoint.x * snapMargin) / snapMargin, Mathf.Round(pPoint.y * snapMargin) / snapMargin);
        }

        #endregion

        #region Overlay

        private void DrawOverlay(Rect pView)
        {
            DrawPolyline(pView, CardShapeUtility.BuildOutline(_Template.shape), new Color(1f, 1f, 1f, 0.2f), 1f);

            if (_showBack)
                return;

            if (IsEditingShape())
            {
                DrawShapeHandles(pView);
                DrawHint(pView, "Drag points to reshape  /  Double-click an edge to add a point  /  Right-click a point to remove it  /  Ctrl snaps");
                return;
            }

            _placeholderStyle ??= new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };

            CardElement lSelected = null;

            foreach (CardElement lElement in _Template.elements)
            {
                if (lElement.id == _SelectedElementId)
                {
                    lSelected = lElement;
                    continue;
                }

                Vector3[] lCorners = GetViewCorners(pView, lElement);
                bool lIsVisible = CardBuilder.IsElementVisible(_Template, lElement);

                // displays placeholder
                if (lElement.type == CardElementType.Image && CardBuilder.GetElementSprite(_Template, lElement) == null)
                {
                    Handles.color = new Color(0.3f, 0.6f, 1f, 0.25f);
                    Handles.DrawAAConvexPolygon(lCorners);
                    GUI.Label(GetViewBounds(lCorners), lElement.name, _placeholderStyle);
                }

                Handles.color = new Color(1f, 1f, 1f, lIsVisible ? 0.35f : 0.15f);
                Handles.DrawAAPolyLine(1f, lCorners[0], lCorners[1], lCorners[2], lCorners[3], lCorners[0]);
            }

            if (lSelected != null)
                DrawSelection(pView, lSelected);

            DrawHint(pView, "Drag to move  /  Corners resize (Shift keeps ratio)  /  Ctrl snaps  /  Scroll zooms  /  Middle mouse pans");
        }

        private void DrawSelection(Rect pView, CardElement pElement)
        {
            Vector3[] lCorners = GetViewCorners(pView, pElement);

            Handles.color = _SelectionColor;
            Handles.DrawAAPolyLine(2f, lCorners[0], lCorners[1], lCorners[2], lCorners[3], lCorners[0]);

            foreach (Vector3 lCorner in lCorners)
            {
                Rect lHandle = new Rect(lCorner.x - 4f, lCorner.y - 4f, 8f, 8f);
                EditorGUI.DrawRect(lHandle, _SelectionColor);
                EditorGUIUtility.AddCursorRect(lHandle, MouseCursor.ScaleArrow);
            }

            if (pElement.type == CardElementType.Image && CardBuilder.GetElementSprite(_Template, pElement) == null)
                GUI.Label(GetViewBounds(lCorners), pElement.name, _placeholderStyle);
        }

        private void DrawShapeHandles(Rect pView)
        {
            List<Vector2> lPoints = _Template.shape.points;
            List<Vector2> lCardPoints = new List<Vector2>(lPoints.Count);

            foreach (Vector2 lPoint in lPoints)
                lCardPoints.Add(Vector2.Scale(lPoint, _Template.shape.size));

            DrawPolyline(pView, lCardPoints, _ShapeColor, 2f);

            foreach (Vector2 lPoint in lCardPoints)
            {
                Vector2 lViewPoint = FromCardToView(pView, lPoint);
                Rect lHandle = new Rect(lViewPoint.x - 4f, lViewPoint.y - 4f, 8f, 8f);
                EditorGUI.DrawRect(lHandle, _ShapeColor);
                EditorGUIUtility.AddCursorRect(lHandle, MouseCursor.MoveArrow);
            }
        }

        private void DrawPolyline(Rect pView, List<Vector2> pPoints, Color pColor, float pWidth)
        {
            if (pPoints.Count < 2)
                return;

            Vector3[] lLine = new Vector3[pPoints.Count + 1];

            for (int i = 0; i < pPoints.Count; i++)
                lLine[i] = FromCardToView(pView, pPoints[i]);

            lLine[pPoints.Count] = lLine[0];

            Handles.color = pColor;
            Handles.DrawAAPolyLine(pWidth, lLine);
        }

        private static void DrawHint(Rect pView, string pHint)
        {
            GUI.Label(new Rect(8f, pView.height - 20f, pView.width - 16f, 18f), pHint, EditorStyles.centeredGreyMiniLabel);
        }

        #endregion

        #region Geometry

        private float GetOrthographicSize(Rect pView)
        {
            Vector2 lSize = _Template.shape.size;
            float lAspect = pView.width / Mathf.Max(pView.height, 1f);

            //Fits the whole card with a margin around it
            return Mathf.Max(lSize.y * 0.5f, lSize.x * 0.5f / lAspect) * 1.15f / _Zoom;
        }

        //GUI pixels per card unit
        private float GetViewScale(Rect pView)
        {
            return pView.height / (2f * GetOrthographicSize(pView));
        }

        private Vector2 FromCardToView(Rect pView, Vector2 pPoint)
        {
            Vector2 lOffset = (pPoint - _Pan) * GetViewScale(pView);
            return new Vector2(pView.center.x + lOffset.x, pView.center.y - lOffset.y);
        }

        private Vector2 ViewToCard(Rect pView, Vector2 pPoint)
        {
            return _Pan + new Vector2(pPoint.x - pView.center.x, pView.center.y - pPoint.y) / GetViewScale(pView);
        }

        private Vector3[] GetViewCorners(Rect pView, CardElement pElement)
        {
            Vector3[] lCorners = new Vector3[4];

            for (int i = 0; i < 4; i++)
                lCorners[i] = FromCardToView(pView, GetCorner(pElement, i));

            return lCorners;
        }

        private static Rect GetViewBounds(Vector3[] pCorners)
        {
            Vector2 lMin = pCorners[0];
            Vector2 lMax = pCorners[0];

            foreach (Vector3 lCorner in pCorners)
            {
                lMin = Vector2.Min(lMin, lCorner);
                lMax = Vector2.Max(lMax, lCorner);
            }

            return Rect.MinMaxRect(lMin.x, lMin.y, lMax.x, lMax.y);
        }

        //Corners in card units, counter-clockwise from the bottom left
        private static Vector2 GetCorner(CardElement pElement, int pCorner)
        {
            Vector2 lHalfSize = pElement.size * 0.5f;
            Vector2 lLocal = new Vector2(pCorner == 1 || pCorner == 2 ? lHalfSize.x : -lHalfSize.x, pCorner >= 2 ? lHalfSize.y : -lHalfSize.y);
            return pElement.position + Rotate(lLocal, pElement.rotation);
        }

        private static bool ContainPoint(CardElement pElement, Vector2 pPoint)
        {
            Vector2 lLocal = Rotate(pPoint - pElement.position, -pElement.rotation);
            //simple check to see if the point is inside the bonds
            return Mathf.Abs(lLocal.x) <= pElement.size.x * 0.5f && Mathf.Abs(lLocal.y) <= pElement.size.y * 0.5f;
        }

        private static Vector2 Rotate(Vector2 pVector, float pDegrees)
        {
            return Quaternion.Euler(0f, 0f, pDegrees) * pVector;
        }

        #endregion
    }
}
