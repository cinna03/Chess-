using System;
using System.Collections.Generic;

namespace Chess.Core
{
    /// <summary>
    /// Hot-seat chess session: selection, legal moves, apply move, reset.
    /// </summary>
    public sealed class ChessGame
    {
        public ChessBoard Board { get; private set; }
        public Square? SelectedSquare { get; private set; }
        public IReadOnlyList<Move> LegalMovesForSelection { get; private set; } = Array.Empty<Move>();

        public event Action OnBoardChanged;
        public event Action<PieceColor> OnTurnChanged;
        public event Action<PieceColor> OnCheck;
        public event Action<string> OnStatusMessage;

        public PieceColor SideToMove => Board.SideToMove;

        public ChessGame()
        {
            Board = new ChessBoard();
            NewGame();
        }

        public void NewGame()
        {
            Board.SetupStartingPosition();
            ClearSelection();
            RaiseBoardChanged();
            OnTurnChanged?.Invoke(Board.SideToMove);
            OnStatusMessage?.Invoke("White to move");
        }

        public void ClearSelection()
        {
            SelectedSquare = null;
            LegalMovesForSelection = Array.Empty<Move>();
        }

        /// <summary>
        /// Tap a square: select own piece, or move to a legal destination.
        /// Returns true if the board state changed.
        /// </summary>
        public bool HandleSquareTap(Square square)
        {
            if (!square.IsValid)
                return false;

            // If a piece is selected and tap is a legal destination → move.
            if (SelectedSquare.HasValue)
            {
                foreach (var move in LegalMovesForSelection)
                {
                    if (move.To == square)
                    {
                        // Prefer queen promotion if multiple promotion moves to same square.
                        var chosen = PreferPromotion(move, LegalMovesForSelection);
                        ApplyMove(chosen);
                        return true;
                    }
                }

                // Tapped another own piece → reselect.
                var piece = Board.GetPiece(square);
                if (!piece.IsEmpty && piece.Color == Board.SideToMove)
                {
                    SelectSquare(square);
                    return false;
                }

                ClearSelection();
                OnBoardChanged?.Invoke();
                return false;
            }

            // No selection: select own piece if present.
            var tapped = Board.GetPiece(square);
            if (!tapped.IsEmpty && tapped.Color == Board.SideToMove)
            {
                SelectSquare(square);
                return false;
            }

            return false;
        }

        public void SelectSquare(Square square)
        {
            SelectedSquare = square;
            LegalMovesForSelection = MoveGenerator.GetLegalMovesFrom(Board, square);
            OnBoardChanged?.Invoke();
        }

        public void ApplyMove(Move move)
        {
            Board.ApplyMove(move);
            ClearSelection();
            RaiseBoardChanged();

            var side = Board.SideToMove;
            OnTurnChanged?.Invoke(side);

            if (MoveGenerator.IsInCheck(Board, side))
            {
                OnCheck?.Invoke(side);
                OnStatusMessage?.Invoke($"{side} is in check — {side} to move");
            }
            else
            {
                OnStatusMessage?.Invoke($"{side} to move");
            }
        }

        static Move PreferPromotion(Move tapped, IReadOnlyList<Move> options)
        {
            Move? queen = null;
            foreach (var m in options)
            {
                if (m.From == tapped.From && m.To == tapped.To)
                {
                    if (m.Promotion == PieceType.Queen)
                        queen = m;
                    else if (m.Promotion == PieceType.None && !queen.HasValue)
                        return m;
                }
            }

            return queen ?? tapped;
        }

        void RaiseBoardChanged() => OnBoardChanged?.Invoke();
    }
}
