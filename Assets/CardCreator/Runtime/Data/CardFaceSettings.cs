using System;
using UnityEngine;

namespace CardCreator
{
    //microclass for the card's backgroud (color or image)
    [Serializable]
    public class CardFaceSettings
    {
        public Color color = Color.white;
        public Sprite sprite;

        public bool isColorPerCard;
        public bool isSpritePerCard;
    }
}
