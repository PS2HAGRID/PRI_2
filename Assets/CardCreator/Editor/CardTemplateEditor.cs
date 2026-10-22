using TMPro;
using UnityEditor;
using UnityEngine;

namespace CardCreator.Editor
{
    [CustomEditor(typeof(CardTemplate))]
    //this is the editor-side generated SO of the TEMPLATE
    public class CardTemplateEditor : UnityEditor.Editor
    {
        private CardTemplate template => (CardTemplate)target;

        //inspector input registry
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open In Card Creator", GUILayout.Height(28f)))
                CardCreatorWindow.Open(template);

            EditorGUILayout.Space();

            if (Event.current.type == EventType.Layout && CardGenerator.SyncDraft(template))
                EditorUtility.SetDirty(template);

            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))  //Dark Evil .Net IDisposable Magic Trick
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

            SerializedProperty lDraft = serializedObject.FindProperty("draft");
            EditorGUILayout.PropertyField(lDraft.FindPropertyRelative("cardName"), new GUIContent("Card Name"));

            DrawContent(lDraft);
            DrawGenerated();

            serializedObject.ApplyModifiedProperties();

            DrawGenerateButton();
        }

        #region Content

        //display of all Variables + Elements (merged)
        private void DrawContent(SerializedProperty pDraft)
        {
            SerializedProperty lVariables = pDraft.FindPropertyRelative("variables");
            SerializedProperty lElements = pDraft.FindPropertyRelative("elements");

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Variables & Elements", EditorStyles.boldLabel);

            if (lVariables.arraySize == 0 && lElements.arraySize == 0)
                EditorGUILayout.HelpBox("Nothing on the card yet, variables and elements are added in the Card Creator.", MessageType.None);

            for (int i = 0; i < lVariables.arraySize; i++)
            {
                SerializedProperty lVariable = lVariables.GetArrayElementAtIndex(i);
                string lId = lVariable.FindPropertyRelative("id").stringValue;
                SerializedProperty lElement = FindElement(lElements, lId);

                if (!BeginEntry(lId, lVariable.FindPropertyRelative("name").stringValue))
                    continue;

                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(lVariable.FindPropertyRelative("type"));

                DrawVariableValue(lVariable, new GUIContent("Value"));

                if (lElement != null)
                    DrawElementOptions(lElement, lVariables, true);

                EndEntry();
            }

            for (int i = 0; i < lElements.arraySize; i++)
            {
                SerializedProperty lElement = lElements.GetArrayElementAtIndex(i);

                if (!string.IsNullOrEmpty(lElement.FindPropertyRelative("contentVariableId").stringValue))
                    continue;

                if (!BeginEntry(lElement.FindPropertyRelative("id").stringValue, lElement.FindPropertyRelative("name").stringValue))
                    continue;

                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(lElement.FindPropertyRelative("type"));

                DrawElementOptions(lElement, lVariables, false);
                EndEntry();
            }

            DrawFace(pDraft.FindPropertyRelative("front"), "Front");
            DrawFace(pDraft.FindPropertyRelative("back"), "Back");
        }

        private static void DrawFace(SerializedProperty pFace, string pName)
        {
            bool lIsColorPerCard = pFace.FindPropertyRelative("isColorPerCard").boolValue;
            bool lIsSpritePerCard = pFace.FindPropertyRelative("isSpritePerCard").boolValue;

            if (!lIsColorPerCard && !lIsSpritePerCard)
                return;

            if (!BeginEntry("Face." + pName, pName))
                return;

            if (lIsColorPerCard)
                EditorGUILayout.PropertyField(pFace.FindPropertyRelative("color"));

            if (lIsSpritePerCard)
                EditorGUILayout.PropertyField(pFace.FindPropertyRelative("sprite"));

            EndEntry();
        }
        //Start of write
        private static bool BeginEntry(string pId, string pName)
        {
            string lKey = "CardTemplateEditor.Expanded." + pId;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            bool lIsExpanded = EditorGUILayout.Foldout(SessionState.GetBool(lKey, false), string.IsNullOrEmpty(pName) ? "(unnamed)" : pName, true);
            SessionState.SetBool(lKey, lIsExpanded);

            if (lIsExpanded)
            {
                EditorGUI.indentLevel++;
                return true;
            }

            EditorGUILayout.EndVertical();
            return false;
        }

        //end of write
        private static void EndEntry()
        {
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Variables
        //display of all Variables values
        internal static void DrawVariableValue(SerializedProperty pVariable, GUIContent pLabel)
        {
            switch ((CardVariableType)pVariable.FindPropertyRelative("type").enumValueIndex)
            {
                case CardVariableType.Int:
                    EditorGUILayout.PropertyField(pVariable.FindPropertyRelative("intValue"), pLabel);
                    break;

                case CardVariableType.Float:
                    EditorGUILayout.PropertyField(pVariable.FindPropertyRelative("floatValue"), pLabel);
                    break;

                case CardVariableType.Bool:
                    EditorGUILayout.PropertyField(pVariable.FindPropertyRelative("boolValue"), pLabel);
                    break;

                case CardVariableType.Sprite:
                    EditorGUILayout.PropertyField(pVariable.FindPropertyRelative("spriteValue"), pLabel);
                    break;

                case CardVariableType.Color:
                    EditorGUILayout.PropertyField(pVariable.FindPropertyRelative("colorValue"), pLabel);
                    break;

                case CardVariableType.String:
                    SerializedProperty lString = pVariable.FindPropertyRelative("stringValue");

                    if (!pVariable.FindPropertyRelative("multiline").boolValue)
                    {
                        EditorGUILayout.PropertyField(lString, pLabel);
                        break;
                    }

                    EditorGUILayout.LabelField(pLabel);
                    DrawTextArea(lString, 48f);
                    break;

                case CardVariableType.Enum:
                    DrawEnumValue(pVariable, pLabel);
                    break;
            }
        }

        private static void DrawEnumValue(SerializedProperty pVariable, GUIContent pLabel)
        {
            SerializedProperty lOptions = pVariable.FindPropertyRelative("enumOptions");
            SerializedProperty lIndex = pVariable.FindPropertyRelative("intValue");

            if (lOptions.arraySize == 0)
            {
                EditorGUILayout.LabelField(pLabel, new GUIContent("No option, add some in the Card Creator"));
                return;
            }

            GUIContent[] lNames = new GUIContent[lOptions.arraySize];

            for (int i = 0; i < lNames.Length; i++)
                lNames[i] = new GUIContent(lOptions.GetArrayElementAtIndex(i).stringValue);

            EditorGUI.BeginChangeCheck();
            int lValue = EditorGUILayout.Popup(pLabel, lIndex.intValue, lNames);

            if (EditorGUI.EndChangeCheck())
                lIndex.intValue = lValue;
        }

        #endregion

        #region Elements
        //deffered display (text OR image)
        private static void DrawElementOptions(SerializedProperty pElement, SerializedProperty pVariables, bool pIsBound)
        {
            bool lIsImage = (CardElementType)pElement.FindPropertyRelative("type").enumValueIndex == CardElementType.Image;

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField(lIsImage ? "Image Options" : "Text Options", EditorStyles.miniBoldLabel);

            if (lIsImage)
                DrawImageOptions(pElement, pVariables, pIsBound);
            else
                DrawTextOptions(pElement, pVariables, pIsBound);
        }

        private static void DrawImageOptions(SerializedProperty pElement, SerializedProperty pVariables, bool pIsBound)
        {
            if (!pIsBound)
                EditorGUILayout.PropertyField(pElement.FindPropertyRelative("sprite"));

            DrawElementColor(pElement, pVariables);
            EditorGUILayout.PropertyField(pElement.FindPropertyRelative("preserveAspect"));
        }

        private static void DrawTextOptions(SerializedProperty pElement, SerializedProperty pVariables, bool pIsBound)
        {
            if (pIsBound)
                EditorGUILayout.PropertyField(pElement.FindPropertyRelative("textFormat"), new GUIContent("Format", "{0} is replaced by the value, e.g. \"Cost: {0}\""));
            else
            {
                EditorGUILayout.LabelField("Text");
                DrawTextArea(pElement.FindPropertyRelative("text"), 36f);
            }

            DrawElementColor(pElement, pVariables);
            EditorGUILayout.PropertyField(pElement.FindPropertyRelative("font"), new GUIContent("Font", "TextMeshPro default font when empty"));
            EditorGUILayout.PropertyField(pElement.FindPropertyRelative("fontSize"));

            SerializedProperty lStyle = pElement.FindPropertyRelative("fontStyle");
            EditorGUI.BeginChangeCheck();
            FontStyles lFontStyle = (FontStyles)EditorGUILayout.EnumFlagsField("Font Style", (FontStyles)lStyle.intValue);

            if (EditorGUI.EndChangeCheck())
                lStyle.intValue = (int)lFontStyle;

            EditorGUILayout.PropertyField(pElement.FindPropertyRelative("alignment"));
            EditorGUILayout.PropertyField(pElement.FindPropertyRelative("autoSize"), new GUIContent("Auto Size", "Shrinks the text when it doesn't fit"));
        }

        private static void DrawElementColor(SerializedProperty pElement, SerializedProperty pVariables)
        {
            string lDriver = FindVariableName(pVariables, pElement.FindPropertyRelative("colorVariableId").stringValue);

            if (lDriver == null)
                EditorGUILayout.PropertyField(pElement.FindPropertyRelative("color"));
            else
                EditorGUILayout.LabelField("Color", "Driven by " + lDriver);
        }

        #endregion

        #region Generated
        //Generated section options (file path, default refferencing on instancing, etc...)
        private void DrawGenerated()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Generated", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cardOutputFolder"), new GUIContent("Output Folder"));

            if (string.IsNullOrWhiteSpace(template.cardOutputFolder))
                EditorGUILayout.LabelField(" ", CardGenerator.GetOutputFolder(template) + " when empty", EditorStyles.miniLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("prefab"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultData"));
            }
        }

        private void DrawGenerateButton()
        {
            string lError = CardGenerator.GetNameError(template.draft.cardName);

            EditorGUILayout.Space(4f);

            using (new EditorGUI.DisabledScope(lError != null))
            {
                if (GUILayout.Button(new GUIContent("Generate", lError), GUILayout.Height(28f)))
                {
                    CardGenerator.Generate(template);
                    GUIUtility.ExitGUI();
                }
            }
        }

        #endregion

        #region Helpers

        private static SerializedProperty FindElement(SerializedProperty pElements, string pVariableId)
        {
            for (int i = 0; i < pElements.arraySize; i++)
            {
                SerializedProperty lElement = pElements.GetArrayElementAtIndex(i);

                if (lElement.FindPropertyRelative("contentVariableId").stringValue == pVariableId)
                    return lElement;
            }

            return null;
        }

        private static string FindVariableName(SerializedProperty pVariables, string pVariableId)
        {
            if (string.IsNullOrEmpty(pVariableId))
                return null;

            for (int i = 0; i < pVariables.arraySize; i++)
            {
                SerializedProperty lVariable = pVariables.GetArrayElementAtIndex(i);

                if (lVariable.FindPropertyRelative("id").stringValue == pVariableId)
                    return lVariable.FindPropertyRelative("name").stringValue;
            }

            return null;
        }

        private static void DrawTextArea(SerializedProperty pProperty, float pMinHeight)
        {
            EditorGUI.BeginChangeCheck();
            string lValue = EditorGUILayout.TextArea(pProperty.stringValue, GUILayout.MinHeight(pMinHeight));

            if (EditorGUI.EndChangeCheck())
                pProperty.stringValue = lValue;
        }

        #endregion
    }
}
