using System.Collections.Generic;
using Chess.Core;
using UnityEngine;

namespace Chess.View
{
    /// <summary>
    /// Builds an 8×8 board and primitive pieces; syncs visuals from ChessGame.
    /// </summary>
    public class ChessBoardView : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] float squareSize = 0.1f;
        [SerializeField] float pieceHeight = 0.08f;
        [SerializeField] float boardY = 0.001f;

        [Header("Colors")]
        [SerializeField] Color lightSquare = new Color(0.92f, 0.85f, 0.72f);
        [SerializeField] Color darkSquare = new Color(0.55f, 0.38f, 0.25f);
        [SerializeField] Color whitePiece = new Color(0.95f, 0.95f, 0.92f);
        [SerializeField] Color blackPiece = new Color(0.15f, 0.15f, 0.18f);

        ChessGame _game;
        readonly ChessSquareView[,] _squares = new ChessSquareView[8, 8];
        readonly List<ChessPieceView> _pieces = new List<ChessPieceView>();
        Transform _squaresRoot;
        Transform _piecesRoot;

        public float SquareSize => squareSize;
        public ChessGame Game => _game;

        public void Bind(ChessGame game)
        {
            if (_game != null)
                _game.OnBoardChanged -= Refresh;

            _game = game;
            EnsureHierarchy();
            BuildSquaresIfNeeded();
            _game.OnBoardChanged += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (_game != null)
                _game.OnBoardChanged -= Refresh;
        }

        void EnsureHierarchy()
        {
            if (_squaresRoot == null)
            {
                var go = new GameObject("Squares");
                go.transform.SetParent(transform, false);
                _squaresRoot = go.transform;
            }

            if (_piecesRoot == null)
            {
                var go = new GameObject("Pieces");
                go.transform.SetParent(transform, false);
                _piecesRoot = go.transform;
            }
        }

        void BuildSquaresIfNeeded()
        {
            if (_squares[0, 0] != null)
                return;

            for (var file = 0; file < 8; file++)
            for (var rank = 0; rank < 8; rank++)
            {
                var isLight = (file + rank) % 2 == 1;
                var squareGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                squareGo.name = $"Square_{file}_{rank}";
                squareGo.transform.SetParent(_squaresRoot, false);
                squareGo.transform.localPosition = SquareToLocal(new Square(file, rank), boardY);
                squareGo.transform.localScale = new Vector3(squareSize * 0.98f, 0.002f, squareSize * 0.98f);

                var view = squareGo.AddComponent<ChessSquareView>();
                view.Initialize(file, rank, isLight ? lightSquare : darkSquare);
                _squares[file, rank] = view;
            }
        }

        public void Refresh()
        {
            if (_game == null)
                return;

            RebuildPieces();
            ClearHighlights();

            if (_game.SelectedSquare.HasValue)
            {
                var sel = _game.SelectedSquare.Value;
                _squares[sel.File, sel.Rank].SetHighlight(SquareHighlight.Selected);

                foreach (var move in _game.LegalMovesForSelection)
                {
                    var highlight = move.IsCapture || move.IsEnPassant
                        ? SquareHighlight.LegalCapture
                        : SquareHighlight.LegalMove;
                    _squares[move.To.File, move.To.Rank].SetHighlight(highlight);
                }
            }
        }

        void ClearHighlights()
        {
            for (var file = 0; file < 8; file++)
            for (var rank = 0; rank < 8; rank++)
                _squares[file, rank]?.SetHighlight(SquareHighlight.None);
        }

        void RebuildPieces()
        {
            foreach (var piece in _pieces)
            {
                if (piece != null)
                    Destroy(piece.gameObject);
            }
            _pieces.Clear();

            for (var file = 0; file < 8; file++)
            for (var rank = 0; rank < 8; rank++)
            {
                var square = new Square(file, rank);
                var piece = _game.Board.GetPiece(square);
                if (piece.IsEmpty)
                    continue;

                var view = CreatePieceVisual(piece, square);
                _pieces.Add(view);
            }
        }

        ChessPieceView CreatePieceVisual(Piece piece, Square square)
        {
            var primitive = piece.Type switch
            {
                PieceType.Pawn => PrimitiveType.Capsule,
                PieceType.Knight => PrimitiveType.Cube,
                PieceType.Bishop => PrimitiveType.Capsule,
                PieceType.Rook => PrimitiveType.Cube,
                PieceType.Queen => PrimitiveType.Sphere,
                PieceType.King => PrimitiveType.Cylinder,
                _ => PrimitiveType.Cube
            };

            var go = GameObject.CreatePrimitive(primitive);
            go.transform.SetParent(_piecesRoot, false);

            var scale = piece.Type switch
            {
                PieceType.Pawn => new Vector3(0.35f, 0.45f, 0.35f) * squareSize,
                PieceType.Knight => new Vector3(0.4f, 0.45f, 0.4f) * squareSize,
                PieceType.Bishop => new Vector3(0.35f, 0.55f, 0.35f) * squareSize,
                PieceType.Rook => new Vector3(0.4f, 0.5f, 0.4f) * squareSize,
                PieceType.Queen => new Vector3(0.45f, 0.45f, 0.45f) * squareSize,
                PieceType.King => new Vector3(0.4f, 0.65f, 0.4f) * squareSize,
                _ => Vector3.one * squareSize * 0.4f
            };
            go.transform.localScale = scale;
            go.transform.localPosition = SquareToLocal(square, boardY + pieceHeight * 0.5f);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                var block = new MaterialPropertyBlock();
                var color = piece.Color == PieceColor.White ? whitePiece : blackPiece;
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                renderer.SetPropertyBlock(block);
            }

            // Collider stays on pieces for raycasts too (optional).
            var view = go.AddComponent<ChessPieceView>();
            view.Configure(piece.Type, piece.Color, square);
            return view;
        }

        public Vector3 SquareToLocal(Square square, float y)
        {
            // Center board on origin: a1 at (-3.5, *, -3.5) * size
            var x = (square.File - 3.5f) * squareSize;
            var z = (square.Rank - 3.5f) * squareSize;
            return new Vector3(x, y, z);
        }

        public bool TryGetSquare(Collider collider, out Square square)
        {
            square = default;
            var view = collider.GetComponentInParent<ChessSquareView>();
            if (view == null)
                return false;
            square = new Square(view.File, view.Rank);
            return true;
        }

        public bool TryGetSquareFromPiece(Collider collider, out Square square)
        {
            square = default;
            var piece = collider.GetComponentInParent<ChessPieceView>();
            if (piece == null)
                return false;
            square = piece.Square;
            return true;
        }
    }
}
