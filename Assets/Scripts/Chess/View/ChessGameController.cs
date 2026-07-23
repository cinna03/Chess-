using Chess.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Chess.View
{
    /// <summary>
    /// Hot-seat input with friendly tips when taps don't apply a move.
    /// </summary>
    public class ChessGameController : MonoBehaviour
    {
        [SerializeField] ChessBoardView boardView;
        [SerializeField] Camera raycastCamera;
        [SerializeField] LayerMask boardLayerMask = ~0;

        ChessGame _game;

        public ChessGame Game => _game;
        public string StatusMessage { get; private set; } = "White's turn — make your move!";
        public string TipMessage { get; private set; } = "Tip: tap your piece, then a glowing spot";

        public event System.Action<string> OnTipChanged;

        void Awake()
        {
            if (boardView == null)
                boardView = GetComponent<ChessBoardView>();
            if (raycastCamera == null)
                raycastCamera = Camera.main;

            _game = new ChessGame();
            _game.OnStatusMessage += msg => StatusMessage = msg;
            _game.OnCheck += color => StatusMessage = $"Check! {color}'s king is in danger";
            boardView.Bind(_game);
        }

        void Update()
        {
            if (boardView != null && boardView.IsBusy)
                return;

            if (WasPointerDown(out var screenPos))
                TryHandlePointer(screenPos);
        }

        bool WasPointerDown(out Vector2 screenPos)
        {
            screenPos = default;

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screenPos = mouse.position.ReadValue();
                return true;
            }

            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                screenPos = touch.primaryTouch.position.ReadValue();
                return true;
            }

            return false;
        }

        void TryHandlePointer(Vector2 screenPos)
        {
            if (raycastCamera == null || boardView == null || _game == null)
                return;

            var ray = raycastCamera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out var hit, 100f, boardLayerMask))
            {
                SetTip("Tap the board to play");
                return;
            }

            if (!boardView.TryGetSquare(hit.collider, out var square)
                && !boardView.TryGetSquareFromPiece(hit.collider, out square))
            {
                SetTip("Tap a piece or a square");
                return;
            }

            var beforeSelected = _game.SelectedSquare;
            var moved = _game.HandleSquareTap(square);
            if (moved)
            {
                SetTip("Nice — watch the board flip for the next player");
                return;
            }

            // Helpful coaching when nothing moved
            var piece = _game.Board.GetPiece(square);
            if (!piece.IsEmpty && piece.Color != _game.SideToMove)
            {
                SetTip($"That's {( _game.SideToMove == PieceColor.White ? "Black" : "White" )}'s piece — wait for their turn");
            }
            else if (_game.SelectedSquare.HasValue && beforeSelected.HasValue)
            {
                SetTip("Pick a green or red spot — or tap another of your pieces");
            }
            else if (_game.SelectedSquare.HasValue)
            {
                SetTip("Now tap a glowing square to move");
            }
            else if (piece.IsEmpty)
            {
                SetTip($"Select a {_game.SideToMove.ToString().ToLower()} piece first");
            }
            else
            {
                SetTip("Now tap a glowing square to move");
            }
        }

        void SetTip(string tip)
        {
            TipMessage = tip;
            OnTipChanged?.Invoke(tip);
        }

        public void ResetGame()
        {
            _game?.NewGame();
            SetTip("Fresh board — White goes first. Good luck!");
        }
    }
}
