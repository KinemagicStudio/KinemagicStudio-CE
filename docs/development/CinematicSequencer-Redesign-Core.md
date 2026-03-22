# CinematicSequencer v2 再設計 - コアパッケージ詳細

- 作成日: 2026-03-22
- 最終更新日: 2026-03-22

関連: [概要ドキュメント](./CinematicSequencer-Redesign-Overview.md)

## 1. パッケージ構成

```
CinematicSequencer/
├── Runtime/
│   ├── Model/
│   │   ├── Sequence.cs
│   │   ├── Track.cs
│   │   ├── Clip.cs
│   │   ├── TrackType.cs
│   │   ├── TimeRange.cs
│   │   └── ChangeTracking/
│   │       ├── IObservableModel.cs
│   │       └── ModelChangeEvent.cs
│   ├── ClipAsset/
│   │   ├── IClipAsset.cs              (基底インターフェース)
│   │   ├── IAnimatableClipAsset.cs    (キーフレーム編集可能)
│   │   ├── IExternalClipAsset.cs      (外部再生ソース)
│   │   ├── AnimationClipAsset.cs      (旧 PoseAnimation/LightPropertiesAnimation を統合)
│   │   └── MotionClipAsset.cs         (FBXモーション用)
│   ├── Animation/
│   │   ├── AnimationCurve.cs          (既存ベース、改善)
│   │   ├── Keyframe.cs               (既存維持)
│   │   ├── TangentMode.cs            (既存維持)
│   │   ├── AnimationPropertyDescriptor.cs
│   │   ├── AnimationFrame.cs          (既存ベース、改善)
│   │   └── Templates/
│   │       ├── PosePropertyTemplate.cs
│   │       ├── LightPropertyTemplate.cs
│   │       └── PostEffectPropertyTemplate.cs
│   ├── Playback/
│   │   ├── SequencePlayer.cs          (既存ベース、改善)
│   │   ├── ClipPlaybackInfo.cs        (再生中クリップの情報)
│   │   └── ITimeProvider.cs
│   ├── Editing/
│   │   ├── IEditCommand.cs
│   │   ├── EditHistory.cs
│   │   ├── SequenceEditor.cs          (旧 KeyframeAnimationEditor の役割を包含)
│   │   └── Commands/
│   │       ├── AddTrackCommand.cs
│   │       ├── RemoveTrackCommand.cs
│   │       ├── AddClipCommand.cs
│   │       ├── RemoveClipCommand.cs
│   │       ├── MoveClipCommand.cs
│   │       ├── ResizeClipCommand.cs
│   │       ├── AddKeyframeCommand.cs
│   │       ├── RemoveKeyframeCommand.cs
│   │       ├── UpdateKeyframeCommand.cs
│   │       └── CompositeCommand.cs     (複数コマンドの一括実行)
│   ├── Serialization/
│   │   ├── ISequenceSerializer.cs
│   │   ├── JsonSequenceSerializer.cs
│   │   └── LegacyFormatReader.cs       (v1形式の読み込み互換)
│   └── IO/
│       ├── ISequenceRepository.cs
│       ├── IClipAssetRepository.cs
│       └── FileSystemRepository.cs     (旧2つのリポジトリを統合)
└── Tests/
    ├── Model/
    ├── Animation/
    ├── Playback/
    └── Editing/
```

## 2. データモデル

### 2.1 Sequence

```csharp
namespace CinematicSequencer
{
    /// <summary>
    /// シーケンスのルートモデル。トラックのコンテナであり、全体の再生管理の単位。
    /// </summary>
    public sealed class Sequence : IObservableModel
    {
        private readonly List<Track> _tracks = new();
        private string _name;

        public Guid Id { get; }
        public string FormatVersion => "2.0.0";

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// 全トラック中の最大EndTimeから自動計算。キャッシュして変更時のみ再計算。
        /// </summary>
        public TimeRange Duration { get; private set; }

        public IReadOnlyList<Track> Tracks => _tracks;

        // 変更通知
        public event Action<ModelChangeEvent> Changed;

        // --- Track操作 ---
        /// <summary>
        /// トラックを追加。TargetIdは同一TrackType内で自動採番される。
        /// </summary>
        public Track AddTrack(string name, TrackType type);
        /// <summary>
        /// トラックを追加。TargetIdを明示的に指定する。
        /// </summary>
        public Track AddTrack(string name, TrackType type, int targetId);
        public bool RemoveTrack(Guid trackId);
        public Track GetTrack(Guid trackId);
        public void ReorderTrack(Guid trackId, int newIndex);

        // --- 内部 ---
        private void RecalculateDuration();
    }
}
```

