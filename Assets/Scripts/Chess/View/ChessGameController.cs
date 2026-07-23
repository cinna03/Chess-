using System.Collections;
using Chess.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Chess.View
{
    public enum PlayMode
    {
        HotSeat,
        VersusComputer
    }

    /// <summary>
    /// Input + mode select: hot-seat or vs computer (simple AI).
    /// </summary>
    public class ChessGameController : MonoBehaviour
    {
        [SerializeField] ChessBoardView boardView;
        [SerializeField] Camera raycastCamera;
        [SerializeField] LayerMask boardLayerMask = ~0;
        [SerializeField] float computerThinkSeconds = 0.35f;

        ChessGame _game;
        PlayMode _mode = PlayMode.HotSeat;
        bool _modeChosen;
        bool _waitingForComputer;
        Coroutine _computerRoutine;

        public ChessGame Game => _game;
        public PlayMode Mode => _mode;
        public bool ModeChosen => _modeChosen;
        public bool IsComputerThinking => _waitingForComputer;

        public string StatusMessage { get; private set; } = "Choose a game mode";
        public string TipMessage { get; private set; } = "Hot-seat for two players, or challenge the computer";

        public event System.Action<string> OnTipChanged;
        public event System.Action OnModeChanged;

        void Awake()
        {
            if (boardView == null)
                boardView = GetComponent<ChessBoardView>();
            if (raycastCamera == null)
                raycastCamera = Camera.main;

            _game = new ChessGame();
            _game.OnStatusMessage += msg => StatusMessage = msg;
            _game.OnCheck += color => StatusMessage = $"Check! {color}'s king is in danger";
            _game.OnMoveApplied += HandleMoveApplied;
            boardView.Bind(_game);
            ApplyModeSettings();

            if (FindAnyObjectByType<Chess.View.UI.ChessMenuUi>() == null)
                gameObject.AddComponent<Chess.View.UI.ChessMenuUi>();
        }

        void OnDestroy()
        {
            if (_game != null)
                _game.OnMoveApplied -= HandleMoveApplied;
        }

        void Update()
        {
            if (!_modeChosen)
                return;

            if (_game != null && _game.IsGameOver)
                return;

            if (boardView != null && boardView.IsBusy)
                return;

            if (_waitingForComputer)
                return;

            if (WasPointerDown(out var screenPos))
                TryHandlePointer(screenPos);
        }

        public void ChooseMode(PlayMode mode)
        {
            _mode = mode;
            _modeChosen = true;
            ApplyModeSettings();
            ResetGame();
            OnModeChanged?.Invoke();
            SetTip(mode == PlayMode.VersusComputer
                ? "You are White — computer plays Black"
                : "Hot-seat: pass the device each turn");
        }

        public void OpenModeSelect()
        {
            _modeChosen = false;
            _waitingForComputer = false;
            if (_computerRoutine != null)
            {
                StopCoroutine(_computerRoutine);
                _computerRoutine = null;
            }

            StatusMessage = "Choose a game mode";
            SetTip("Hot-seat for two players, or challenge the computer");
            OnModeChanged?.Invoke();
        }

        void ApplyModeSettings()
        {
            if (boardView == null)
                return;

            // Rotate for both modes so each side "faces" the current player / camera.
            boardView.RotateOnTurnChange = true;
        }

        void HandleMoveApplied(MoveEvent moveEvent)
        {
            if (!_modeChosen || _mode != PlayMode.VersusComputer)
                return;

            if (moveEvent.ResultAfterMove != GameResult.Playing)
                return;

            if (moveEvent.SideToMoveAfter == PieceColor.Black)
                QueueComputerMove();
        }

        void QueueComputerMove()
        {
            if (_computerRoutine != null)
                StopCoroutine(_computerRoutine);
            _computerRoutine = StartCoroutine(ComputerMoveRoutine());
        }

        IEnumerator ComputerMoveRoutine()
        {
            _waitingForComputer = true;
            StatusMessage = "Computer is thinking…";
            SetTip("Hang tight — Black is choosing a move");

            // Wait until board animations finish, then a short think pause
            while (boardView != null && boardView.IsBusy)
                yield return null;

            yield return new WaitForSeconds(computerThinkSeconds);

            if (_game == null || _game.IsGameOver || _game.SideToMove != PieceColor.Black)
            {
                _waitingForComputer = false;
                yield break;
            }

            // Depth-3 search can hitch — yield one frame before computing
            yield return null;

            if (SimpleChessAi.TryChooseMove(_game.Board, out var move))
            {
                if (!_game.IsGameOver)
                    _game.ApplyMove(move);
                SetTip(_game.IsGameOver ? "Game over!" : "Your turn — reply as White");
            }
            else
            {
                StatusMessage = "Computer has no moves";
                SetTip("Game may be over — try New Game");
            }

            _waitingForComputer = false;
            _computerRoutine = null;
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

            // In vs computer, human only moves White
            if (_mode == PlayMode.VersusComputer && _game.SideToMove != PieceColor.White)
            {
                SetTip("Computer is playing Black — please wait");
                return;
            }

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
                SetTip(_mode == PlayMode.VersusComputer
                    ? "Nice move — computer will reply"
                    : "Nice — watch the board flip for the next player");
                return;
            }

            var piece = _game.Board.GetPiece(square);
            if (!piece.IsEmpty && piece.Color != _game.SideToMove)
            {
                var other = _game.SideToMove == PieceColor.White ? "Black" : "White";
                SetTip($"That's {other}'s piece — wait for their turn");
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
            if (_computerRoutine != null)
            {
                StopCoroutine(_computerRoutine);
                _computerRoutine = null;
            }

            _waitingForComputer = false;
            ApplyModeSettings();
            _game?.NewGame();
            SetTip(_mode == PlayMode.VersusComputer
                ? "Fresh game — you are White vs computer"
                : "Fresh board — White goes first. Good luck!");
        }
    }
}
