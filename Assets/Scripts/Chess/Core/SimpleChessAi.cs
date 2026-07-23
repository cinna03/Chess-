using System.Collections.Generic;
using UnityEngine;

namespace Chess.Core
{
    /// <summary>
    /// Minimax AI with alpha-beta pruning and material + simple positional scoring.
    /// </summary>
    public static class SimpleChessAi
    {
        const int Depth = 3;

        static readonly int[] PieceValue =
        {
            0,   // None
            100, // Pawn
            320, // Knight
            330, // Bishop
            500, // Rook
            900, // Queen
            20000 // King
        };

        // Encourages pawns/knights toward center early
        static readonly int[,] PawnTable =
        {
            { 0, 0, 0, 0, 0, 0, 0, 0 },
            { 50, 50, 50, 50, 50, 50, 50, 50 },
            { 10, 10, 20, 30, 30, 20, 10, 10 },
            { 5, 5, 10, 25, 25, 10, 5, 5 },
            { 0, 0, 0, 20, 20, 0, 0, 0 },
            { 5, -5, -10, 0, 0, -10, -5, 5 },
            { 5, 10, 10, -20, -20, 10, 10, 5 },
            { 0, 0, 0, 0, 0, 0, 0, 0 }
        };

        static readonly int[,] KnightTable =
        {
            { -50, -40, -30, -30, -30, -30, -40, -50 },
            { -40, -20, 0, 0, 0, 0, -20, -40 },
            { -30, 0, 10, 15, 15, 10, 0, -30 },
            { -30, 5, 15, 20, 20, 15, 5, -30 },
            { -30, 0, 15, 20, 20, 15, 0, -30 },
            { -30, 5, 10, 15, 15, 10, 5, -30 },
            { -40, -20, 0, 5, 5, 0, -20, -40 },
            { -50, -40, -30, -30, -30, -30, -40, -50 }
        };

        public static bool TryChooseMove(ChessBoard board, out Move chosen)
        {
            chosen = default;
            var moves = MoveGenerator.GetLegalMoves(board);
            if (moves.Count == 0)
                return false;

            // Shuffle for variety among equal scores
            Shuffle(moves);

            var maximizing = board.SideToMove == PieceColor.White;
            var bestScore = maximizing ? int.MinValue : int.MaxValue;
            Move best = moves[0];

            foreach (var move in moves)
            {
                var clone = board.Clone();
                clone.ApplyMove(move);
                var score = Minimax(clone, Depth - 1, int.MinValue, int.MaxValue, !maximizing);

                // Tiny noise so games don't always look identical
                score += Random.Range(-2, 3);

                if (maximizing && score > bestScore)
                {
                    bestScore = score;
                    best = move;
                }
                else if (!maximizing && score < bestScore)
                {
                    bestScore = score;
                    best = move;
                }
            }

            chosen = best;
            return true;
        }

        static int Minimax(ChessBoard board, int depth, int alpha, int beta, bool maximizing)
        {
            var result = ChessGame.EvaluateResult(board);
            if (result == GameResult.Checkmate)
                return maximizing ? -100000 - depth : 100000 + depth;
            if (result == GameResult.Stalemate)
                return 0;
            if (depth == 0)
                return Evaluate(board);

            var moves = MoveGenerator.GetLegalMoves(board);
            if (maximizing)
            {
                var value = int.MinValue;
                foreach (var move in moves)
                {
                    var clone = board.Clone();
                    clone.ApplyMove(move);
                    value = Mathf.Max(value, Minimax(clone, depth - 1, alpha, beta, false));
                    alpha = Mathf.Max(alpha, value);
                    if (alpha >= beta)
                        break;
                }

                return value;
            }
            else
            {
                var value = int.MaxValue;
                foreach (var move in moves)
                {
                    var clone = board.Clone();
                    clone.ApplyMove(move);
                    value = Mathf.Min(value, Minimax(clone, depth - 1, alpha, beta, true));
                    beta = Mathf.Min(beta, value);
                    if (alpha >= beta)
                        break;
                }

                return value;
            }
        }

        static int Evaluate(ChessBoard board)
        {
            var score = 0;
            for (var file = 0; file < 8; file++)
            for (var rank = 0; rank < 8; rank++)
            {
                var piece = board.GetPiece(new Square(file, rank));
                if (piece.IsEmpty)
                    continue;

                var value = PieceValue[(int)piece.Type] + PositionalBonus(piece, file, rank);
                score += piece.Color == PieceColor.White ? value : -value;
            }

            return score;
        }

        static int PositionalBonus(Piece piece, int file, int rank)
        {
            var r = piece.Color == PieceColor.White ? 7 - rank : rank;
            return piece.Type switch
            {
                PieceType.Pawn => PawnTable[r, file],
                PieceType.Knight => KnightTable[r, file],
                PieceType.Bishop => (file >= 2 && file <= 5 && rank >= 2 && rank <= 5) ? 10 : 0,
                PieceType.King => rank == (piece.Color == PieceColor.White ? 0 : 7) ? 20 : -10,
                _ => 0
            };
        }

        static void Shuffle(List<Move> moves)
        {
            for (var i = moves.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (moves[i], moves[j]) = (moves[j], moves[i]);
            }
        }
    }
}