**現行からの改善点:**
- `Id`: `Guid.Parse(string)` → コンストラクタで直接`Guid`生成
- `Duration`: 手動呼び出しの`UpdateDuration()` → プロパティアクセスでキャッシュ値返却、トラック/クリップ変更時に自動再計算
- `_tracks.Find(t => t.Id == trackId)` → `Dictionary<Guid, Track>`による O(1) ルックアップ（内部実装）
- `TargetId`の改善: 現行のint自動採番を維持しつつ、外部からの明示的なバインディング指定にも対応。シーケンスデータをシーンオブジェクトから独立させる設計意図はそのまま継承
- `GetActiveClipsAtTime`の`_activeClips`辞書をインスタンスフィールドで持ち回す問題 → スレッドセーフな設計に

### 2.2 Track

```csharp
public sealed class Track : IObservableModel
{
    private readonly List<Clip> _clips = new();
    private string _name;

    public Guid Id { get; }
    public TrackType Type { get; }

    /// <summary>
    /// シーンオブジェクト（カメラ、ライト等）とのバインディング用ID。
    /// シーケンスデータをシーンから独立させ、再利用可能にするための設計。
    /// 同一TrackType内で一意。アプリ側のアダプターがこのIDを使って
    /// 実際のオブジェクト（CameraActor等）にマッピングする。
    /// </summary>
    public int TargetId { get; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    /// <summary>
    /// UI上の表示順序。ドラッグによるトラック並び替えに使用。
    /// </summary>
    public int SortOrder { get; set; }

    public IReadOnlyList<Clip> Clips => _clips;
    public TimeRange TimeRange { get; private set; } // 全クリップを包含する時間範囲

    public event Action<ModelChangeEvent> Changed;

    // --- Clip操作 ---
    public Clip AddClip(Guid clipAssetId, TimeRange placement);
    public bool RemoveClip(Guid clipId);
    public Clip GetClip(Guid clipId);

    /// <summary>
    /// 指定時刻にアクティブなクリップを返す。
    /// 同一トラック上でクリップが重なっている場合、後ろのクリップが優先。
    /// </summary>
    public Clip GetActiveClipAt(int timeMs);
}
```

**現行からの改善点:**
- `Id`: `static int _nextId++` → `Guid`（シリアライズ可能、衝突なし）
- `TargetId`の維持: シーケンスデータをシーンオブジェクトから独立させる現行の設計意図を継承。現行のint自動採番に加え、外部から明示的にTargetIdを指定するオーバーロードも追加
- `TryAddClip`の`Type`チェック: Track.Typeに合わないClipAssetの追加をコンパイル時に防ぐことは難しいが、ランタイムバリデーションを明確化

### 2.3 Clip

```csharp
public sealed class Clip : IObservableModel
{
    public Guid Id { get; }

    /// <summary>
    /// このClipが参照するClipAssetのID。
    /// ClipAssetは遅延ロードされ、再生時またはエディタで開いた時に読み込まれる。
    /// </summary>
    public Guid ClipAssetId { get; }

    /// <summary>
    /// シーケンス上の配置（開始時刻と表示Duration）。
    /// </summary>
    public TimeRange Placement { get; set; }

    /// <summary>
    /// 再生速度。1.0が等速、2.0が2倍速。
    /// EndTime = Placement.Start + Placement.Duration となる（PlaybackRateはClipAsset内の再生速度に影響）。
    /// </summary>
    public float PlaybackRate { get; set; } = 1.0f;

    /// <summary>
    /// ClipAsset内のどの範囲を使用するか（トリミング）。
    /// </summary>
    public TimeRange SourceRange { get; set; }

    /// <summary>
    /// 遅延ロード可能なClipAsset参照。
    /// </summary>
    [JsonIgnore]
    public IClipAsset ClipAsset { get; set; }

    public event Action<ModelChangeEvent> Changed;

    // --- 時間計算 ---
    public bool ContainsTime(int timeMs);
    public float GetLocalTime(float globalTime);
}
```

**現行からの改善点:**
- `Id`: `static int _nextId++` → `Guid`
- `StartTime` / `Duration` / `TimeScale` → `Placement` (`TimeRange`) + `PlaybackRate` + `SourceRange` で意図が明確に
- `SourceRange`の追加: ClipAssetの一部だけを使うトリミング機能
- `_clipData`と`_clipDataId`の二重管理 → `ClipAssetId`（永続参照）と`ClipAsset`（遅延ロード済みオブジェクト）の明確な分離

### 2.4 TimeRange

```csharp
/// <summary>
/// 不変の時間範囲。開始時刻とDurationのペア。
/// 内部はミリ秒int精度、APIはfloat秒で公開。
/// </summary>
public readonly struct TimeRange : IEquatable<TimeRange>
{
    public int StartMs { get; }
    public int DurationMs { get; }
    public int EndMs => StartMs + DurationMs;

    public float Start => StartMs * 0.001f;
    public float Duration => DurationMs * 0.001f;
    public float End => EndMs * 0.001f;

    public TimeRange(float startSeconds, float durationSeconds);
    public TimeRange(int startMs, int durationMs);

    public bool Contains(int timeMs) => timeMs >= StartMs && timeMs <= EndMs;
    public bool Overlaps(TimeRange other);

    public TimeRange WithStart(float newStart);
    public TimeRange WithDuration(float newDuration);
    public TimeRange Offset(float deltaSeconds);

    // IEquatable
    public bool Equals(TimeRange other);
    public override int GetHashCode();
}
```

