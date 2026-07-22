using System;
using System.Text;

namespace Chess.Core
{
    /// <summary>
    /// 8×8 chess position with castling rights, en passant target, and side to move.
    /// </summary>
    public sealed class ChessBoard
    {
        readonly Piece[] _squares = new Piece[64];

        public PieceColor SideToMove { get; private set; } = PieceColor.White;

        public bool WhiteCanCastleKingSide { get; private set; } = true;
        public bool WhiteCanCastleQueenSide { get; private set; } = true;
        public bool BlackCanCastleKingSide { get; private set; } = true;
        public bool BlackCanCastleQueenSide { get; private set; } = true;

        /// <summary>Square behind the pawn that just double-moved, or null.</summary>
        public Square? EnPassantTarget { get; private set; }

        public Piece this[Square square]
        {
            get => GetPiece(square);
            set => SetPiece(square, value);
        }

        public Piece GetPiece(Square square)
        {
            if (!square.IsValid)
                return Piece.Empty;
            return _squares[Index(square)];
        }

        public void SetPiece(Square square, Piece piece)
        {
            if (!square.IsValid)
                return;
            _squares[Index(square)] = piece;
        }

        public void Clear()
        {
            Array.Clear(_squares, 0, _squares.Length);
            SideToMove = PieceColor.White;
            WhiteCanCastleKingSide = WhiteCanCastleQueenSide = true;
            BlackCanCastleKingSide = BlackCanCastleQueenSide = true;
            EnPassantTarget = null;
        }

        public void SetupStartingPosition()
        {
            Clear();

            PlaceBackRank(0, PieceColor.White);
            for (var file = 0; file < 8; file++)
                SetPiece(new Square(file, 1), new Piece(PieceType.Pawn, PieceColor.White));

            PlaceBackRank(7, PieceColor.Black);
            for (var file = 0; file < 8; file++)
                SetPiece(new Square(file, 6), new Piece(PieceType.Pawn, PieceColor.Black));
        }

        void PlaceBackRank(int rank, PieceColor color)
        {
            SetPiece(new Square(0, rank), new Piece(PieceType.Rook, color));
            SetPiece(new Square(1, rank), new Piece(PieceType.Knight, color));
            SetPiece(new Square(2, rank), new Piece(PieceType.Bishop, color));
            SetPiece(new Square(3, rank), new Piece(PieceType.Queen, color));
            SetPiece(new Square(4, rank), new Piece(PieceType.King, color));
            SetPiece(new Square(5, rank), new Piece(PieceType.Bishop, color));
            SetPiece(new Square(6, rank), new Piece(PieceType.Knight, color));
            SetPiece(new Square(7, rank), new Piece(PieceType.Rook, color));
        }

        public Square? FindKing(PieceColor color)
        {
            for (var i = 0; i < 64; i++)
            {
                var piece = _squares[i];
                if (piece.Type == PieceType.King && piece.Color == color)
                    return FromIndex(i);
            }
            return null;
        }

        public ChessBoard Clone()
        {
            var copy = new ChessBoard();
            Array.Copy(_squares, copy._squares, 64);
            copy.SideToMove = SideToMove;
            copy.WhiteCanCastleKingSide = WhiteCanCastleKingSide;
            copy.WhiteCanCastleQueenSide = WhiteCanCastleQueenSide;
            copy.BlackCanCastleKingSide = BlackCanCastleKingSide;
            copy.BlackCanCastleQueenSide = BlackCanCastleQueenSide;
            copy.EnPassantTarget = EnPassantTarget;
            return copy;
        }

        public void ApplyMove(Move move)
        {
            var moving = GetPiece(move.From);
            if (moving.IsEmpty)
                return;

            // En passant capture removes the pawn behind the landing square.
            if (move.IsEnPassant)
            {
                var capturedRank = moving.Color == PieceColor.White ? move.To.Rank - 1 : move.To.Rank + 1;
                SetPiece(new Square(move.To.File, capturedRank), Piece.Empty);
            }

            SetPiece(move.From, Piece.Empty);

            var placed = moving;
            if (move.Promotion != PieceType.None)
                placed = new Piece(move.Promotion, moving.Color);

            SetPiece(move.To, placed);

            if (move.IsCastle)
            {
                // King moved two files; bring rook along.
                if (move.To.File == 6) // king-side
                {
                    var rookFrom = new Square(7, move.From.Rank);
                    var rookTo = new Square(5, move.From.Rank);
                    SetPiece(rookTo, GetPiece(rookFrom));
                    SetPiece(rookFrom, Piece.Empty);
                }
                else if (move.To.File == 2) // queen-side
                {
                    var rookFrom = new Square(0, move.From.Rank);
                    var rookTo = new Square(3, move.From.Rank);
                    SetPiece(rookTo, GetPiece(rookFrom));
                    SetPiece(rookFrom, Piece.Empty);
                }
            }

            UpdateCastlingRights(moving, move);
            UpdateEnPassant(moving, move);

            SideToMove = SideToMove == PieceColor.White ? PieceColor.Black : PieceColor.White;
        }

        void UpdateCastlingRights(Piece moving, Move move)
        {
            if (moving.Type == PieceType.King)
            {
                if (moving.Color == PieceColor.White)
                {
                    WhiteCanCastleKingSide = false;
                    WhiteCanCastleQueenSide = false;
                }
                else
                {
                    BlackCanCastleKingSide = false;
                    BlackCanCastleQueenSide = false;
                }
            }

            if (moving.Type == PieceType.Rook)
            {
                if (moving.Color == PieceColor.White)
                {
                    if (move.From.File == 0 && move.From.Rank == 0) WhiteCanCastleQueenSide = false;
                    if (move.From.File == 7 && move.From.Rank == 0) WhiteCanCastleKingSide = false;
                }
                else
                {
                    if (move.From.File == 0 && move.From.Rank == 7) BlackCanCastleQueenSide = false;
                    if (move.From.File == 7 && move.From.Rank == 7) BlackCanCastleKingSide = false;
                }
            }

            // Capturing a rook removes that side's castling right.
            InvalidateCastleIfRookCaptured(move.To);
        }

        void InvalidateCastleIfRookCaptured(Square to)
        {
            if (to.File == 0 && to.Rank == 0) WhiteCanCastleQueenSide = false;
            if (to.File == 7 && to.Rank == 0) WhiteCanCastleKingSide = false;
            if (to.File == 0 && to.Rank == 7) BlackCanCastleQueenSide = false;
            if (to.File == 7 && to.Rank == 7) BlackCanCastleKingSide = false;
        }

        void UpdateEnPassant(Piece moving, Move move)
        {
            EnPassantTarget = null;
            if (moving.Type != PieceType.Pawn)
                return;

            var delta = Math.Abs(move.To.Rank - move.From.Rank);
            if (delta == 2)
            {
                var midRank = (move.From.Rank + move.To.Rank) / 2;
                EnPassantTarget = new Square(move.From.File, midRank);
            }
        }

        public string ToDebugString()
        {
            var sb = new StringBuilder();
            for (var rank = 7; rank >= 0; rank--)
            {
                for (var file = 0; file < 8; file++)
                    sb.Append(GetPiece(new Square(file, rank)).ToString()).Append(' ');
                sb.AppendLine();
            }
            sb.Append("Side: ").Append(SideToMove);
            return sb.ToString();
        }

        static int Index(Square square) => square.Rank * 8 + square.File;

        static Square FromIndex(int index) => new Square(index % 8, index / 8);
    }
}
