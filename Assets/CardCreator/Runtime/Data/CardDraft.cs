using System;
using System.Collections.Generic;

namespace CardCreator
{
    //microclass to store data in syncs, could've been a struct but serialized structs are a pain to deal with
    [Serializable]
    public class CardDraft
    {
        public string cardName = "";
        public List<CardVariable> variables = new List<CardVariable>();
        public List<CardElement> elements = new List<CardElement>();
        public CardFaceSettings front = new CardFaceSettings();
        public CardFaceSettings back = new CardFaceSettings();
    }
}
