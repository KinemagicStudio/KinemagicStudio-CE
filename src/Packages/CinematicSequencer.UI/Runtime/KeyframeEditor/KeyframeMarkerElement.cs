using System;
using CinematicSequencer.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI.KeyframeEditor
{
    /// <summary>
    /// キーフレームのダイヤモンド形マーカー。タイムライン行上に絶対配置される。
    /// </summary>
    public sealed class KeyframeMarkerElement : VisualElement
    {
        private const float MarkerSize = 10f;

        public KeyframeId Id { get; }

        public event Action<KeyframeId> OnClicked;
        public event Action<KeyframeId> OnDeleteRequested;

        public KeyframeMarkerElement(KeyframeId id, float pixelX)
        {
            Id = id;

            AddToClassList("keyframe-marker");

            style.position = Position.Absolute;
            style.width = MarkerSize;
            style.height = MarkerSize;
            style.backgroundColor = new Color(1f, 0.85f, 0f); // yellow diamond
            style.rotate = new Rotate(45f);
            style.translate = new Translate(-MarkerSize / 2f, -MarkerSize / 2f);

            SetPosition(pixelX);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<ContextualMenuPopulateEvent>(OnContextMenu);
        }

        public void SetPosition(float pixelX)
        {
            style.left = pixelX;
        }

        public void SetSelected(bool selected)
        {
            EnableInClassList("selected", selected);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button == 0)
            {
                evt.StopPropagation();
                OnClicked?.Invoke(Id);
            }
        }

        private void OnContextMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Delete keyframe", _ => OnDeleteRequested?.Invoke(Id));
        }
    }
}