**設計意図:**
- 現行の`Keyframe.TimeMs`で採用していたミリ秒int精度をモデル全体に統一
- `readonly struct`で不変性を保証しつつ、`With*`メソッドで変更コピーを生成
- `float`の比較問題（`Mathf.Approximately`の必要性）をint比較で解消

### 2.5 TrackType

```csharp
/// <summary>
/// トラックの種類。現行のDataTypeを整理・拡張。
/// </summary>
public enum TrackType
{
    CameraPose,
    CameraProperties,
    LightPose,
    LightProperties,
    Effect,
    Audio,
    Motion,         // FBXモーション等の外部アニメーションソース
    PostEffect,     // ポストプロセスエフェクトのパラメータ制御
    // 将来の拡張用
    // Custom = 100,
}
```

### 2.6 変更通知

```csharp
/// <summary>
/// モデルの変更通知を発行するインターフェース。
/// </summary>
public interface IObservableModel
{
    event Action<ModelChangeEvent> Changed;
}

public readonly struct ModelChangeEvent
{
    public enum ChangeType
    {
        PropertyChanged,    // 名前等のプロパティ変更
        ChildAdded,         // 子要素の追加
        ChildRemoved,       // 子要素の削除
        ChildModified,      // 子要素の変更
        Reordered,          // 並び順の変更
    }

    public ChangeType Type { get; }
    public string PropertyName { get; }
    public object Source { get; }
}
```

**設計意図:**
- 現行の`UpdateTimelineUI(_timeline)`による全再構築を、差分更新に置き換えるための基盤
- UIは`Changed`イベントを監視し、影響のある要素のみ更新

---

## 3. クリップアセットシステム

### 3.1 IClipAsset インターフェース階層

クリップアセットは再生方式により2つのインターフェースに分類される。

```csharp
/// <summary>
/// 全クリップアセットの基底インターフェース。
/// シーケンス上での配置管理に必要な最小限の契約。
/// </summary>
public interface IClipAsset
{
    Guid Id { get; }
    string Name { get; set; }
    TrackType Type { get; }
    float GetDuration();
}

/// <summary>
/// キーフレーム編集可能なクリップアセット。
/// シーケンサー内蔵のAnimationCurveでプロパティ値を補間する。
/// カメラPose、ライトProperties、ポストエフェクト等が該当。
/// </summary>
public interface IAnimatableClipAsset : IClipAsset
{
    IReadOnlyList<AnimationPropertyDescriptor> Properties { get; }
    AnimationFrame Evaluate(float time);
    AnimationCurve GetCurve(string propertyName);
    IReadOnlyList<Keyframe> GetKeyframes(string propertyName);
    int AddKeyframe(string propertyName, Keyframe keyframe);
    bool RemoveKeyframe(string propertyName, float time);
    bool UpdateKeyframeValue(string propertyName, float time, float value);
}

/// <summary>
/// 外部再生ソースを参照するクリップアセット。
/// シーケンサーは再生タイミングのみを制御し、実際の評価は
/// アプリ側アダプターが外部プレイヤーに委譲する。
/// FBXモーション、Audio等が該当。
/// </summary>
public interface IExternalClipAsset : IClipAsset
{
    /// <summary>
    /// 外部データソースの識別子（ファイルパス、アセットID等）。
    /// アプリ側アダプターがこの値を使って外部プレイヤーを特定・生成する。
    /// </summary>
    string ExternalSourceId { get; }
}
```

**設計意図:**
- `IClipAsset`（基底）: シーケンス上での配置・移動・リサイズに必要な情報のみ。全クリップ共通
- `IAnimatableClipAsset`: キーフレーム編集UIの対象。`AnimationClipAsset`が実装する
- `IExternalClipAsset`: 外部プレイヤーへの委譲。シーケンサーは「いつ再生するか」のみを管理し、「何をどう再生するか」はアプリ側の責務

### 3.2 MotionClipAsset（FBXモーション用）

```csharp
/// <summary>
/// 外部モーションデータを参照するクリップアセット。
/// FBXファイル等の外部ソースからモーションを再生する。
/// キーフレーム編集は不可。シーケンス上での配置・リサイズのみ。
/// </summary>
public sealed class MotionClipAsset : IExternalClipAsset
{
    public Guid Id { get; }
    public string Name { get; set; }
    public TrackType Type => TrackType.Motion;

    /// <summary>
    /// モーションデータのソースパス（FBXファイルパス等）。
    /// アプリ側がこのパスを使ってFbxAnimationControllerを生成する。
    /// </summary>
    public string ExternalSourceId { get; set; }

    /// <summary>
    /// ソース内のクリップインデックス（FBXに複数アニメーションが含まれる場合）。
    /// </summary>
    public int ClipIndex { get; set; }

    /// <summary>
    /// キャッシュされたDuration。ソースファイルのロード時にアプリ側が設定する。
    /// </summary>
    public float CachedDuration { get; set; }

    public float GetDuration() => CachedDuration;
}
```

