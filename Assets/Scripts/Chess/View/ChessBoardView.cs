using System.Collections;
using System.Collections.Generic;
using Chess.Core;
using UnityEngine;

namespace Chess.View
{
    /// <summary>
    /// Builds board + pieces; animates moves/captures; rotates for the side to move.
    /// </summary>
    public class ChessBoardView : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] float squareSize = 0.1f;
        [SerializeField] float pieceHeight = 0.08f;
        [SerializeField] float boardY = 0.001f;

        [Header("Look")]
        [SerializeField] Color lightSquare = new Color(0.93f, 0.84f, 0.70f);
        [SerializeField] Color darkSquare = new Color(0.48f, 0.32f, 0.22f);
        [SerializeField] Color whitePiece = new Color(0.96f, 0.94f, 0.90f);
        [SerializeField] Color blackPiece = new Color(0.18f, 0.16f, 0.20f);
        [SerializeField] Color boardFrame = new Color(0.35f, 0.22f, 0.14f);

        [Header("Animation")]
        [SerializeField] float moveDuration = 0.35f;
        [SerializeField] float hopHeight = 0.04f;
        [SerializeField] float captureDuration = 0.45f;
        [SerializeField] float turnRotateDuration = 0.55f;

        ChessGame _game;
        readonly ChessSquareView[,] _squares = new ChessSquareView[8, 8];
        readonly Dictionary<int, ChessPieceView> _piecesBySquare = new Dictionary<int, ChessPieceView>();
        Transform _squaresRoot;
        Transform _piecesRoot;
        Transform _frame;
        Coroutine _turnRoutine;
        Coroutine _moveRoutine;

        public float SquareSize => squareSize;
        public ChessGame Game => _game;
        public bool IsBusy { get; private set; }

        public void Bind(ChessGame game)
        {
            if (_game != null)
            {
                _game.OnBoardChanged -= RefreshHighlights;
                _game.OnNewGame -= HandleNewGame;
                _game.OnMoveApplied -= HandleMoveApplied;
            }

            _game = game;
            EnsureHierarchy();
            BuildBoardVisualsIfNeeded();
            _game.OnBoardChanged += RefreshHighlights;
            _game.OnNewGame += HandleNewGame;
            _game.OnMoveApplied += HandleMoveApplied;
            HandleNewGame();
        }

        void OnDestroy()
        {
            if (_game == null)
                return;
            _game.OnBoardChanged -= RefreshHighlights;
            _game.OnNewGame -= HandleNewGame;
            _game.OnMoveApplied -= HandleMoveApplied;
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

        void BuildBoardVisualsIfNeeded()
        {
            if (_squares[0, 0] != null)
                return;

            // Soft frame under the squares
            var frameGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frameGo.name = "BoardFrame";
            frameGo.transform.SetParent(transform, false);
            frameGo.transform.localPosition = new Vector3(0f, boardY - 0.004f, 0f);
            frameGo.transform.localScale = new Vector3(squareSize * 8.4f, 0.008f, squareSize * 8.4f);
            ApplyColor(frameGo.GetComponent<Renderer>(), boardFrame);
            Object.Destroy(frameGo.GetComponent<Collider>());
            _frame = frameGo.transform;

            for (var file = 0; file < 8; file++)
            for (var rank = 0; rank < 8; rank++)
            {
                var isLight = (file + rank) % 2 == 1;
                var squareGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                squareGo.name = $"Square_{file}_{rank}";
                squareGo.transform.SetParent(_squaresRoot, false);
                squareGo.transform.localPosition = SquareToLocal(new Square(file, rank), boardY);
                squareGo.transform.localScale = new Vector3(squareSize * 0.96f, 0.003f, squareSize * 0.96f);

                var view = squareGo.AddComponent<ChessSquareView>();
                view.Initialize(file, rank, isLight ? lightSquare : darkSquare);
                _squares[file, rank] = view;
            }
        }

        void HandleNewGame()
        {
            if (_moveRoutine != null)
                StopCoroutine(_moveRoutine);
            if (_turnRoutine != null)
                StopCoroutine(_turnRoutine);

            IsBusy = false;
            transform.localRotation = Quaternion.identity;
            RebuildAllPieces();
            RefreshHighlights();
        }

        void HandleMoveApplied(MoveEvent moveEvent)
        {
            if (_moveRoutine != null)
                StopCoroutine(_moveRoutine);
            _moveRoutine = StartCoroutine(PlayMoveAnimation(moveEvent));
        }

        IEnumerator PlayMoveAnimation(MoveEvent moveEvent)
        {
            IsBusy = true;
            var move = moveEvent.Move;

            // Capture exit first (or in parallel with move)
            ChessPieceView capturedView = null;
            if (move.IsEnPassant)
            {
                var capRank = moveEvent.SideThatMoved == PieceColor.White ? move.To.Rank - 1 : move.To.Rank + 1;
                TryTakePiece(new Square(move.To.File, capRank), out capturedView);
            }
            else if (moveEvent.WasCapture)
            {
                TryTakePiece(move.To, out capturedView);
            }

            if (!TryTakePiece(move.From, out var moverView) || moverView == null)
            {
                RebuildAllPieces();
                IsBusy = false;
                yield break;
            }

            Coroutine captureCo = null;
            if (capturedView != null)
            {
                var away = capturedView.transform.localPosition + new Vector3(
                    capturedView.Color == PieceColor.White ? -squareSize * 3f : squareSize * 3f,
                    pieceHeight * 2f,
                    0f);
                captureCo = StartCoroutine(capturedView.AnimateCaptureExit(away, captureDuration));
            }

            var target = PieceLocalPosition(move.To);
            yield return moverView.AnimateMoveTo(target, moveDuration, hopHeight);
            moverView.SetSquare(move.To);
            _piecesBySquare[Key(move.To)] = moverView;

            // Castle: also slide rook
            if (move.IsCastle)
                yield return AnimateCastleRook(move);

            // Promotion visual swap
            if (move.Promotion != PieceType.None)
            {
                RemovePieceAt(move.To);
                var promoted = CreatePieceVisual(new Piece(move.Promotion, moveEvent.SideThatMoved), move.To);
                _piecesBySquare[Key(move.To)] = promoted;
            }

            if (captureCo != null)
                yield return captureCo;

            // Flip board to face the player whose turn it is now
            yield return RotateForSide(moveEvent.SideToMoveAfter);

            IsBusy = false;
            RefreshHighlights();
        }

        IEnumerator AnimateCastleRook(Move move)
        {
            Square rookFrom;
            Square rookTo;
            if (move.To.File == 6)
            {
                rookFrom = new Square(7, move.From.Rank);
                rookTo = new Square(5, move.From.Rank);
            }
            else
            {
                rookFrom = new Square(0, move.From.Rank);
                rookTo = new Square(3, move.From.Rank);
            }

            if (!TryTakePiece(rookFrom, out var rook) || rook == null)
                yield break;

            yield return rook.AnimateMoveTo(PieceLocalPosition(rookTo), moveDuration * 0.9f, hopHeight * 0.4f);
            rook.SetSquare(rookTo);
            _piecesBySquare[Key(rookTo)] = rook;
        }

        IEnumerator RotateForSide(PieceColor side)
        {
            IsBusy = true;
            var targetYaw = side == PieceColor.White ? 0f : 180f;
            var start = transform.localRotation;
            var end = Quaternion.Euler(0f, targetYaw, 0f);
            var elapsed = 0f;

            while (elapsed < turnRotateDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / turnRotateDuration);
                transform.localRotation = Quaternion.Slerp(start, end, t);
                yield return null;
            }

            transform.localRotation = end;
            IsBusy = false;
        }

        void RefreshHighlights()
        {
            if (_game == null)
                return;

            ClearHighlights();
            if (!_game.SelectedSquare.HasValue)
                return;

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

        void ClearHighlights()
        {
            for (var file = 0; file < 8; file++)
            for (var rank = 0; rank < 8; rank++)
                _squares[file, rank]?.SetHighlight(SquareHighlight.None);
        }

        void RebuildAllPieces()
        {
            foreach (var piece in _piecesBySquare.Values)
            {
                if (piece != null)
                    Destroy(piece.gameObject);
            }
            _piecesBySquare.Clear();

            for (var file = 0; file < 8; file++)
            for (var rank = 0; rank < 8; rank++)
            {
                var square = new Square(file, rank);
                var piece = _game.Board.GetPiece(square);
                if (piece.IsEmpty)
                    continue;
                _piecesBySquare[Key(square)] = CreatePieceVisual(piece, square);
            }
        }

        bool TryTakePiece(Square square, out ChessPieceView view)
        {
            var key = Key(square);
            if (_piecesBySquare.TryGetValue(key, out view))
            {
                _piecesBySquare.Remove(key);
                return view != null;
            }

            view = null;
            return false;
        }

        void RemovePieceAt(Square square)
        {
            if (TryTakePiece(square, out var view) && view != null)
                Destroy(view.gameObject);
        }

        ChessPieceView CreatePieceVisual(Piece piece, Square square)
        {
            var go = BuildPieceMesh(piece);
            go.transform.SetParent(_piecesRoot, false);
            go.transform.localPosition = PieceLocalPosition(square);

            var view = go.AddComponent<ChessPieceView>();
            view.Configure(piece.Type, piece.Color, square);
            return view;
        }

        GameObject BuildPieceMesh(Piece piece)
        {
            // Stacked primitives for a slightly cuter silhouette
            var root = new GameObject($"{piece.Color}_{piece.Type}");
            var color = piece.Color == PieceColor.White ? whitePiece : blackPiece;

            void AddPart(PrimitiveType type, Vector3 localPos, Vector3 localScale)
            {
                var part = GameObject.CreatePrimitive(type);
                part.transform.SetParent(root.transform, false);
                part.transform.localPosition = localPos;
                part.transform.localScale = localScale;
                ApplyColor(part.GetComponent<Renderer>(), color);
                var col = part.GetComponent<Collider>();
                if (col != null)
                    Object.Destroy(col);
            }

            var s = squareSize;
            switch (piece.Type)
            {
                case PieceType.Pawn:
                    AddPart(PrimitiveType.Cylinder, new Vector3(0f, 0.012f * s / 0.1f, 0f), new Vector3(0.32f, 0.12f, 0.32f) * s);
                    AddPart(PrimitiveType.Sphere, new Vector3(0f, 0.038f * s / 0.1f, 0f), new Vector3(0.28f, 0.28f, 0.28f) * s);
                    break;
                case PieceType.Rook:
                    AddPart(PrimitiveType.Cube, new Vector3(0f, 0.02f * s / 0.1f, 0f), new Vector3(0.34f, 0.28f, 0.34f) * s);
                    AddPart(PrimitiveType.Cube, new Vector3(0f, 0.04f * s / 0.1f, 0f), new Vector3(0.38f, 0.1f, 0.38f) * s);
                    break;
                case PieceType.Knight:
                    AddPart(PrimitiveType.Cube, new Vector3(0f, 0.018f * s / 0.1f, 0f), new Vector3(0.3f, 0.22f, 0.34f) * s);
                    AddPart(PrimitiveType.Cube, new Vector3(0.04f * s / 0.1f, 0.04f * s / 0.1f, 0.02f * s / 0.1f), new Vector3(0.22f, 0.2f, 0.28f) * s);
                    break;
                case PieceType.Bishop:
                    AddPart(PrimitiveType.Cylinder, new Vector3(0f, 0.02f * s / 0.1f, 0f), new Vector3(0.28f, 0.22f, 0.28f) * s);
                    AddPart(PrimitiveType.Sphere, new Vector3(0f, 0.05f * s / 0.1f, 0f), new Vector3(0.22f, 0.32f, 0.22f) * s);
                    break;
                case PieceType.Queen:
                    AddPart(PrimitiveType.Cylinder, new Vector3(0f, 0.02f * s / 0.1f, 0f), new Vector3(0.34f, 0.2f, 0.34f) * s);
                    AddPart(PrimitiveType.Sphere, new Vector3(0f, 0.05f * s / 0.1f, 0f), new Vector3(0.34f, 0.34f, 0.34f) * s);
                    AddPart(PrimitiveType.Sphere, new Vector3(0f, 0.07f * s / 0.1f, 0f), new Vector3(0.14f, 0.14f, 0.14f) * s);
                    break;
                case PieceType.King:
                    AddPart(PrimitiveType.Cylinder, new Vector3(0f, 0.024f * s / 0.1f, 0f), new Vector3(0.34f, 0.28f, 0.34f) * s);
                    AddPart(PrimitiveType.Cube, new Vector3(0f, 0.06f * s / 0.1f, 0f), new Vector3(0.12f, 0.22f, 0.12f) * s);
                    AddPart(PrimitiveType.Cube, new Vector3(0f, 0.068f * s / 0.1f, 0f), new Vector3(0.22f, 0.08f, 0.08f) * s);
                    break;
                default:
                    AddPart(PrimitiveType.Cube, Vector3.zero, Vector3.one * 0.3f * s);
                    break;
            }

            // One collider on root for tapping
            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(0.45f, 0.7f, 0.45f) * s;
            box.center = new Vector3(0f, 0.35f * s, 0f);
            return root;
        }

        static void ApplyColor(Renderer renderer, Color color)
        {
            if (renderer == null)
                return;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }

        Vector3 PieceLocalPosition(Square square) =>
            SquareToLocal(square, boardY + pieceHeight * 0.35f);

        public Vector3 SquareToLocal(Square square, float y)
        {
            var x = (square.File - 3.5f) * squareSize;
            var z = (square.Rank - 3.5f) * squareSize;
            return new Vector3(x, y, z);
        }

        static int Key(Square square) => square.Rank * 8 + square.File;

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
