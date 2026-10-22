using UnityEditor;
using UnityEngine;

namespace CardCreator.Editor
{
    [CustomEditor(typeof(CardData))]
    //this is the editor-side generated SO of the FINAL ASSET
    public class CardDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            //Making sure all fields are not readonly for quick modifications
            using (new EditorGUI.DisabledScope(false)) //Evil .NET IDisposable black magic stuff
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cardName"));

                SerializedProperty lValues = serializedObject.FindProperty("values");

                if (lValues.arraySize > 0)
                {
                    EditorGUILayout.Space(6f);
                    EditorGUILayout.LabelField("Values", EditorStyles.boldLabel);
                }

                for (int i = 0; i < lValues.arraySize; i++)
                {
                    SerializedProperty lValue = lValues.GetArrayElementAtIndex(i);
                    CardTemplateEditor.DrawVariableValue(lValue, new GUIContent(lValue.FindPropertyRelative("name").stringValue));
                }

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Faces", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("frontColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("frontSprite"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("backColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("backSprite"));
            }
        }
    }
}