**FBXAnimationPlayerとの統合フロー:**
1. ライブラリからFBXファイルを選択 → `MotionClipAsset`を作成、`ExternalSourceId`にパスを設定
2. シーケンス上にクリップを配置（Placement: 開始時刻 + Duration）
3. 再生時: `SequencePlayer`が`ClipPlaybackInfo`を発行 → アプリ側アダプターが受信
4. アダプター: `ExternalSourceId`から`FbxAnimationController`を特定、`Seek(localTime)`で再生位置を制御
5. `FbxAnimationController`は`UseManualUpdate = true`モードで動作し、シーケンサーが時間を完全制御

---

## 4. アニメーションシステム

### 4.1 AnimationCurve（改善）

現行の`AnimationCurve`は設計が良好。以下を改善:

```csharp
public sealed class AnimationCurve
{
    // 既存機能はそのまま維持
    // - BinarySearch、Hermite補間、SmoothTangents

    // 追加: WrapMode拡張
    public enum WrapMode
    {
        ClampForever,   // 既存
        Loop,           // ループ再生
        PingPong,       // 往復再生
    }

    // 追加: 一括操作（Undo/Redoのスナップショット用）
    public AnimationCurveSnapshot CreateSnapshot();
    public void RestoreSnapshot(AnimationCurveSnapshot snapshot);
}
```

### 4.2 AnimationClipAsset（switch文の解消）

現行の`PoseAnimation`（338行）と`LightPropertiesAnimation`（290行）は、6-7個のswitch文を持つ巨大クラス。新設計では汎用化する。

カメラPose、ライトProperties、**ポストエフェクト**のいずれも`AnimationClipAsset`の1クラスで対応可能。

```csharp
/// <summary>
/// キーフレーム編集可能なアニメーションクリップのアセット。
/// 複数のAnimationCurveを名前付きプロパティとして保持する。
/// カメラPose、ライトProperties、ポストエフェクト等に汎用的に使用。
/// </summary>
public sealed class AnimationClipAsset : IAnimatableClipAsset
{
    private readonly Dictionary<string, AnimationCurve> _curves;
    private readonly AnimationPropertyDescriptor[] _propertyDescriptors;
    private AnimationFrame _cachedFrame; // 再利用でアロケーション回避

    public Guid Id { get; }
    public string Name { get; set; }
    public string FormatVersion => "2.0.0";
    public TrackType Type { get; }
    public IReadOnlyList<AnimationPropertyDescriptor> Properties => _propertyDescriptors;

    /// <summary>
    /// プロパティ記述子の配列からClipAssetを構築。
    /// 各プロパティに対して、開始/終了キーフレーム付きのAnimationCurveが自動生成される。
    /// </summary>
    public AnimationClipAsset(TrackType type, AnimationPropertyDescriptor[] descriptors, float defaultDuration = 60f)
    {
        Id = GuidExtensions.CreateVersion7();
        Type = type;
        _propertyDescriptors = descriptors;
        _curves = new Dictionary<string, AnimationCurve>(descriptors.Length);
        _cachedFrame = new AnimationFrame(type, descriptors.Length);

        foreach (var desc in descriptors)
        {
            _curves[desc.Name] = new AnimationCurve(new[]
            {
                new Keyframe(0f, desc.DefaultValue),
                new Keyframe(defaultDuration, desc.DefaultValue)
            });
        }
    }

    public float GetDuration()
    {
        float max = 0f;
        foreach (var curve in _curves.Values)
        {
            if (curve.Length > 0)
                max = Math.Max(max, curve[^1].Time);
        }
        return max;
    }

    /// <summary>
    /// 指定時刻のアニメーション値を評価。
    /// switch文なしで全プロパティをループ処理。
    /// </summary>
    public AnimationFrame Evaluate(float time)
    {
        _cachedFrame.SetTime(time);
        for (int i = 0; i < _propertyDescriptors.Length; i++)
        {
            var name = _propertyDescriptors[i].Name;
            var value = _curves[name].Evaluate(time);
            _cachedFrame.SetProperty(i, name, value);
        }
        return _cachedFrame;
    }

    // --- キーフレーム操作（すべてDictionaryルックアップ、switch文なし） ---

    public AnimationCurve GetCurve(string propertyName)
    {
        return _curves.TryGetValue(propertyName, out var curve) ? curve : null;
    }

    public IReadOnlyList<Keyframe> GetKeyframes(string propertyName)
    {
        return GetCurve(propertyName)?.Keys ?? Array.Empty<Keyframe>();
    }

    public int AddKeyframe(string propertyName, Keyframe keyframe)
    {
        var curve = GetCurve(propertyName);
        return curve?.AddKey(keyframe) ?? -1;
    }

    public bool RemoveKeyframe(string propertyName, float time)
    {
        var curve = GetCurve(propertyName);
        return curve?.RemoveKeyAtTime(time) ?? false;
    }

    public bool UpdateKeyframeValue(string propertyName, float time, float value)
    {
        var curve = GetCurve(propertyName);
        return curve?.UpdateKeyValue(time, value) ?? false;
    }
}
```

