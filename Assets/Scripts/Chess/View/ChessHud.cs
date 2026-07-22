using UnityEngine;

namespace Chess.View
{
    /// <summary>
    /// Minimal on-screen status + Reset for Editor / device playtesting.
    /// Full UI polish comes later.
    /// </summary>
    public class ChessHud : MonoBehaviour
    {
        [SerializeField] ChessGameController controller;
        [SerializeField] bool showGui = true;

        void Awake()
        {
            if (controller == null)
                controller = FindFirstObjectByType<ChessGameController>();
        }

        void OnGUI()
        {
            if (!showGui || controller == null)
                return;

            const float pad = 12f;
            GUI.Box(new Rect(pad, pad, 280, 90), "AR Tabletop Chess — Hot Seat");
            GUI.Label(new Rect(pad + 12, pad + 28, 250, 24), controller.StatusMessage ?? "");
            if (GUI.Button(new Rect(pad + 12, pad + 54, 120, 28), "Reset Board"))
                controller.ResetGame();
        }
    }
}
