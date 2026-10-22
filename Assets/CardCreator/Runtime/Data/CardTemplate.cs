using System.Collections.Generic;
using UnityEngine;

namespace CardCreator
{
    public enum CardRenderMode
    {
        //2D, built with a Canvas (Image / TextMeshProUGUI)
        UI,
        //2D, built with renderers
        World2D,
        //Flat double-sided mesh
        World3D
    }

    //SO of the TEMPLATE
    public class CardTemplate : ScriptableObject
    {
        public string templateName = "NewCard";
        public CardRenderMode renderMode = CardRenderMode.UI;

        //Canvas pixels per card unit, used by the UI
        [Min(1f)] public float pixelsPerUnit = 100f;

        public CardShapeSettings shape = new CardShapeSettings();
        public CardFaceSettings front = new CardFaceSettings();
        public CardFaceSettings back = new CardFaceSettings { color = new Color(0.15f, 0.2f, 0.35f) };

        public List<CardVariable> variables = new List<CardVariable>();

        public List<CardElement> elements = new List<CardElement>();

        [Header("Generated")]
        public string outputFolder;
        public string codeNamespace;
        public GameObject prefab;
        public CardData defaultData;

        public string cardOutputFolder;
        public CardDraft draft = new CardDraft();

        //True if the shape sprite is drawn on the front instead of the front sprite
        public bool UsesShapeSprite()
        {
            return shape.source == CardShapeSource.Sprite && shape.spriteUsage == CardSpriteUsage.Visual && shape.sprite != null;
        }

        public Sprite GetFrontSprite()
        {
            return UsesShapeSprite() ? shape.sprite : front.sprite;
        }

        public CardVariable FindVariable(string pId)
        {
            foreach (CardVariable lVariable in variables)
            {
                if (lVariable.id == pId)
                    return lVariable;
            }

            return null;
        }

        public CardElement FindElement(string pId)
        {
            foreach (CardElement lElement in elements)
            {
                if (lElement.id == pId)
                    return lElement;
            }

            return null;
        }

        //display of the value of the given variable, null when the variable isn't shown
        public CardElement FindContentElement(string pVariableId)
        {
            foreach (CardElement lElement in elements)
            {
                if (lElement.contentVariableId == pVariableId)
                    return lElement;
            }

            return null;
        }
    }
}