**比較:**
| 観点 | 現行 (PoseAnimation) | 新設計 (AnimationClipAsset) |
|---|---|---|
| プロパティ追加 | 6箇所のswitch文に全てケース追加 | `AnimationPropertyDescriptor`を配列に追加するだけ |
| コード量 | ~338行 (PoseAnimation) + ~290行 (LightProperties) | ~100行 (汎用1クラス) |
| 新タイプ追加 | 新クラス作成 + switch文コピー | `PropertyTemplate`定義のみ |

### 4.3 AnimationPropertyDescriptor

```csharp
/// <summary>
/// アニメーションプロパティのメタデータ。
/// </summary>
public sealed class AnimationPropertyDescriptor
{
    public string Name { get; }
    public float DefaultValue { get; }
    public float? MinValue { get; }   // UIでのバリデーション・スライダー範囲
    public float? MaxValue { get; }
    public string DisplayName { get; } // UI表示用（日本語対応も可）
    public string Group { get; }       // UIでのグルーピング（"Position", "Rotation"等）

    public AnimationPropertyDescriptor(
        string name, float defaultValue,
        string displayName = null, string group = null,
        float? minValue = null, float? maxValue = null);
}
```

### 4.4 プロパティテンプレート

```csharp
/// <summary>
/// よく使うプロパティセットのテンプレート。
/// </summary>
public static class PropertyTemplates
{
    public static AnimationPropertyDescriptor[] CreatePoseProperties() => new[]
    {
        new AnimationPropertyDescriptor("PositionX", 0f, "X", "Position"),
        new AnimationPropertyDescriptor("PositionY", 0f, "Y", "Position"),
        new AnimationPropertyDescriptor("PositionZ", 0f, "Z", "Position"),
        new AnimationPropertyDescriptor("EulerAngleX", 0f, "X", "Rotation"),
        new AnimationPropertyDescriptor("EulerAngleY", 0f, "Y", "Rotation"),
        new AnimationPropertyDescriptor("EulerAngleZ", 0f, "Z", "Rotation"),
    };

    public static AnimationPropertyDescriptor[] CreateLightProperties() => new[]
    {
        new AnimationPropertyDescriptor("ColorR", 1f, "R", "Color", 0f, 1f),
        new AnimationPropertyDescriptor("ColorG", 1f, "G", "Color", 0f, 1f),
        new AnimationPropertyDescriptor("ColorB", 1f, "B", "Color", 0f, 1f),
        new AnimationPropertyDescriptor("Intensity", 1f, "Intensity", null, 0f, null),
        new AnimationPropertyDescriptor("Range", 10f, "Range", null, 0f, null),
    };

    /// <summary>
    /// ScreenEdgeColorエフェクトのプロパティ。
    /// KinemagicRenderPipelineのScreenEdgeColor VolumeComponentに対応。
    /// </summary>
    public static AnimationPropertyDescriptor[] CreateScreenEdgeColorProperties() => new[]
    {
        new AnimationPropertyDescriptor("Intensity", 0f, "Intensity", null, 0f, 1f),
        new AnimationPropertyDescriptor("TopLeftColorR", 0f, "R", "TopLeftColor", 0f, 1f),
        new AnimationPropertyDescriptor("TopLeftColorG", 1f, "G", "TopLeftColor", 0f, 1f),
        new AnimationPropertyDescriptor("TopLeftColorB", 1f, "B", "TopLeftColor", 0f, 1f),
        new AnimationPropertyDescriptor("TopRightColorR", 1f, "R", "TopRightColor", 0f, 1f),
        new AnimationPropertyDescriptor("TopRightColorG", 0f, "G", "TopRightColor", 0f, 1f),
        new AnimationPropertyDescriptor("TopRightColorB", 1f, "B", "TopRightColor", 0f, 1f),
        new AnimationPropertyDescriptor("BottomLeftColorR", 1f, "R", "BottomLeftColor", 0f, 1f),
        new AnimationPropertyDescriptor("BottomLeftColorG", 1f, "G", "BottomLeftColor", 0f, 1f),
        new AnimationPropertyDescriptor("BottomLeftColorB", 0f, "B", "BottomLeftColor", 0f, 1f),
        new AnimationPropertyDescriptor("BottomRightColorR", 1f, "R", "BottomRightColor", 0f, 1f),
        new AnimationPropertyDescriptor("BottomRightColorG", 0f, "G", "BottomRightColor", 0f, 1f),
        new AnimationPropertyDescriptor("BottomRightColorB", 0f, "B", "BottomRightColor", 0f, 1f),
    };

    /// <summary>
    /// テンプレートからClipAssetを生成するファクトリ。
    /// ポストエフェクトは複数種類があるため、エフェクト名を指定するオーバーロードを提供。
    /// </summary>
    public static AnimationClipAsset CreateClipAsset(TrackType type)
    {
        var descriptors = type switch
        {
            TrackType.CameraPose => CreatePoseProperties(),
            TrackType.LightPose => CreatePoseProperties(),
            TrackType.LightProperties => CreateLightProperties(),
            _ => throw new NotSupportedException($"No template for {type}")
        };
        return new AnimationClipAsset(type, descriptors);
    }

    /// <summary>
    /// ポストエフェクト用: エフェクト名を指定してClipAssetを生成。
    /// 新しいポストエフェクトはここにテンプレートを追加するだけで対応可能。
    /// </summary>
    public static AnimationClipAsset CreatePostEffectClipAsset(string effectName)
    {
        var descriptors = effectName switch
        {
            "ScreenEdgeColor" => CreateScreenEdgeColorProperties(),
            // 新エフェクト追加時: ここにケースを追加
            _ => throw new NotSupportedException($"No template for post-effect: {effectName}")
        };
        return new AnimationClipAsset(TrackType.PostEffect, descriptors);
    }
}
```

