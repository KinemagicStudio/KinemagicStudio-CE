# CinematicSequencer v2 再設計 - 概要

- 作成日: 2026-03-22
- 最終更新日: 2026-03-22

## 1. 現行実装の分析

### 1.1 全体構成

現行は2つのUnityパッケージで構成されている。

| パッケージ | 役割 | ファイル数 |
|---|---|---|
| `CinematicSequencer` | コアロジック（シーケンス・アニメーション・永続化） | 26 .cs |
| `CinematicSequencer.UI` | UI Toolkitベースのエディタ画面 | 15 .cs |

### 1.2 設計方針

CinematicSequencerは現状では特定アプリのリポジトリ内に含まれているが、**汎用的なパッケージとして独立利用可能にする**ことを方針としている。そのため、アプリ固有のオブジェクト（カメラ、ライト等）への直接依存を避け、TargetIdやインターフェース抽象化によりアプリ側と疎結合な設計としている。パッケージ単体で見ると冗長や分かりづらく見える部分は、この方針に起因するものがある。

### 1.3 現行アーキテクチャの良い点

- **Pure C#のAnimationCurve実装**: Unity依存なしのHermite/Linear補間・バイナリサーチ。パフォーマンスを意識した設計（`Profiler.BeginSample`、アロケーション回避コメント）
- **アプリとの疎結合設計**: TargetIdによるシーンオブジェクトとの間接的なバインディング。シーケンスデータを特定のシーンに依存させず再利用可能にしている
- **MVP系パターン**: Presenterを介したView-Model分離の意図がある
- **リポジトリパターン**: `ITimelineRepository` / `IClipDataRepository`によるストレージ抽象化
- **シリアライザ抽象化**: `IClipDataSerializer` / `ITimelineSerializer`で、JSONフォーマット以外にも差し替え可能
- **UUIDv7**: 時系列ソート可能なIDの採用

### 1.3 改善が必要な課題

#### A. データモデル設計

| 課題 | 現行 | 影響 |
|---|---|---|
| **static連番ID** | `TimelineTrack._nextId`, `TimelineClip._nextId` がstaticインクリメント | 複数シーケンス同時使用時にID衝突、シリアライズ不可（`[JsonIgnore]`で除外している） |
| **命名の揺れ** | `Timeline` class に `// CinematicSequence` コメント、`Sequence` と `Timeline` が混在 | 認知負荷 |
| **プロパティ名ベースのswitch文** | `PoseAnimation`, `LightPropertiesAnimation` のメソッド群が全てswitch/caseで分岐（各6-7ケース） | 新プロパティ追加時に全メソッドの修正が必要、300行超のボイラープレート |
| **TargetId管理** | Trackが`TargetId`(int)を持ち、同じTypeの次のIDを自動採番。シーケンスデータをシーンオブジェクトから独立させる設計 | 設計意図は妥当だが、intの連番IDのため外部からの明示的なバインディング指定ができない。Guid化やマッピングの柔軟性向上が望ましい |
| **型安全性の欠如** | プロパティ値がすべて`float`、プロパティ識別が文字列 | コンパイル時検査ができない |

#### B. アーキテクチャ設計

| 課題 | 現行 | 影響 |
|---|---|---|
| **グローバルシングルトン** | `CinematicSequenceSystem.SequencePlayer` が静的プロパティ | テスト困難、複数インスタンス不可 |
| **Undo/Redo未実装** | 変更操作にコマンドパターンなし | シーケンスエディタとして致命的 |
| **変更通知の欠如** | データモデルが変更イベントを発行しない | UI更新のたびに `UpdateTimelineUI(_timeline)` で全再構築 |
| **イベント接続の手動管理** | PresenterのコンストラクタとDisposeで20以上の手動subscribe/unsubscribe | 漏れのリスク |
| **async void** | `AddClip`が`async void` | 例外の握りつぶしリスク |

#### C. UI設計

