# CinematicSequencer v2 再設計 - UIパッケージ詳細

- 作成日: 2026-03-22
- 最終更新日: 2026-03-22

関連: [概要ドキュメント](./CinematicSequencer-Redesign-Overview.md) | [コアパッケージ](./CinematicSequencer-Redesign-Core.md)

## 1. パッケージ構成

```
CinematicSequencer.UI/
├── Runtime/
│   ├── Core/
│   │   ├── TimeRulerElement.cs          # タイムルーラー（共通）
│   │   ├── PlayheadElement.cs           # 再生ヘッド（共通）
│   │   ├── ScrollSyncGroup.cs           # スクロール同期（共通）
│   │   ├── TrackListElement.cs          # トラックヘッダー＋コンテンツ
│   │   ├── SnappingService.cs           # スナッピング計算
│   │   ├── SelectionState.cs            # 選択状態管理
│   │   ├── ZoomState.cs                 # ズーム状態管理
│   │   └── KeyboardShortcutHandler.cs   # ショートカット
│   ├── SequenceEditor/
│   │   ├── SequenceEditorController.cs  # コントローラ（旧Presenter）
│   │   ├── SequenceEditorView.cs        # メインView
│   │   ├── TrackHeaderView.cs           # トラックヘッダー行
│   │   ├── TrackContentView.cs          # トラックコンテンツ行
│   │   ├── ClipElement.cs              # クリップUI要素
│   │   ├── ClipManipulator.cs          # クリップ操作（D&D/リサイズ）
│   │   └── PlaybackToolbar.cs          # 再生コントロール
│   ├── KeyframeEditor/
│   │   ├── KeyframeEditorController.cs  # コントローラ
│   │   ├── KeyframeEditorView.cs        # メインView
│   │   ├── KeyframeMarkerElement.cs     # キーフレームUI要素
│   │   ├── PropertyEditorPanel.cs       # プロパティ値編集パネル
│   │   └── CurveEditorView.cs          # カーブ可視化（将来対応）
│   ├── Library/
│   │   ├── LibraryController.cs
│   │   ├── LibraryView.cs
│   │   └── LibraryItemElement.cs
│   ├── Dialogs/
│   │   ├── SaveConfirmationDialog.cs
│   │   └── DialogService.cs
│   └── UIAssets/
│       ├── SequenceEditor.uxml
│       ├── SequenceEditor.uss
│       ├── KeyframeEditor.uxml
│       ├── KeyframeEditor.uss
│       └── Common.uss
└── Tests/
```

## 2. アーキテクチャ

### 2.1 レイヤー構造

```
┌──────────────────────────────────────────────┐
│  View (UI Toolkit VisualElement)             │
│  - DOM操作のみ、ロジックなし                    │
│  - イベントの発行とデータバインディング            │
├──────────────────────────────────────────────┤
│  Controller (旧 Presenter)                    │
│  - Viewイベントをコマンドに変換                  │
│  - EditorStateの参照・更新                     │
│  - Modelの変更を監視してViewを更新               │
├──────────────────────────────────────────────┤
│  EditorState (UI状態)                         │
│  - SelectionState: 選択中のトラック/クリップ      │
│  - ZoomState: ズームレベル、ピクセル/秒           │
│  - ScrollState: スクロール位置                  │
│  - EditMode: ノーマル/キーフレーム編集            │
├──────────────────────────────────────────────┤
│  Model (CinematicSequencer Core)             │
│  - Sequence / Track / Clip                    │
│  - SequenceEditor (コマンド実行)               │
│  - SequencePlayer (再生)                      │
└──────────────────────────────────────────────┘
```

### 2.2 現行との比較

| 観点 | 現行 | 新設計 |
|---|---|---|
| **View → Model** | Viewが直接Presenterメソッドを呼ぶ | ViewがイベントのみをController経由 → コマンド発行 |
| **Model → View** | `UpdateTimelineUI(timeline)` で全再構築 | `IObservableModel.Changed` → 差分更新 |
| **状態管理** | 各Viewに`_selectedClip`、`_currentZoom`等が分散 | `EditorState`に集約 |
| **イベント接続** | コンストラクタで20以上のsubscribe手動管理 | Controllerのライフサイクルに紐付け |

