using Chess.View;
using UnityEngine;

namespace Chess.AR
{
    /// <summary>
    /// Lightweight AR HUD: placement status + Reset placement / Reset game.
    /// </summary>
    public class ARChessHud : MonoBehaviour
    {
        [SerializeField] ARChessBoardPlacer placer;
        [SerializeField] ChessGameController gameController;
        [SerializeField] bool showGui = true;

        void Awake()
        {
            if (placer == null)
                placer = FindAnyObjectByType<ARChessBoardPlacer>();
            if (gameController == null)
                gameController = FindAnyObjectByType<ChessGameController>();
        }

        void OnGUI()
        {
            if (!showGui)
                return;

            const float pad = 12f;
            GUI.Box(new Rect(pad, pad, 320, 130), "AR Tabletop Chess");

            var status = placer != null ? placer.StatusText : "No placer";
            if (placer != null && placer.IsPlaced && gameController != null)
                status = gameController.StatusMessage;

            GUI.Label(new Rect(pad + 12, pad + 28, 296, 40), status);

            if (GUI.Button(new Rect(pad + 12, pad + 72, 140, 28), "Replace Board"))
                placer?.ResetPlacement();

            if (GUI.Button(new Rect(pad + 160, pad + 72, 140, 28), "New Game"))
                gameController?.ResetGame();
        }
    }
}
