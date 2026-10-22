using System.Collections.Generic;

namespace CardCreator.Editor
{
    //sync file so changes arent lost on reload/recompile/unfocus etc...
    public static class CardDraftSync
    {
        public static bool Sync(CardDraft pDraft, List<CardVariable> pVariables, List<CardElement> pElements, CardFaceSettings pFront, CardFaceSettings pBack)
        {
            bool lHasChanged = false;
            HashSet<object> lUsed = new HashSet<object>();

            List<CardVariable> lVariables = new List<CardVariable>(pVariables.Count);

            foreach (CardVariable lSource in pVariables)
            {
                CardVariable lDraft = pDraft.variables.Find(pVariable => pVariable.id == lSource.id && !lUsed.Contains(pVariable));

                if (lDraft == null)
                {
                    lDraft = CopyVariable(lSource);
                    lHasChanged = true;
                }
                else
                    lHasChanged |= SyncVariable(lSource, lDraft);

                lUsed.Add(lDraft);
                lVariables.Add(lDraft);
            }

            List<CardElement> lElements = new List<CardElement>(pElements.Count);

            foreach (CardElement lSource in pElements)
            {
                CardElement lDraft = pDraft.elements.Find(pElement => pElement.id == lSource.id && !lUsed.Contains(pElement));

                if (lDraft == null)
                {
                    lDraft = CopyElement(lSource);
                    lHasChanged = true;
                }
                else
                    lHasChanged |= SyncElement(lSource, lDraft);

                lUsed.Add(lDraft);
                lElements.Add(lDraft);
            }

            lHasChanged |= !IsSameList(pDraft.variables, lVariables) || !IsSameList(pDraft.elements, lElements);
            pDraft.variables = lVariables;
            pDraft.elements = lElements;

            if (pDraft.front == null)
            {
                pDraft.front = CopyFace(pFront);
                lHasChanged = true;
            }
            else
                lHasChanged |= SyncFace(pFront, pDraft.front);

            if (pDraft.back == null)
            {
                pDraft.back = CopyFace(pBack);
                lHasChanged = true;
            }
            else
                lHasChanged |= SyncFace(pBack, pDraft.back);

            return lHasChanged;
        }

        public static void Reset(CardDraft pDraft, List<CardVariable> pVariables, List<CardElement> pElements, CardFaceSettings pFront, CardFaceSettings pBack)
        {
            pDraft.cardName = "";
            pDraft.variables = pVariables.ConvertAll(CopyVariable);
            pDraft.elements = pElements.ConvertAll(CopyElement);
            pDraft.front = CopyFace(pFront);
            pDraft.back = CopyFace(pBack);
        }

        public static CardVariable CopyVariable(CardVariable pSource)
        {
            CardVariable lCopy = new CardVariable();
            CopyVariableSettings(pSource, lCopy);
            CopyVariableValues(pSource, lCopy);
            return lCopy;
        }

        public static CardElement CopyElement(CardElement pSource)
        {
            CardElement lCopy = new CardElement();
            CopyElementLayout(pSource, lCopy);
            CopyElementValues(pSource, lCopy);
            return lCopy;
        }

        public static CardFaceSettings CopyFace(CardFaceSettings pSource)
        {
            return new CardFaceSettings
            {
                color = pSource.color,
                sprite = pSource.sprite,
                isColorPerCard = pSource.isColorPerCard,
                isSpritePerCard = pSource.isSpritePerCard
            };
        }

        private static bool SyncVariable(CardVariable pSource, CardVariable pDraft)
        {
            bool lHasTypeChanged = pSource.type != pDraft.type;
            bool lHasChanged = CopyVariableSettings(pSource, pDraft);

            if (lHasTypeChanged)
                CopyVariableValues(pSource, pDraft);

            return lHasChanged;
        }

        private static bool SyncElement(CardElement pSource, CardElement pDraft)
        {
            bool lHasTypeChanged = pSource.type != pDraft.type;
            bool lHasChanged = CopyElementLayout(pSource, pDraft);

            if (lHasTypeChanged)
                CopyElementValues(pSource, pDraft);

            return lHasChanged;
        }