---

## 3. 共通コンポーネント

### 3.1 TimeRulerElement

現行では`TimelinePlaybackControlView`と`KeyframeEditorView`の両方に目盛り生成とカーソル操作のコードが重複している。これを共通化。

```csharp
/// <summary>
/// タイムルーラー。目盛りの生成、ズーム対応、クリックによる時間設定を行う。
/// SequenceEditorとKeyframeEditorの両方から共有される。
/// </summary>
public sealed class TimeRulerElement : VisualElement
{
    // --- 状態 ---
    private float _totalDuration;
    private float _pixelsPerSecond;

    // --- イベント ---
    public event Action<float> TimeClicked;   // クリック/ドラッグで時間を選択

    // --- 公開メソッド ---
    public void SetDuration(float duration);
    public void SetZoom(float pixelsPerSecond);

    // --- 内部 ---
    /// <summary>
    /// ズームレベルに応じて最適な目盛り間隔を自動計算。
    /// 現行の固定値（1秒/0.25秒 or 5秒/1秒の2段階）ではなく、
    /// ズームに応じた連続的な目盛り調整を行う。
    /// </summary>
    private (float major, float minor) CalculateTickIntervals()
    {
        // 画面上で50-150pxごとにmajor tickが来るように調整
        float targetPixelSpacing = 100f;
        float rawInterval = targetPixelSpacing / _pixelsPerSecond;

        // 1, 2, 5, 10, 15, 30, 60... のステップに正規化
        float[] steps = { 0.1f, 0.25f, 0.5f, 1f, 2f, 5f, 10f, 15f, 30f, 60f };
        float major = steps[0];
        foreach (var step in steps)
        {
            if (step >= rawInterval) { major = step; break; }
        }

        float minor = major / 4f;
        return (major, minor);
    }

    private void RegenerateRuler();
}
```

### 3.2 PlayheadElement

```csharp
/// <summary>
/// 再生ヘッド（タイムカーソル）。
/// ルーラー上のヘッドと、トラック領域を貫通する縦線の2パーツ。
/// </summary>
public sealed class PlayheadElement : VisualElement
{
    private float _pixelsPerSecond;
    private VisualElement _headPart;     // 上部の三角形/円
    private VisualElement _linePart;     // 縦線

    // --- イベント ---
    public event Action<float> TimeDragged; // ドラッグで時間を変更

    // --- 公開メソッド ---
    public void SetTime(float time);
    public void SetZoom(float pixelsPerSecond);

    // --- 内部 ---
    /// <summary>
    /// ドラッグ操作。PointerCapture APIを使用して
    /// 要素外にマウスが出てもドラッグを継続。
    /// 現行の分散した複数のドラッグ実装を統一。
    /// </summary>
    private void SetupDragManipulator();
}
```

### 3.3 ScrollSyncGroup

```csharp
/// <summary>
/// 複数のScrollViewの同期を管理する。
/// 現行では各Viewで個別にセットアップしている同期ロジックを共通化。
/// </summary>
public sealed class ScrollSyncGroup
{
    public enum SyncAxis { Horizontal, Vertical, Both }

    /// <summary>
    /// 2つのScrollViewを指定軸で同期させる。
    /// </summary>
    public void Sync(ScrollView a, ScrollView b, SyncAxis axis);

    /// <summary>
    /// すべての同期接続を解除する。
    /// </summary>
    public void Dispose();
}
```

### 3.4 SelectionState

```csharp
/// <summary>
/// エディタ全体の選択状態を一元管理。
/// 現行ではTimelineEditorViewとTimelinePresenterの両方に
/// _selectedClipが存在し、状態の整合性が担保されていなかった。
/// </summary>
public sealed class SelectionState
{
    // --- 選択中のオブジェクト ---
    public IReadOnlyList<Guid> SelectedTrackIds { get; }
    public IReadOnlyList<Guid> SelectedClipIds { get; }
    public IReadOnlyList<KeyframeId> SelectedKeyframeIds { get; }

    // --- イベント ---
    public event Action SelectionChanged;

    // --- 操作 ---
    public void SelectTrack(Guid trackId, bool addToSelection = false);
    public void SelectClip(Guid clipId, bool addToSelection = false);
    public void SelectClipsInRange(Rect selectionRect); // 矩形選択
    public void SelectKeyframe(KeyframeId id, bool addToSelection = false);
    public void ClearSelection();

    // --- ヘルパー ---
    public bool IsTrackSelected(Guid trackId);
    public bool IsClipSelected(Guid clipId);
    public bool IsKeyframeSelected(KeyframeId id);
}
```

