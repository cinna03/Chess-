#if UNITY_EDITOR
using Chess.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Chess.EditorTools
{
    /// <summary>
    /// Creates/opens a dedicated non-AR scene for testing chess with the mouse.
    /// </summary>
    public static class ChessPlaytestSceneMenu
    {
        const string ScenePath = "Assets/Scenes/ChessPlaytest.unity";

        [MenuItem("Chess/Open Editor Playtest Scene", false, 0)]
        public static void OpenPlaytestScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EnsurePlaytestSceneExists();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Selection.activeGameObject = GameObject.Find("ChessBoard");
            Debug.Log("[Chess] Opened ChessPlaytest scene. Press Play, then click pieces to move.");
        }

        [MenuItem("Chess/Recreate Playtest Scene", false, 1)]
        public static void RecreatePlaytestScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildContents();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[Chess] Saved playtest scene at {ScenePath}. Press Play to test.");
        }

        static void EnsurePlaytestSceneExists()
        {
            if (System.IO.File.Exists(ScenePath))
                return;
            RecreatePlaytestScene();
        }

        static void BuildContents()
        {
            // Camera
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.2f, 0.24f);
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 0.65f, -0.65f);
            camGo.transform.LookAt(Vector3.zero);

            // Light
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Board root
            var boardGo = new GameObject("ChessBoard");
            boardGo.AddComponent<ChessBoardView>();
            boardGo.AddComponent<ChessGameController>();
            boardGo.AddComponent<ChessHud>();

            // Bootstrap builds board in Awake only if ChessBoardView was just added empty —
            // ChessGameController.Bind already builds via Bind/Refresh. ChessBoardView.BuildSquares
            // runs on Bind. No ChessEditorBootstrap needed here.
        }
    }
}
#endif
