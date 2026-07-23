#if UNITY_EDITOR
using Chess.AR;
using Chess.View;
using Chess.View.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

namespace Chess.EditorTools
{
    /// <summary>
    /// Injects the AR chess board + placer into the open Mobile AR SampleScene.
    /// </summary>
    public static class ARChessSceneMenu
    {
        [MenuItem("Chess/Setup AR Chess In Open Scene", false, 20)]
        public static void SetupArChessInOpenScene()
        {
            var raycast = Object.FindAnyObjectByType<ARRaycastManager>();
            if (raycast == null)
            {
                EditorUtility.DisplayDialog(
                    "AR Chess",
                    "Open Assets/Scenes/SampleScene.unity first (the Mobile AR template).\n\nIt must contain an XR Origin with ARRaycastManager.",
                    "OK");
                return;
            }

            // Disable template "tap to place objects" UI / spawners so they don't fight chess taps.
            DisableTemplateObjectCreators();

            var boardGo = GameObject.Find("ChessBoard");
            if (boardGo == null)
            {
                boardGo = new GameObject("ChessBoard");
                Undo.RegisterCreatedObjectUndo(boardGo, "Create ChessBoard");
            }

            var boardView = boardGo.GetComponent<ChessBoardView>() ?? Undo.AddComponent<ChessBoardView>(boardGo);
            var controller = boardGo.GetComponent<ChessGameController>() ?? Undo.AddComponent<ChessGameController>(boardGo);

            // Fancy uGUI replaces legacy OnGUI HUDs
            var editorHud = boardGo.GetComponent<ChessHud>();
            if (editorHud != null)
                Undo.DestroyObjectImmediate(editorHud);

            if (boardGo.GetComponent<ChessMenuUi>() == null)
                Undo.AddComponent<ChessMenuUi>(boardGo);

            var placerGo = GameObject.Find("ARChessPlacer");
            if (placerGo == null)
            {
                placerGo = new GameObject("ARChessPlacer");
                Undo.RegisterCreatedObjectUndo(placerGo, "Create ARChessPlacer");
            }

            var placer = placerGo.GetComponent<ARChessBoardPlacer>() ?? Undo.AddComponent<ARChessBoardPlacer>(placerGo);
            // Keep ARChessHud for fallback; ChessMenuUi disables it at runtime
            var arHud = placerGo.GetComponent<ARChessHud>() ?? Undo.AddComponent<ARChessHud>(placerGo);

            var placerSo = new SerializedObject(placer);
            placerSo.FindProperty("boardRoot").objectReferenceValue = boardGo.transform;
            placerSo.FindProperty("raycastManager").objectReferenceValue = raycast;
            placerSo.FindProperty("hideBoardUntilPlaced").boolValue = true;
            placerSo.ApplyModifiedPropertiesWithoutUndo();

            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("boardView").objectReferenceValue = boardView;
            var mainCam = Camera.main;
            if (mainCam != null)
                controllerSo.FindProperty("raycastCamera").objectReferenceValue = mainCam;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            var hudSo = new SerializedObject(arHud);
            hudSo.FindProperty("placer").objectReferenceValue = placer;
            hudSo.FindProperty("gameController").objectReferenceValue = controller;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            // Sensible tabletop size (~32 cm)
            var viewSo = new SerializedObject(boardView);
            viewSo.FindProperty("squareSize").floatValue = 0.04f;
            viewSo.FindProperty("pieceHeight").floatValue = 0.032f;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            boardGo.SetActive(false);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Selection.activeGameObject = placerGo;
            EditorUtility.DisplayDialog(
                "AR Chess Ready",
                "AR chess is set up in this scene.\n\n" +
                "1. File → Save As… → Assets/Scenes/ARChess.unity (recommended)\n" +
                "2. Build & Run on iPhone (ARKit)\n" +
                "3. Scan a table → tap to place board → pick a mode → play\n" +
                "4. Use Replace in the HUD to re-place the board\n\n" +
                "Editor tip: XR Simulation can approximate planes; real device is the real test.",
                "OK");
        }

        static void DisableTemplateObjectCreators()
        {
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>())
            {
                var typeName = behaviour.GetType().Name;
                if (typeName is "ARTemplateMenuManager" or "GoalManager" or "ObjectSpawner" or "ARInteractorSpawnTrigger" or "ARContactSpawnTrigger")
                {
                    Undo.RecordObject(behaviour.gameObject, "Disable AR template spawner");
                    behaviour.gameObject.SetActive(false);
                }
            }

            // Hide tap-to-place prompt canvas children named like prompts if present
            var prompts = GameObject.Find("TapToPlacePrompt");
            if (prompts != null)
            {
                Undo.RecordObject(prompts, "Disable tap prompt");
                prompts.SetActive(false);
            }
        }
    }
}
#endif