### 3.5 ZoomState

```csharp
/// <summary>
/// ズーム状態の管理。ピクセル/秒の変換と、ズーム操作のハンドリング。
/// </summary>
public sealed class ZoomState
{
    private float _pixelsPerSecond;
    private readonly float _minPixelsPerSecond;
    private readonly float _maxPixelsPerSecond;

    public float PixelsPerSecond => _pixelsPerSecond;

    public event Action<float> ZoomChanged;

    public ZoomState(float initial = 100f, float min = 20f, float max = 500f);

    /// <summary>
    /// ズーム倍率を変更。
    /// </summary>
    public void SetZoom(float pixelsPerSecond);

    /// <summary>
    /// マウスホイールによるズーム。ポインタ位置を中心にズーム。
    /// </summary>
    /// <param name="delta">ホイールデルタ（正で拡大、負で縮小）</param>
    /// <param name="pivotTimeSeconds">ズーム中心の時刻（ポインタ位置から算出）</param>
    /// <returns>スクロール位置の補正量</returns>
    public float ZoomAtPoint(float delta, float pivotTimeSeconds);

    // --- 変換ユーティリティ ---
    public float TimeToPixels(float timeSeconds) => timeSeconds * _pixelsPerSecond;
    public float PixelsToTime(float pixels) => pixels / _pixelsPerSecond;
}
```

### 3.6 SnappingService

```csharp
/// <summary>
/// スナッピング機能。クリップの移動・リサイズ時に
/// グリッドや他のクリップ端にスナップさせる。
/// </summary>
public sealed class SnappingService
{
    public enum SnapMode
    {
        None,
        Grid,           // 固定グリッド（フレーム/秒/拍）
        ClipEdges,      // 他クリップの開始/終了時刻
        Both,
    }

    public SnapMode Mode { get; set; } = SnapMode.Both;
    public float GridInterval { get; set; } = 1f;  // 秒単位
    public float SnapThresholdPixels { get; set; } = 10f; // スナップが効く範囲

    /// <summary>
    /// 時刻をスナッピング。
    /// </summary>
    /// <param name="rawTime">ドラッグ中の生の時刻</param>
    /// <param name="sequence">スナップ対象（他クリップの端）を取得するためのシーケンス</param>
    /// <param name="excludeClipId">自分自身を除外</param>
    /// <param name="pixelsPerSecond">スナップ閾値の計算用</param>
    /// <returns>スナップ後の時刻</returns>
    public float Snap(float rawTime, Sequence sequence, Guid? excludeClipId, float pixelsPerSecond);
}
```

### 3.7 KeyboardShortcutHandler

```csharp
/// <summary>
/// キーボードショートカットの処理。
/// </summary>
public sealed class KeyboardShortcutHandler
{
    public KeyboardShortcutHandler(
        SequenceEditor editor,
        SequencePlayer player,
        SelectionState selection)
    {
        // VisualElementのKeyDownEventに登録
    }

    public void RegisterTo(VisualElement root)
    {
        root.RegisterCallback<KeyDownEvent>(OnKeyDown);
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        // Ctrl+Z: Undo
        // Ctrl+Shift+Z / Ctrl+Y: Redo
        // Space: 再生/一時停止トグル
        // K: 現在時刻にキーフレーム追加
        // Delete / Backspace: 選択中のクリップ/キーフレーム削除
        // Ctrl+S: 保存
        // Ctrl+A: 全選択
        // Escape: 選択解除
        // Home: 先頭にシーク
        // End: 末尾にシーク
        // Left/Right: 1フレーム移動
    }
}
```

---

## 4. シーケンスエディタ

### 4.1 SequenceEditorController

現行の`TimelinePresenter`を整理。

