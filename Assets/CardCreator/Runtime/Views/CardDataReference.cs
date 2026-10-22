using UnityEngine;

namespace CardCreator
{
    //microcomponent to get the SO from the prefab
    public class CardDataReference : MonoBehaviour
    {
        [SerializeField] private CardData _data;

        public CardData data
        {
            get => _data;
            set => _data = value;
        }
    }
}
