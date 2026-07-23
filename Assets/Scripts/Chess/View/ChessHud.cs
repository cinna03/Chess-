using Chess.Core;
using UnityEngine;

namespace Chess.View
{
    /// <summary>
    /// Mode select, turn banner, tips, and game controls.
    /// </summary>
    public class ChessHud : MonoBehaviour
    {
        [SerializeField] ChessGameController controller;
        [SerializeField] bool showGui = true;

        string _headline = "AR Tabletop Chess";
        string _subline = "Pick how you want to play";
        string _tip = "Hot-seat = two humans · Computer = you vs AI";
        float _pulse = 1f;
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
            game.OnNewGame += HandleNewGame;
            controller.OnTipChanged += HandleTip;
            controller.OnModeChanged += HandleModeChanged;
            HandleModeChanged();
        }

        void OnDestroy()
        {
            var game = controller != null ? controller.Game : null;
            if (game != null)
            {
                game.OnStatusMessage -= HandleStatus;
                game.OnMoveApplied -= HandleMove;
                game.OnTurnChanged -= HandleTurn;
                game.OnNewGame -= HandleNewGame;
            }

            if (controller != null)
            {
                controller.OnTipChanged -= HandleTip;
                controller.OnModeChanged -= HandleModeChanged;
            }
        }

        void HandleTip(string tip) => _tip = tip;

        void HandleModeChanged()
        {
            if (controller == null)
                return;

            if (!controller.ModeChosen)
            {
                _headline = "AR Tabletop Chess";
                _subline = "How do you want to play?";
                _tip = "Hot-seat for two people, or challenge the computer";
                _pulse = 1f;
                return;
            }

            if (controller.Mode == PlayMode.VersusComputer)
            {
                _headline = "You vs Computer";
                _subline = "You play White — board flips when the computer plays";
                _tip = "Green dots = moves · red rings = captures";
            }
            else
            {
                _headline = "Hot-seat mode";
                _subline = "Two players, one device — board flips each turn";
                _tip = "Pass the device when the board turns";
            }

            _pulse = 1f;
        }

        void HandleNewGame()
        {
            _side = PieceColor.White;
            if (controller != null && controller.ModeChosen && controller.Mode == PlayMode.VersusComputer)
            {
                _headline = "New game vs Computer";
                _subline = "Your move as White";
            }
            else
            {
                _headline = "New game!";
                _subline = "White goes first — good luck";
            }

            _pulse = 1f;
        }

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
            else if (controller != null && controller.Mode == PlayMode.VersusComputer && moveEvent.SideThatMoved == PieceColor.Black)
            {
                _headline = "Computer moved";
                _subline = "Your turn as White";
            }
            else
            {
                _headline = MoveLines[Random.Range(0, MoveLines.Length)];
                _subline = controller != null && controller.Mode == PlayMode.VersusComputer
                    ? "Computer will reply shortly"
                    : "Board is turning for the next player";
            }

            _pulse = 1f;
        }

        void HandleTurn(PieceColor side)
        {
            _side = side;
            if (controller != null && controller.Mode == PlayMode.VersusComputer)
            {
                if (side == PieceColor.White)
                {
                    _headline = "Your turn";
                    _subline = "Play as White";
                    _tip = "Tip: green dots = safe moves, red rings = captures";
                }
                else
                {
                    _headline = "Computer's turn";
                    _subline = controller.IsComputerThinking ? "Computer is thinking…" : "Black is moving";
                    _tip = "Sit back — Black is automatic";
                }
            }
            else
            {
                _headline = side == PieceColor.White ? "White's turn" : "Black's turn";
                _subline = "Pass the device — the board faces you now";
                _tip = "Tip: green dots = safe moves, red rings = captures";
            }

            _pulse = 1f;
        }

        void Update()
        {
            if (_pulse > 0f)
                _pulse = Mathf.Max(0f, _pulse - Time.deltaTime);

            if (controller != null && controller.IsComputerThinking)
            {
                _headline = "Computer is thinking…";
                _subline = "Black is choosing a move";
            }
        }

        void OnGUI()
        {
            if (!showGui || controller == null)
                return;

            if (!controller.ModeChosen)
            {
                DrawModeSelect();
                return;
            }

            DrawInGameHud();
        }

        void DrawModeSelect()
        {
            var width = Mathf.Min(480f, Screen.width - 24f);
            var height = 200f;
            var x = (Screen.width - width) * 0.5f;
            var y = (Screen.height - height) * 0.35f;

            GUI.backgroundColor = new Color(0.92f, 0.9f, 0.86f);
            GUI.Box(new Rect(x, y, width, height), "");

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.15f, 0.12f, 0.1f) }
            };
            var subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.3f, 0.28f, 0.25f) }
            };

            GUI.Label(new Rect(x + 12f, y + 16f, width - 24f, 36f), "AR Tabletop Chess", titleStyle);
            GUI.Label(new Rect(x + 20f, y + 56f, width - 40f, 36f), "Choose a mode to start", subStyle);

            var btnW = (width - 48f) * 0.5f;
            if (GUI.Button(new Rect(x + 16f, y + 110f, btnW, 48f), "Hot-seat\n(2 players)"))
                controller.ChooseMode(PlayMode.HotSeat);

            if (GUI.Button(new Rect(x + 32f + btnW, y + 110f, btnW, 48f), "vs Computer\n(You = White)"))
                controller.ChooseMode(PlayMode.VersusComputer);
        }

        void DrawInGameHud()
        {
            var width = Mathf.Min(460f, Screen.width - 24f);
            var x = (Screen.width - width) * 0.5f;
            var y = 14f;
            var pop = 1f + _pulse * 0.05f;

            var prevMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(
                new Vector3(Screen.width * 0.5f, y + 60f, 0f),
                Quaternion.identity,
                Vector3.one * pop) * Matrix4x4.TRS(new Vector3(-Screen.width * 0.5f, -(y + 60f), 0f), Quaternion.identity, Vector3.one);

            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = _side == PieceColor.White
                ? new Color(0.95f, 0.93f, 0.88f)
                : new Color(0.25f, 0.24f, 0.28f);
            GUI.Box(new Rect(x, y, width, 140f), "");
            GUI.backgroundColor = prevColor;

            var titleColor = _side == PieceColor.White ? new Color(0.15f, 0.12f, 0.1f) : new Color(0.95f, 0.93f, 0.9f);
            var subColor = _side == PieceColor.White ? new Color(0.3f, 0.28f, 0.25f) : new Color(0.8f, 0.78f, 0.75f);

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
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

            GUI.Label(new Rect(x + 12f, y + 8f, width - 24f, 28f), _headline, titleStyle);
            GUI.Label(new Rect(x + 16f, y + 36f, width - 32f, 28f), _subline, subStyle);
            GUI.Label(new Rect(x + 16f, y + 64f, width - 32f, 24f), _tip, tipStyle);

            var btnY = y + 96f;
            if (GUI.Button(new Rect(x + 16f, btnY, 130f, 28f), "New Game"))
                controller.ResetGame();

            if (GUI.Button(new Rect(x + width - 146f, btnY, 130f, 28f), "Change Mode"))
                controller.OpenModeSelect();

            GUI.matrix = prevMatrix;
        }
    }
}