```csharp
/// <summary>
/// シーケンスエディタのコントローラ。
/// Viewのイベントをコマンドに変換し、Modelの変更をViewに反映する。
/// </summary>
public sealed class SequenceEditorController : IDisposable
{
    private readonly SequenceEditor _editor;       // コアのエディタ
    private readonly SequencePlayer _player;
    private readonly IClipAssetRepository _clipAssetRepository;
    private readonly ISequenceRepository _sequenceRepository;

    private readonly SequenceEditorView _view;
    private readonly SelectionState _selection;
    private readonly ZoomState _zoom;
    private readonly SnappingService _snapping;
    private readonly KeyboardShortcutHandler _shortcuts;

    public SequenceEditorController(/* DI */)
    {
        // Viewイベントの購読
        _view.OnClipDragCompleted += HandleClipDragCompleted;
        _view.OnClipResized += HandleClipResized;
        _view.OnTrackContextMenu += HandleTrackContextMenu;
        // ... 他のイベント

        // Modelの変更監視
        _editor.SequenceChanged += HandleSequenceChanged;

        // 選択状態の監視
        _selection.SelectionChanged += HandleSelectionChanged;

        // ズーム状態の監視
        _zoom.ZoomChanged += HandleZoomChanged;
    }

    // --- Viewイベント → コマンド ---

    private void HandleClipDragCompleted(Guid clipId, Guid newTrackId, float rawNewStartTime)
    {
        var snappedTime = _snapping.Snap(rawNewStartTime, _editor.Sequence, clipId, _zoom.PixelsPerSecond);
        var newPlacement = new TimeRange(snappedTime, /* 既存duration */);
        _editor.MoveClip(clipId, newTrackId, newPlacement);
    }

    private void HandleClipResized(Guid clipId, float newDuration)
    {
        _editor.ResizeClip(clipId, newDuration);
    }

    // --- Model変更 → View更新（差分） ---

    private void HandleSequenceChanged(ModelChangeEvent change)
    {
        // 全再構築ではなく、変更種別に応じた差分更新
        switch (change.Type)
        {
            case ModelChangeEvent.ChangeType.ChildAdded:
                if (change.Source is Track track)
                    _view.AddTrackUI(track);
                else if (change.Source is Clip clip)
                    _view.AddClipUI(clip);
                break;

            case ModelChangeEvent.ChangeType.ChildRemoved:
                if (change.Source is Track removedTrack)
                    _view.RemoveTrackUI(removedTrack.Id);
                else if (change.Source is Clip removedClip)
                    _view.RemoveClipUI(removedClip.Id);
                break;

            case ModelChangeEvent.ChangeType.ChildModified:
                if (change.Source is Clip modifiedClip)
                    _view.UpdateClipUI(modifiedClip);
                break;
        }
    }
}
```

### 4.2 SequenceEditorView

現行のTimelineEditorView (680行) を責務ごとに分割。

```csharp
/// <summary>
/// シーケンスエディタのメインView。
/// UIの構築と差分更新を行う。ロジックはControllerに委譲。
/// </summary>
public sealed class SequenceEditorView : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;

    // --- 共通コンポーネント ---
    private TimeRulerElement _timeRuler;
    private PlayheadElement _playhead;
    private ScrollSyncGroup _scrollSync;

    // --- UI要素管理 ---
    /// <summary>
    /// Clip IDをキーとするUI要素のマップ。
    /// 差分更新のために、UI要素とモデルの対応関係を保持。
    /// </summary>
    private readonly Dictionary<Guid, ClipElement> _clipElements = new();
    private readonly Dictionary<Guid, TrackHeaderView> _trackHeaders = new();
    private readonly Dictionary<Guid, TrackContentView> _trackContents = new();

    // --- イベント ---
    public event Action<Guid, Guid, float> OnClipDragCompleted;  // clipId, newTrackId, newStartTime
    public event Action<Guid, float> OnClipResized;
    public event Action<Guid> OnTrackContextMenu;

    // --- 差分更新メソッド ---

    /// <summary>
    /// トラックUI要素を追加。全再構築ではない。
    /// </summary>
    public void AddTrackUI(Track track)
    {
        var header = new TrackHeaderView(track);
        var content = new TrackContentView(track);
        _trackHeaders[track.Id] = header;
        _trackContents[track.Id] = content;
        // DOMに追加
    }

    /// <summary>
    /// トラックUI要素を削除。該当トラック上のクリップUIも連動して削除。
    /// </summary>
    public void RemoveTrackUI(Guid trackId)
    {
        if (_trackHeaders.Remove(trackId, out var header))
            header.RemoveFromHierarchy();
        if (_trackContents.Remove(trackId, out var content))
            content.RemoveFromHierarchy();
        // 関連するクリップUIも削除
    }

    /// <summary>
    /// クリップUI要素を追加。
    /// </summary>
    public void AddClipUI(Clip clip)
    {
        var element = new ClipElement(clip, _zoom);
        element.AttachManipulator(new ClipManipulator(/* snapping, zoom */));
        _clipElements[clip.Id] = element;
        // 適切なTrackContentViewに追加
    }

    /// <summary>
    /// クリップの位置/サイズのみ更新。DOM再構築なし。
    /// </summary>
    public void UpdateClipUI(Clip clip)
    {
        if (_clipElements.TryGetValue(clip.Id, out var element))
        {
            element.UpdateFromModel(clip);
        }
    }

    public void RemoveClipUI(Guid clipId)
    {
        if (_clipElements.Remove(clipId, out var element))
            element.RemoveFromHierarchy();
    }
}
```

