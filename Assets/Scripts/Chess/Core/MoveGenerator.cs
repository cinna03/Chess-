using System.Collections.Generic;

namespace Chess.Core
{
    /// <summary>
    /// Generates legal moves for the side to move (filters out moves that leave own king in check).
    /// </summary>
    public static class MoveGenerator
    {
        static readonly int[] KnightFiles = { -1, -2, -2, -1, 1, 2, 2, 1 };
        static readonly int[] KnightRanks = { 2, 1, -1, -2, -2, -1, 1, 2 };

        static readonly int[] KingFiles = { -1, 0, 1, -1, 1, -1, 0, 1 };
        static readonly int[] KingRanks = { 1, 1, 1, 0, 0, -1, -1, -1 };

        static readonly int[] BishopFiles = { 1, 1, -1, -1 };
        static readonly int[] BishopRanks = { 1, -1, 1, -1 };

        static readonly int[] RookFiles = { 1, -1, 0, 0 };
        static readonly int[] RookRanks = { 0, 0, 1, -1 };

        public static List<Move> GetLegalMoves(ChessBoard board)
        {
            var legal = new List<Move>();
            var side = board.SideToMove;

            foreach (var move in GetPseudoLegalMoves(board, side))
            {
                if (IsLegal(board, move, side))
                    legal.Add(move);
            }

            return legal;
        }

        public static List<Move> GetLegalMovesFrom(ChessBoard board, Square from)
        {
            var result = new List<Move>();
            var piece = board.GetPiece(from);
            if (piece.IsEmpty || piece.Color != board.SideToMove)
                return result;

            foreach (var move in GetLegalMoves(board))
            {
                if (move.From == from)
                    result.Add(move);
            }

            return result;
        }

        public static bool IsInCheck(ChessBoard board, PieceColor color)
        {
            var king = board.FindKing(color);
            if (!king.HasValue)
                return false;
            return IsSquareAttacked(board, king.Value, Opposite(color));
        }

        static bool IsLegal(ChessBoard board, Move move, PieceColor side)
        {
            var clone = board.Clone();
            clone.ApplyMove(move);
            // After ApplyMove, side to move flips — check the king of the side that just moved.
            return !IsInCheck(clone, side);
        }

        static IEnumerable<Move> GetPseudoLegalMoves(ChessBoard board, PieceColor side)
        {
            for (var file = 0; file < 8; file++)
            for (var rank = 0; rank < 8; rank++)
            {
                var from = new Square(file, rank);
                var piece = board.GetPiece(from);
                if (piece.IsEmpty || piece.Color != side)
                    continue;

                switch (piece.Type)
                {
                    case PieceType.Pawn:
                        foreach (var m in GeneratePawnMoves(board, from, side)) yield return m;
                        break;
                    case PieceType.Knight:
                        foreach (var m in GenerateKnightMoves(board, from, side)) yield return m;
                        break;
                    case PieceType.Bishop:
                        foreach (var m in GenerateSlidingMoves(board, from, side, BishopFiles, BishopRanks))
                            yield return m;
                        break;
                    case PieceType.Rook:
                        foreach (var m in GenerateSlidingMoves(board, from, side, RookFiles, RookRanks))
                            yield return m;
                        break;
                    case PieceType.Queen:
                        foreach (var m in GenerateSlidingMoves(board, from, side, BishopFiles, BishopRanks))
                            yield return m;
                        foreach (var m in GenerateSlidingMoves(board, from, side, RookFiles, RookRanks))
                            yield return m;
                        break;
                    case PieceType.King:
                        foreach (var m in GenerateKingMoves(board, from, side)) yield return m;
                        break;
                }
            }
        }

        static IEnumerable<Move> GeneratePawnMoves(ChessBoard board, Square from, PieceColor side)
        {
            var dir = side == PieceColor.White ? 1 : -1;
            var startRank = side == PieceColor.White ? 1 : 6;
            var promoRank = side == PieceColor.White ? 7 : 0;

            var one = new Square(from.File, from.Rank + dir);
            if (one.IsValid && board.GetPiece(one).IsEmpty)
            {
                foreach (var m in WithPromotion(from, one, false, promoRank))
                    yield return m;

                if (from.Rank == startRank)
                {
                    var two = new Square(from.File, from.Rank + 2 * dir);
                    if (two.IsValid && board.GetPiece(two).IsEmpty)
                        yield return new Move(from, two);
                }
            }

            for (var df = -1; df <= 1; df += 2)
            {
                var to = new Square(from.File + df, from.Rank + dir);
                if (!to.IsValid)
                    continue;

                var target = board.GetPiece(to);
                if (!target.IsEmpty && target.Color != side)
                {
                    foreach (var m in WithPromotion(from, to, true, promoRank))
                        yield return m;
                }
                else if (board.EnPassantTarget.HasValue && board.EnPassantTarget.Value == to)
                {
                    yield return new Move(from, to, isCapture: true, isEnPassant: true);
                }
            }
        }

        static IEnumerable<Move> WithPromotion(Square from, Square to, bool capture, int promoRank)
        {
            if (to.Rank == promoRank)
            {
                yield return new Move(from, to, PieceType.Queen, capture);
                yield return new Move(from, to, PieceType.Rook, capture);
                yield return new Move(from, to, PieceType.Bishop, capture);
                yield return new Move(from, to, PieceType.Knight, capture);
            }
            else
            {
                yield return new Move(from, to, isCapture: capture);
            }
        }

