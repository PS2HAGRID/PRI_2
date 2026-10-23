using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace CardCreator.Editor
{
    //Card template editor: live preview on the left, card settings and variables on the right
    // The window edits a COPY of the template, written back to the asset when saving
    public partial class CardCreatorWindow : EditorWindow
    {
        [SerializeField] private CardTemplate _Template;
        [SerializeField] private CardTemplate _TemplateAsset;
        [SerializeField] private bool _isDirty;
        [SerializeField] private float _inspectorWidth = 380f;

        private SerializedObject _serializedTemplate;
        private bool _isDraggingSplitter;

        [MenuItem("Tools/Card Creator")]
        private static void OpenWindow()
        {
            GetWindow<CardCreatorWindow>();
        }

        public static void Open(CardTemplate pTemplate)
        {
            CardCreatorWindow lWindow = GetWindow<CardCreatorWindow>();
            lWindow.LoadTemplate(pTemplate);
            lWindow.Focus();
        }

        [OnOpenAsset]
        private static bool OnOpenAsset(EntityId pInstanceId, int pLine)
        {
            if (EditorUtility.EntityIdToObject(pInstanceId) is CardTemplate lTemplate)
            {
                //Debug.Log(lTemplate.ToString());
                Open(lTemplate);
                return true;
            }

            return false;
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Card Creator");
            minSize = new Vector2(720f, 420f);
            saveChangesMessage = "This card template has unsaved changes. Do you want to save them?";

            if (_Template == null)
                _Template = CreateWorkingCopy(null);

            hasUnsavedChanges = _isDirty;
            _Preview = new CardPreviewRenderer();
            _isPreviewDirty = true;
            RefreshTextResources();

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            _Preview.Dispose();
        }

        private void OnDestroy()
        {
            Undo.ClearUndo(_Template);
            DestroyImmediate(_Template);
        }

        private void OnProjectChange()
        {
            RefreshTextResources();
            _isPreviewDirty = true;
            Repaint();
        }

        public override void SaveChanges()
        {
            Save();

            if (!_isDirty)
                base.SaveChanges();
        }

        private void OnGUI()
        {
            if (_serializedTemplate == null || _serializedTemplate.targetObject != _Template)
                _serializedTemplate = new SerializedObject(_Template);

            _serializedTemplate.Update();

            DrawToolbar();

            float lTop = EditorStyles.toolbar.fixedHeight;
            _inspectorWidth = Mathf.Clamp(_inspectorWidth, 300f, position.width - 300f);

            Rect lPreviewRect = new Rect(0f, lTop, position.width - _inspectorWidth, position.height - lTop);
            Rect lInspectorRect = new Rect(lPreviewRect.xMax + 1f, lTop, _inspectorWidth - 1f, lPreviewRect.height);

            HandleSplitter(new Rect(lPreviewRect.xMax - 3f, lTop, 6f, lPreviewRect.height));
            EditorGUI.DrawRect(new Rect(lPreviewRect.xMax, lTop, 1f, lPreviewRect.height), new Color(0f, 0f, 0f, 0.5f));

            GUILayout.BeginArea(lInspectorRect);
            DrawInspector();
            GUILayout.EndArea();

            if (_serializedTemplate.ApplyModifiedProperties())
                OnTemplateChanged();

            DrawPreview(lPreviewRect);
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(45f)))
            {
                NewTemplate();
                GUIUtility.ExitGUI();
            }

            EditorGUI.BeginChangeCheck();
            CardTemplate lAsset = (CardTemplate)EditorGUILayout.ObjectField(_TemplateAsset, typeof(CardTemplate), false, GUILayout.Width(220f));

            if (EditorGUI.EndChangeCheck() && lAsset != null)
            {
                LoadTemplate(lAsset);
                GUIUtility.ExitGUI();
            }

            if (_TemplateAsset == null)
                GUILayout.Label("Not saved yet", EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(_isDirty ? "Save*" : "Save", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            {
                Save();
                GUIUtility.ExitGUI();
            }

            GUILayout.EndHorizontal();
        }

        private void HandleSplitter(Rect pRect)
        {
            EditorGUIUtility.AddCursorRect(pRect, MouseCursor.ResizeHorizontal);
            Event lEvent = Event.current;

            if (lEvent.type == EventType.MouseDown && pRect.Contains(lEvent.mousePosition))
            {
                _isDraggingSplitter = true;
                lEvent.Use();
            }
            else if (_isDraggingSplitter && lEvent.type == EventType.MouseDrag)
            {
                _inspectorWidth = position.width - lEvent.mousePosition.x;
                lEvent.Use();
                Repaint();
            }
            else if (lEvent.type == EventType.MouseUp)
                _isDraggingSplitter = false;
        }

        private void NewTemplate()
        {
            if (ConfirmDiscard())
                ReplaceWorkingCopy(CreateWorkingCopy(null), null);
        }

        private void LoadTemplate(CardTemplate pAsset)
        {
            if (pAsset != _TemplateAsset && ConfirmDiscard())
                ReplaceWorkingCopy(CreateWorkingCopy(pAsset), pAsset);
        }

        private void Save()
        {
            string lError = CardNaming.ValidateCardName(_Template);

            if (lError != null)
            {
                EditorUtility.DisplayDialog("Card Creator", lError, "OK");
                return;
            }

            if (string.IsNullOrEmpty(_Template.outputFolder))
                _Template.outputFolder = "Assets/Cards/" + _Template.templateName;

            CreateFolders(_Template.outputFolder);

            CardTemplate lInspectorState = null;

            if (_TemplateAsset == null)
            {
                _TemplateAsset = CreateInstance<CardTemplate>();
                AssetDatabase.CreateAsset(_TemplateAsset, AssetDatabase.GenerateUniqueAssetPath($"{_Template.outputFolder}/{_Template.templateName}Template.asset"));
            }
            else
            {
                lInspectorState = Instantiate(_TemplateAsset);
                lInspectorState.hideFlags = HideFlags.HideAndDontSave;
            }

            EditorUtility.CopySerialized(_Template, _TemplateAsset);
            _TemplateAsset.hideFlags = HideFlags.None;
            _TemplateAsset.name = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(_TemplateAsset));

            if (lInspectorState != null)
            {
                CopyInspectorFields(lInspectorState, _TemplateAsset);
                DestroyImmediate(lInspectorState);
            }
            EditorUtility.SetDirty(_TemplateAsset);
            AssetDatabase.SaveAssetIfDirty(_TemplateAsset);

            _serializedTemplate.Update();
            SetDirtyState(false);
        }

        private bool ConfirmDiscard()
        {
            if (!_isDirty)
                return true;

            int lChoice = EditorUtility.DisplayDialogComplex("Card Creator", "The current template has unsaved changes.", "Save", "Cancel", "Discard");

            if (lChoice == 0)
            {
                Save();
                return !_isDirty;
            }

            return lChoice == 2;
        }

        //ugly
        private void ReplaceWorkingCopy(CardTemplate pCopy, CardTemplate pAsset)
        {
            //DebugCopy(pCopy);
            //DebugCopy(pAsset);

            Undo.ClearUndo(_Template);
            DestroyImmediate(_Template);

            _Template = pCopy;
            _TemplateAsset = pAsset;
            _serializedTemplate = null;
            _SelectedElementId = null;
            _isEditingShape = false;
            _expandedRows.Clear();

            FramePreview();
            _isPreviewDirty = true;
            SetDirtyState(false);
            Repaint();
        }

        private void DebugCopy(CardTemplate pCopy)
        {
            Debug.Log(pCopy.name);

            foreach (CardVariable var in pCopy.variables) 
            {
                Debug.Log(var);
            }
        }

        ///Applies a change made directly on the template instead of through serialized properties
        private void ModifyTemplate(string pUndoName, Action pChange)
        {
            _serializedTemplate.ApplyModifiedProperties();
            Undo.RecordObject(_Template, pUndoName);
            pChange();
            _serializedTemplate.Update();
            OnTemplateChanged();
        }

        private void OnTemplateChanged()
        {
            //Elements showing a variable are named after it
            foreach (CardElement lElement in _Template.elements)
            {
                CardVariable lVariable = _Template.FindVariable(lElement.contentVariableId);

                if (lVariable != null)
                    lElement.name = lVariable.name;
            }

            _isPreviewDirty = true;
            SetDirtyState(true);
            Repaint();
        }

        private void OnUndoRedo()
        {
            _serializedTemplate?.Update();
            OnTemplateChanged();
        }

        private void SetDirtyState(bool pIsDirty)
        {
            _isDirty = pIsDirty;
            hasUnsavedChanges = pIsDirty;
        }

        //Little known working copy trick, works great we used it everywhere to make, clear and/or copy templates
        private static CardTemplate CreateWorkingCopy(CardTemplate pAsset)
        {
            CardTemplate lCopy = pAsset != null ? Instantiate(pAsset) : CreateInstance<CardTemplate>();

            lCopy.name = pAsset != null ? pAsset.name : "NewCard"; //double ternary? in THIS economy?
            
            lCopy.hideFlags = HideFlags.DontSave;
            return lCopy;
        }

        private static void CopyInspectorFields(CardTemplate pFrom, CardTemplate pTo)
        {
            SerializedObject lFrom = new SerializedObject(pFrom);
            SerializedObject lTo = new SerializedObject(pTo);

            foreach (string lField in CardGenerator.InspectorFields)
                lTo.CopyFromSerializedProperty(lFrom.FindProperty(lField));

            lTo.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void CreateFolders(string pPath)
        {
            string[] lParts = pPath.Split('/');
            string lCurrent = lParts[0];

            for (int i = 1; i < lParts.Length; i++)
            {
                string lNext = lCurrent + "/" + lParts[i];

                if (!AssetDatabase.IsValidFolder(lNext))
                    AssetDatabase.CreateFolder(lCurrent, lParts[i]);

                lCurrent = lNext;
            }
        }
    }
}