---

## 5. コマンドシステム（Undo/Redo）

### 5.1 基本インターフェース

```csharp
public interface IEditCommand
{
    /// <summary>コマンドの説明（Undo/Redoメニュー表示用）</summary>
    string Description { get; }

    void Execute();
    void Undo();
}
```

### 5.2 EditHistory

```csharp
public sealed class EditHistory
{
    private readonly List<IEditCommand> _undoStack = new();
    private readonly List<IEditCommand> _redoStack = new();
    private readonly int _maxHistorySize;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public string UndoDescription => CanUndo ? _undoStack[^1].Description : null;
    public string RedoDescription => CanRedo ? _redoStack[^1].Description : null;

    public event Action HistoryChanged;

    public EditHistory(int maxHistorySize = 100);

    /// <summary>
    /// コマンドを実行し、Undoスタックに積む。Redoスタックはクリアされる。
    /// </summary>
    public void Execute(IEditCommand command)
    {
        command.Execute();
        _undoStack.Add(command);
        _redoStack.Clear();

        if (_undoStack.Count > _maxHistorySize)
            _undoStack.RemoveAt(0);

        HistoryChanged?.Invoke();
    }

    public void Undo()
    {
        if (!CanUndo) return;
        var command = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        command.Undo();
        _redoStack.Add(command);
        HistoryChanged?.Invoke();
    }

    public void Redo()
    {
        if (!CanRedo) return;
        var command = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        command.Execute();
        _undoStack.Add(command);
        HistoryChanged?.Invoke();
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        HistoryChanged?.Invoke();
    }
}
```

### 5.3 コマンド実装例

```csharp
public sealed class AddTrackCommand : IEditCommand
{
    private readonly Sequence _sequence;
    private readonly string _name;
    private readonly TrackType _type;
    private Track _addedTrack;

    public string Description => $"Add {_type} Track";

    public AddTrackCommand(Sequence sequence, string name, TrackType type)
    {
        _sequence = sequence;
        _name = name;
        _type = type;
    }

    public void Execute()
    {
        _addedTrack = _sequence.AddTrack(_name, _type);
    }

    public void Undo()
    {
        _sequence.RemoveTrack(_addedTrack.Id);
    }
}

public sealed class MoveClipCommand : IEditCommand
{
    private readonly Sequence _sequence;
    private readonly Guid _clipId;
    private readonly Guid _oldTrackId;
    private readonly Guid _newTrackId;
    private readonly TimeRange _oldPlacement;
    private readonly TimeRange _newPlacement;

    public string Description => "Move Clip";

    public void Execute()
    {
        // 移動実行
    }

    public void Undo()
    {
        // 元の位置に戻す
    }
}

/// <summary>
/// 複数のコマンドを1つのUndo単位として扱う。
/// 例: 「全プロパティにキーフレーム追加」を1回のUndoで取り消す。
/// </summary>
public sealed class CompositeCommand : IEditCommand
{
    private readonly IEditCommand[] _commands;

    public string Description { get; }

    public CompositeCommand(string description, params IEditCommand[] commands)
    {
        Description = description;
        _commands = commands;
    }

    public void Execute()
    {
        foreach (var cmd in _commands)
            cmd.Execute();
    }

    public void Undo()
    {
        // 逆順でUndo
        for (int i = _commands.Length - 1; i >= 0; i--)
            _commands[i].Undo();
    }
}
```

---

## 6. SequenceEditor（統合エディタ）

現行では`KeyframeAnimationEditor`がシーケンスエディタとは独立して「一時的なSequence」を作る不自然な設計になっている。新設計では`SequenceEditor`がシーケンスとキーフレーム両方の編集を統合管理する。

