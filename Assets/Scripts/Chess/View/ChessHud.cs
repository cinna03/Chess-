using Chess.Core;
using UnityEngine;

namespace Chess.View
{
    /// <summary>
    /// Color-coded turn banner, tips, and new-game control.
    /// </summary>
    public class ChessHud : MonoBehaviour
    {
        [SerializeField] ChessGameController controller;
        [SerializeField] bool showGui = true;

        string _headline = "White's turn";
        string _subline = "Tap a piece, then a glowing square";
        string _tip = "Tip: green = move, red = capture";
        float _pulse;
        PieceColor _side = PieceColor.White;

        static readonly string[] CaptureLines =
        {
            "Gotcha!",
            "Nice take!",
            "Into the capture tray!",
            "Captured!"
        };

        static readonly string[] MoveLines =
        {
            "Smooth move!",
            "Looking good!",
            "Passing the board…"
        };

        void Awake()
        {
            if (controller == null)
                controller = FindAnyObjectByType<ChessGameController>();
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
                _side = PieceColor.White;
                _pulse = 1f;
            };

            controller.OnTipChanged += HandleTip;
        }

        void OnDestroy()
        {
            var game = controller != null ? controller.Game : null;
            if (game != null)
            {
                game.OnStatusMessage -= HandleStatus;
                game.OnMoveApplied -= HandleMove;
                game.OnTurnChanged -= HandleTurn;
            }

            if (controller != null)
                controller.OnTipChanged -= HandleTip;
        }

        void HandleTip(string tip) => _tip = tip;

        void HandleStatus(string msg)
        {
            if (!string.IsNullOrEmpty(msg))
                _subline = msg;
        }

        void HandleMove(MoveEvent moveEvent)
        {
            if (moveEvent.WasCapture)
            {
                _headline = CaptureLines[Random.Range(0, CaptureLines.Length)];
                _subline = $"{moveEvent.Captured.Type} joins the capture tray";
            }
            else if (moveEvent.GaveCheck)
            {
                _headline = "Check!";
                _subline = $"Protect the {moveEvent.SideToMoveAfter} king";
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
            _side = side;
            _headline = side == PieceColor.White ? "White's turn" : "Black's turn";
            _subline = "Pass the device — the board faces you now";
            _tip = "Tip: green dots = safe moves, red rings = captures";
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

            var width = Mathf.Min(460f, Screen.width - 24f);
            var x = (Screen.width - width) * 0.5f;
            var y = 14f;
            var pop = 1f + _pulse * 0.05f;

            var prevMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3(Screen.width * 0.5f, y + 55f, 0f),
                Quaternion.identity,
                Vector3.one * pop) * Matrix4x4.TRS(new Vector3(-Screen.width * 0.5f, -(y + 55f), 0f), Quaternion.identity, Vector3.one);

            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = _side == PieceColor.White
                ? new Color(0.95f, 0.93f, 0.88f)
                : new Color(0.25f, 0.24f, 0.28f);
            GUI.Box(new Rect(x, y, width, 128f), "");
            GUI.backgroundColor = prevColor;

            var titleColor = _side == PieceColor.White ? new Color(0.15f, 0.12f, 0.1f) : new Color(0.95f, 0.93f, 0.9f);
            var subColor = _side == PieceColor.White ? new Color(0.3f, 0.28f, 0.25f) : new Color(0.8f, 0.78f, 0.75f);

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = titleColor }
            };
            var subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = subColor }
            };
            var tipStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = subColor }
            };

            GUI.Label(new Rect(x + 12f, y + 8f, width - 24f, 30f), _headline, titleStyle);
            GUI.Label(new Rect(x + 16f, y + 38f, width - 32f, 28f), _subline, subStyle);
            GUI.Label(new Rect(x + 16f, y + 66f, width - 32f, 24f), _tip, tipStyle);

            if (GUI.Button(new Rect(x + width * 0.5f - 70f, y + 94f, 140f, 26f), "New Game"))
                controller.ResetGame();

            GUI.matrix = prevMatrix;
        }
    }
}
