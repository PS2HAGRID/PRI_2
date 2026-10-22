using System.Collections.Generic;
using UnityEngine;

namespace CardCreator
{
    //SO of the FINAL ASSET
    public class CardData : ScriptableObject
    {
        public string cardName;
        public List<CardVariable> values = new List<CardVariable>();

        public Color frontColor = Color.white;
        public Sprite frontSprite;
        public Color backColor = Color.white;
        public Sprite backSprite;

        public CardVariable GetValue(string pName)
        {
            foreach (CardVariable lValue in values)
            {
                if (lValue.name == pName)
                    return lValue;
            }

            return null;
        }

        public int GetInt(string pName)
        {
            CardVariable lValue = GetValue(pName);
            return lValue != null ? lValue.intValue : 0;
        }

        public float GetFloat(string pName)
        {
            CardVariable lValue = GetValue(pName);
            return lValue != null ? lValue.floatValue : 0f;
        }

        public string GetString(string pName)
        {
            CardVariable lValue = GetValue(pName);
            return lValue != null ? lValue.stringValue : null;
        }

        public bool GetBool(string pName)
        {
            CardVariable lValue = GetValue(pName);
            return lValue != null && lValue.boolValue;
        }

        public Sprite GetSprite(string pName)
        {
            CardVariable lValue = GetValue(pName);
            return lValue != null ? lValue.spriteValue : null;
        }

        public Color GetColor(string pName)
        {
            CardVariable lValue = GetValue(pName);
            return lValue != null ? lValue.colorValue : Color.clear;
        }

        public string GetEnum(string pName)
        {
            CardVariable lValue = GetValue(pName);

            if (lValue == null || lValue.intValue < 0 || lValue.intValue >= lValue.enumOptions.Count)
                return null;

            return lValue.enumOptions[lValue.intValue];
        }
    }
}