```csharp
/// <summary>
/// シーケンスの編集セッションを管理する。
/// UIからのコマンド発行窓口であり、Undo/Redo、変更追跡、保存状態を統括する。
/// </summary>
public sealed class SequenceEditor : IDisposable
{
    private readonly EditHistory _editHistory;
    private readonly IClipAssetRepository _clipAssetRepository;

    private Sequence _sequence;
    private bool _hasUnsavedChanges;

    public Sequence Sequence => _sequence;
    public EditHistory History => _editHistory;
    public bool HasUnsavedChanges => _hasUnsavedChanges;

    public event Action<ModelChangeEvent> SequenceChanged;
    public event Action UnsavedChangesStateChanged;

    // --- シーケンス操作（全てEditHistory経由） ---

    public void AddTrack(string name, TrackType type)
    {
        _editHistory.Execute(new AddTrackCommand(_sequence, name, type));
        _hasUnsavedChanges = true;
    }

    public void RemoveTrack(Guid trackId)
    {
        _editHistory.Execute(new RemoveTrackCommand(_sequence, trackId));
        _hasUnsavedChanges = true;
    }

    public void AddClip(Guid trackId, Guid clipAssetId, TimeRange placement)
    {
        _editHistory.Execute(new AddClipCommand(_sequence, trackId, clipAssetId, placement));
        _hasUnsavedChanges = true;
    }

    public void MoveClip(Guid clipId, Guid newTrackId, TimeRange newPlacement)
    {
        _editHistory.Execute(new MoveClipCommand(...));
        _hasUnsavedChanges = true;
    }

    // --- キーフレーム操作 ---

    /// <summary>
    /// ClipAssetのキーフレームを編集する。
    /// 現行のKeyframeAnimationEditorが持っていた「一時的なSequence作成」は不要。
    /// プレビュー再生はSequencePlayerの通常機能を使用。
    /// </summary>
    public void AddKeyframe(Guid clipAssetId, string propertyName, Keyframe keyframe)
    {
        _editHistory.Execute(new AddKeyframeCommand(...));
        _hasUnsavedChanges = true;
    }

    // --- Undo/Redo ---

    public void Undo() => _editHistory.Undo();
    public void Redo() => _editHistory.Redo();
}
```

---

## 7. 再生エンジン

### 7.1 SequencePlayer（改善）

```csharp
public sealed class SequencePlayer
{
    private Sequence _sequence;
    private float _currentTime;
    private float _playbackSpeed = 1f;

    public bool IsPlaying { get; private set; }
    public bool IsLooping { get; set; }
    public float CurrentTime => _currentTime;

    public float PlaybackSpeed
    {
        get => _playbackSpeed;
        set => _playbackSpeed = Math.Max(0.01f, value);
    }

    public Sequence Sequence
    {
        get => _sequence;
        set { _sequence = value; _currentTime = 0f; IsPlaying = false; }
    }

    // --- 再生状態イベント ---
    public event Action OnPlay;
    public event Action OnPause;
    public event Action OnStop;
    public event Action OnComplete;
    public event Action<float> OnTimeChanged;

    /// <summary>
    /// アクティブなクリップの再生情報を通知する。
    /// Update毎にアクティブな全クリップに対して発行される。
    /// アプリ側アダプターがこのイベントを受信し、クリップの種類に応じた
    /// 評価・適用を行う（AnimationFrame評価、FBXプレイヤー制御等）。
    /// </summary>
    public event Action<ClipPlaybackInfo> OnClipPlayback;

    public void Play();
    public void Pause();
    public void Stop();
    public void Seek(float time);

    public void Update(float deltaTime)
    {
        if (!IsPlaying) return;

        _currentTime += deltaTime * _playbackSpeed;
        OnTimeChanged?.Invoke(_currentTime);

        var currentTimeMs = (int)(_currentTime * 1000);
        foreach (var track in _sequence.Tracks)
        {
            var clip = track.GetActiveClipAt(currentTimeMs);
            if (clip?.ClipAsset == null) continue;

            var localTime = clip.GetLocalTime(_currentTime);
            OnClipPlayback?.Invoke(new ClipPlaybackInfo(
                track.Id, track.TargetId, track.Type,
                clip.Id, clip.ClipAsset, localTime));
        }
    }
}
```

### 7.2 ClipPlaybackInfo

```csharp
/// <summary>
/// 再生中のクリップの情報。SequencePlayerからアプリ側アダプターに通知される。
/// クリップの種類（IAnimatableClipAsset / IExternalClipAsset）に応じて
/// アダプター側で適切な処理を行う。
/// </summary>
public readonly struct ClipPlaybackInfo
{
    public Guid TrackId { get; }
    public int TargetId { get; }        // シーンオブジェクトとのバインディング用
    public TrackType Type { get; }
    public Guid ClipId { get; }
    public IClipAsset ClipAsset { get; }
    public float LocalTime { get; }     // クリップ内のローカル時刻
}
```

### 7.3 アプリ側アダプターでの処理例

