using Chess.View;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Chess.View
{
    /// <summary>
    /// Builds a mouse-playable chess board for Editor testing (no AR required).
    /// </summary>
    public class ChessEditorBootstrap : MonoBehaviour
    {
        [SerializeField] bool createOnAwake = true;

        void Awake()
        {
            if (createOnAwake)
                Build();
        }

        [ContextMenu("Build Playtest Board")]
        public void Build()
        {
            if (GetComponent<ChessBoardView>() != null)
                return;

            gameObject.AddComponent<ChessBoardView>();
            gameObject.AddComponent<ChessGameController>();
            gameObject.AddComponent<ChessHud>();

            if (Camera.main == null)
            {
                var camGo = new GameObject("Playtest Camera");
                var cam = camGo.AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.transform.position = new Vector3(0f, 0.55f, -0.55f);
                cam.transform.LookAt(Vector3.zero);
            }
            else
            {
                var cam = Camera.main.transform;
                cam.position = new Vector3(0f, 0.55f, -0.55f);
                cam.LookAt(transform.position);
            }

            if (Object.FindFirstObjectByType<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            Debug.Log("[Chess] Editor playtest board ready. Click pieces/squares to play hot-seat.");
        }

#if UNITY_EDITOR
        [MenuItem("GameObject/Chess/Create Editor Playtest Board", false, 10)]
        static void CreateFromMenu()
        {
            var go = new GameObject("ChessPlaytest");
            Undo.RegisterCreatedObjectUndo(go, "Create Chess Playtest");
            var bootstrap = go.AddComponent<ChessEditorBootstrap>();
            bootstrap.Build();
            Selection.activeGameObject = go;
        }
#endif
    }
}