        private static bool SyncFace(CardFaceSettings pSource, CardFaceSettings pDraft)
        {
            bool lHasChanged = Set(ref pDraft.isColorPerCard, pSource.isColorPerCard);
            lHasChanged |= Set(ref pDraft.isSpritePerCard, pSource.isSpritePerCard);

            if (!pSource.isColorPerCard)
                lHasChanged |= Set(ref pDraft.color, pSource.color);

            if (!pSource.isSpritePerCard)
                lHasChanged |= Set(ref pDraft.sprite, pSource.sprite);

            return lHasChanged;
        }

        //byte mapping
        private static bool CopyVariableSettings(CardVariable pSource, CardVariable pTarget)
        {
            bool lHasChanged = Set(ref pTarget.id, pSource.id);
            lHasChanged |= Set(ref pTarget.name, pSource.name);
            lHasChanged |= Set(ref pTarget.type, pSource.type);
            lHasChanged |= Set(ref pTarget.multiline, pSource.multiline);
            lHasChanged |= SetList(ref pTarget.enumOptions, pSource.enumOptions);
            lHasChanged |= SetList(ref pTarget.formerNames, pSource.formerNames);
            return lHasChanged;
        }

        private static void CopyVariableValues(CardVariable pSource, CardVariable pTarget)
        {
            pTarget.intValue = pSource.intValue;
            pTarget.floatValue = pSource.floatValue;
            pTarget.stringValue = pSource.stringValue;
            pTarget.boolValue = pSource.boolValue;
            pTarget.spriteValue = pSource.spriteValue;
            pTarget.colorValue = pSource.colorValue;
        }

        //more byte mapping
        private static bool CopyElementLayout(CardElement pSource, CardElement pTarget)
        {
            bool lHasChanged = Set(ref pTarget.id, pSource.id);
            lHasChanged |= Set(ref pTarget.name, pSource.name);
            lHasChanged |= Set(ref pTarget.type, pSource.type);
            lHasChanged |= Set(ref pTarget.position, pSource.position);
            lHasChanged |= Set(ref pTarget.size, pSource.size);
            lHasChanged |= Set(ref pTarget.rotation, pSource.rotation);
            lHasChanged |= Set(ref pTarget.contentVariableId, pSource.contentVariableId);
            lHasChanged |= Set(ref pTarget.colorVariableId, pSource.colorVariableId);
            lHasChanged |= Set(ref pTarget.visibilityVariableId, pSource.visibilityVariableId);
            return lHasChanged;
        }

        private static void CopyElementValues(CardElement pSource, CardElement pTarget)
        {
            pTarget.color = pSource.color;
            pTarget.sprite = pSource.sprite;
            pTarget.preserveAspect = pSource.preserveAspect;
            pTarget.text = pSource.text;
            pTarget.font = pSource.font;
            pTarget.fontSize = pSource.fontSize;
            pTarget.fontStyle = pSource.fontStyle;
            pTarget.alignment = pSource.alignment;
            pTarget.autoSize = pSource.autoSize;
            pTarget.textFormat = pSource.textFormat;
        }


        //Helpers, templated for style points

        private static bool Set<T>(ref T pField, T pValue)
        {
            bool lResult = false;

            if (EqualityComparer<T>.Default.Equals(pField, pValue))
                return lResult;

            pField = pValue;
            lResult = true;
            return lResult;
        }

        private static bool SetList(ref List<string> pField, List<string> pValue)
        {
            if (IsSameStrings(pField, pValue))
                return false;

            pField = pValue != null ? new List<string>(pValue) : new List<string>();
            return true;
        }

        private static bool IsSameStrings(List<string> pA, List<string> pB)
        {
            if (pA == null || pB == null)
                return pA == pB;

            if (pA.Count != pB.Count)
                return false;

            for (int i = 0; i < pA.Count; i++)
            {
                if (pA[i] != pB[i])
                    return false;
            }

            return true;
        }

        private static bool IsSameList<T>(List<T> pA, List<T> pB) where T : class
        {
            if (pA.Count != pB.Count)
                return false;

            for (int i = 0; i < pA.Count; i++)
            {
                if (pA[i] != pB[i])
                    return false;
            }

            return true;
        }
    }
}
