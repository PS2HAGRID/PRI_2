using UnityEngine;

namespace CardCreator
{

    //world renderer, deprecated
    public class CardWorldImage : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Vector2 _size = Vector2.one;
        [SerializeField] private bool _preserveAspect = true;

        public SpriteRenderer spriteRenderer => _renderer;

        public Sprite sprite
        {
            get => _renderer.sprite;
            set
            {
                _renderer.sprite = value;
                Fit();
            }
        }

        public Color color
        {
            get => _renderer.color;
            set => _renderer.color = value;
        }

        public void Setup(SpriteRenderer pRenderer, Vector2 pSize, bool pPreserveAspect)
        {
            _renderer = pRenderer;
            _size = pSize;
            _preserveAspect = pPreserveAspect;
            Fit();
        }

        private void Fit()
        {
            Sprite lSprite = _renderer.sprite;

            if (lSprite == null)
                return;

            Vector2 lScale = _size / (Vector2)lSprite.bounds.size;

            if (_preserveAspect)
                lScale = Vector2.one * Mathf.Min(lScale.x, lScale.y);

            Transform lTransform = _renderer.transform;
            lTransform.localScale = new Vector3(lScale.x, lScale.y, 1f);
            //Recenters sprites
            lTransform.localPosition = -Vector2.Scale(lSprite.bounds.center, lScale);
        }
    }
}
