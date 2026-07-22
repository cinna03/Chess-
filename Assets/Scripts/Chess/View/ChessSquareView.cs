using UnityEngine;

namespace Chess.View
{
    /// <summary>
    /// One board square: visual + click/tap target.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ChessSquareView : MonoBehaviour
    {
        public int File { get; private set; }
        public int Rank { get; private set; }

        Renderer _renderer;
        Color _baseColor;
        MaterialPropertyBlock _block;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public void Initialize(int file, int rank, Color baseColor)
        {
            File = file;
            Rank = rank;
            _baseColor = baseColor;
            _renderer = GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();
            SetHighlight(SquareHighlight.None);
        }

        public void SetHighlight(SquareHighlight highlight)
        {
            if (_renderer == null)
                return;

            var color = highlight switch
            {
                SquareHighlight.Selected => Color.Lerp(_baseColor, Color.yellow, 0.55f),
                SquareHighlight.LegalMove => Color.Lerp(_baseColor, Color.green, 0.45f),
                SquareHighlight.LegalCapture => Color.Lerp(_baseColor, Color.red, 0.45f),
                _ => _baseColor
            };

            _renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            _block.SetColor(ColorId, color);
            _renderer.SetPropertyBlock(_block);
        }
    }

    public enum SquareHighlight
    {
        None,
        Selected,
        LegalMove,
        LegalCapture
    }
}