### 4.3 ClipElement

```csharp
/// <summary>
/// シーケンス上のクリップを表すUI要素。
/// </summary>
public sealed class ClipElement : VisualElement
{
    private readonly Label _label;
    private Guid _clipId;
    private Guid _trackId;

    public Guid ClipId => _clipId;
    public Guid TrackId { get => _trackId; set => _trackId = value; }

    public ClipElement(Clip clip, ZoomState zoom)
    {
        _clipId = clip.Id;
        AddToClassList("sequence-clip");
        AddToClassList(GetTypeClass(clip.ClipAsset?.Type));

        _label = new Label(clip.ClipAsset?.Name ?? "");
        _label.AddToClassList("clip-label");
        Add(_label);

        UpdateFromModel(clip);
    }

    /// <summary>
    /// モデルの変更をUIに反映。styleプロパティの直接更新のみ（DOM再構築なし）。
    /// </summary>
    public void UpdateFromModel(Clip clip)
    {
        // positionとwidthの更新のみ
        style.left = clip.Placement.Start * _zoom.PixelsPerSecond;
        style.width = clip.Placement.Duration * _zoom.PixelsPerSecond;
    }

    /// <summary>
    /// リサイズハンドルの有無で操作モードを切り替え。
    /// </summary>
    public void AttachManipulator(ClipManipulator manipulator)
    {
        this.AddManipulator(manipulator);
    }

    private string GetTypeClass(TrackType? type) => type switch
    {
        TrackType.CameraPose or TrackType.CameraProperties => "camera-clip",
        TrackType.LightPose or TrackType.LightProperties => "light-clip",
        TrackType.Effect => "effect-clip",
        TrackType.Audio => "audio-clip",
        TrackType.Motion => "motion-clip",
        TrackType.PostEffect => "posteffect-clip",
        _ => "",
    };
}
```

### 4.4 ClipManipulator（D&D + リサイズ統合）

現行の`ClipElementDragAndDropManipulator`を拡張し、リサイズ機能を統合。

