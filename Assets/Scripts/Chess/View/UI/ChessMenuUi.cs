using System.Collections;
using Chess.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.View.UI
{
    /// <summary>
    /// Builds a polished uGUI (Fredoka + animated panels) at runtime.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public class ChessMenuUi : MonoBehaviour
    {
        [SerializeField] ChessGameController controller;
        [SerializeField] Font headlineFont;
        [SerializeField] Font bodyFont;

        Canvas _canvas;
        RectTransform _root;

        GameObject _modePanel;
        GameObject _hudPanel;
        GameObject _gameOverPanel;
        CanvasGroup _modeGroup;
        CanvasGroup _hudGroup;
        CanvasGroup _gameOverGroup;

        Text _hudHeadline;
        Text _hudSubline;
        Text _hudTip;
        Text _hudModeBadge;
        Image _hudBanner;

        Text _overTitle;
        Text _overSubtitle;

        ParticleSystem _celebrate;

        static readonly Color Cream = new Color(0.98f, 0.95f, 0.90f);
        static readonly Color Ink = new Color(0.18f, 0.14f, 0.12f);
        static readonly Color Accent = new Color(0.95f, 0.45f, 0.35f);
        static readonly Color Mint = new Color(0.35f, 0.72f, 0.58f);
        static readonly Color Night = new Color(0.16f, 0.17f, 0.22f);
        static readonly Color NightText = new Color(0.95f, 0.93f, 0.90f);

        void Awake()
        {
            if (controller == null)
                controller = FindAnyObjectByType<ChessGameController>();

            ResolveFonts();
            BuildUi();
            DisableLegacyHud();
        }

        void Start()
        {
            if (controller == null)
                return;

            controller.OnTipChanged += OnTip;
            controller.OnModeChanged += RefreshModeVisibility;
            var game = controller.Game;
            if (game != null)
            {
                game.OnStatusMessage += OnStatus;
                game.OnMoveApplied += OnMove;
                game.OnTurnChanged += OnTurn;
                game.OnNewGame += OnNewGame;
                game.OnGameOver += OnGameOver;
            }

            RefreshModeVisibility();
        }

        void OnDestroy()
        {
            if (controller == null)
                return;
            controller.OnTipChanged -= OnTip;
            controller.OnModeChanged -= RefreshModeVisibility;
            var game = controller.Game;
            if (game == null)
                return;
            game.OnStatusMessage -= OnStatus;
            game.OnMoveApplied -= OnMove;
            game.OnTurnChanged -= OnTurn;
            game.OnNewGame -= OnNewGame;
            game.OnGameOver -= OnGameOver;
        }

        void ResolveFonts()
        {
            if (headlineFont == null)
                headlineFont = Resources.Load<Font>("Fonts/Fredoka-Bold");
            if (bodyFont == null)
                bodyFont = Resources.Load<Font>("Fonts/Fredoka-SemiBold");

            // Fallback: load from Assets via Resources path we set up, else Unity builtin
            if (headlineFont == null || bodyFont == null)
            {
                var builtins = Font.CreateDynamicFontFromOSFont(new[] { "Arial Rounded MT Bold", "Avenir Next", "Helvetica Neue", "Arial" }, 64);
                if (headlineFont == null) headlineFont = builtins;
                if (bodyFont == null) bodyFont = builtins;
            }
        }

        void DisableLegacyHud()
        {
            foreach (var hud in FindObjectsByType<ChessHud>())
                hud.enabled = false;
        }

        void BuildUi()
        {
            var canvasGo = new GameObject("ChessFancyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 50;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            _root = canvasGo.GetComponent<RectTransform>();

            EnsureEventSystem();
            _modePanel = BuildModePanel();
            _hudPanel = BuildHudPanel();
            _gameOverPanel = BuildGameOverPanel();
            _celebrate = BuildCelebration();

            _modeGroup = _modePanel.GetComponent<CanvasGroup>();
            _hudGroup = _hudPanel.GetComponent<CanvasGroup>();
            _gameOverGroup = _gameOverPanel.GetComponent<CanvasGroup>();
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
                return;

            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        GameObject BuildModePanel()
        {
            var panel = CreatePanel("ModeSelectPanel", new Color(0.1f, 0.1f, 0.12f, 0.55f), true);
            var card = CreateCard(panel.transform, new Vector2(0.5f, 0.55f), new Vector2(720, 560), Cream);
            AddText(card.transform, "AR Tabletop Chess", 54, Ink, new Vector2(0, 190), new Vector2(640, 80), headlineFont, FontStyle.Bold);
            AddText(card.transform, "Cute chess. Real rules. Pick a vibe.", 26, new Color(0.35f, 0.3f, 0.28f), new Vector2(0, 120), new Vector2(600, 50), bodyFont, FontStyle.Normal);

            CreateModeButton(card.transform, "Hot-seat", "2 players · one device\nBoard flips each turn", Mint, new Vector2(-160, -20), () => controller.ChooseMode(PlayMode.HotSeat));
            CreateModeButton(card.transform, "vs Computer", "You are White\nSmarter minimax AI", Accent, new Vector2(160, -20), () => controller.ChooseMode(PlayMode.VersusComputer));

            AddText(card.transform, "Tip: green dots = moves · red rings = captures", 20, new Color(0.4f, 0.36f, 0.33f), new Vector2(0, -200), new Vector2(620, 40), bodyFont, FontStyle.Italic);
            return panel;
        }

        GameObject BuildHudPanel()
        {
            var panel = CreatePanel("HudPanel", Color.clear, false);
            var banner = CreateCard(panel.transform, new Vector2(0.5f, 0.92f), new Vector2(860, 210), Cream);
            _hudBanner = banner.GetComponent<Image>();

            _hudModeBadge = AddText(banner.transform, "HOT-SEAT", 18, Accent, new Vector2(0, 72), new Vector2(200, 30), bodyFont, FontStyle.Bold);
            _hudHeadline = AddText(banner.transform, "White's turn", 42, Ink, new Vector2(0, 28), new Vector2(800, 50), headlineFont, FontStyle.Bold);
            _hudSubline = AddText(banner.transform, "Make your move", 22, new Color(0.35f, 0.32f, 0.3f), new Vector2(0, -10), new Vector2(800, 36), bodyFont, FontStyle.Normal);
            _hudTip = AddText(banner.transform, "Tip: tap a piece, then a glowing square", 18, new Color(0.45f, 0.4f, 0.38f), new Vector2(0, -48), new Vector2(800, 30), bodyFont, FontStyle.Italic);

            CreatePillButton(panel.transform, "New Game", new Vector2(0.22f, 0.04f), Mint, () => controller.ResetGame());
            CreatePillButton(panel.transform, "Modes", new Vector2(0.78f, 0.04f), Accent, () =>
            {
                controller.OpenModeSelect();
                RefreshModeVisibility();
            });

            panel.SetActive(false);
            return panel;
        }

        GameObject BuildGameOverPanel()
        {
            var panel = CreatePanel("GameOverPanel", new Color(0.05f, 0.05f, 0.08f, 0.65f), true);
            var card = CreateCard(panel.transform, new Vector2(0.5f, 0.5f), new Vector2(700, 420), Cream);
            _overTitle = AddText(card.transform, "Checkmate!", 56, Accent, new Vector2(0, 110), new Vector2(640, 70), headlineFont, FontStyle.Bold);
            _overSubtitle = AddText(card.transform, "White wins the game", 26, Ink, new Vector2(0, 40), new Vector2(620, 50), bodyFont, FontStyle.Normal);
            AddText(card.transform, "Rematch or switch modes anytime.", 20, new Color(0.4f, 0.36f, 0.33f), new Vector2(0, -10), new Vector2(600, 36), bodyFont, FontStyle.Italic);

            CreatePillButton(card.transform, "Rematch", new Vector2(0.5f, 0.28f), Mint, () =>
            {
                controller.ResetGame();
                HideGameOver();
            }, true);
            CreatePillButton(card.transform, "Change Mode", new Vector2(0.5f, 0.12f), Accent, () =>
            {
                controller.OpenModeSelect();
                HideGameOver();
                RefreshModeVisibility();
            }, true);

            panel.SetActive(false);
            return panel;
        }

        ParticleSystem BuildCelebration()
        {
            var go = new GameObject("Celebrate");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.4f;
            main.startSpeed = 3.5f;
            main.startSize = 0.08f;
            main.maxParticles = 80;
            main.loop = false;
            main.playOnAwake = false;
            main.startColor = new ParticleSystem.MinMaxGradient(Accent, Mint);
            main.gravityModifier = 0.8f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25;
            go.transform.position = new Vector3(0f, 0.4f, -0.2f);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            return ps;
        }

        void RefreshModeVisibility()
        {
            if (controller == null)
                return;

            if (!controller.ModeChosen)
            {
                _gameOverPanel.SetActive(false);
                _hudPanel.SetActive(false);
                _modePanel.SetActive(true);
                StartCoroutine(ChessUiAnimator.PopIn(_modeGroup, _modePanel.transform.GetChild(0) as RectTransform));
            }
            else
            {
                _modePanel.SetActive(false);
                _hudPanel.SetActive(true);
                StartCoroutine(ChessUiAnimator.PopIn(_hudGroup, _hudPanel.transform.GetChild(0) as RectTransform));
                _hudModeBadge.text = controller.Mode == PlayMode.VersusComputer ? "VS COMPUTER" : "HOT-SEAT";
            }
        }

        void OnTip(string tip)
        {
            if (_hudTip != null)
                _hudTip.text = tip;
        }

        void OnStatus(string status)
        {
            if (_hudSubline != null)
                _hudSubline.text = status;
        }

        void OnTurn(PieceColor side)
        {
            var vsAi = controller != null && controller.Mode == PlayMode.VersusComputer;
            if (vsAi)
            {
                _hudHeadline.text = side == PieceColor.White ? "Your turn" : "Computer's turn";
                _hudSubline.text = side == PieceColor.White ? "Play as White" : "Black is thinking…";
            }
            else
            {
                _hudHeadline.text = side == PieceColor.White ? "White's turn" : "Black's turn";
                _hudSubline.text = "Pass the device — board faces you";
            }

            ApplyBannerTheme(side);
            if (_hudBanner != null)
                StartCoroutine(ChessUiAnimator.PunchScale(_hudBanner.rectTransform));
        }

        void OnMove(MoveEvent moveEvent)
        {
            if (moveEvent.WasCapture)
            {
                _hudHeadline.text = "Gotcha!";
                _hudSubline.text = $"{moveEvent.Captured.Type} joins the capture tray";
            }
            else if (moveEvent.GaveCheck && moveEvent.ResultAfterMove == GameResult.Playing)
            {
                _hudHeadline.text = "Check!";
                _hudSubline.text = $"Protect the {moveEvent.SideToMoveAfter} king";
            }

            if (_hudBanner != null)
                StartCoroutine(ChessUiAnimator.PunchScale(_hudBanner.rectTransform, 1.06f, 0.22f));
        }

        void OnNewGame()
        {
            HideGameOver();
            _hudHeadline.text = "New game!";
            _hudSubline.text = controller != null && controller.Mode == PlayMode.VersusComputer
                ? "You are White — good luck"
                : "White goes first";
            ApplyBannerTheme(PieceColor.White);
        }

        void OnGameOver(GameResult result, PieceColor? winner)
        {
            _gameOverPanel.SetActive(true);
            if (result == GameResult.Checkmate)
            {
                _overTitle.text = "Checkmate!";
                _overSubtitle.text = winner.HasValue ? $"{winner.Value} wins the game" : "Game over";
                _celebrate?.Play();
            }
            else
            {
                _overTitle.text = "Stalemate!";
                _overSubtitle.text = "It's a peaceful draw";
            }

            StartCoroutine(ChessUiAnimator.PopIn(_gameOverGroup, _gameOverPanel.transform.GetChild(0) as RectTransform, 0.4f));
        }

        void HideGameOver()
        {
            if (_gameOverPanel != null)
                _gameOverPanel.SetActive(false);
        }

        void ApplyBannerTheme(PieceColor side)
        {
            if (_hudBanner == null)
                return;

            if (side == PieceColor.White)
            {
                _hudBanner.color = Cream;
                SetTextColor(_hudHeadline, Ink);
                SetTextColor(_hudSubline, new Color(0.35f, 0.32f, 0.3f));
                SetTextColor(_hudTip, new Color(0.45f, 0.4f, 0.38f));
                SetTextColor(_hudModeBadge, Accent);
            }
            else
            {
                _hudBanner.color = Night;
                SetTextColor(_hudHeadline, NightText);
                SetTextColor(_hudSubline, new Color(0.8f, 0.78f, 0.75f));
                SetTextColor(_hudTip, new Color(0.7f, 0.68f, 0.65f));
                SetTextColor(_hudModeBadge, Mint);
            }
        }

        static void SetTextColor(Text text, Color color)
        {
            if (text != null)
                text.color = color;
        }

        GameObject CreatePanel(string name, Color backdrop, bool stretchFull)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(_root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = backdrop;
            img.raycastTarget = backdrop.a > 0.01f;
            go.GetComponent<CanvasGroup>();
            return go;
        }

        GameObject CreateCard(Transform parent, Vector2 anchor, Vector2 size, Color color)
        {
            var go = new GameObject("Card", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = color;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.12f);
            outline.effectDistance = new Vector2(3, -3);
            return go;
        }

        void CreateModeButton(Transform parent, string title, string subtitle, Color color, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(title + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.45f);
            rt.sizeDelta = new Vector2(280, 170);
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.color = color;
            var button = go.GetComponent<Button>();
            button.targetGraphic = img;
            button.onClick.AddListener(onClick);
            AddText(go.transform, title, 30, Color.white, new Vector2(0, 28), new Vector2(250, 40), headlineFont, FontStyle.Bold);
            AddText(go.transform, subtitle, 18, new Color(1f, 1f, 1f, 0.92f), new Vector2(0, -30), new Vector2(250, 70), bodyFont, FontStyle.Normal);
        }

        void CreatePillButton(Transform parent, string label, Vector2 anchor, Color color, UnityEngine.Events.UnityAction onClick, bool relativeToCard = false)
        {
            var go = new GameObject(label + "Btn", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (relativeToCard)
            {
                rt.anchorMin = rt.anchorMax = anchor;
                rt.anchoredPosition = Vector2.zero;
            }
            else
            {
                rt.anchorMin = rt.anchorMax = anchor;
                rt.anchoredPosition = Vector2.zero;
            }

            rt.sizeDelta = new Vector2(240, 64);
            var img = go.GetComponent<Image>();
            img.color = color;
            var button = go.GetComponent<Button>();
            button.targetGraphic = img;
            button.onClick.AddListener(onClick);
            AddText(go.transform, label, 24, Color.white, Vector2.zero, new Vector2(220, 50), headlineFont, FontStyle.Bold);
        }

        Text AddText(Transform parent, string content, int size, Color color, Vector2 pos, Vector2 sizeDelta, Font font, FontStyle style)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;
            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
