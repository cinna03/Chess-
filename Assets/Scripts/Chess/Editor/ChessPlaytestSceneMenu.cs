#if UNITY_EDITOR
using Chess.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Chess.EditorTools
{
    /// <summary>
    /// Creates/opens a dedicated non-AR scene for testing chess with the mouse.
    /// </summary>
    public static class ChessPlaytestSceneMenu
    {
        public const string ScenePath = "Assets/Scenes/ChessPlaytest.unity";

        [MenuItem("Chess/Open Editor Playtest Scene", false, 0)]
        public static void OpenPlaytestScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (!System.IO.File.Exists(ScenePath))
                CreateAndSavePlaytestScene();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var board = GameObject.Find("ChessBoard");
            if (board != null)
                Selection.activeGameObject = board;

            EditorUtility.DisplayDialog(
                "Chess Playtest",
                "ChessPlaytest scene is open.\n\nPress Play, then click a white piece and a highlighted square to move.",
                "OK");

            Debug.Log($"[Chess] Opened {ScenePath}. Roots: {scene.rootCount}");
        }

        [MenuItem("Chess/Recreate Playtest Scene", false, 1)]
        public static void RecreatePlaytestScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            CreateAndSavePlaytestScene();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorUtility.DisplayDialog(
                "Chess Playtest",
                "Playtest scene recreated at Assets/Scenes/ChessPlaytest.unity.\n\nPress Play to test.",
                "OK");
        }

        static void CreateAndSavePlaytestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildContents();
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[Chess] Saved playtest scene at {ScenePath}");
        }

        static void BuildContents()
        {
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.2f, 0.24f);
            cam.nearClipPlane = 0.01f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 0.65f, -0.65f);
            camGo.transform.rotation = Quaternion.Euler(45f, 0f, 0f);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var boardGo = new GameObject("ChessBoard");
            var boardView = boardGo.AddComponent<ChessBoardView>();
            var controller = boardGo.AddComponent<ChessGameController>();
            var hud = boardGo.AddComponent<ChessHud>();

            var so = new SerializedObject(controller);
            so.FindProperty("boardView").objectReferenceValue = boardView;
            so.FindProperty("raycastCamera").objectReferenceValue = cam;
            so.ApplyModifiedPropertiesWithoutUndo();

            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("controller").objectReferenceValue = controller;
            hudSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
