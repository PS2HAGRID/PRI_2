using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace CardCreator.Editor
{
    //Right panel of the card creator: card settings, per-card values, fixed content and output
    public partial class CardCreatorWindow
    {
        private static readonly string[] _dimensionLabels = { "2D", "3D" };
        private static readonly string[] _render2DLabels = { "UI (Canvas)", "World (Sprites)" };
        private static readonly Color _selectedRowColor = new Color(0.25f, 0.5f, 1f, 0.2f);

        [SerializeField] private Vector2 _inspectorScroll;
        [SerializeField] private bool _isCardExpanded = true;
        [SerializeField] private bool _isPerCardExpanded = true;
        [SerializeField] private bool _isFixedContentExpanded = true;
        [SerializeField] private bool _isOutputExpanded = true;

        //Rows are identified by the variable id, or the element id for fixed elements
        private readonly HashSet<string> _expandedRows = new HashSet<string>();
        private bool _hasTextResources;

        private void DrawInspector()
        {
            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);
            EditorGUILayout.Space(4f);

            if (!_hasTextResources)
            {
                EditorGUILayout.HelpBox("TextMeshPro is either broken or not instaled", MessageType.Warning);

                if (GUILayout.Button("TMP essential ressources is missing"))
                    TMP_PackageResourceImporter.ImportResources(true, false, false);
            }

            DrawCardSection();
            DrawPerCardSection();
            DrawFixedContentSection();
            DrawOutputSection();

            EditorGUILayout.Space(10f);
            EditorGUILayout.EndScrollView();
        }

        private void RefreshTextResources()
        {
            _hasTextResources = Resources.Load<TMP_Settings>("TMP Settings") != null;
        }

        #region Card

        private void DrawCardSection()
        {
            _isCardExpanded = EditorGUILayout.Foldout(_isCardExpanded, "Card", true, EditorStyles.foldoutHeader);

            if (!_isCardExpanded)
                return;

            EditorGUILayout.PropertyField(_serializedTemplate.FindProperty("templateName"), new GUIContent("Name"));
            DrawRenderMode();
            DrawShape();
            DrawFaces();
        }

        private void DrawRenderMode()
        {
            SerializedProperty lMode = _serializedTemplate.FindProperty("renderMode");
            CardRenderMode lCurrent = (CardRenderMode)lMode.enumValueIndex;

            int lDimension = EditorGUILayout.Popup("Dimension", lCurrent == CardRenderMode.World3D ? 1 : 0, _dimensionLabels);

            if (lDimension == 1)
                lMode.enumValueIndex = (int)CardRenderMode.World3D;
            else
            {
                int lRender2D = EditorGUILayout.Popup("Render", lCurrent == CardRenderMode.World2D ? 1 : 0, _render2DLabels);
                lMode.enumValueIndex = (int)(lRender2D == 1 ? CardRenderMode.World2D : CardRenderMode.UI);
            }

            if (lMode.enumValueIndex == (int)CardRenderMode.UI)
                EditorGUILayout.PropertyField(_serializedTemplate.FindProperty("pixelsPerUnit"));
        }

        private void DrawShape()
        {
            CardShapeSettings lShape = _template.shape;
            SerializedProperty lProperty = _serializedTemplate.FindProperty("shape");

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            CardShapeSource lSource = (CardShapeSource)EditorGUILayout.EnumPopup("Source", lShape.source);

            if (EditorGUI.EndChangeCheck())
            {
                ModifyTemplate("Change Card Shape", () => SetShapeSource(lSource));
                GUIUtility.ExitGUI();
            }

            switch (lShape.source)
            {
                case CardShapeSource.Preset:
                    EditorGUILayout.PropertyField(lProperty.FindPropertyRelative("preset"));

                    if (lShape.preset == CardShapePreset.RegularPolygon)
                    {
                        EditorGUILayout.PropertyField(lProperty.FindPropertyRelative("polygonSides"), new GUIContent("Sides"));
                        EditorGUILayout.PropertyField(lProperty.FindPropertyRelative("polygonRotation"), new GUIContent("Rotation"));
                    }
                    else if (lShape.preset == CardShapePreset.Ellipse)
                        EditorGUILayout.PropertyField(lProperty.FindPropertyRelative("ellipseSegments"), new GUIContent("Segments"));
                    break;

                case CardShapeSource.Polygon:
                    EditorGUILayout.BeginHorizontal();
                    _isEditingShape = GUILayout.Toggle(_isEditingShape, "Edit Points In Preview", "Button");

                    if (GUILayout.Button("Reset", GUILayout.Width(60f)))
                    {
                        ModifyTemplate("Reset Card Shape", () => lShape.points = BuildPolygonPoints(CardShapeSource.Preset));
                        GUIUtility.ExitGUI();
                    }

                    EditorGUILayout.EndHorizontal();

                    if (PolygonTriangulator.IsSelfIntersecting(lShape.points))
                        EditorGUILayout.HelpBox("The outline crosses itself, the card can't be filled correctly.", MessageType.Warning);
                    break;

                case CardShapeSource.Sprite:
                    EditorGUILayout.PropertyField(lProperty.FindPropertyRelative("sprite"));
                    EditorGUILayout.PropertyField(lProperty.FindPropertyRelative("spriteUsage"), new GUIContent("Use Sprite As"));

                    if (lShape.sprite != null && GUILayout.Button("Match Sprite Aspect"))
                    {
                        ModifyTemplate("Match Sprite Aspect", () => lShape.size.x = lShape.size.y * lShape.sprite.rect.width / lShape.sprite.rect.height);
                        GUIUtility.ExitGUI();
                    }
                    break;
            }

            if (CardShapeUtility.SupportsRounding(lShape))
            {
                EditorGUILayout.PropertyField(lProperty.FindPropertyRelative("cornerRadius"));
                EditorGUILayout.PropertyField(lProperty.FindPropertyRelative("cornerSegments"));
            }

            EditorGUILayout.PropertyField(lProperty.FindPropertyRelative("size"));
        }

        private void DrawFaces()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Front", EditorStyles.boldLabel);
            DrawFaceSettings(_serializedTemplate.FindProperty("front"), _template.UsesShapeSprite());

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Back", EditorStyles.boldLabel);
            DrawFaceSettings(_serializedTemplate.FindProperty("back"), false);
        }

        private void DrawFaceSettings(SerializedProperty pFace, bool pUsesShapeSprite)
        {
            DrawPerCardField(pFace.FindPropertyRelative("color"), pFace.FindPropertyRelative("isColorPerCard"), new GUIContent("Color"));

            if (pUsesShapeSprite)
            {
                //The outline always comes from the shape sprite, a per card sprite only replaces the image
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Sprite", "Shape sprite");
                DrawPerCardToggle(pFace.FindPropertyRelative("isSpritePerCard"));
                EditorGUILayout.EndHorizontal();
            }
            else
                DrawPerCardField(pFace.FindPropertyRelative("sprite"), pFace.FindPropertyRelative("isSpritePerCard"), new GUIContent("Sprite"));
        }

        private static void DrawPerCardField(SerializedProperty pValue, SerializedProperty pIsPerCard, GUIContent pLabel)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(pValue, pLabel);
            DrawPerCardToggle(pIsPerCard);
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawPerCardToggle(SerializedProperty pIsPerCard)
        {
            GUIContent lContent = new GUIContent("Per card", "Each card set its own value, the one on the left is the default.\nOff: the same value for evry card, baked in the prfab.");
            pIsPerCard.boolValue = GUILayout.Toggle(pIsPerCard.boolValue, lContent, EditorStyles.miniButton, GUILayout.Width(62f));
        }

        private void SetShapeSource(CardShapeSource pSource)
        {
            //new polygon starts from the current outline so it can be reshaped from there
            if (pSource == CardShapeSource.Polygon && _template.shape.points.Count < 3)
                _template.shape.points = BuildPolygonPoints(_template.shape.source);

            _template.shape.source = pSource;
            _isEditingShape = pSource == CardShapeSource.Polygon;
        }

        private List<Vector2> BuildPolygonPoints(CardShapeSource pFrom)
        {
            CardShapeSettings lShape = _template.shape;
            CardShapeSource lSource = lShape.source;

            lShape.source = pFrom;
            List<Vector2> lPoints = CardShapeUtility.Normalize(CardShapeUtility.BuildCorners(lShape), lShape.size);
            lShape.source = lSource;

            return lPoints;
        }

        #endregion

        #region Per card values

        private void DrawPerCardSection()
        {
            EditorGUILayout.Space(6f);
            _isPerCardExpanded = EditorGUILayout.Foldout(_isPerCardExpanded, "Per-Card Values", true, EditorStyles.foldoutHeader);

            if (!_isPerCardExpanded)
                return;

            if (GUILayout.Button(new GUIContent(" Add Variable", EditorGUIUtility.IconContent("Toolbar Plus").image), GUILayout.Height(26f)))
                ShowAddVariableMenu();

            if (_template.variables.Count == 0)
                EditorGUILayout.HelpBox("Values that change from one card to another (name, artwork, price...). Each card asset sets its own values.", MessageType.None);

            for (int i = 0; i < _template.variables.Count; i++)
                DrawVariableRow(i);
        }

        private void ShowAddVariableMenu()
        {
            GenericMenu lMenu = new GenericMenu();

            foreach (CardVariableType lType in Enum.GetValues(typeof(CardVariableType)))
                lMenu.AddItem(new GUIContent(GetTypeLabel(lType)), false, () => AddVariable(lType));

            lMenu.ShowAsContext();
        }

        private void DrawVariableRow(int pIndex)
        {
            CardVariable lVariable = _template.variables[pIndex];
            SerializedProperty lProperty = _serializedTemplate.FindProperty("variables").GetArrayElementAtIndex(pIndex);
            CardElement lElement = _template.FindContentElement(lVariable.id);
            bool lIsExpanded = _expandedRows.Contains(lVariable.id);

            Rect lRow = EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (lElement != null && lElement.id == _SelectedElementId)
                EditorGUI.DrawRect(lRow, _selectedRowColor);

            EditorGUILayout.BeginHorizontal();
            bool lWantsExpanded = GUILayout.Toggle(lIsExpanded, GUIContent.none, EditorStyles.foldout, GUILayout.Width(12f));
            EditorGUILayout.PropertyField(lProperty.FindPropertyRelative("name"), GUIContent.none);
            GUILayout.Label(GetTypeLabel(lVariable.type), EditorStyles.miniLabel, GUILayout.Width(42f));

            if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash"), EditorStyles.iconButton, GUILayout.Width(20f)))
            {
                ModifyTemplate("Remove Variable", () => RemoveVariable(lVariable));
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();

            string lError = CardNaming.GetErrorInName(_template, lVariable);

            if (lError != null)
                EditorGUILayout.HelpBox(lError, MessageType.Error);

            if (lWantsExpanded)
                DrawVariableBody(lVariable, lProperty, lElement);

            EditorGUILayout.EndVertical();

            if (lWantsExpanded != lIsExpanded)
                SetRowExpanded(lVariable.id, lWantsExpanded, lElement);

            // Clicking the row background selects the element in the preview
            if (lElement != null && Event.current.type == EventType.MouseDown && lRow.Contains(Event.current.mousePosition))
            {
                _SelectedElementId = lElement.id;
                Repaint();
            }
        }

        private void DrawVariableBody(CardVariable pVariable, SerializedProperty pProperty, CardElement pElement)
        {
            DrawDefaultValue(pVariable, pProperty);

            switch (pVariable.type)
            {
                case CardVariableType.String:
                    EditorGUILayout.PropertyField(pProperty.FindPropertyRelative("multiline"));
                    break;

                case CardVariableType.Enum:
                    EditorGUILayout.PropertyField(pProperty.FindPropertyRelative("enumOptions"), new GUIContent("Options"), true);
                    break;

                case CardVariableType.Color:
                    DrawBindingTargets(pVariable, "Tints", true);
                    break;

                case CardVariableType.Bool:
                    DrawBindingTargets(pVariable, "Shows / Hides", false);
                    break;
            }

            if (!IsDisplayable(pVariable.type))
                return;

            EditorGUI.BeginChangeCheck();
            bool lIsShown = EditorGUILayout.Toggle("Show On Card", pElement != null);

            if (EditorGUI.EndChangeCheck())
            {
                ModifyTemplate(lIsShown ? "Show Variable" : "Hide Variable", () =>
                {
                    if (lIsShown)
                        AddContentElement(pVariable);
                    else
                        RemoveElement(pElement);
                });
                GUIUtility.ExitGUI();
            }

            if (pElement != null)
                DrawElementSettings(pElement);
        }

        private void DrawDefaultValue(CardVariable pVariable, SerializedProperty pProperty)
        {
            GUIContent lLabel = new GUIContent("Default Value");

            switch (pVariable.type)
            {
                case CardVariableType.Int:
                    EditorGUILayout.PropertyField(pProperty.FindPropertyRelative("intValue"), lLabel);
                    break;

                case CardVariableType.Float:
                    EditorGUILayout.PropertyField(pProperty.FindPropertyRelative("floatValue"), lLabel);
                    break;

                case CardVariableType.Bool:
                    EditorGUILayout.PropertyField(pProperty.FindPropertyRelative("boolValue"), lLabel);
                    break;

                case CardVariableType.Sprite:
                    EditorGUILayout.PropertyField(pProperty.FindPropertyRelative("spriteValue"), lLabel);
                    break;

                case CardVariableType.Color:
                    EditorGUILayout.PropertyField(pProperty.FindPropertyRelative("colorValue"), lLabel);
                    break;

                case CardVariableType.String:
                    SerializedProperty lString = pProperty.FindPropertyRelative("stringValue");

                    if (!pVariable.multiline)
                    {
                        EditorGUILayout.PropertyField(lString, lLabel);
                        break;
                    }

                    EditorGUILayout.LabelField(lLabel);
                    EditorGUI.BeginChangeCheck();
                    string lText = EditorGUILayout.TextArea(lString.stringValue, GUILayout.MinHeight(48f));

                    if (EditorGUI.EndChangeCheck())
                        lString.stringValue = lText;
                    break;

                case CardVariableType.Enum:
                    SerializedProperty lIndex = pProperty.FindPropertyRelative("intValue");
                    EditorGUI.BeginChangeCheck();
                    int lOption = EditorGUILayout.Popup(lLabel.text, lIndex.intValue, pVariable.enumOptions.ToArray());

                    if (EditorGUI.EndChangeCheck())
                        lIndex.intValue = lOption;
                    break;
            }
        }

        // Lists all elements a color or bool variable can drive.

        private void DrawBindingTargets(CardVariable pVariable, string pLabel, bool pIsColor)
        {
            EditorGUILayout.LabelField(pLabel, EditorStyles.miniBoldLabel);

            if (_template.elements.Count == 0)
            {
                EditorGUILayout.LabelField("No element on the card yet.", EditorStyles.miniLabel);
                return;
            }

            EditorGUI.indentLevel++;

            foreach (CardElement lElement in _template.elements)
            {
                bool lIsBound = (pIsColor ? lElement.colorVariableId : lElement.visibilityVariableId) == pVariable.id;

                EditorGUI.BeginChangeCheck();
                bool lWantsBound = EditorGUILayout.ToggleLeft(lElement.name, lIsBound);

                if (EditorGUI.EndChangeCheck())
                {
                    ModifyTemplate("Change Variable Target", () =>
                    {
                        string lId = lWantsBound ? pVariable.id : null;

                        if (pIsColor)
                            lElement.colorVariableId = lId;
                        else
                            lElement.visibilityVariableId = lId;
                    });
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUI.indentLevel--;
        }

        private void AddVariable(CardVariableType pType)
        {
            ModifyTemplate("Add Variable", () =>
            {
                CardVariable lVariable = new CardVariable
                {
                    type = pType,
                    name = CardNaming.GetUniqueVariableName(_template, GetDefaultVariableName(pType))
                };

                if (pType == CardVariableType.String)
                    lVariable.stringValue = "Text";
                else if (pType == CardVariableType.Enum)
                    lVariable.enumOptions.AddRange(new[] { "OptionA", "OptionB" });

                _template.variables.Add(lVariable);
                _expandedRows.Add(lVariable.id);

                if (IsDisplayable(pType))
                    AddContentElement(lVariable);
            });
        }

        private void RemoveVariable(CardVariable pVariable)
        {
            _template.variables.Remove(pVariable);

            CardElement lContent = _template.FindContentElement(pVariable.id);

            if (lContent != null)
                RemoveElement(lContent);

            foreach (CardElement lElement in _template.elements)
            {
                if (lElement.colorVariableId == pVariable.id)
                    lElement.colorVariableId = null;

                if (lElement.visibilityVariableId == pVariable.id)
                    lElement.visibilityVariableId = null;
            }
        }

        // Creates the element displaying a variable value, laid out like a classic card (artwork on top, texts below)
        private void AddContentElement(CardVariable pVariable)
        {
            Vector2 lCardSize = _template.shape.size;
            bool lIsImage = pVariable.type == CardVariableType.Sprite;

            CardElement lElement = new CardElement
            {
                name = pVariable.name,
                type = lIsImage ? CardElementType.Image : CardElementType.Text,
                contentVariableId = pVariable.id,
                position = lIsImage ? new Vector2(0f, lCardSize.y * 0.15f) : new Vector2(0f, -lCardSize.y * 0.25f),
                size = lIsImage ? new Vector2(lCardSize.x * 0.8f, lCardSize.y * 0.45f) : new Vector2(lCardSize.x * 0.8f, lCardSize.y * 0.12f),
                color = lIsImage ? Color.white : Color.black
            };

            _template.elements.Add(lElement);
            _SelectedElementId = lElement.id;
        }

        private static bool IsDisplayable(CardVariableType pType)
        {
            return pType != CardVariableType.Bool && pType != CardVariableType.Color;
        }

        private static string GetTypeLabel(CardVariableType pType)
        {
            switch (pType)
            {
                case CardVariableType.Sprite:
                case CardVariableType.Color:
                    return pType.ToString();
                default:
                    return pType.ToString().ToLowerInvariant();
            }
        }

        private static string GetDefaultVariableName(CardVariableType pType)
        {
            switch (pType)
            {
                case CardVariableType.Int:
                    return "number";
                case CardVariableType.Float:
                    return "value";
                case CardVariableType.String:
                    return "text";
                case CardVariableType.Bool:
                    return "toggle";
                case CardVariableType.Sprite:
                    return "sprite";
                case CardVariableType.Color:
                    return "color";
                default:
                    return "category";
            }
        }

        #endregion

        #region Fixed content

        private void DrawFixedContentSection()
        {
            EditorGUILayout.Space(6f);
            _isFixedContentExpanded = EditorGUILayout.Foldout(_isFixedContentExpanded, "Fixed Content", true, EditorStyles.foldoutHeader);

            if (!_isFixedContentExpanded)
                return;

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(new GUIContent(" Add Image", EditorGUIUtility.IconContent("Toolbar Plus").image)))
            {
                ModifyTemplate("Add Fixed Element", () => AddFixedElement(CardElementType.Image));
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button(new GUIContent(" Add Text", EditorGUIUtility.IconContent("Toolbar Plus").image)))
            {
                ModifyTemplate("Add Fixed Element", () => AddFixedElement(CardElementType.Text));
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();

            if (!_template.elements.Exists(pElement => string.IsNullOrEmpty(pElement.contentVariableId)))
                EditorGUILayout.HelpBox("Images and texts that are the same on every card (frame, labels, messages...). They are baked into the prefab.", MessageType.None);

            for (int i = 0; i < _template.elements.Count; i++)
            {
                if (string.IsNullOrEmpty(_template.elements[i].contentVariableId))
                    DrawFixedElementRow(i);
            }
        }

        private void DrawFixedElementRow(int pIndex)
        {
            CardElement lElement = _template.elements[pIndex];
            SerializedProperty lProperty = _serializedTemplate.FindProperty("elements").GetArrayElementAtIndex(pIndex);
            bool lIsExpanded = _expandedRows.Contains(lElement.id);

            Rect lRow = EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (lElement.id == _SelectedElementId)
                EditorGUI.DrawRect(lRow, _selectedRowColor);

            EditorGUILayout.BeginHorizontal();
            bool lWantsExpanded = GUILayout.Toggle(lIsExpanded, GUIContent.none, EditorStyles.foldout, GUILayout.Width(12f));
            EditorGUILayout.PropertyField(lProperty.FindPropertyRelative("name"), GUIContent.none);
            GUILayout.Label(lElement.type.ToString(), EditorStyles.miniLabel, GUILayout.Width(42f));

            if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash"), EditorStyles.iconButton, GUILayout.Width(20f)))
            {
                ModifyTemplate("Remove Fixed Element", () => RemoveElement(lElement));
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();

            if (lWantsExpanded)
                DrawElementSettings(lElement);

            EditorGUILayout.EndVertical();

            if (lWantsExpanded != lIsExpanded)
                SetRowExpanded(lElement.id, lWantsExpanded, lElement);

            if (Event.current.type == EventType.MouseDown && lRow.Contains(Event.current.mousePosition))
            {
                _SelectedElementId = lElement.id;
                Repaint();
            }
        }

        private void AddFixedElement(CardElementType pType)
        {
            Vector2 lCardSize = _template.shape.size;
            bool lIsImage = pType == CardElementType.Image;

            CardElement lElement = new CardElement
            {
                type = pType,
                name = lIsImage ? "Image" : "Label",
                size = lIsImage ? Vector2.one * lCardSize.x * 0.3f : new Vector2(lCardSize.x * 0.6f, lCardSize.y * 0.1f),
                color = lIsImage ? Color.white : Color.black
            };

            _template.elements.Add(lElement);
            _SelectedElementId = lElement.id;
            _expandedRows.Add(lElement.id);
        }

        #endregion

        #region Elements

        private void DrawElementSettings(CardElement pElement)
        {
            int lIndex = _template.elements.IndexOf(pElement);
            SerializedProperty lProperty = _serializedTemplate.FindProperty("elements").GetArrayElementAtIndex(lIndex);
            bool lIsBound = !string.IsNullOrEmpty(pElement.contentVariableId);

            EditorGUILayout.Space(2f);
            EditorGUILayout.PropertyField(lProperty.FindPropertyRelative("position"));
            EditorGUILayout.PropertyField(lProperty.FindPropertyRelative("size"));
            EditorGUILayout.PropertyField(lProperty.FindPropertyRelative("rotation"));

            CardVariable lColorVariable = _template.FindVariable(pElement.colorVariableId);

            if (lColorVariable == null)
                EditorGUILayout.PropertyField(lProperty.FindPropertyRelative("color"));
            else
                EditorGUILayout.LabelField("Color", "Driven by " + lColorVariable.name);

            if (pElement.type == CardElementType.Image)
            {
                if (!lIsBound)
                    EditorGUILayout.PropertyField(lProperty.FindPropertyRelative("sprite"));

                EditorGUILayout.PropertyField(lProperty.FindPropertyRelative("preserveAspect"));
            }
            else
                DrawTextSettings(lProperty, lIsBound);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Draw Order");

            using (new EditorGUI.DisabledScope(lIndex == 0))
            {
                if (GUILayout.Button("Send Backward", EditorStyles.miniButtonLeft))
                {
                    ModifyTemplate("Change Draw Order", () => SwapElements(lIndex, lIndex - 1));
                    GUIUtility.ExitGUI();
                }
            }

            using (new EditorGUI.DisabledScope(lIndex == _template.elements.Count - 1))
            {
                if (GUILayout.Button("Bring Forward", EditorStyles.miniButtonRight))
                {
                    ModifyTemplate("Change Draw Order", () => SwapElements(lIndex, lIndex + 1));
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTextSettings(SerializedProperty pProperty, bool pIsBound)
        {
            if (pIsBound)
                EditorGUILayout.PropertyField(pProperty.FindPropertyRelative("textFormat"), new GUIContent("Format", "{0} is replaced by the value, e.g. \"Cost: {0}\""));
            else
            {
                SerializedProperty lText = pProperty.FindPropertyRelative("text");
                EditorGUILayout.LabelField("Text");
                EditorGUI.BeginChangeCheck();
                string lValue = EditorGUILayout.TextArea(lText.stringValue, GUILayout.MinHeight(36f));

                if (EditorGUI.EndChangeCheck())
                    lText.stringValue = lValue;
            }

            EditorGUILayout.PropertyField(pProperty.FindPropertyRelative("font"), new GUIContent("Font", "TextMeshPro default font when empty"));
            EditorGUILayout.PropertyField(pProperty.FindPropertyRelative("fontSize"));

            SerializedProperty lStyle = pProperty.FindPropertyRelative("fontStyle");
            EditorGUI.BeginChangeCheck();
            FontStyles lFontStyle = (FontStyles)EditorGUILayout.EnumFlagsField("Font Style", (FontStyles)lStyle.intValue);

            if (EditorGUI.EndChangeCheck())
                lStyle.intValue = (int)lFontStyle;

            EditorGUILayout.PropertyField(pProperty.FindPropertyRelative("alignment"));
            EditorGUILayout.PropertyField(pProperty.FindPropertyRelative("autoSize"), new GUIContent("Auto Size", "Shrinks the text when it doesn't fit"));
        }

        private void RemoveElement(CardElement pElement)
        {
            _template.elements.Remove(pElement);

            if (_SelectedElementId == pElement.id)
                _SelectedElementId = null;
        }

        private void SwapElements(int pA, int pB)
        {
            (_template.elements[pA], _template.elements[pB]) = (_template.elements[pB], _template.elements[pA]);
        }

        private void SetRowExpanded(string pRowId, bool pIsExpanded, CardElement pElement)
        {
            if (pIsExpanded)
            {
                _expandedRows.Add(pRowId);

                if (pElement != null)
                    _SelectedElementId = pElement.id;
            }
            else
                _expandedRows.Remove(pRowId);
        }

        #endregion

        #region Output

        private void DrawOutputSection()
        {
            EditorGUILayout.Space(6f);
            _isOutputExpanded = EditorGUILayout.Foldout(_isOutputExpanded, "Output", true, EditorStyles.foldoutHeader);

            if (!_isOutputExpanded)
                return;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_serializedTemplate.FindProperty("outputFolder"), new GUIContent("Folder"));

            if (GUILayout.Button("...", GUILayout.Width(26f)))
            {
                string lFolder = EditorUtility.OpenFolderPanel("Card Output Folder", "Assets", "");

                if (lFolder.StartsWith(Application.dataPath))
                    ModifyTemplate("Change Output Folder", () => _template.outputFolder = "Assets" + lFolder.Substring(Application.dataPath.Length));

                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(_template.outputFolder))
                EditorGUILayout.LabelField(" ", $"Assets/Cards/{_template.templateName} when empty", EditorStyles.miniLabel);

            EditorGUILayout.PropertyField(_serializedTemplate.FindProperty("codeNamespace"), new GUIContent("Namespace"));
        }

        #endregion
    }
}
