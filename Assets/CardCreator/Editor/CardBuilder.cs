using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CardCreator.Editor
{
    
    //GO produced by a card build
    public class CardBuildResult
    {
        public GameObject root;
        public GameObject front;
        public GameObject back;
        public readonly Dictionary<string, GameObject> elementObjects = new Dictionary<string, GameObject>();

        //Meshes and materials created by the build
        public readonly List<Object> generatedAssets = new List<Object>();
    }

    //Builds the card hierarchy described by the provided template
    public class CardBuilder
    {
        private readonly CardTemplate _template;
        private readonly HideFlags _hideFlags;
        private readonly CardBuildResult _result = new CardBuildResult();

        private CardBuilder(CardTemplate pTemplate, HideFlags pHideFlags)
        {
            _template = pTemplate;
            _hideFlags = pHideFlags;
        }

        //Builds card in the active scene
        public static CardBuildResult Build(CardTemplate pTemplate, HideFlags pHideFlags = HideFlags.None)
        {
            CardBuilder lBuilder = new CardBuilder(pTemplate, pHideFlags);
            List<Vector2> lOutline = CardShapeUtility.BuildOutline(pTemplate.shape);

            if (pTemplate.renderMode == CardRenderMode.UI)
                lBuilder.BuildUI(lOutline);
            else
                lBuilder.BuildWorld(lOutline);

            return lBuilder._result;
        }

        #region Getters
        //Getters

        public static string GetElementText(CardTemplate pTemplate, CardElement pElement)
        {
            CardVariable lVariable = pTemplate.FindVariable(pElement.contentVariableId);

            if (lVariable == null)
                return pElement.text;

            string lValue = GetValueText(lVariable);
            return string.IsNullOrEmpty(pElement.textFormat) ? lValue : pElement.textFormat.Replace("{0}", lValue);
        }

        public static Sprite GetElementSprite(CardTemplate pTemplate, CardElement pElement)
        {
            CardVariable lVariable = pTemplate.FindVariable(pElement.contentVariableId);
            return lVariable != null ? lVariable.spriteValue : pElement.sprite;
        }

        public static bool IsElementVisible(CardTemplate pTemplate, CardElement pElement)
        {
            CardVariable lVariable = pTemplate.FindVariable(pElement.visibilityVariableId);
            return lVariable == null || lVariable.boolValue;
        }

        private static Color GetElementColor(CardTemplate pTemplate, CardElement pElement)
        {
            CardVariable lVariable = pTemplate.FindVariable(pElement.colorVariableId);
            return lVariable != null ? lVariable.colorValue : pElement.color;
        }

        private static string GetValueText(CardVariable pVariable)
        {
            switch (pVariable.type)
            {
                case CardVariableType.Int:
                    return pVariable.intValue.ToString();
                case CardVariableType.Float:
                    return pVariable.floatValue.ToString();
                case CardVariableType.Enum:
                    return pVariable.intValue >= 0 && pVariable.intValue < pVariable.enumOptions.Count ? pVariable.enumOptions[pVariable.intValue] : "";
                default:
                    return pVariable.stringValue;
            }
        }

        #endregion

        //Builds a UI version of the card, for 2D cards only
        private void BuildUI(List<Vector2> pOutline)
        {
            float lPixelsPerUnit = _template.pixelsPerUnit;
            List<Vector2> lNormalizedOutline = CardShapeUtility.Normalize(pOutline, _template.shape.size);

            _result.root = CreateUIObject(_template.templateName, null);
            ((RectTransform)_result.root.transform).sizeDelta = _template.shape.size * lPixelsPerUnit;

            _result.front = CreateUIObject("Front", _result.root.transform);
            Stretch(_result.front);

            CardShapeGraphic lBody = CreateUIObject("Body", _result.front.transform).AddComponent<CardShapeGraphic>();
            Stretch(lBody.gameObject);
            lBody.SetOutline(lNormalizedOutline);
            lBody.sprite = _template.GetFrontSprite();
            lBody.color = _template.front.color;

            foreach (CardElement lElement in _template.elements)
            {
                GameObject lObject = CreateUIObject(lElement.name, _result.front.transform);

                if (lElement.type == CardElementType.Image)
                {
                    Image lImage = lObject.AddComponent<Image>();
                    lImage.sprite = GetElementSprite(_template, lElement);
                    lImage.enabled = lImage.sprite != null;
                    lImage.preserveAspect = lElement.preserveAspect;
                    lImage.color = GetElementColor(_template, lElement);
                    lImage.raycastTarget = false;
                }
                else ApplyTextStyle(lElement, lObject.AddComponent<TextMeshProUGUI>(), lElement.fontSize);

                RectTransform lTransform = (RectTransform)lObject.transform;
                lTransform.anchoredPosition = lElement.position * lPixelsPerUnit;
                lTransform.sizeDelta = lElement.size * lPixelsPerUnit;
                lTransform.localEulerAngles = new Vector3(0f, 0f, lElement.rotation);

                lObject.SetActive(IsElementVisible(_template, lElement));
                _result.elementObjects[lElement.id] = lObject;
            }

            _result.back = CreateUIObject("Back", _result.root.transform);
            Stretch(_result.back);

            CardShapeGraphic lBack = _result.back.AddComponent<CardShapeGraphic>();
            lBack.SetOutline(lNormalizedOutline);
            lBack.sprite = _template.back.sprite;
            lBack.color = _template.back.color;
            _result.back.SetActive(false);
        }

        //Builds a World version of the card, for 3D cards
        private void BuildWorld(List<Vector2> pOutline)
        {
            CardShapeSettings lShape = _template.shape;
            bool lIs3D = _template.renderMode == CardRenderMode.World3D;
            Sprite lFrontSprite = _template.GetFrontSprite();
            Rect lFrontUV = CardShapeUtility.GetSpriteUVRect(lFrontSprite);
            Rect lBackUV = CardShapeUtility.GetSpriteUVRect(_template.back.sprite);

            _result.root = CreateObject(_template.templateName, null);
            _result.front = CreateObject("Front", _result.root.transform);

            GameObject lBody = CreateObject("Body", _result.front.transform);

            if (lIs3D)
            {
                //A single double-sided mesh, the back face is always there
                Mesh lMesh = CardMeshBuilder.Build(pOutline, lShape.size, lFrontUV, lBackUV, true);
                Material lFront = CreateLitMaterial("Front", lFrontSprite, _template.front.color);
                Material lBack = CreateLitMaterial("Back", _template.back.sprite, _template.back.color);
                AddMeshRenderer(lBody, lMesh, new[] { lFront, lBack });
            }
            else
            {
                Mesh lMesh = CardMeshBuilder.Build(pOutline, lShape.size, lFrontUV, lBackUV, false);
                AddMeshRenderer(lBody, lMesh, new[] { CreateSpriteMaterial("Front", lFrontSprite, _template.front.color) });

                //The back of a 2D card is a separate face shown when the card is turned over
                _result.back = CreateObject("Back", _result.root.transform);
                Mesh lBackMesh = CardMeshBuilder.Build(pOutline, lShape.size, lBackUV, lBackUV, false);
                lBackMesh.name = "CardBack";
                AddMeshRenderer(_result.back, lBackMesh, new[] { CreateSpriteMaterial("Back", _template.back.sprite, _template.back.color) });
                _result.back.SetActive(false);
            }

            for (int i = 0; i < _template.elements.Count; i++)
            {
                CardElement lElement = _template.elements[i];
                GameObject lObject;

                if (lElement.type == CardElementType.Image)
                {
                    lObject = CreateObject(lElement.name, _result.front.transform);

                    SpriteRenderer lRenderer = CreateObject("Sprite", lObject.transform).AddComponent<SpriteRenderer>();
                    lRenderer.sprite = GetElementSprite(_template, lElement);
                    lRenderer.color = GetElementColor(_template, lElement);
                    lRenderer.sortingOrder = i + 1;

                    lObject.AddComponent<CardWorldImage>().Setup(lRenderer, lElement.size, lElement.preserveAspect);
                }
                else
                {
                    lObject = CreateUIObject(lElement.name, _result.front.transform);

                    //World texts are 10 times smaller than UI texts for the same font size
                    TextMeshPro lText = lObject.AddComponent<TextMeshPro>();
                    ApplyTextStyle(lElement, lText, lElement.fontSize / _template.pixelsPerUnit * 10f);
                    lText.sortingOrder = i + 1;
                    ((RectTransform)lObject.transform).sizeDelta = lElement.size;
                }

                //slight offsets so 3D cards don't z-fight
                float lEpsilon = -0.001f;
                lObject.transform.localPosition = new Vector3(lElement.position.x, lElement.position.y, lIs3D ? lEpsilon * (i + 1) : 0f);
                lObject.transform.localEulerAngles = new Vector3(0f, 0f, lElement.rotation);
                lObject.SetActive(IsElementVisible(_template, lElement));
                _result.elementObjects[lElement.id] = lObject;
            }
        }

        private void ApplyTextStyle(CardElement pElement, TMP_Text pText, float pFontSize)
        {
            if (pElement.font != null)
                pText.font = pElement.font;

            pText.text = GetElementText(_template, pElement);
            pText.fontSize = pFontSize;
            pText.fontStyle = pElement.fontStyle;
            pText.alignment = pElement.alignment;
            pText.color = GetElementColor(_template, pElement);
            pText.raycastTarget = false;
            pText.enableAutoSizing = pElement.autoSize;
            pText.fontSizeMax = pFontSize;
            pText.fontSizeMin = pFontSize * 0.2f; //Why TMP, why is it so long to just switch style
        }

        private void AddMeshRenderer(GameObject pObject, Mesh pMesh, Material[] pMaterials)
        {
            pObject.AddComponent<MeshFilter>().sharedMesh = pMesh;

            MeshRenderer lRenderer = pObject.AddComponent<MeshRenderer>();
            lRenderer.sharedMaterials = pMaterials;

            if (pMaterials.Length == 1)
            {
                lRenderer.shadowCastingMode = ShadowCastingMode.Off;
                lRenderer.receiveShadows = false;
            }

            pMesh.hideFlags = _hideFlags;
            _result.generatedAssets.Add(pMesh);

            foreach (Material lMaterial in pMaterials)
            {
                lMaterial.hideFlags = _hideFlags;
                _result.generatedAssets.Add(lMaterial);
            }
        }

        private static Material CreateSpriteMaterial(string pName, Sprite pSprite, Color pColor)
        {
            return new Material(Shader.Find("Sprites/Default"))
            {
                name = pName,
                mainTexture = pSprite != null ? pSprite.texture : null,
                color = pColor
            };
        }

        private static Material CreateLitMaterial(string pName, Sprite pSprite, Color pColor)
        {
            Shader lShader = GraphicsSettings.currentRenderPipeline.defaultShader; //Couldn't remember the name of the default lit shader ngl, and this one is more universal anyway

            Material lMaterial = new Material(lShader)
            {
                name = pName,
                mainTexture = pSprite != null ? pSprite.texture : null,
                color = pColor
            };

            //Alpha clipping cuts transparent sprite pixels on the card edges
            lMaterial.SetFloat("_AlphaClip", 1f);
            lMaterial.SetFloat("_Cutoff", 0.5f);
            lMaterial.EnableKeyword("_ALPHATEST_ON");
            lMaterial.renderQueue = (int)RenderQueue.AlphaTest;
            
            return lMaterial;
        }

        private GameObject CreateObject(string pName, Transform pParent)
        {
            GameObject lObject = new GameObject(pName) { hideFlags = _hideFlags };
            lObject.transform.SetParent(pParent, false);
            return lObject;
        }

        private GameObject CreateUIObject(string pName, Transform pParent)
        {
            GameObject lObject = new GameObject(pName, typeof(RectTransform)) { hideFlags = _hideFlags };
            lObject.transform.SetParent(pParent, false);
            return lObject;
        }

        private static void Stretch(GameObject pObject)
        {
            RectTransform lTransform = (RectTransform)pObject.transform;
            lTransform.anchorMin = Vector2.zero;
            lTransform.anchorMax = Vector2.one;
            lTransform.offsetMin = Vector2.zero;
            lTransform.offsetMax = Vector2.zero;
        }
    }
}
