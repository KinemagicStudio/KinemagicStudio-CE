using System;
using CinematicSequencer.IO;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI
{
    /// <summary>
    /// ライブラリアイテムのUI要素。ClipAssetまたはSequenceの1行を表示する。
    /// </summary>
    public sealed class LibraryItemElement : VisualElement
    {
        private readonly Label _nameLabel;

        public Guid ItemId { get; private set; }
        public string ItemName { get; private set; }
        public TrackType? ItemType { get; private set; }

        /// <summary>クリックコールバック。ListViewの再利用時に再代入されるためデリゲートプロパティ。</summary>
        public Action<Guid> OnClicked { get; set; }

        /// <summary>ダブルクリックコールバック。</summary>
        public Action<Guid> OnDoubleClicked { get; set; }

        public LibraryItemElement()
        {
            AddToClassList("library-item");

            _nameLabel = new Label();
            _nameLabel.AddToClassList("library-item-label");
            Add(_nameLabel);

            RegisterCallback<ClickEvent>(OnClick);
            RegisterCallback<ContextualMenuPopulateEvent>(OnContextMenu);
        }

        public void Bind(ClipAssetInfo info)
        {
            ItemId = info.Id;
            ItemName = info.Name;
            ItemType = info.Type;
            _nameLabel.text = info.Name;

            ClearTypeClasses();
            AddToClassList(GetTrackTypeClass(info.Type));
        }

        public void Bind(SequenceInfo info)
        {
            ItemId = info.Id;
            ItemName = info.Name;
            ItemType = null;
            _nameLabel.text = info.Name;

            ClearTypeClasses();
            AddToClassList("sequence-item");
        }

        private void OnClick(ClickEvent evt)
        {
            if (evt.clickCount >= 2)
                OnDoubleClicked?.Invoke(ItemId);
            else
                OnClicked?.Invoke(ItemId);
            evt.StopPropagation();
        }

        private void OnContextMenu(ContextualMenuPopulateEvent evt)
        {
            if (ItemType.HasValue)
            {
                evt.menu.AppendAction("Edit Keyframes", _ => OnDoubleClicked?.Invoke(ItemId));
            }
        }

        private void ClearTypeClasses()
        {
            RemoveFromClassList("camera-item");
            RemoveFromClassList("light-item");
            RemoveFromClassList("effect-item");
            RemoveFromClassList("audio-item");
            RemoveFromClassList("motion-item");
            RemoveFromClassList("posteffect-item");
            RemoveFromClassList("sequence-item");
        }

        private static string GetTrackTypeClass(TrackType type)
        {
            return type switch
            {
                TrackType.CameraPose => "camera-item",
                TrackType.CameraProperties => "camera-item",
                TrackType.LightPose => "light-item",
                TrackType.LightProperties => "light-item",
                TrackType.Effect => "effect-item",
                TrackType.Audio => "audio-item",
                TrackType.Motion => "motion-item",
                TrackType.PostEffect => "posteffect-item",
                _ => "library-item-default",
            };
        }
    }
}
