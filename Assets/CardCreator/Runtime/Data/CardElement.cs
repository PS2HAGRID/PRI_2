using System;
using TMPro;
using UnityEngine;

namespace CardCreator
{
    public enum CardElementType
    {
        Image,
        Text
    }


    //generic class for all elements added
    [Serializable]
    public class CardElement
    {
        [HideInInspector] public string id = Guid.NewGuid().ToString("N");
        public string name = "Element";
        public CardElementType type = CardElementType.Image;

        // Center and size in card units relative to the card center
        public Vector2 position;
        [Min(0.05f)] public Vector2 size = Vector2.one;
        public float rotation;
        public Color color = Color.white;

        // Static content, used when no variable drives the content
        public Sprite sprite;
        public bool preserveAspect = true;
        public string text = "Text";

        // Text style, the font size is expressed in canvas pixels (see CardTemplate.pixelsPerUnit)
        public TMP_FontAsset font;
        [Min(1f)] public float fontSize = 24f;
        public FontStyles fontStyle = FontStyles.Normal;
        public TextAlignmentOptions alignment = TextAlignmentOptions.Center;
        public bool autoSize;
        public string textFormat = "{0}";

        // Bound variable ids, editor/SO stuff
        public string contentVariableId;
        public string colorVariableId;
        public string visibilityVariableId;
    }
}