```csharp
// アプリ側（CinematicSequenceSystemAdapter）
private void OnClipPlayback(ClipPlaybackInfo info)
{
    switch (info.ClipAsset)
    {
        // キーフレーム編集可能なクリップ → 評価してシーンに適用
        case IAnimatableClipAsset animatable:
        {
            var frame = animatable.Evaluate(info.LocalTime);
            switch (info.Type)
            {
                case TrackType.CameraPose:
                    ApplyCameraPose(info.TargetId, frame);
                    break;
                case TrackType.LightProperties:
                    ApplyLightProperties(info.TargetId, frame);
                    break;
                case TrackType.PostEffect:
                    ApplyPostEffect(info.TargetId, frame);
                    break;
            }
            break;
        }

        // 外部再生ソース → 外部プレイヤーに時刻を委譲
        case IExternalClipAsset external:
        {
            switch (info.Type)
            {
                case TrackType.Motion:
                    // FbxAnimationControllerにSeek
                    var controller = GetFbxController(external.ExternalSourceId);
                    controller?.Seek(info.LocalTime);
                    break;
            }
            break;
        }
    }
}

/// <summary>
/// ポストエフェクトのAnimationFrame値をVolumeComponentに適用する。
/// </summary>
private void ApplyPostEffect(int targetId, AnimationFrame frame)
{
    var screenEdgeColor = VolumeManager.instance.stack.GetComponent<ScreenEdgeColor>();
    if (screenEdgeColor == null) return;

    foreach (var (name, value) in frame.Properties)
    {
        // AnimationPropertyDescriptorの名前とVolumeComponentのパラメータを対応づける
        switch (name)
        {
            case "Intensity": screenEdgeColor.IntensityParam.value = value; break;
            case "TopLeftColorR": /* ... */ break;
            // ...
        }
    }
}
```

### 7.4 PlayerLoop統合（アプリ側）

```csharp
// アプリ側（CinematicSequencer.Unity パッケージ or アプリコード）
public static class SequencePlayerLoopIntegration
{
    public static void Register(SequencePlayer player)
    {
        // Unity PlayerLoopへの挿入（現行CinematicSequenceSystemの責務を移管）
    }
}
```

---

## 8. シリアライゼーション

### 8.1 新フォーマット

```json
{
  "FormatVersion": "2.0.0",
  "Id": "01987896-17fe-7543-8651-a2fad414e3ed",
  "Name": "MySequence",
  "Tracks": [
    {
      "Id": "...",
      "Name": "Camera_1",
      "Type": "CameraPose",
      "SortOrder": 0,
      "Clips": [
        {
          "Id": "...",
          "ClipAssetId": "019878ad-dc9d-7e3c-b753-650a67ca4d68",
          "Placement": { "StartMs": 0, "DurationMs": 60000 },
          "PlaybackRate": 1.0,
          "SourceRange": { "StartMs": 0, "DurationMs": 60000 }
        }
      ]
    }
  ]
}
```

### 8.2 互換性

```csharp
public sealed class LegacyFormatReader
{
    /// <summary>
    /// v1形式のJSONを読み込み、v2のSequenceに変換する。
    /// </summary>
    public Sequence ReadV1Sequence(byte[] data);
    public AnimationClipAsset ReadV1ClipData(byte[] data);
}
```

### 8.3 リポジトリの統合

現行の`FileSystemClipDataRepository`と`FileSystemTimelineRepository`は構造がほぼ同一。統合する。

```csharp
public sealed class FileSystemRepository : ISequenceRepository, IClipAssetRepository
{
    private readonly string _basePath;
    private readonly ISequenceSerializer _serializer;

    public string SequenceDirectory => Path.Combine(_basePath, "Sequences");
    public string ClipAssetDirectory => Path.Combine(_basePath, "ClipAssets");

    // ISequenceRepository
    public async UniTask<Sequence> LoadSequenceAsync(Guid id, CancellationToken ct);
    public async UniTask SaveSequenceAsync(Sequence sequence, CancellationToken ct);
    public async UniTask<List<SequenceInfo>> GetSequenceListAsync(CancellationToken ct);

    // IClipAssetRepository
    public async UniTask<IClipAsset> LoadClipAssetAsync(Guid id, CancellationToken ct);
    public async UniTask SaveClipAssetAsync(IClipAsset asset, CancellationToken ct);
    public async UniTask<List<ClipAssetInfo>> GetClipAssetListAsync(CancellationToken ct);
}
```

---

## 9. テスト戦略

コアパッケージはUnity非依存のPure C#なので、標準的なユニットテストフレームワークでテスト可能。

### テストカバレッジ対象

| レイヤー | テスト内容 |
|---|---|
| **Model** | Sequence/Track/Clip のCRUD操作、TimeRange計算、変更通知 |
| **Animation** | AnimationCurveの補間精度、キーフレーム追加/削除、SmoothTangent |
| **Editing** | 各コマンドのExecute/Undo、EditHistoryのスタック管理、CompositeCommand |
| **Serialization** | 新フォーマットの往復変換、v1フォーマットの読み込み互換 |
| **Playback** | 再生・一時停止・シーク、ループ、速度変更、EvaluationResult |
