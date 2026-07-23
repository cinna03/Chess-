using System.Collections.Generic;
using Chess.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Chess.AR
{
    /// <summary>
    /// Tap a detected horizontal plane to place the chess board once.
    /// After placement, piece selection uses ChessGameController raycasts.
    /// </summary>
    public class ARChessBoardPlacer : MonoBehaviour
    {
        [SerializeField] Transform boardRoot;
        [SerializeField] ARRaycastManager raycastManager;
        [SerializeField] bool hideBoardUntilPlaced = true;
        [SerializeField] float yOffset = 0.002f;
        [SerializeField] bool allowRepositionWithTwoFingerTap;

        static readonly List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

        public bool IsPlaced { get; private set; }
        public string StatusText { get; private set; } = "Scan a table, then tap to place the chess board";

        public event System.Action OnBoardPlaced;
        public event System.Action OnPlacementReset;

        void Awake()
        {
            if (raycastManager == null)
                raycastManager = FindAnyObjectByType<ARRaycastManager>();

            if (boardRoot == null)
            {
                var view = FindAnyObjectByType<ChessBoardView>();
                if (view != null)
                    boardRoot = view.transform;
            }

            if (boardRoot != null && hideBoardUntilPlaced && !IsPlaced)
                boardRoot.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!WasPrimaryTap(out var screenPos))
                return;

            if (IsPointerOverUI(screenPos))
                return;

            if (IsPlaced && !allowRepositionWithTwoFingerTap)
                return;

            // Optional: two-finger tap to move board again
            if (IsPlaced && allowRepositionWithTwoFingerTap)
            {
                var touchscreen = Touchscreen.current;
                if (touchscreen == null || touchscreen.touches.Count < 2)
                    return;
            }

            TryPlaceAtScreenPosition(screenPos);
        }

        public void TryPlaceAtScreenPosition(Vector2 screenPos)
        {
            if (raycastManager == null || boardRoot == null)
            {
                StatusText = "AR not ready — missing raycast manager or board";
                return;
            }

            if (!raycastManager.Raycast(screenPos, s_Hits, TrackableType.PlaneWithinPolygon))
            {
                StatusText = "Aim at a detected surface, then tap";
                return;
            }

            var pose = s_Hits[0].pose;
            boardRoot.SetPositionAndRotation(
                pose.position + Vector3.up * yOffset,
                AlignYawToCamera(pose.rotation));

            if (!boardRoot.gameObject.activeSelf)
                boardRoot.gameObject.SetActive(true);

            IsPlaced = true;
            StatusText = "Board placed — choose a mode and play";
            OnBoardPlaced?.Invoke();
        }

        public void ResetPlacement()
        {
            IsPlaced = false;
            StatusText = "Scan a table, then tap to place the chess board";
            if (boardRoot != null && hideBoardUntilPlaced)
                boardRoot.gameObject.SetActive(false);

            var controller = boardRoot != null
                ? boardRoot.GetComponent<ChessGameController>()
                : null;
            controller?.ResetGame();
            OnPlacementReset?.Invoke();
        }

        static Quaternion AlignYawToCamera(Quaternion planeRotation)
        {
            var camera = Camera.main;
            if (camera == null)
                return planeRotation;

            var forward = camera.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                return planeRotation;

            forward.Normalize();
            // Board's +Z faces the player roughly
            return Quaternion.LookRotation(forward, Vector3.up);
        }

        static bool WasPrimaryTap(out Vector2 screenPos)
        {
            screenPos = default;

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screenPos = mouse.position.ReadValue();
                return true;
            }

            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                screenPos = touch.primaryTouch.position.ReadValue();
                return true;
            }

            return false;
        }

        static bool IsPointerOverUI(Vector2 screenPos)
        {
            if (EventSystem.current == null)
                return false;

            var eventData = new PointerEventData(EventSystem.current) { position = screenPos };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            return results.Count > 0;
        }
    }
}
