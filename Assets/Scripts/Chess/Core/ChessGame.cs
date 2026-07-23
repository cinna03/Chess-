using System;
using System.Collections.Generic;

namespace Chess.Core
{
    public enum GameResult
    {
        Playing,
        Checkmate,
        Stalemate
    }

    public readonly struct MoveEvent
    {
        public readonly Move Move;
        public readonly Piece Mover;
        public readonly Piece Captured;
        public readonly PieceColor SideThatMoved;
        public readonly PieceColor SideToMoveAfter;
        public readonly bool GaveCheck;
        public readonly GameResult ResultAfterMove;

        public MoveEvent(
            Move move,
            Piece mover,
            Piece captured,
            PieceColor sideThatMoved,
            PieceColor sideToMoveAfter,
            bool gaveCheck,
            GameResult resultAfterMove)
        {
            Move = move;
            Mover = mover;
            Captured = captured;
            SideThatMoved = sideThatMoved;
            SideToMoveAfter = sideToMoveAfter;
            GaveCheck = gaveCheck;
            ResultAfterMove = resultAfterMove;
        }

        public bool WasCapture => !Captured.IsEmpty || Move.IsEnPassant || Move.IsCapture;
    }

    /// <summary>
    /// Hot-seat / vs-AI chess session with endgame detection.
    /// </summary>
    public sealed class ChessGame
    {
        public ChessBoard Board { get; private set; }
        public Square? SelectedSquare { get; private set; }
        public IReadOnlyList<Move> LegalMovesForSelection { get; private set; } = Array.Empty<Move>();
        public GameResult Result { get; private set; } = GameResult.Playing;
        public PieceColor? Winner { get; private set; }

        public bool IsGameOver => Result != GameResult.Playing;

        public event Action OnBoardChanged;
        public event Action OnNewGame;
        public event Action<PieceColor> OnTurnChanged;
        public event Action<PieceColor> OnCheck;
        public event Action<string> OnStatusMessage;
        public event Action<MoveEvent> OnMoveApplied;
        public event Action<GameResult, PieceColor?> OnGameOver;

        public PieceColor SideToMove => Board.SideToMove;

        public ChessGame()
        {
            Board = new ChessBoard();
            NewGame();
        }

        public void NewGame()
        {
            Board.SetupStartingPosition();
            Result = GameResult.Playing;
            Winner = null;
            ClearSelection();
            OnNewGame?.Invoke();
            RaiseBoardChanged();
            OnTurnChanged?.Invoke(Board.SideToMove);
            OnStatusMessage?.Invoke("White's turn — make your move!");
        }

        public void ClearSelection()
        {
            SelectedSquare = null;
            LegalMovesForSelection = Array.Empty<Move>();
        }

        public bool HandleSquareTap(Square square)
        {
            if (IsGameOver || !square.IsValid)
                return false;

            if (SelectedSquare.HasValue)
            {
                foreach (var move in LegalMovesForSelection)
                {
                    if (move.To == square)
                    {
                        var chosen = PreferPromotion(move, LegalMovesForSelection);
                        ApplyMove(chosen);
                        return true;
                    }
                }

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
            if (IsGameOver)
                return;

            SelectedSquare = square;
            LegalMovesForSelection = MoveGenerator.GetLegalMovesFrom(Board, square);
            OnBoardChanged?.Invoke();
        }

        public void ApplyMove(Move move)
        {
            if (IsGameOver)
                return;

            var sideThatMoved = Board.SideToMove;
            var mover = Board.GetPiece(move.From);
            var captured = ResolveCapturedPiece(move, mover);

            Board.ApplyMove(move);
            ClearSelection();

            var sideAfter = Board.SideToMove;
            var inCheck = MoveGenerator.IsInCheck(Board, sideAfter);
            var result = EvaluateResult(Board);

            Result = result;
            if (result == GameResult.Checkmate)
                Winner = sideThatMoved;
            else if (result == GameResult.Stalemate)
                Winner = null;

            var moveEvent = new MoveEvent(move, mover, captured, sideThatMoved, sideAfter, inCheck, result);
            OnMoveApplied?.Invoke(moveEvent);

            RaiseBoardChanged();
            OnTurnChanged?.Invoke(sideAfter);

            if (result == GameResult.Checkmate)
            {
                OnStatusMessage?.Invoke($"Checkmate! {sideThatMoved} wins!");
                OnGameOver?.Invoke(result, Winner);
            }
            else if (result == GameResult.Stalemate)
            {
                OnStatusMessage?.Invoke("Stalemate — it's a draw!");
                OnGameOver?.Invoke(result, null);
            }
            else if (inCheck)
            {
                OnCheck?.Invoke(sideAfter);
                OnStatusMessage?.Invoke($"Check! {sideAfter}'s king is in danger");
            }
            else if (!captured.IsEmpty || move.IsCapture)
            {
                OnStatusMessage?.Invoke($"Nice capture! {sideAfter}'s turn");
            }
            else
            {
                OnStatusMessage?.Invoke($"{sideAfter}'s turn — make your move!");
            }
        }

        public static GameResult EvaluateResult(ChessBoard board)
        {
            var legal = MoveGenerator.GetLegalMoves(board);
            if (legal.Count > 0)
                return GameResult.Playing;

            return MoveGenerator.IsInCheck(board, board.SideToMove)
                ? GameResult.Checkmate
                : GameResult.Stalemate;
        }

        Piece ResolveCapturedPiece(Move move, Piece mover)
        {
            if (move.IsEnPassant)
            {
                var capturedRank = mover.Color == PieceColor.White ? move.To.Rank - 1 : move.To.Rank + 1;
                return Board.GetPiece(new Square(move.To.File, capturedRank));
            }

            return Board.GetPiece(move.To);
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
