using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardCreator
{
    public enum CardVariableType
    {
        Int,
        Float,
        String,
        Bool,
        Sprite,
        Color,
        Enum
    }

    //microclass for editor variable handling
    [Serializable]
    public class CardVariable
    {
        [HideInInspector] public string id = Guid.NewGuid().ToString("N");
        public string name = "newVariable";
        public CardVariableType type = CardVariableType.Int;

        //default values
        public int intValue;
        public float floatValue;
        public string stringValue = "";
        public bool boolValue = true;
        public Sprite spriteValue;
        public Color colorValue = Color.white;

        //generates a [TextArea] field
        public bool multiline;
        public List<string> enumOptions = new List<string>();

        //names written by previous generations, turned into [FormerlySerializedAs] so renaming keeps card values
        [HideInInspector] public List<string> formerNames = new List<string>();
    }
}
