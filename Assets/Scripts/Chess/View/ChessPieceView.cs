using Chess.Core;
using UnityEngine;

namespace Chess.View
{
    /// <summary>
    /// Visual chess piece. Position is driven by ChessBoardView.
    /// </summary>
    public class ChessPieceView : MonoBehaviour
    {
        public PieceType Type { get; private set; }
        public PieceColor Color { get; private set; }
        public Square Square { get; private set; }

        public void Configure(PieceType type, PieceColor color, Square square)
        {
            Type = type;
            Color = color;
            Square = square;
            name = $"{color}_{type}_{square}";
        }

        public void SetSquare(Square square)
        {
            Square = square;
            name = $"{Color}_{Type}_{square}";
        }
    }
}
