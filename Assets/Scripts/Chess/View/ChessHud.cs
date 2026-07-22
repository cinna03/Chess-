using Chess.Core;
using UnityEngine;

namespace Chess.View
{
    /// <summary>
    /// Friendly feedback banner + reset for the game panel.
    /// </summary>
    public class ChessHud : MonoBehaviour
    {
        [SerializeField] ChessGameController controller;
        [SerializeField] ChessBoardView boardView;
        [SerializeField] bool showGui = true;

        string _headline = "White's turn";
        string _subline = "Tap a piece, then a glowing square";
        float _pulse;

        static readonly string[] CaptureLines =
        {
            "Gotcha!",
            "Nice take!",
            "Off the board!",
            "Captured!"
        };

        static readonly string[] MoveLines =
        {
            "Smooth move!",
            "Looking good!",
            "Your turn swap incoming…"
        };

        void Awake()
        {
            if (controller == null)
                controller = FindAnyObjectByType<ChessGameController>();
            if (boardView == null)
                boardView = FindAnyObjectByType<ChessBoardView>();
        }

        void OnEnable()
        {
            if (controller == null)
                return;
            // Hook after controller creates the game in Awake — delay one frame via Start.
        }

        void Start()
        {
            var game = controller != null ? controller.Game : null;
            if (game == null)
                return;

            game.OnStatusMessage += HandleStatus;
            game.OnMoveApplied += HandleMove;
            game.OnTurnChanged += HandleTurn;
            game.OnNewGame += () =>
            {
                _headline = "New game!";
                _subline = "White goes first — good luck";
            };
        }

        void OnDestroy()
        {
            var game = controller != null ? controller.Game : null;
            if (game == null)
                return;
            game.OnStatusMessage -= HandleStatus;
            game.OnMoveApplied -= HandleMove;
            game.OnTurnChanged -= HandleTurn;
        }

        void HandleStatus(string msg)
        {
            if (string.IsNullOrEmpty(msg))
                return;
            // Keep headline from turn/move handlers; use status as subline fallback.
            _subline = msg;
        }

        void HandleMove(MoveEvent moveEvent)
        {
            if (moveEvent.WasCapture)
            {
                _headline = CaptureLines[Random.Range(0, CaptureLines.Length)];
                _subline = $"{moveEvent.Captured.Type} leaves the board";
            }
            else if (moveEvent.GaveCheck)
            {
                _headline = "Check!";
                _subline = $"Watch out, {moveEvent.SideToMoveAfter}";
            }
            else
            {
                _headline = MoveLines[Random.Range(0, MoveLines.Length)];
                _subline = "Board is turning for the next player";
            }

            _pulse = 1f;
        }

        void HandleTurn(PieceColor side)
        {
            _headline = side == PieceColor.White ? "White's turn" : "Black's turn";
            _subline = "Pass the device — board faces you now";
            _pulse = 1f;
        }

        void Update()
        {
            if (_pulse > 0f)
                _pulse = Mathf.Max(0f, _pulse - Time.deltaTime);
        }

        void OnGUI()
        {
            if (!showGui || controller == null)
                return;

            var width = Mathf.Min(420f, Screen.width - 24f);
            var x = (Screen.width - width) * 0.5f;
            var y = 16f;
            var pop = 1f + _pulse * 0.04f;

            var prev = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3(Screen.width * 0.5f, y + 40f, 0f),
                Quaternion.identity,
                Vector3.one * pop) * Matrix4x4.TRS(new Vector3(-Screen.width * 0.5f, -(y + 40f), 0f), Quaternion.identity, Vector3.one);

            GUI.Box(new Rect(x, y, width, 110f), "");
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            var subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            GUI.Label(new Rect(x + 12f, y + 12f, width - 24f, 32f), _headline, titleStyle);
            GUI.Label(new Rect(x + 16f, y + 44f, width - 32f, 36f), _subline, subStyle);

            if (GUI.Button(new Rect(x + width * 0.5f - 70f, y + 78f, 140f, 24f), "New Game"))
                controller.ResetGame();

            GUI.matrix = prev;
        }
    }
}
