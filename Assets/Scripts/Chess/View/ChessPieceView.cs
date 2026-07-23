using System.Collections;
using Chess.Core;
using UnityEngine;

namespace Chess.View
{
    /// <summary>
    /// Visual chess piece with move, capture-tray, and selection bob animations.
    /// </summary>
    public class ChessPieceView : MonoBehaviour
    {
        public PieceType Type { get; private set; }
        public PieceColor Color { get; private set; }
        public Square Square { get; private set; }

        public bool IsAnimating { get; private set; }
        public bool IsSelected { get; private set; }

        Collider _collider;
        Vector3 _restLocalPos;
        Vector3 _baseScale;
        float _bobTime;

        public void Configure(PieceType type, PieceColor color, Square square)
        {
            Type = type;
            Color = color;
            Square = square;
            name = $"{color}_{type}_{square}";
            _collider = GetComponent<Collider>();
            _restLocalPos = transform.localPosition;
            _baseScale = transform.localScale;
        }

        public void SetSquare(Square square)
        {
            Square = square;
            name = $"{Color}_{Type}_{square}";
            _restLocalPos = transform.localPosition;
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            _bobTime = 0f;
            if (!selected && !IsAnimating)
                transform.localPosition = _restLocalPos;
        }

        public void SetInteractable(bool enabled)
        {
            if (_collider == null)
                _collider = GetComponent<Collider>();
            if (_collider != null)
                _collider.enabled = enabled;
        }

        void Update()
        {
            if (!IsSelected || IsAnimating)
                return;

            _bobTime += Time.deltaTime * 3.5f;
            var lift = 0.012f + Mathf.Sin(_bobTime) * 0.006f;
            var p = _restLocalPos;
            p.y += lift;
            transform.localPosition = p;
            transform.localScale = _baseScale * (1f + Mathf.Sin(_bobTime * 0.5f) * 0.03f);
        }

        public IEnumerator AnimateMoveTo(Vector3 localTarget, float duration, float hopHeight)
        {
            IsAnimating = true;
            IsSelected = false;
            SetInteractable(false);
            transform.localScale = _baseScale;

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
            _restLocalPos = localTarget;
            IsAnimating = false;
            SetInteractable(true);
        }

        public IEnumerator AnimateToTray(Vector3 localTrayPos, float duration)
        {
            IsAnimating = true;
            IsSelected = false;
            SetInteractable(false);

            var start = transform.localPosition;
            var startScale = transform.localScale;
            var endScale = _baseScale * 0.55f;
            var elapsed = 0f;
            duration = Mathf.Max(0.05f, duration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                var mid = Vector3.Lerp(start, localTrayPos, t);
                mid.y += Mathf.Sin(t * Mathf.PI) * 0.05f;
                transform.localPosition = mid;
                transform.localScale = Vector3.Lerp(startScale, endScale, t);
                transform.Rotate(0f, 420f * Time.deltaTime, 0f, Space.Self);
                yield return null;
            }

            transform.localPosition = localTrayPos;
            transform.localScale = endScale;
            _restLocalPos = localTrayPos;
            _baseScale = endScale;
            IsAnimating = false;
            name = $"{Color}_{Type}_captured";
        }
    }
}
