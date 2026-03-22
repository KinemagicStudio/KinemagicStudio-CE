using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CinematicSequencer.UI
{
    /// <summary>
    /// 複数のScrollViewの同期を管理する。
    /// 現行では各Viewで個別にセットアップしている同期ロジックを共通化。
    /// </summary>
    public sealed class ScrollSyncGroup : IDisposable
    {
        public enum SyncAxis { Horizontal, Vertical, Both }

        private readonly List<SyncEntry> _entries = new();
        private bool _isSyncing;

        /// <summary>
        /// 2つのScrollViewを指定軸で同期させる。
        /// </summary>
        public void Sync(ScrollView a, ScrollView b, SyncAxis axis)
        {
            var entry = new SyncEntry(a, b, axis);

            a.verticalScroller.valueChanged += _ => OnScrollChanged(a, b, axis);
            a.horizontalScroller.valueChanged += _ => OnScrollChanged(a, b, axis);
            b.verticalScroller.valueChanged += _ => OnScrollChanged(b, a, axis);
            b.horizontalScroller.valueChanged += _ => OnScrollChanged(b, a, axis);

            _entries.Add(entry);
        }

        /// <summary>
        /// すべての同期接続を解除する。
        /// </summary>
        public void Dispose()
        {
            _entries.Clear();
        }

        private void OnScrollChanged(ScrollView source, ScrollView target, SyncAxis axis)
        {
            if (_isSyncing) return;
            _isSyncing = true;

            try
            {
                var offset = source.scrollOffset;

                if (axis == SyncAxis.Horizontal || axis == SyncAxis.Both)
                    target.scrollOffset = new Vector2(offset.x, target.scrollOffset.y);

                if (axis == SyncAxis.Vertical || axis == SyncAxis.Both)
                    target.scrollOffset = new Vector2(target.scrollOffset.x, offset.y);
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private readonly struct SyncEntry
        {
            public readonly ScrollView A;
            public readonly ScrollView B;
            public readonly SyncAxis Axis;

            public SyncEntry(ScrollView a, ScrollView b, SyncAxis axis)
            {
                A = a;
                B = b;
                Axis = axis;
            }
        }
    }
}