```csharp
/// <summary>
/// クリップの移動とリサイズを行うManipulator。
///
/// 操作モード:
/// - クリップ中央をドラッグ → 移動
/// - クリップ右端をドラッグ → リサイズ
///
/// 現行との違い:
/// - スナッピングサポート
/// - リサイズ機能の追加
/// - PointerCapture APIの適切な使用
/// </summary>
public sealed class ClipManipulator : PointerManipulator
{
    private enum DragMode { None, Move, ResizeEnd }

    private readonly SnappingService _snapping;
    private readonly ZoomState _zoom;

    private DragMode _mode;
    private Vector2 _startPointerPosition;
    private TimeRange _startPlacement;

    // --- リサイズハンドル ---
    private const float ResizeHandleWidth = 8f; // ピクセル

    public event Action<Guid, Guid, float> MoveCompleted;    // clipId, newTrackId, newStartTime
    public event Action<Guid, float> ResizeCompleted;         // clipId, newDuration

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(OnPointerDown);
        target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        target.RegisterCallback<PointerUpEvent>(OnPointerUp);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (evt.button != 0) return;

        // マウス位置がクリップ右端の近くならリサイズモード
        var localX = target.WorldToLocal(evt.position).x;
        if (localX > target.resolvedStyle.width - ResizeHandleWidth)
        {
            _mode = DragMode.ResizeEnd;
        }
        else
        {
            _mode = DragMode.Move;
        }

        target.CapturePointer(evt.pointerId);
        _startPointerPosition = evt.position;
        // ... 初期状態を記録
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (_mode == DragMode.None) return;

        var delta = evt.position - _startPointerPosition;

        if (_mode == DragMode.Move)
        {
            // クリップの移動（現行のロジックを改善）
            // スナッピングを適用してプレビュー位置を更新
        }
        else if (_mode == DragMode.ResizeEnd)
        {
            // クリップの右端リサイズ
            var newWidth = Math.Max(10f, target.resolvedStyle.width + delta.x);
            target.style.width = newWidth;
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        target.ReleasePointer(evt.pointerId);

        if (_mode == DragMode.Move)
        {
            // 最終位置を確定してイベント発行
            MoveCompleted?.Invoke(/* ... */);
        }
        else if (_mode == DragMode.ResizeEnd)
        {
            var newDuration = _zoom.PixelsToTime(target.resolvedStyle.width);
            ResizeCompleted?.Invoke(/* ... */);
        }

        _mode = DragMode.None;
    }
}
```

---

## 5. キーフレームエディタ

### 5.0 クリップタイプによるUI分岐

シーケンスエディタ上でクリップをダブルクリックした際の動作は、クリップアセットの種類によって異なる。

| クリップアセットの種類 | ダブルクリック時の動作 |
|---|---|
| `IAnimatableClipAsset`（カメラPose、ライト、ポストエフェクト） | キーフレームエディタを開く。プロパティのキーフレーム編集が可能 |
| `IExternalClipAsset`（FBXモーション） | キーフレームエディタは開かない。プロパティパネルにクリップ情報（名前、Duration、ソースパス等）を読み取り専用で表示 |

### 5.1 KeyframeEditorController

現行の`KeyframeAnimationEditorPresenter`を改善。

```csharp
public sealed class KeyframeEditorController : IDisposable
{
    private readonly SequenceEditor _editor;
    private readonly SequencePlayer _player;
    private readonly KeyframeEditorView _view;
    private readonly SelectionState _selection;

    private Guid _currentClipAssetId;
    private IAnimatableClipAsset _currentClipAsset; // IAnimatableClipAssetのみ編集可能

    /// <summary>
    /// ClipAssetを開いてキーフレーム編集を開始。
    /// IAnimatableClipAssetでない場合は何もしない（シーケンスエディタ側で情報表示）。
    /// 現行のように「一時的なSequence」を作る必要はない。
    /// プレビュー再生はSequencePlayerの通常機能を使用。
    /// </summary>
    public void OpenClipAsset(Guid clipAssetId);

    /// <summary>
    /// 現在時刻の全プロパティにキーフレームを追加。
    /// CompositeCommandで1つのUndo単位にまとめる。
    /// </summary>
    private void HandleAddKeyframe()
    {
        var commands = _currentClipAsset.Properties
            .Select(prop => new AddKeyframeCommand(
                _currentClipAsset, prop.Name,
                new Keyframe(_player.CurrentTime, GetCurrentValue(prop.Name))))
            .ToArray();

        _editor.History.Execute(new CompositeCommand("Add Keyframes", commands));
    }
}
```

### 5.2 KeyframeEditorView

現行の928行あるViewを責務分割。

```csharp
public sealed class KeyframeEditorView : MonoBehaviour
{
    // --- 共通コンポーネント（シーケンスエディタと共有） ---
    private TimeRulerElement _timeRuler;
    private PlayheadElement _playhead;
    private ScrollSyncGroup _scrollSync;

    // --- 固有コンポーネント ---
    private PropertyEditorPanel _propertyPanel;

    // --- キーフレームマーカー管理 ---
    /// <summary>
    /// キーフレームマーカーをDictionaryで管理。
    /// 追加/削除が差分で行えるようにする。
    /// （現行と同じだが、UIの全再構築は行わない）
    /// </summary>
    private readonly Dictionary<KeyframeId, KeyframeMarkerElement> _markers = new();

    // --- イベント ---
    public event Action OnAddKeyframeRequested;
    public event Action<KeyframeId> OnKeyframeClicked;
    public event Action<KeyframeId> OnKeyframeDeleteRequested;
    public event Action<string, float> OnPropertyValueEdited;
    public event Action OnSaveRequested;
    public event Action OnCloseRequested;
}
```

