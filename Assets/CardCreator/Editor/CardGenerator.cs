using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardCreator.Editor
{
    //This generates the prefab from the editor SO
    public static class CardGenerator
    {
        internal static readonly string[] InspectorFields = { "draft", "cardOutputFolder", "prefab", "defaultData" };

        public static string GetOutputFolder(CardTemplate pTemplate)
        {
            string lFolder = string.IsNullOrWhiteSpace(pTemplate.cardOutputFolder) ? "Assets/Cards/" + pTemplate.templateName : pTemplate.cardOutputFolder.Trim();
            return lFolder.Replace('\\', '/').TrimEnd('/');
        }

        public static bool SyncDraft(CardTemplate pTemplate)
        {
            return CardDraftSync.Sync(pTemplate.draft, pTemplate.variables, pTemplate.elements, pTemplate.front, pTemplate.back);
        }

        //reset between confirms, ease of use
        public static void ResetDraft(CardTemplate pTemplate)
        {
            CardDraftSync.Reset(pTemplate.draft, pTemplate.variables, pTemplate.elements, pTemplate.front, pTemplate.back);
        }

        public static string GetNameError(string pName)
        {
            if (string.IsNullOrWhiteSpace(pName))
                return "Give the card a name first.";

            if (pName.Trim().IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return $"\"{pName.Trim()}\" can't be used as a file name, remove characters like / \\ : * ? \" < > |";

            return null;
        }

        public static string GetFolderError(string pFolder)
        {
            if (pFolder != "Assets" && !pFolder.StartsWith("Assets/"))
                return $"The output folder \"{pFolder}\" must be inside the Assets folder.";

            return null;
        }

        //this is where we actually make the card prefab and SO, UI-Mode only, the 3D is still WIP since it doesn't work properly in the editor
        public static void Generate(CardTemplate pTemplate)
        {
            SyncDraft(pTemplate);

            string lFolder = GetOutputFolder(pTemplate);
            string lError = GetNameError(pTemplate.draft.cardName) ?? GetFolderError(lFolder);

            if (lError != null)
            {
                EditorUtility.DisplayDialog("Card Creator", lError, "OK");
                return;
            }

            string lPrefabFolder = lFolder + "/Prefabs";
            string lDataFolder = lFolder + "/Data";
            CardCreatorWindow.CreateFolders(lPrefabFolder);
            CardCreatorWindow.CreateFolders(lDataFolder);

            string lName = GetUniqueName(lPrefabFolder, lDataFolder, pTemplate.draft.cardName.Trim());
            string lPrefabPath = $"{lPrefabFolder}/{lName}.prefab";
            string lDataPath = $"{lDataFolder}/{lName}.asset";

            CardTemplate lCard = CreateCardTemplate(pTemplate);
            CardData lData = CreateData(lCard, lName);
            AssetDatabase.CreateAsset(lData, lDataPath);

            GameObject lPrefab = null;
            Scene lScene = EditorSceneManager.NewPreviewScene();

            try //this is why i hate working with files, "try/finally" are we in C??? is it 1999???
            {
                CardBuildResult lBuild = CardBuilder.Build(lCard);
                SceneManager.MoveGameObjectToScene(lBuild.root, lScene);
                lBuild.root.name = lName;

                foreach (Object lAsset in lBuild.generatedAssets)
                {
                    lAsset.hideFlags = HideFlags.HideInHierarchy;
                    AssetDatabase.AddObjectToAsset(lAsset, lData);
                }

                lBuild.root.AddComponent<CardDataReference>().data = lData;
                lPrefab = PrefabUtility.SaveAsPrefabAsset(lBuild.root, lPrefabPath);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(lScene);
                Object.DestroyImmediate(lCard);
            }

            if (lPrefab == null)
            {
                AssetDatabase.DeleteAsset(lDataPath);
                EditorUtility.DisplayDialog("Card Creator", $"The prefab \"{lPrefabPath}\" couldn't be saved, see the console for details.", "OK");
                return;
            }

            AssetDatabase.SaveAssetIfDirty(lData);

            Undo.RecordObject(pTemplate, "Generate Card");
            pTemplate.prefab = lPrefab;
            pTemplate.defaultData = lData;
            ResetDraft(pTemplate);
            EditorUtility.SetDirty(pTemplate);
            AssetDatabase.SaveAssetIfDirty(pTemplate);

            EditorGUIUtility.PingObject(lPrefab);
        }

        //helpers
        private static CardTemplate CreateCardTemplate(CardTemplate pTemplate)
        {
            CardTemplate lCard = Object.Instantiate(pTemplate);
            lCard.hideFlags = HideFlags.HideAndDontSave;
            lCard.variables = lCard.draft.variables;
            lCard.elements = lCard.draft.elements;
            lCard.front = lCard.draft.front;
            lCard.back = lCard.draft.back;
            return lCard;
        }

        private static CardData CreateData(CardTemplate pCard, string pName)
        {
            CardData lData = ScriptableObject.CreateInstance<CardData>();
            lData.cardName = pName;
            lData.values = pCard.variables.ConvertAll(CardDraftSync.CopyVariable);
            lData.frontColor = pCard.front.color;
            lData.frontSprite = pCard.GetFrontSprite();
            lData.backColor = pCard.back.color;
            lData.backSprite = pCard.back.sprite;
            return lData;
        }

        private static string GetUniqueName(string pPrefabFolder, string pDataFolder, string pName)
        {
            string lName = pName;

            for (int i = 1; File.Exists($"{pPrefabFolder}/{lName}.prefab") || File.Exists($"{pDataFolder}/{lName}.asset"); i++)
                lName = $"{pName} {i}";

            return lName;
        }
    }
}