| 課題 | 現行 | 影響 |
|---|---|---|
| **UI全再構築** | トラック追加・クリップ移動のたびに`RegenerateTimelineUI()`で全UI再生成 | パフォーマンス問題、ちらつき |
| **コメントアウト過多** | 多数のTODOやコメントアウトされたコード | 保守性低下 |
| **重複コード** | `TimelineEditorView`と`KeyframeEditorView`でスクロール同期・カーソルドラッグが独立実装 | DRY違反 |
| **マジックナンバー** | `targetId == 999`、`_timeCursorOffset = 11.5f` | 可読性・保守性低下 |
| **D&Dの実装** | ライブラリからのD&Dとクリップ移動のD&Dが別々の仕組み | 統一感のない操作性 |
| **選択状態管理** | 複数箇所で`_selectedClip`を保持、同期が不明 | 選択状態の不整合 |

#### D. 操作性

| 課題 | 影響 |
|---|---|
| **スナッピング機能なし** | クリップの正確な配置が困難 |
| **キーボードショートカットなし** | 操作効率が低い |
| **複数選択なし** | クリップの一括操作不可 |
| **クリップのリサイズ不可** | UIでのDuration調整ができない |
| **ズームがUI操作のみ** | ホイールズームやピンチズーム未対応 |
| **タイムルーラーの目盛り調整なし** | ズームレベルに応じた最適な目盛り表示がない |

---

## 2. 計画中の機能統合

再設計にあたり、以下の外部システムとの統合を考慮する。

### 2.1 FBXアニメーション再生（FBXAnimationPlayer）