### 5.3 PropertyEditorPanel

現行の`KeyframeEditorView`内に埋め込まれているプロパティ編集UIを独立コンポーネント化。

```csharp
/// <summary>
/// キーフレームのプロパティ値を編集するパネル。
/// AnimationPropertyDescriptorのメタデータを活用して
/// 適切なUIコントロール（スライダー、フィールド）を自動生成。
/// </summary>
public sealed class PropertyEditorPanel : VisualElement
{
    private readonly Dictionary<string, VisualElement> _fields = new();

    /// <summary>
    /// プロパティ記述子からUIを生成。
    /// MinValue/MaxValueがあればSlider、なければFloatFieldを使用。
    /// Groupプロパティでグルーピング。
    /// </summary>
    public void SetProperties(AnimationPropertyDescriptor[] descriptors);

    /// <summary>
    /// 値を更新（イベント発火なし）。再生中のリアルタイム表示に使用。
    /// </summary>
    public void UpdateValues(AnimationFrame frame, bool editable);

    /// <summary>
    /// 値の変更イベント。
    /// </summary>
    public event Action<string, float> ValueChanged;
}
```

---

## 6. ライブラリビュー

### 6.1 LibraryController / LibraryView

現行の`CinematicSequenceLibraryView`（637行）を分割。

```csharp
public sealed class LibraryController : IDisposable
{
    private readonly ISequenceRepository _sequenceRepo;
    private readonly IClipAssetRepository _clipAssetRepo;
    private readonly LibraryView _view;

    // --- シーケンス操作 ---
    public async UniTask RefreshAsync();
    private void HandleSequenceSelected(Guid id);
    private void HandleCreateNewSequence();

    // --- クリップアセット操作 ---
    private void HandleCreateNewClipAsset(TrackType type);
    private void HandleClipAssetDropped(Guid clipAssetId, Guid trackId, float startTime);
}

public sealed class LibraryView : VisualElement
{
    // タブ切り替え
    // アイテム一覧（ListView使用で仮想化）
    // D&Dプレビュー

    /// <summary>
    /// ListViewを使用してアイテムを表示。
    /// 現行のVisualElement手動生成ではなく、
    /// ListViewのmakeItem/bindItemパターンでUI要素を仮想化・再利用。
    /// </summary>
    private ListView _itemListView;
}
```

---

## 7. ダイアログサービス

```csharp
/// <summary>
/// ダイアログの表示を統一管理。
/// 現行ではSaveConfirmationDialogViewが各Presenterで直接参照されているが、
/// DialogServiceを介して表示することでPresenterとダイアログの結合度を下げる。
/// </summary>
public sealed class DialogService
{
    public UniTask<SaveDialogResult> ShowSaveConfirmationAsync();
    public UniTask<bool> ShowConfirmationAsync(string title, string message);
}
```

---

## 8. UIレイアウト改善

### 8.1 UXML構造

現行UXMLの問題:
- sample要素がハードコードされている（`track-row-sample`等）
- ネストが深く、構造が読みにくい

新設計:

```xml
<UXML>
  <Style src="Common.uss" />
  <Style src="SequenceEditor.uss" />

  <VisualElement name="sequence-editor-root" class="sequence-editor-root">

    <!-- 上段: ライブラリ + シーンビュー -->
    <VisualElement name="top-panel" class="split-horizontal">
      <!-- LibraryViewが動的に構築 -->
      <VisualElement name="library-container" class="panel" />
      <!-- SceneViewが動的に構築 -->
      <VisualElement name="scene-view-container" class="panel flex-grow" />
    </VisualElement>

    <!-- 中段: ツールバー -->
    <VisualElement name="toolbar" class="toolbar">
      <!-- PlaybackToolbarが動的に構築 -->
    </VisualElement>

    <!-- 下段: シーケンス -->
    <VisualElement name="sequence-panel" class="split-horizontal">
      <!-- 左: トラックヘッダー -->
      <VisualElement name="track-headers-panel" class="panel" />
      <!-- 右: タイムルーラー + トラックコンテンツ -->
      <VisualElement name="tracks-panel" class="panel flex-grow" />
    </VisualElement>

  </VisualElement>
</UXML>
```

