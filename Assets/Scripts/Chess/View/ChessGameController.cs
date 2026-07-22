using Chess.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Chess.View
{
    /// <summary>
    /// Hot-seat input: mouse (Editor) or touch raycasts onto board squares/pieces.
    /// Uses the Input System package (project activeInputHandler = Input System Only).
    /// </summary>
    public class ChessGameController : MonoBehaviour
    {
        [SerializeField] ChessBoardView boardView;
        [SerializeField] Camera raycastCamera;
        [SerializeField] LayerMask boardLayerMask = ~0;

        ChessGame _game;

        public ChessGame Game => _game;
        public string StatusMessage { get; private set; } = "White to move";

        void Awake()
        {
            if (boardView == null)
                boardView = GetComponent<ChessBoardView>();
            if (raycastCamera == null)
                raycastCamera = Camera.main;

            _game = new ChessGame();
            _game.OnStatusMessage += msg => StatusMessage = msg;
            _game.OnCheck += color => StatusMessage = $"{color} is in check";
            boardView.Bind(_game);
        }

        void Update()
        {
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
            if (raycastCamera == null)
                return;

            var ray = raycastCamera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out var hit, 100f, boardLayerMask))
                return;

            if (boardView.TryGetSquare(hit.collider, out var square)
                || boardView.TryGetSquareFromPiece(hit.collider, out square))
            {
                _game.HandleSquareTap(square);
            }
        }

        public void ResetGame()
        {
            _game?.NewGame();
        }
    }
}