**ソース:** [FBXAnimationPlayer](https://github.com/sotanmochi/FBXAnimationPlayer/tree/main/src/FBXAnimationPlayer/Assets/FbxAnimationPlayer)

FBXファイルからモーションデータを読み込み、キャラクターに適用する再生機能をシーケンサーに統合する。

**FBXAnimationPlayerの特性:**
- `FbxAnimationController` が `UseManualUpdate` モードで外部から `Seek(float)` / `Update(float deltaTime)` で制御可能
- 評価結果はキーフレームのfloat値ではなく、Unity `AnimationClip.SampleAnimation()` によるボーン変換（`HumanPose`）
- クリップは不透明（opaque）— 個別キーフレーム編集は不可、再生位置の制御のみ
- 1つのFBXに複数のAnimationClipが含まれうる

**設計上の影響:**
- `IClipAsset` がキーフレーム編集可能なアセット（`AnimationClipAsset`）のみを前提としていてはならない
- `SequencePlayer` の評価モデルが、外部プレイヤーへの再生時刻の委譲に対応する必要がある
- モーショントラックのクリップはシーケンスエディタ上で配置・移動・リサイズは可能だが、キーフレーム編集画面は不要

### 2.2 ポストエフェクトパラメータ再生（KinemagicRenderPipeline）

**ソース:** [KinemagicRenderPipeline](https://github.com/KinemagicStudio/KinemagicRenderPipeline/tree/develop/src/UniversalRenderPipeline)

URP拡張のポストプロセスエフェクト（ScreenEdgeColor等）のパラメータをシーケンサーでアニメーション制御する。

**KinemagicRenderPipelineの特性:**
- `VolumeComponent` ベースのエフェクト定義（`ClampedFloatParameter`, `ColorParameter`等）
- パラメータの直接操作: `component.IntensityParam.value = 0.5f`
- カメラ毎のカスタムポストプロセススタック（`KinemagicCameraData.PostProcessStack`）

**設計上の影響:**
- ポストエフェクトのパラメータ（float、Color等）はキーフレームアニメーションで制御可能 → 既存の`AnimationClipAsset`の仕組みで対応可能（float分解: ColorR/G/B等）
- `PropertyTemplates` にエフェクト毎のテンプレートを追加するだけで新エフェクトに対応
- アプリ側アダプターが `AnimationFrame` の値を `VolumeComponent` のパラメータに適用する

---

## 3. 再設計の方針

### 3.1 設計原則

1. **コアロジックはUnity非依存** - `CinematicSequencer`パッケージはPure C#を維持。テスタビリティとポータビリティを確保
2. **データ駆動** - モデルの変更がObserverパターンで自動的にUIに伝搬
3. **コマンドベースの編集** - 全ての編集操作をコマンドとして抽象化し、Undo/Redoを標準装備
4. **拡張可能なトラック/クリップシステム** - 新しいデータタイプの追加が最小限のコード変更で完結
5. **パフォーマンスファースト** - 差分更新、仮想化リスト、プール可能なデータ構造

### 3.2 パッケージ構成

```
CinematicSequencer/           # コアパッケージ（Unity非依存）
├── Runtime/
│   ├── Model/                # データモデル
│   ├── Animation/            # アニメーションカーブ・補間
│   ├── Playback/             # 再生エンジン
│   ├── Editing/              # 編集操作（Command/Undo/Redo）
│   ├── Serialization/        # シリアライズ
│   └── IO/                   # リポジトリ
└── Tests/

CinematicSequencer.UI/        # UIパッケージ（Unity UI Toolkit依存）
├── Runtime/
│   ├── Core/                 # 共通UIコンポーネント
│   ├── SequenceEditor/       # シーケンスエディタ
│   ├── KeyframeEditor/       # キーフレームエディタ
│   ├── Library/              # アセットライブラリ
│   └── UIAssets/             # UXML/USS
└── Tests/
```

---

## 4. コアパッケージ設計概要

詳細: [CinematicSequencer-Redesign-Core.md](./CinematicSequencer-Redesign-Core.md)

### 4.1 データモデル

```
Sequence (旧 Timeline)
├── Guid Id
├── string Name
├── TimeSpan Duration (自動計算)
└── List<Track>
    ├── Guid Id (static連番ではなくGuid)
    ├── string Name
    ├── TrackType Type (拡張可能なenum)
    ├── int SortOrder
    └── List<Clip>
        ├── Guid Id
        ├── Guid ClipAssetId (外部参照)
        ├── TimeRange Placement (StartTime + Duration)
        ├── float PlaybackRate
        └── ClipAsset (遅延ロード可能)
```

**主な改善点:**
- ID: static連番 → Guid（シリアライズ安全、複数インスタンス対応）
- TrackType: 従来の`DataType` enum → 拡張ポイントを持つ型安全な設計
- 時間: `float` → 内部的にはミリ秒int、APIはTimeSpanベースで精度保証
- 変更通知: `INotifyPropertyChanged`相当のイベント発行

### 4.2 クリップアセットの分類

クリップアセットは再生方式により2種類に大別される:

| 分類 | 説明 | 例 |
|---|---|---|
| **キーフレーム編集可能** (`IAnimatableClipAsset`) | シーケンサー内蔵のAnimationCurveでプロパティ値を補間。キーフレームの追加・編集が可能 | カメラPose、ライトProperties、ポストエフェクト |
| **外部再生ソース** (`IExternalClipAsset`) | 外部プレイヤーに再生時刻を委譲。シーケンサー上では配置・移動・リサイズのみ | FBXモーション、Audio |

### 4.3 アニメーションシステム

- `AnimationCurve`の基本設計は維持（Pure C#、バイナリサーチ、Hermite補間）
- **プロパティswitch文の解消**: `Dictionary<string, AnimationCurve>`ベースの汎用`AnimationClipAsset`
- `AnimationPropertyDescriptor`でプロパティのメタデータ（名前、デフォルト値、範囲）を定義
- プロパティ群のテンプレート（`PosePropertyTemplate`, `LightPropertyTemplate`等）で定型パターンを簡易作成

### 4.4 コマンドシステム（Undo/Redo）

```csharp
interface IEditCommand
{
    void Execute();
    void Undo();
    string Description { get; }
}

class EditHistory
{
    void Execute(IEditCommand command);
    bool CanUndo { get; }
    bool CanRedo { get; }
    void Undo();
    void Redo();
}
```

全編集操作（トラック追加/削除、クリップ追加/削除/移動/リサイズ、キーフレーム追加/削除/値変更）をコマンドとして実装。

### 4.5 再生エンジン

- `SequencePlayer`は現行設計をベースに改善
- シングルトンを廃止し、DIで注入可能に
- `PlayerLoop`への挿入は外部（アプリ側アダプター）の責務に

---

## 5. UIパッケージ設計概要

詳細: [CinematicSequencer-Redesign-UI.md](./CinematicSequencer-Redesign-UI.md)

### 5.1 アーキテクチャ

MVPパターンを強化し、以下の層に分割:

```
View (UI Toolkit VisualElement)
  ↕ events / data binding
 ViewController (UIイベント処理、View操作)
  ↕ commands / queries
EditorState (UIの状態管理：選択、ズーム、スクロール)
  ↕ observe
Model (CinematicSequencer Core)
```

### 5.2 差分更新

全再構築をやめ、変更箇所のみ更新:
- TrackのUI要素はIDで管理し、追加/削除のみ差分適用
- クリップのposition/widthは`style`プロパティの直接更新
- `ListView`/仮想化リストを活用して大量トラック時のパフォーマンス確保

### 5.3 操作性の改善

| 機能 | 実装方針 |
|---|---|
| **Undo/Redo** | Ctrl+Z / Ctrl+Shift+Z でコマンド履歴操作 |
| **スナッピング** | グリッド（フレーム/拍/秒単位）、他クリップの端にスナップ |
| **複数選択** | Ctrl+Click / Shift+Click / 矩形選択 |
| **クリップリサイズ** | クリップ端のドラッグハンドル |
| **ホイールズーム** | Ctrl+ホイールでズーム、ポインタ位置を中心に |
| **キーボードショートカット** | Space=再生/一時停止、K=キーフレーム追加、Delete=選択削除 |
| **ドラッグ&ドロップ統一** | ライブラリからのD&DとクリップD&Dを同じManipulatorベースで統一 |

### 5.4 共通コンポーネント化

`SequenceEditorView`と`KeyframeEditorView`で重複しているコンポーネントを共通化:

| コンポーネント | 用途 |
|---|---|
| `TimeRulerElement` | タイムルーラー（目盛り生成、ズーム対応） |
| `PlayheadElement` | 再生ヘッド（ドラッグ、位置更新） |
| `ScrollSyncGroup` | 複数ScrollViewの同期 |
| `TrackListElement` | トラックヘッダー＋コンテンツの同期スクロール |

---

## 6. マイグレーション戦略

### 6.1 段階的移行

| フェーズ | 内容 | 目安 |
|---|---|---|
| **Phase 1** | コアモデルの再設計（ID改善、変更通知、コマンドシステム）、既存UIからの接続 | コア基盤 |
| **Phase 2** | AnimationClipAssetの汎用化（switch文解消）、シリアライズ互換レイヤー | データ層 |
| **Phase 3** | UIの段階的リプレース（共通コンポーネント → シーケンスエディタ → キーフレームエディタ） | UI層 |
| **Phase 4** | 操作性強化（Undo/Redo UI、スナッピング、複数選択、キーボードショートカット） | UX向上 |

### 6.2 後方互換性

- 既存JSONフォーマットの読み込みサポート（`LegacyFormatReader`）
- 新フォーマットバージョン（`"2.0.0"`）として保存
- アプリ側アダプター（`CinematicSequenceSystemAdapter`）のインターフェースは維持

---

## 7. 参考資料

### 類似ソフトウェアの設計パターン

| ソフトウェア | 参考にすべき点 |
|---|---|
| **Unity Timeline** | PlayableGraph、トラック/クリップの抽象化、ブレンド |
| **Blender NLA Editor** | アクション(≒ClipAsset)のストリップ(≒Clip)配置モデル |
| **After Effects** | コンポジション構造、エクスプレッション、キーフレーム補間の多様さ |
| **DaVinci Resolve (Fusion)** | ノードベースとシーケンスの統合 |
| **Godot AnimationPlayer** | トラックタイプの拡張性、メソッド呼び出しトラック |

これらのソフトウェアに共通する設計パターン:
1. **トラック/クリップの抽象基底型** - 型ごとのswitch文ではなく、ポリモーフィズムで拡張
2. **コマンドパターンによるUndo/Redo** - 全エディタ操作の基盤
3. **非破壊編集** - 元データを変更せず、参照とオフセットで配置
4. **シーケンスとアセットの分離** - クリップ定義（アセット）とシーケンス上の配置（インスタンス）を明確に分離
