using UnityEngine;

namespace CardCreator
{
    //base class for prefab comps
    public abstract class CardView : MonoBehaviour
    {
        [SerializeField] protected CardData m_data;

        public CardData data => m_data;

        protected virtual void Awake()
        {
            if (m_data != null)
                Refresh();
        }

        public void SetData(CardData pData)
        {
            m_data = pData;
            Refresh();
        }

        public abstract void Refresh();
    }
}


