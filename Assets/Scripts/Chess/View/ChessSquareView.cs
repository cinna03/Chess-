using UnityEngine;

namespace Chess.View
{
    /// <summary>
    /// One board square: color highlight + optional move/capture marker.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ChessSquareView : MonoBehaviour
    {
        public int File { get; private set; }
        public int Rank { get; private set; }

        Renderer _renderer;
        Color _baseColor;
        MaterialPropertyBlock _block;
        Transform _marker;
        Renderer _markerRenderer;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public void Initialize(int file, int rank, Color baseColor)
        {
            File = file;
            Rank = rank;
            _baseColor = baseColor;
            _renderer = GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();
            EnsureMarker();
            SetHighlight(SquareHighlight.None);
        }

        void EnsureMarker()
        {
            if (_marker != null)
                return;

            var markerGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            markerGo.name = "MoveMarker";
            markerGo.transform.SetParent(transform, false);
            markerGo.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            markerGo.transform.localScale = new Vector3(0.28f, 0.08f, 0.28f);
            Object.Destroy(markerGo.GetComponent<Collider>());
            _marker = markerGo.transform;
            _markerRenderer = markerGo.GetComponent<Renderer>();
            markerGo.SetActive(false);
        }

        public void SetHighlight(SquareHighlight highlight)
        {
            if (_renderer == null)
                return;

            var color = highlight switch
            {
                SquareHighlight.Selected => Color.Lerp(_baseColor, new Color(1f, 0.92f, 0.35f), 0.6f),
                SquareHighlight.LegalMove => Color.Lerp(_baseColor, new Color(0.45f, 0.85f, 0.55f), 0.25f),
                SquareHighlight.LegalCapture => Color.Lerp(_baseColor, new Color(0.95f, 0.4f, 0.4f), 0.3f),
                SquareHighlight.LastMoveFrom => Color.Lerp(_baseColor, new Color(0.75f, 0.7f, 0.35f), 0.4f),
                SquareHighlight.LastMoveTo => Color.Lerp(_baseColor, new Color(0.85f, 0.78f, 0.4f), 0.5f),
                SquareHighlight.InCheck => Color.Lerp(_baseColor, new Color(1f, 0.2f, 0.25f), 0.65f),
                _ => _baseColor
            };

            _renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            _block.SetColor(ColorId, color);
            _renderer.SetPropertyBlock(_block);

            if (_marker == null)
                return;

            if (highlight == SquareHighlight.LegalMove)
            {
                _marker.gameObject.SetActive(true);
                _marker.localScale = new Vector3(0.28f, 0.08f, 0.28f);
                SetMarkerColor(new Color(0.35f, 0.9f, 0.55f, 0.95f));
            }
            else if (highlight == SquareHighlight.LegalCapture)
            {
                _marker.gameObject.SetActive(true);
                _marker.localScale = new Vector3(0.55f, 0.06f, 0.55f);
                SetMarkerColor(new Color(1f, 0.35f, 0.35f, 0.85f));
            }
            else
            {
                _marker.gameObject.SetActive(false);
            }
        }

        void SetMarkerColor(Color color)
        {
            if (_markerRenderer == null)
                return;
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            _markerRenderer.SetPropertyBlock(block);
        }
    }

    public enum SquareHighlight
    {
        None,
        Selected,
        LegalMove,
        LegalCapture,
        LastMoveFrom,
        LastMoveTo,
        InCheck
    }
}