### 8.2 USS改善

現行の問題:
- マジックナンバーが多い（`height: 30px`等のハードコード）
- CSS変数（カスタムプロパティ）の不使用

新設計:

```css
:root {
  /* カラーテーマ */
  --track-bg: #2d2d2d;
  --track-bg-alt: #333333;
  --track-header-width: 200px;
  --track-height: 36px;
  --clip-border-radius: 4px;

  /* クリップタイプカラー */
  --camera-clip-color: #4a90d9;
  --light-clip-color: #e6a817;
  --effect-clip-color: #7b68ee;
  --audio-clip-color: #50c878;
  --motion-clip-color: #e06c75;
  --posteffect-clip-color: #c678dd;

  /* シーケンス */
  --ruler-height: 28px;
  --playhead-color: #ff4444;
  --snap-guide-color: #ffff00;
}

.track-row {
  height: var(--track-height);
  background-color: var(--track-bg);
}

.track-row:nth-child(even) {
  background-color: var(--track-bg-alt);
}

.sequence-clip {
  border-radius: var(--clip-border-radius);
  position: absolute;
  /* リサイズハンドルのカーソル */
}

.sequence-clip:hover {
  /* ホバー時のハイライト */
}

.sequence-clip.selected {
  /* 選択時のボーダー */
  border-width: 2px;
  border-color: white;
}

/* リサイズハンドル（右端） */
.sequence-clip::after {
  content: "";
  position: absolute;
  right: 0;
  top: 0;
  bottom: 0;
  width: 8px;
  cursor: e-resize;
}
```

---

## 9. 操作性の改善まとめ

### 9.1 現行 → 新設計 の操作比較

| 操作 | 現行 | 新設計 |
|---|---|---|
| **クリップ追加** | ライブラリからD&D | ライブラリからD&D（改善: スナップ付き） |
| **クリップ移動** | D&D（スナップなし） | D&D（グリッド/クリップ端スナップ、ガイドライン表示） |
| **クリップ削除** | 右クリック → "Delete Clip" | 選択 + Delete キー、または右クリックメニュー |
| **クリップリサイズ** | 不可 | クリップ右端ドラッグ |
| **トラック追加** | ボタンまたは右クリック | 右クリックメニュー（改善: トラックタイプ選択UI） |
| **トラック削除** | 未実装（コメントアウト） | 右クリック → "Delete Track"（確認ダイアログ付き） |
| **ズーム** | UI未実装（コメントアウト） | Ctrl+ホイール（ポインタ中心）、+/- キー |
| **キーフレーム追加** | ボタンクリック | ボタンクリック、K キー |
| **Undo/Redo** | 未実装 | Ctrl+Z / Ctrl+Shift+Z |
| **複数選択** | 不可 | Ctrl+Click / Shift+Click / 矩形選択 |
| **再生/停止** | ボタンクリック | ボタンクリック、Space キー |
| **シーク** | ルーラークリック | ルーラークリック/ドラッグ、Left/Right キー |
| **保存** | ボタンクリック | ボタンクリック、Ctrl+S |

### 9.2 フィードバック改善

| 機能 | 説明 |
|---|---|
| **スナップガイドライン** | スナップ発動時に黄色の縦線を表示 |
| **ドラッグプレビュー** | 移動先の半透明プレビュー表示 |
| **選択ハイライト** | 選択中のクリップに白ボーダー |
| **ホバーエフェクト** | クリップにマウスオーバーで明度変更 |
| **リサイズカーソル** | クリップ右端でカーソルが`e-resize`に変化 |
| **未保存インジケータ** | タイトルに`*`を表示（例: `MySequence *`） |
| **Undo/Redo表示** | メニュー/ツールバーに「Undo: Add Track」のように操作内容を表示 |