        static IEnumerable<Move> GenerateKnightMoves(ChessBoard board, Square from, PieceColor side)
        {
            for (var i = 0; i < 8; i++)
            {
                var to = new Square(from.File + KnightFiles[i], from.Rank + KnightRanks[i]);
                if (!to.IsValid)
                    continue;
                var target = board.GetPiece(to);
                if (target.IsEmpty || target.Color != side)
                    yield return new Move(from, to, isCapture: !target.IsEmpty);
            }
        }

        static IEnumerable<Move> GenerateKingMoves(ChessBoard board, Square from, PieceColor side)
        {
            for (var i = 0; i < 8; i++)
            {
                var to = new Square(from.File + KingFiles[i], from.Rank + KingRanks[i]);
                if (!to.IsValid)
                    continue;
                var target = board.GetPiece(to);
                if (target.IsEmpty || target.Color != side)
                    yield return new Move(from, to, isCapture: !target.IsEmpty);
            }

            foreach (var castle in GenerateCastling(board, from, side))
                yield return castle;
        }

        static IEnumerable<Move> GenerateCastling(ChessBoard board, Square from, PieceColor side)
        {
            if (IsInCheck(board, side))
                yield break;

            var rank = side == PieceColor.White ? 0 : 7;
            if (from.File != 4 || from.Rank != rank)
                yield break;

            var enemy = Opposite(side);

            // King-side
            var canKing = side == PieceColor.White ? board.WhiteCanCastleKingSide : board.BlackCanCastleKingSide;
            if (canKing
                && board.GetPiece(new Square(5, rank)).IsEmpty
                && board.GetPiece(new Square(6, rank)).IsEmpty
                && !IsSquareAttacked(board, new Square(5, rank), enemy)
                && !IsSquareAttacked(board, new Square(6, rank), enemy))
            {
                yield return new Move(from, new Square(6, rank), isCastle: true);
            }

            // Queen-side
            var canQueen = side == PieceColor.White ? board.WhiteCanCastleQueenSide : board.BlackCanCastleQueenSide;
            if (canQueen
                && board.GetPiece(new Square(1, rank)).IsEmpty
                && board.GetPiece(new Square(2, rank)).IsEmpty
                && board.GetPiece(new Square(3, rank)).IsEmpty
                && !IsSquareAttacked(board, new Square(2, rank), enemy)
                && !IsSquareAttacked(board, new Square(3, rank), enemy))
            {
                yield return new Move(from, new Square(2, rank), isCastle: true);
            }
        }

        static IEnumerable<Move> GenerateSlidingMoves(
            ChessBoard board,
            Square from,
            PieceColor side,
            int[] fileDirs,
            int[] rankDirs)
        {
            for (var d = 0; d < fileDirs.Length; d++)
            {
                var f = from.File + fileDirs[d];
                var r = from.Rank + rankDirs[d];
                while (f >= 0 && f < 8 && r >= 0 && r < 8)
                {
                    var to = new Square(f, r);
                    var target = board.GetPiece(to);
                    if (target.IsEmpty)
                    {
                        yield return new Move(from, to);
                    }
                    else
                    {
                        if (target.Color != side)
                            yield return new Move(from, to, isCapture: true);
                        break;
                    }

                    f += fileDirs[d];
                    r += rankDirs[d];
                }
            }
        }

        public static bool IsSquareAttacked(ChessBoard board, Square square, PieceColor byColor)
        {
            // Pawn attacks
            var pawnDir = byColor == PieceColor.White ? 1 : -1;
            for (var df = -1; df <= 1; df += 2)
            {
                var from = new Square(square.File + df, square.Rank - pawnDir);
                if (!from.IsValid) continue;
                var p = board.GetPiece(from);
                if (p.Type == PieceType.Pawn && p.Color == byColor)
                    return true;
            }

            // Knights
            for (var i = 0; i < 8; i++)
            {
                var from = new Square(square.File + KnightFiles[i], square.Rank + KnightRanks[i]);
                if (!from.IsValid) continue;
                var p = board.GetPiece(from);
                if (p.Type == PieceType.Knight && p.Color == byColor)
                    return true;
            }

            // King
            for (var i = 0; i < 8; i++)
            {
                var from = new Square(square.File + KingFiles[i], square.Rank + KingRanks[i]);
                if (!from.IsValid) continue;
                var p = board.GetPiece(from);
                if (p.Type == PieceType.King && p.Color == byColor)
                    return true;
            }

            if (IsAttackedBySlider(board, square, byColor, BishopFiles, BishopRanks, PieceType.Bishop, PieceType.Queen))
                return true;
            if (IsAttackedBySlider(board, square, byColor, RookFiles, RookRanks, PieceType.Rook, PieceType.Queen))
                return true;

            return false;
        }

        static bool IsAttackedBySlider(
            ChessBoard board,
            Square square,
            PieceColor byColor,
            int[] fileDirs,
            int[] rankDirs,
            PieceType sliderA,
            PieceType sliderB)
        {
            for (var d = 0; d < fileDirs.Length; d++)
            {
                var f = square.File + fileDirs[d];
                var r = square.Rank + rankDirs[d];
                while (f >= 0 && f < 8 && r >= 0 && r < 8)
                {
                    var p = board.GetPiece(new Square(f, r));
                    if (!p.IsEmpty)
                    {
                        if (p.Color == byColor && (p.Type == sliderA || p.Type == sliderB))
                            return true;
                        break;
                    }

                    f += fileDirs[d];
                    r += rankDirs[d];
                }
            }

            return false;
        }

        static PieceColor Opposite(PieceColor color) =>
            color == PieceColor.White ? PieceColor.Black : PieceColor.White;
    }
}
