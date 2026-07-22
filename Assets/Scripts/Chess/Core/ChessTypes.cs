namespace Chess.Core
{
    public enum PieceColor
    {
        White = 0,
        Black = 1
    }

    public enum PieceType
    {
        None = 0,
        Pawn = 1,
        Knight = 2,
        Bishop = 3,
        Rook = 4,
        Queen = 5,
        King = 6
    }

    public readonly struct Piece
    {
        public readonly PieceType Type;
        public readonly PieceColor Color;

        public Piece(PieceType type, PieceColor color)
        {
            Type = type;
            Color = color;
        }

        public bool IsEmpty => Type == PieceType.None;

        public static Piece Empty => new Piece(PieceType.None, PieceColor.White);

        public PieceColor OppositeColor => Color == PieceColor.White ? PieceColor.Black : PieceColor.White;

        public override string ToString()
        {
            if (IsEmpty) return ".";
            var letter = Type switch
            {
                PieceType.Pawn => "P",
                PieceType.Knight => "N",
                PieceType.Bishop => "B",
                PieceType.Rook => "R",
                PieceType.Queen => "Q",
                PieceType.King => "K",
                _ => "?"
            };
            return Color == PieceColor.White ? letter : letter.ToLowerInvariant();
        }
    }

    /// <summary>Board square: file a–h = 0–7, rank 1–8 = 0–7.</summary>
    public readonly struct Square
    {
        public readonly int File;
        public readonly int Rank;

        public Square(int file, int rank)
        {
            File = file;
            Rank = rank;
        }

        public bool IsValid => File >= 0 && File < 8 && Rank >= 0 && Rank < 8;

        public static bool TryParse(string algebraic, out Square square)
        {
            square = default;
            if (string.IsNullOrEmpty(algebraic) || algebraic.Length != 2)
                return false;

            var file = char.ToLowerInvariant(algebraic[0]) - 'a';
            var rank = algebraic[1] - '1';
            if (file < 0 || file > 7 || rank < 0 || rank > 7)
                return false;

            square = new Square(file, rank);
            return true;
        }

        public string ToAlgebraic()
        {
            return $"{(char)('a' + File)}{Rank + 1}";
        }

        public override string ToString() => ToAlgebraic();

        public override bool Equals(object obj) => obj is Square other && Equals(other);

        public bool Equals(Square other) => File == other.File && Rank == other.Rank;

        public override int GetHashCode() => (File << 3) | Rank;

        public static bool operator ==(Square a, Square b) => a.Equals(b);
        public static bool operator !=(Square a, Square b) => !a.Equals(b);
    }

    public readonly struct Move
    {
        public readonly Square From;
        public readonly Square To;
        public readonly PieceType Promotion;
        public readonly bool IsCapture;
        public readonly bool IsEnPassant;
        public readonly bool IsCastle;

        public Move(
            Square from,
            Square to,
            PieceType promotion = PieceType.None,
            bool isCapture = false,
            bool isEnPassant = false,
            bool isCastle = false)
        {
            From = from;
            To = to;
            Promotion = promotion;
            IsCapture = isCapture;
            IsEnPassant = isEnPassant;
            IsCastle = isCastle;
        }

        public override string ToString()
        {
            var text = $"{From}{To}";
            if (Promotion != PieceType.None)
                text += $"={Promotion}";
            return text;
        }
    }
}
