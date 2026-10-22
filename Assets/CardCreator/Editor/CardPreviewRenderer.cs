using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CardCreator.Editor
{
    // Previews Cards in ortho view
    public class CardPreviewRenderer : IDisposable
    {
        private readonly PreviewRenderUtility _PreviewUtility = new PreviewRenderUtility();
        private CardBuildResult _build;
        private GameObject _container;

        public CardPreviewRenderer()
        {
            Camera lCamera = _PreviewUtility.camera;
            lCamera.orthographic = true;
            lCamera.nearClipPlane = 0.01f;
            lCamera.farClipPlane = 100f;
            lCamera.clearFlags = CameraClearFlags.SolidColor;
            lCamera.backgroundColor = new Color(0.17f, 0.17f, 0.17f);
            lCamera.transform.rotation = Quaternion.identity;

            // Light coming from the camera side, sligtly from the top left for card/VFX inspection
            _PreviewUtility.lights[0].intensity = 1.1f;
            _PreviewUtility.lights[0].transform.rotation = Quaternion.Euler(25f, 20f, 0f);
            _PreviewUtility.lights[1].intensity = 0f;
            _PreviewUtility.ambientColor = new Color(0.45f, 0.45f, 0.45f);
        }

        public void Rebuild(CardTemplate pTemplate, bool pShowBack)
        {
            Clear();

            _build = CardBuilder.Build(pTemplate, HideFlags.HideAndDontSave);
            _container = _build.root;

            if (pTemplate.renderMode == CardRenderMode.UI)
            {
                // UI cards are displayed by a world space canvas, scled back to card units
                _container = new GameObject("PreviewCanvas", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
                Canvas lCanvas = _container.AddComponent<Canvas>();
                lCanvas.renderMode = RenderMode.WorldSpace;
                lCanvas.worldCamera = _PreviewUtility.camera;
                _container.transform.localScale = Vector3.one / pTemplate.pixelsPerUnit;
                _build.root.transform.SetParent(_container.transform, false);
            }

            if (pShowBack)
            {
                if (pTemplate.renderMode == CardRenderMode.World3D)
                    _build.root.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                else
                {
                    _build.front.SetActive(false);
                    _build.back.SetActive(true);
                }
            }

            SceneManager.MoveGameObjectToScene(_container, _PreviewUtility.camera.scene);

            // Forced text gen
            foreach (TMP_Text lText in _container.GetComponentsInChildren<TMP_Text>())
            {
                if (lText.font != null)
                    lText.ForceMeshUpdate();
            }
        }

        public Texture Render(Rect pRect, Vector2 pCenter, float pOrthographicSize)
        {
            _PreviewUtility.BeginPreview(pRect, GUIStyle.none);

            Camera lCamera = _PreviewUtility.camera;
            lCamera.orthographicSize = pOrthographicSize;
            lCamera.transform.position = new Vector3(pCenter.x, pCenter.y, -10f);

            Canvas.ForceUpdateCanvases();
            _PreviewUtility.Render(true, false);

            return _PreviewUtility.EndPreview();
        }

        public void Dispose()
        {
            Clear();
            _PreviewUtility.Cleanup();
        }

        private void Clear()
        {
            if (_container != null)
                Object.DestroyImmediate(_container);

            if (_build != null)
            {
                foreach (Object lAsset in _build.generatedAssets)
                    Object.DestroyImmediate(lAsset);
            }

            _build = null;
        }
    }
}
