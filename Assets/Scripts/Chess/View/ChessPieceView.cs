using System.Collections;
using Chess.Core;
using UnityEngine;

namespace Chess.View
{
    /// <summary>
    /// Visual chess piece with move and capture-exit animations.
    /// </summary>
    public class ChessPieceView : MonoBehaviour
    {
        public PieceType Type { get; private set; }
        public PieceColor Color { get; private set; }
        public Square Square { get; private set; }

        public bool IsAnimating { get; private set; }

        Collider _collider;

        public void Configure(PieceType type, PieceColor color, Square square)
        {
            Type = type;
            Color = color;
            Square = square;
            name = $"{color}_{type}_{square}";
            _collider = GetComponent<Collider>();
        }

        public void SetSquare(Square square)
        {
            Square = square;
            name = $"{Color}_{Type}_{square}";
        }

        public void SetInteractable(bool enabled)
        {
            if (_collider == null)
                _collider = GetComponent<Collider>();
            if (_collider != null)
                _collider.enabled = enabled;
        }

        public IEnumerator AnimateMoveTo(Vector3 localTarget, float duration, float hopHeight)
        {
            IsAnimating = true;
            SetInteractable(false);

            var start = transform.localPosition;
            var elapsed = 0f;
            duration = Mathf.Max(0.05f, duration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                var pos = Vector3.Lerp(start, localTarget, t);
                pos.y += Mathf.Sin(t * Mathf.PI) * hopHeight;
                transform.localPosition = pos;
                yield return null;
            }

            transform.localPosition = localTarget;
            IsAnimating = false;
            SetInteractable(true);
        }

        public IEnumerator AnimateCaptureExit(Vector3 localAway, float duration)
        {
            IsAnimating = true;
            SetInteractable(false);

            var start = transform.localPosition;
            var startScale = transform.localScale;
            var elapsed = 0f;
            duration = Mathf.Max(0.05f, duration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                transform.localPosition = Vector3.Lerp(start, localAway, t);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                transform.Rotate(0f, 360f * Time.deltaTime, 0f, Space.Self);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
