# SyncFreeD 仕様書 v0.2

- ドキュメント種別: 設計仕様書 / 実装方針書
- パッケージ名: `com.mizotake.syncfreed`
- 想定 Unity バージョン: `2022.3 LTS` 以上
- 想定配布形態: Unity Package Manager (UPM)
- 実装方針: **Pure C# コア + Unity Adapter 層 + UPM Samples + Tests**

---

## 1. 概要

`SyncFreeD` は、PTZ カメラに限らず、複数種のカメラ・トラッキング入力を共通のカメラ状態に正規化し、
同期・補正・遅延調整を経たうえで Free-D を中心とした外部出力へ変換するための Unity 向けパッケージである。

本パッケージの主眼は、単なる Free-D 送信器ではなく、以下を担う **Camera Sync Core** を中心に据えることにある。

- 異なる入力ソースの正規化
- 実機と仮想カメラの同時駆動時の補正
- カメラ挙動・レンズ挙動・遅延のチューニング
- 出力形式としての Free-D 生成
- サンプルシーン・テスト・Editor 支援を含む UPM 提供

初版では Free-D D1 出力を主対象とする。
VISCA 実装は本 package の対象外とし、package 内では実機 client / inquiry / telemetry receiver を提供しない。
Sony FR7 の公開資料では、Free-D は UDP 送信・D1 メッセージ・29 バイト・ビッグエンディアンで構成されること、ソフトウェアバージョン差により送出可能なメタデータや送信先数が変化することが明記されている。これを実装設計の基準とする。  
参考: Sony ILME-FR7 free-d integration manual v2.00

---

## 2. 目的

### 2.1 主要目的

`SyncFreeD` の目的は次のとおり。

1. Unity 上で扱うカメラ制御・トラッキング入力を統一的に扱えるようにする
2. Unity 仮想カメラや外部入力の同時駆動・追従・補正を可能にする
3. PTZ を含む各種カメラや外部トラッカーにも同じ同期モデルを適用可能にする
4. 外部システムとの接続のために Free-D 出力を提供する
5. UPM パッケージとして再利用可能な形で配布する
6. テスト容易性・保守性・拡張性を高く保つ

### 2.2 非目的

初版では以下を目的外とする。

- すべてのメーカー固有挙動の完全再現
- OpenTrackIO の正式実装
- full 6DoF リグの完全対応
- レンズキャリブレーションの GUI 完全自動化
- Genlock と完全同期した deterministic 出力保証
- Free-D 受信機能を中心とした汎用トラッキングハブ化
- VISCA 実機 client の内蔵実装

---

## 3. 想定ユースケース

### 3.1 Virtual Camera Only

- Unity Camera / CineCamera / 独自 Rig の Transform と Lens 情報から Free-D を生成
- PTZ 実機を持たないプレビュー・検証環境でも利用できる

### 3.2 外部トラッカー起点

- 外部トラッカーから 3DoF / 6DoF の姿勢を取得
- レンズ情報は別ソースから取得
- 共通状態へ統合して Free-D 出力

### 3.3 録画済みトラッキングデータ再生

- JSON / CSV / バイナリなどに記録したトラッキングデータを再生
- リグ挙動の再現、テスト、デバッグに使用

### 3.4 将来拡張

- Free-D 以外の出力（OpenTrackIO / 独自 JSON / Record / Debug）へ拡張
- Free-D 受信ブリッジの追加
- PTZ カメラ以外のジンバル・固定カメラ・エンコーダ付きレンズ運用への拡張

---

## 4. 外部仕様に基づく設計前提

### 4.1 Sony FR7 free-d 仕様を基準にする理由

Sony FR7 の公開マニュアルでは、Free-D D1 のメッセージ構造、バイト長、座標系、ソフトウェアバージョン差、送信周波数、レンズキャリブレーション上の推奨設定、運用上の制約事項まで一貫して提示されている。初版の設計根拠として十分に具体的であるため、これを Free-D 実装の基準とする。

### 4.2 Free-D D1 の基本仕様

Sony FR7 の資料によれば、Free-D 出力は以下である。

- UDP 送信
- D1 メッセージ
- 29 バイト
- ビッグエンディアン
- カメラ ID、パン、チルト、ロール、X/Y/Z、ズーム、フォーカス、ユーザー定義領域、チェックサムを含む

### 4.3 バージョン差分

Sony FR7 の free-d 資料では、ソフトウェアバージョンごとの差異がある。

- 2.x: 最大 1 ユニキャスト
- 3.x: 最大 4 ユニキャスト + 位置オフセット設定
- 4.x 以降: 最大 4 ユニキャスト + 最大 1 マルチキャスト、イメージセンサー基準の位置/向き計算、ステージ原点オフセット、スライドベース位置

このため `SyncFreeD` では、Device 単位ではなく **FirmwareBehaviorProfile** も分けて扱う。

### 4.4 座標系

Sony FR7 資料における Free-D 座標系は次のとおり。

- X: 右向き正
- Y: 正面向き正
- Z: 上向き正
- Tilt: X 軸回転
- Roll: Y 軸回転
- Pan: Z 軸回転

Unity の典型的な座標系は X 右 / Y 上 / Z 前で近いが、回転定義とオイラー変換で差異が出るため、Quaternion から専用変換器を通す必要がある。

### 4.5 実運用で重要な追加知見

Aximmetry の Free-D / PTZ ドキュメントでは、次の点が実務上重要とされている。

- UDP/IP を基本とする
- 専用 LAN と固定 IP を推奨
- NIC 指定やマルチキャスト参加が必要な場合がある
- PTZ は all-in-one だけでなく Pan/Tilt ヘッド + Zoom Device 構成もある

Pixotope の同期資料では、動画とトラッキングの遅延は別扱いで調整すべきとされている。
そのため本パッケージでも `TrackingDelayMs` と `VideoAlignmentDelayMs` を別パラメータとして扱う。

### 4.6 Unity パッケージ設計の前提

Unity 公式ドキュメントでは、カスタム UPM パッケージについて、`Runtime`, `Editor`, `Tests/Editor`, `Tests/Runtime`, `Samples~`, `Documentation~` を含むレイアウトが推奨されている。また、サンプルは `Samples~` に置き、テストは `Tests/Editor` と `Tests/Runtime` に分けることが推奨される。

さらに Assembly Definition により、アセンブリ境界と依存方向を明示できる。Pure C# コアを中心にする本パッケージでは、この構造が必須となる。

### 4.7 既存資産との整合

本パッケージは、Pure C# コア、外部 source を差し込める構造、薄い MonoBehaviour 層、Editor ツール、UPM 配布を基本方針とする。特定プロトコル実装を内包せず、同期・補正・出力の責務を中心に拡張可能な構成とする。VISCA についても package 内では concrete 実装を持たない。

---

## 5. 設計思想

### 5.1 中心は Free-D ではなく Sync Engine

本パッケージの中心は `FreeDPacketBuilder` ではなく `CameraSyncEngine` とする。
依存の流れは次のとおり。

```text
Camera Sources
   ↓
CameraSyncEngine
   ↓
Corrected Canonical State
   ↓
Outputs (Free-D / Debug / Recording / ...)
```

これにより、Free-D を 1 出力形式として扱い、将来拡張が容易になる。

### 5.2 Pure C# コア

コアロジックは UnityEngine 非依存の Pure C# とする。

Pure C# に含めるもの:

- 状態モデル
- 同期ロジック
- 遅延補償
- 補正アルゴリズム
- Free-D パケット生成
- チェックサム計算
- カーブ評価の最小実装
- 診断用集計ロジック

Unity 依存を許容するもの:

- MonoBehaviour
- Transform / Camera / Lens 取得
- UDP ソケット実送信の Unity 運用補助
- Editor UI
- Samples のシーン・Prefab

### 5.3 入力と出力の分離

- 入力: `ICameraSource`
- 同期: `ICameraSynchronizer`
- 補正: `ISyncCorrector`
- 出力: `ICameraOutput`

この分離により、PTZ / 非 PTZ / 外部トラッカー / 再生データを同一フレームワークに乗せられる。

### 5.4 Capability ベース設計

カメラ種別ではなく、持つ能力で扱う。

例:

- PanTilt
- Roll
- Position
- Zoom
- Focus
- Iris
- Preset
- ExternalTracking

これにより、PTZ だけでなく固定カメラ、ジンバル、Pan/Tilt head + Lens Encoder、バーチャルカメラにも同じ設計を適用できる。

---

## 6. スコープ

### 6.1 初版に含めるもの

- Pure C# `CameraSyncEngine`
- Canonical Camera State
- Free-D D1 Packet Builder
- UDP Unicast 送信
- Camera ID / Destination / Multicast 設計枠
- Unity Camera Source
- Sync Mode
- Tuning Profile
- Diagnostics
- UPM package layout
- Samples~
- Edit Mode / Play Mode Tests
- asmdef 分離

### 6.2 初版で対象外とするもの

- OpenTrackIO 出力
- 完全なマルチキャスト運用支援 UI
- すべての PTZ メーカー癖の吸収
- VISCA 実機 client / inquiry / telemetry receiver
- 実機 CGI 設定ツール
- レンズ歪み自動キャリブレーション
- ネットワーク障害解析ツール一式

---

## 7. 全体アーキテクチャ

```text
com.mizotake.syncfreed
├─ Core (Pure C#)
│  ├─ Models
│  ├─ Sources.Abstractions
│  ├─ Sync
│  ├─ Correction
│  ├─ Conversion
│  ├─ Outputs
│  └─ Diagnostics
├─ Runtime (Unity Adapter)
│  ├─ Behaviours
│  ├─ Sources
│  ├─ Outputs
│  ├─ Networking
│  └─ ScriptableObjects
├─ Editor
│  ├─ Inspectors
│  ├─ Windows
│  └─ Setup
├─ Samples~
└─ Tests
```

### 7.1 レイヤーごとの責務

#### Core

- UnityEngine 非依存
- 同期・補正・変換の中核

#### Runtime

- Unity Camera / Transform / Lens との橋渡し
- 実行時の Source / Output の結線

#### Editor

- セットアップ支援
- デバッグ可視化
- 事前検証

#### Samples~

- 導入例
- 動作確認例
- 最低限の検証フロー

#### Tests

- Core の単体テスト
- Runtime の結合テスト
- シーンベースの Play Mode 検証

---

## 8. Canonical State

### 8.1 目的

異なる入力ソースを統一的に扱うため、すべてのカメラ状態は Canonical State に変換する。

### 8.2 基本構造

```csharp
public struct CameraSyncState
{
    public string SourceId;
    public int CameraId;

    public PoseState Command;
    public PoseState Predicted;
    public PoseState Observed;
    public PoseState Corrected;

    public LensState Lens;
    public TimingState Timing;
    public ValidityState Validity;
}
```

```csharp
public struct PoseState
{
    public double PanDeg;
    public double TiltDeg;
    public double RollDeg;

    public double Xmm;
    public double Ymm;
    public double Zmm;

    public long TimestampTicks;
}
```

```csharp
public struct LensState
{
    public double ZoomNormalized;
    public double FocusNormalized;
    public double IrisFNumber;
    public double FocalLengthMm;
}
```

```csharp
public struct TimingState
{
    public int CommandDelayMs;
    public int TrackingDelayMs;
    public int OutputDelayMs;
    public int VideoAlignmentDelayMs;
    public ushort FrameModulo16;
}
```

```csharp
public struct ValidityState
{
    public bool IsTrackingValid;
    public bool IsLensValid;
    public bool IsDegraded;
    public bool IsFallbackMode;
}
```

### 8.3 Command / Predicted / Observed / Corrected を分ける理由

この区別を設けることで、次が可能になる。

- 操作直後は `Predicted` を使ってレスポンスを高く保つ
- 実機や外部トラッカーから受信した `Observed` を使ってズレを補正する
- 最終的に `Corrected` を Free-D に流す

PTZ 実機と Unity 仮想カメラの同時駆動では、この分離が特に重要である。

---

## 9. 入力ソース設計

### 9.1 Source 抽象

```csharp
public interface ICameraSource
{
    string SourceId { get; }
    CameraCapabilities Capabilities { get; }
    bool TryGetObservedState(out CameraObservedFrame frame);
}
```

### 9.2 想定ソース

- `UnityCameraSource`
- `TrackerCameraSource`
- `ReplayCameraSource`
- `FreeDInputSource`（将来）
- `CustomCameraSource`

### 9.3 補助インターフェース

必要に応じて以下を分割する。

```csharp
public interface ICommandInputSource { }
public interface ILensDataSource { }
public interface ITimingSource { }
```

これにより、姿勢とレンズとタイミングを別系統で取得できる。

---

## 10. Capability モデル

```csharp
[Flags]
public enum CameraCapabilities
{
    None = 0,
    PanTilt = 1 << 0,
    Roll = 1 << 1,
    Position = 1 << 2,
    Zoom = 1 << 3,
    Focus = 1 << 4,
    Iris = 1 << 5,
    Preset = 1 << 6,
    ExternalTracking = 1 << 7
}
```

### 10.1 例

#### All-in-one PTZ camera
- PanTilt
- Roll（機種次第）
- Zoom
- Focus
- Iris

#### Motorized Pan/Tilt Head + Lens Encoder
- PanTilt
- Zoom
- Focus
- Iris
- Position（外部トラッカーがある場合）

#### Fixed Camera + Lens only
- Zoom
- Focus
- Iris

#### External Tracker + Lens
- Position
- Roll
- PanTilt
- Zoom
- Focus

---

## 11. Sync Mode

```csharp
public enum SyncMode
{
    VirtualMaster,
    RealMaster,
    DualDrive,
    ExternalTrackingMaster,
    ReplayMaster
}
```

### 11.1 VirtualMaster

Unity の仮想カメラを真値とする。

### 11.2 RealMaster

実機の実測値を真値とし、Unity を追従させる。

### 11.3 DualDrive

入力を実機と Unity に同時に適用する。
操作中は command / predicted を優先して応答性を確保し、停止後は observed を使って corrected へ収束させる。

運用フロー:

- 操作開始で `Driving`
- 入力停止後は `Settling`
- 短周期の observed 評価で pose / lens の誤差を確認
- 閾値内に連続 N 回入ったら `Settled`
- 再入力で常に `Driving` に戻る

AR 合成では、操作中は predicted 系、着地後は corrected / observed 系を基準とする。

### 11.4 ExternalTrackingMaster

外部トラッキング値を真値とし、必要に応じてレンズ情報をマージする。

### 11.5 ReplayMaster

記録データを真値として再生する。

---

## 12. 同期エンジン

### 12.1 抽象

```csharp
public interface ICameraSynchronizer
{
    CameraSyncState Update(CameraSyncContext context);
}
```

### 12.2 代表実装

- `DefaultCameraSynchronizer`
- `DualDriveCameraSynchronizer`
- `RealMasterCameraSynchronizer`
- `ReplayCameraSynchronizer`

### 12.3 入力コンテキスト

```csharp
public readonly struct CameraSyncContext
{
    public readonly long TimestampTicks;
    public readonly CameraObservedFrame? ObservedFrame;
    public readonly CameraCommandFrame? CommandFrame;
    public readonly SyncMode SyncMode;
    public readonly SyncTuningProfile Tuning;
}
```

### 12.4 出力ポリシー

最終出力ソースは選択可能にする。

- `Predicted`
- `Observed`
- `Blended`
- `Corrected`

初期値は `Corrected` を推奨する。

---

## 13. 補正モデル

### 13.1 目的

- 実機と仮想の速度差吸収
- 停止タイミング差の吸収
- 遅延補償
- 長時間運用でのドリフト吸収

### 13.2 抽象

```csharp
public interface ISyncCorrector
{
    PoseState Correct(
        in PoseState predicted,
        in PoseState observed,
        in SyncTuningProfile profile);
}
```

### 13.3 基本ポリシー

- 小さな誤差はソフト補正
- 大きな誤差はスナップ補正
- ハードスナップ多用は禁止

### 13.4 調整対象

- Pan/Tilt/Zoom の速度スケール
- 加速・減速
- Dead Zone
- Rotation/Position/Zoom Correction Gain
- SnapThreshold
- TrackingDelayMs
- VideoAlignmentDelayMs

### 13.5 PTZ 同時駆動で重要な点

- 実機の応答速度と Unity の応答速度は一致しない
- ズームは画角体感が線形でない
- 停止時の減速挙動に差が出やすい
- 長時間で累積誤差が出やすい

このため `SyncTuningProfile` を必須概念とする。

---

## 14. プロファイル設計

### 14.1 DeviceProfile

```csharp
public sealed class DeviceProfile
{
    public string DeviceName;
    public CameraCapabilities Capabilities;
    public bool SupportsRoll;
    public bool SupportsPosition;
}
```

### 14.2 FirmwareBehaviorProfile

```csharp
public sealed class FirmwareBehaviorProfile
{
    public string VersionLabel;
    public bool SupportsMultiUnicast;
    public bool SupportsMulticast;
    public bool UsesImageSensorBasedOrientation;
    public bool UsesImageSensorBasedPosition;
    public bool SupportsSlideBase;
}
```

### 14.3 LensProfile

```csharp
public sealed class LensProfile
{
    public string LensName;
    public double MinFocalLengthMm;
    public double MaxFocalLengthMm;
    public CurveDefinition ZoomCurve;
    public CurveDefinition FocusCurve;
}
```

### 14.4 MountProfile

```csharp
public sealed class MountProfile
{
    public Vector3Data SensorOffsetMm;
    public Vector3Data RotationOffsetDeg;
    public Vector3Data TrackingOriginOffsetMm;
}
```

### 14.5 SyncTuningProfile

```csharp
public sealed class SyncTuningProfile
{
    public double PanSpeedScale;
    public double TiltSpeedScale;
    public double ZoomSpeedScale;

    public double RotationCorrectionGain;
    public double PositionCorrectionGain;
    public double ZoomCorrectionGain;

    public double SnapThresholdDeg;
    public double SnapThresholdMm;
    public double SnapThresholdZoom;

    public int IdleToSettleDelayMs;
    public int SettleIntervalMs;
    public int SettleTimeoutMs;
    public int RequiredConsecutiveMatches;
    public int TrackingDelayMs;
    public int OutputDelayMs;
    public int VideoAlignmentDelayMs;
}
```

既定値:

- `IdleToSettleDelayMs = 150`
- `SettleIntervalMs = 50`
- `SettleTimeoutMs = 1000`
- `RequiredConsecutiveMatches = 3`

着地判定の閾値は既定では `SnapThresholdDeg`、`SnapThresholdMm`、`SnapThresholdZoom` を流用する。

### 14.6 Pure C# 維持のための注意

Core 層では `AnimationCurve` に依存しない。
必要なカーブは独自構造またはサンプル点ベースの補間で実装する。
Unity 用 ScriptableObject は Runtime Adapter 層でこれをラップする。

---

## 15. Free-D 出力仕様

### 15.1 対応範囲

初版は Free-D D1 の送信のみを正式対応とする。

### 15.2 パケット構成

以下を生成する。

- D1
- CA
- PH PM PL
- TH TM TL
- RH RM RL
- XH XM XL
- YH YM YL
- HH HM HL
- ZH ZM ZL
- FH FM FL
- SH SL
- CK

### 15.3 値のエンコード

Sony FR7 の資料を基準に次を採用する。

- 角度: 24bit 2 の補数
- 位置: 24bit 2 の補数
- Zoom: 24bit unsigned 相当の変換枠
- Focus: 24bit unsigned 相当の変換枠
- User Area: Iris F 値 + Frame 番号

### 15.4 Checksum

FR7 資料準拠のチェックサム計算を行う。

### 15.5 出力モード

- Single Destination Unicast
- Multi Destination Unicast（初版で基礎対応）
- Multicast（初版では設計枠優先）

### 15.6 Timing 情報

`FrameModulo16` を保持し、必要に応じて User Area 上位 4bit へ詰める。

### 15.7 Packet Builder 抽象

```csharp
public interface IFreeDPacketBuilder
{
    int Build(in CameraSyncState state, Span<byte> destination);
}
```

---

## 16. 出力層設計

### 16.1 抽象

```csharp
public interface ICameraOutput
{
    void Send(in CameraSyncState state);
}
```

### 16.2 初版実装

- `FreeDOutput`
- `DebugLogOutput`
- `RecordingOutput`（任意）

### 16.3 将来拡張

- `OpenTrackIOOutput`
- `JsonUdpOutput`
- `CsvRecordingOutput`
- `FreeDLoopbackOutput`

---

## 17. Runtime 層

### 17.1 目的

Core と Unity の橋渡しを行う。

### 17.2 主なコンポーネント

#### `SyncFreeDBehaviour`
- エントリーポイント
- Source / Sync / Output をまとめる

#### `UnityCameraSourceBehaviour`
- Camera / Transform から Canonical State 用情報を取得

#### `CompositeCameraSourceBehaviour`
- pose source と lens source を統合
- fixed camera や lens encoder 併用を扱う


#### `FreeDUdpOutputBehaviour`
- Runtime で UDP 送信を実行

#### `SyncDiagnosticsBehaviour`
- エラー値、遅延、補正発生回数を表示

### 17.3 Unity 依存の限定

Runtime Adapter 層でのみ Unity の以下に依存する。

- `Transform`
- `Camera`
- `MonoBehaviour`
- `ScriptableObject`
- Unity UI / Gizmo / Editor 拡張周辺

---

## 18. Editor 層

### 18.1 役割

- セットアップ効率の改善
- デバッグ可視化
- パケット検証
- 設定ミス低減

### 18.2 提供機能

#### Inspector
- Destination 一覧
- Camera ID
- SyncMode
- Source / Output 接続状況
- Profile 適用状況
- ログレベル

#### Debug Window
- Command / Predicted / Observed / Corrected の差分表示
- パン・チルト・ロール差分
- 位置差分
- ズーム差分
- フレーム番号
- 29 バイトパケットの Hex 表示
- Checksum 表示

#### Setup Wizard
- Camera Rig 生成
- 主要 Behaviour 自動追加
- サンプル Profile 生成
- サンプルシーンセットアップ

---

## 19. UPM パッケージ構成

Unity の推奨構成に合わせて次のレイアウトを採用する。

```text
Packages/com.mizotake.syncfreed
├─ package.json
├─ README.md
├─ CHANGELOG.md
├─ LICENSE.md
├─ Third Party Notices.md
├─ Documentation~
│  ├─ Overview.md
│  ├─ Architecture.md
│  ├─ SyncModel.md
│  ├─ FreeDOutput.md
│  ├─ Samples.md
│  └─ Testing.md
├─ Runtime
│  ├─ com.mizotake.syncfreed.asmdef
│  ├─ Core
│  │  ├─ Abstractions
│  │  ├─ Models
│  │  ├─ Sync
│  │  ├─ Correction
│  │  ├─ Conversion
│  │  ├─ Outputs
│  │  └─ Diagnostics
│  ├─ UnityAdapters
│  │  ├─ Behaviours
│  │  ├─ Sources
│  │  ├─ Outputs
│  │  └─ ScriptableObjects
│  └─ Networking
├─ Editor
│  ├─ com.mizotake.syncfreed.editor.asmdef
│  ├─ Inspectors
│  ├─ Windows
│  └─ Setup
├─ Tests
│  ├─ Editor
│  │  └─ com.mizotake.syncfreed.editor.tests.asmdef
│  └─ Runtime
│     └─ com.mizotake.syncfreed.runtime.tests.asmdef
└─ Samples~
   ├─ BasicVirtualCamera
   ├─ ExternalTrackerSample
   ├─ ReplaySample
   └─ OutputInspectorSample
```

### 19.1 Samples の扱い

正式サンプルは `Samples~` に格納し、Package Manager 経由で `Assets/Samples/SyncFreeD/<version>/...` に展開される運用を前提とする。

---

## 20. Assets にサンプル動作を置く方針

### 20.1 配布上の前提

UPM の標準導線に従うため、リポジトリ本体には `Samples~` を用いる。

### 20.2 実利用上の見え方

ユーザーは Package Manager からサンプルを Import することで、`Assets/Samples/...` に展開された動作サンプルを利用できる。

### 20.3 この方式を採用する理由

- UPM と整合する
- バージョンごとのサンプル差分管理がしやすい
- 本体コードと導入用サンプルを分離できる
- Git URL インストールでも運用しやすい

---

## 21. サンプル仕様

### 21.1 BasicVirtualCamera

目的:
- Unity Camera を Free-D 送信する最小構成

内容:
- 1 台の Camera
- 1 つの `SyncFreeDBehaviour`
- 1 つの `FreeDUdpOutputBehaviour`
- Hex プレビュー UI

### 21.2 ExternalTrackerSample

目的:
- 外部トラッカー起点の同期例

内容:
- Pose 入力
- Lens 情報マージ
- Corrected 状態の確認

### 21.3 ReplaySample

目的:
- 記録データから状態再生

内容:
- JSON / CSV 読み込み
- フレーム送り
- 出力再現

### 21.4 OutputInspectorSample

目的:
- Free-D 生成結果の確認

内容:
- Free-D 生成結果の確認
- D1 29 バイト表示
- Camera ID
- Checksum
- User Area
- FrameModulo16

### 21.5 FreeDControllerSample

目的:
- Unity 内で Free-D 操作確認を素早く行う

内容:
- keyboard controller による camera 操作
- loopback と diagnostics の同時確認
- preset 再利用の確認

### 21.6 SharedPresets

目的:
- sample 間で再利用する送信設定 / 操作設定をまとめる

内容:
- `LocalLoopbackOutput.asset`
- `ComfortController.asset`

---

## 22. asmdef 設計

### 22.1 基本方針

Assembly Definition でアセンブリ境界を明示し、依存方向を固定する。

### 22.2 構成

- `com.mizotake.syncfreed`
- `com.mizotake.syncfreed.editor`
- `com.mizotake.syncfreed.runtime.tests`
- `com.mizotake.syncfreed.editor.tests`

### 22.3 依存方向

```text
runtime.tests -> runtime
editor -> runtime
editor.tests -> editor, runtime
```

### 22.4 原則

- Runtime から Editor を参照しない
- Core を Runtime asmdef 内の純粋 C# 名前空間に閉じ込める
- テストは必要なら追加 asmdef で細分化可能

---

## 23. テスト戦略

### 23.1 目的

- プロトコル誤りの早期検出
- 補正ロジックの回帰防止
- Runtime 統合の安定化
- CI への組み込み

### 23.2 Edit Mode テスト

対象:

- 座標変換
- Quaternion → Pan/Tilt/Roll 変換
- 24bit 2 の補数エンコード
- Packet Builder
- Checksum
- Tuning Profile の適用
- Delay 補償ロジック
- Snap 判定
- Blend ロジック

### 23.3 Play Mode テスト

対象:

- MonoBehaviour 初期化
- Camera/Transform 取得
- UDP 送信の結合確認
- サンプルシーン起動
- SyncMode 切り替え
- Diagnostics 更新

### 23.4 Fake / Mock 戦略

- Fake tracker source
- Fake lens source
- Fake output sink
- Loopback UDP receiver

これにより実機なしで CI 実行可能とする。

### 23.5 回帰テスト重点ポイント

- D1 29 バイト生成
- Camera ID パック
- User Area 計算
- Multi destination 送信順
- OutputSource 切り替え
- SyncMode 切り替え
- LensProfile 差し替え

---

## 24. Diagnostics / Logging

### 24.1 ログレベル

- None
- Error
- Warning
- Info
- Packet
- Verbose

### 24.2 表示項目

- SourceId
- CameraId
- SyncMode
- Command / Predicted / Observed / Corrected 差分
- Pan/Tilt/Roll Error
- Position Error
- Zoom Error
- TrackingDelayMs
- VideoAlignmentDelayMs
- Packet Hex
- Checksum
- Send Success / Failure Count

### 24.3 Degraded 状態

Sony FR7 の制約事項を参考に、単純な接続有無だけでなく、劣化状態を扱う。

例:
- tracking valid だが timing unreliable
- lens valid だが zoom unreliable
- fallback mode へ移行

---

## 25. ネットワーク設計

### 25.1 基本方針

Aximmetry の運用知見にならい、UDP/IP を基本とし、専用 LAN・固定 IP を推奨する。

### 25.2 設定項目

- Bind Address
- Destination IP
- Destination Port
- NIC 指定
- Camera ID filter
- Join Multicast Group
- Socket buffer size
- Packet send mode

### 25.3 多宛先送信

多宛先送信は対応するが、後順位ほど遅延が増える可能性があるため、Diagnostics で可視化する。

### 25.4 送信タイミング

- `LateUpdate`
- 固定周期タイマー
- `ManualTick`

用途に応じて選択できるようにする。

---

## 26. PTZ 同時駆動チューニング

### 26.1 考慮すべき差分

- 実機と Unity の速度差
- 実機の慣性・減速
- ズームの見え方差
- 停止タイミング差
- ネットワーク遅延
- 動画と tracking のズレ

### 26.2 必須パラメータ

- `PanSpeedScale`
- `TiltSpeedScale`
- `ZoomSpeedScale`
- `RotationCorrectionGain`
- `PositionCorrectionGain`
- `ZoomCorrectionGain`
- `SnapThresholdDeg`
- `SnapThresholdMm`
- `SnapThresholdZoom`
- `IdleToSettleDelayMs`
- `SettleIntervalMs`
- `SettleTimeoutMs`
- `RequiredConsecutiveMatches`
- `TrackingDelayMs`
- `VideoAlignmentDelayMs`

### 26.3 Observed 利用

observed を返せる source は pose / lens の実測値を使って corrected を更新できる。
操作停止後は settle 用の短周期評価を行う。

settle 判定:

- `IdleToSettleDelayMs` 経過後に `Settling` へ移行
- `SettleIntervalMs` ごとに observed を評価
- pose / lens 誤差が閾値内に `RequiredConsecutiveMatches` 回連続で入ったら `Settled`
- `SettleTimeoutMs` を超えたら timeout として終了

source 契約:

- observed は `CameraObservedFrame` 相当で pose / lens / timing / validity を返す

---

## 27. PTZ 以外のカメラへの追従

### 27.1 Fixed Camera

- 姿勢変化なしでも Lens 情報や offset のみ変動する場合がある
- Tracking 無効でも Lens 有効の状態を扱う

### 27.2 Gimbal Camera

- Roll の連続性が重要
- 高頻度の補間と smooth correction が必要

### 27.3 Pan/Tilt Head + Lens Encoder

- 姿勢とレンズが別ソースになる
- Source 合成が必要

### 27.4 External Tracking System

- 姿勢は tracker
- Lens は別系統
- Camera ID との紐付けが必要

`SyncFreeD` はこれらを同じ Canonical State へ統合して扱う。

---

## 28. 実装優先度

### M1

- パッケージ骨組み
- Canonical State
- Free-D D1 Packet Builder
- BasicVirtualCamera Sample
- Edit Mode Tests

### M2

- Sync Engine
- Tuning Profile
- Diagnostics
- Play Mode Tests

### M3

- DualDrive 対応

### M4

- ReplaySample
- ExternalTrackerSample
- 多宛先送信
- Profile 強化

### M5

- マルチキャスト強化
- より詳細な Editor UI
- OpenTrackIO への拡張準備

---

## 29. 主要クラス一覧

### Core

- `CameraSyncEngine`
- `CameraSyncContext`
- `CameraSyncState`
- `PoseState`
- `LensState`
- `TimingState`
- `ValidityState`
- `DefaultCameraSynchronizer`
- `DualDriveCameraSynchronizer`
- `DefaultSyncCorrector`
- `DelayCompensator`
- `StateInterpolator`
- `FreeDPacketBuilder`
- `FreeDChecksumCalculator`
- `CameraObservedFrame`
- `CameraCommandFrame`

### Runtime

- `SyncFreeDBehaviour`
- `UnityCameraSourceBehaviour`
- `CompositeCameraSourceBehaviour`
- `LensEncoderSourceBehaviour`
- `TrackerCameraSourceBehaviour`
- `ReplayCameraSourceBehaviour`
- `FreeDUdpOutputBehaviour`
- `SyncDiagnosticsBehaviour`

### Editor

- `SyncFreeDBehaviourEditor`
- `SyncFreeDDebugWindow`
- `SyncFreeDSetupWizard`

---

## 30. 導入手順（想定）

1. Git URL で UPM インストール
2. `Samples~` から `BasicVirtualCamera` を Import
3. `SyncFreeDBehaviour` を Camera Rig に追加
4. Destination / Camera ID / SyncMode を設定
5. 必要なら `LensProfile`, `MountProfile`, `SyncTuningProfile` を適用
6. Packet Preview で D1 29 バイトを確認
7. Loopback Receiver または外部システムで受信確認

---

## 31. リスクと注意点

### 31.1 実機完全一致は目標外

同じ Free-D でも機種やファームウェアで意味づけが異なる場合がある。

### 31.2 Zoom / Focus の解釈差

受け側システムごとに期待値が異なる可能性があるため、固定実装ではなく Profile で吸収する。

### 31.3 PTZ 同時駆動は調整前提

DualDrive は原理上ズレが発生しやすく、キャリブレーション・補正パラメータが必要になる。

### 31.4 ネットワーク依存

NIC 指定、固定 IP、マルチキャスト参加、ポート競合回避など、ソフトウェアだけでは吸収しきれない運用条件がある。

---

## 32. 結論

`SyncFreeD` は、PTZ 固有の問題に引っ張られすぎず、しかし PTZ 同時駆動という現実的な難所も扱えるようにするため、

**Camera Sync Core + Free-D Output Adapter**

として設計するのが最適である。

この構成であれば、

- PTZ カメラ
- 固定カメラ
- ジンバル
- 外部トラッカー付きカメラ
- バーチャルカメラ
- 録画データ再生

を同じ枠組みで扱うことができる。

また、UPM / Samples~ / Tests / asmdef を正しく使うことで、配布・保守・再利用・CI 組み込みのすべてで破綻しにくい。

---

## 33. 参考資料

### Free-D / カメラ仕様

1. Sony ILME-FR7 カメラトラッキングデータ出力機能のインテグレーションマニュアル (free-d 編) v2.00  
   https://www.sony.jp/ls-camera/download/pdf/FR7/ILME-FR7_free-d_integration_camera_v2_00.pdf

### Unity Package / Assembly / Test

2. Unity Manual - Creating custom packages  
   https://docs.unity3d.com/2022.3/Documentation/Manual/CustomPackages.html

3. Unity Manual - Package layout  
   https://docs.unity3d.com/2022.3/Documentation/Manual/cus-layout.html

4. Unity Manual - Assembly definitions  
   https://docs.unity3d.com/2020.1/Documentation/Manual/ScriptCompilationAssemblyDefinitionFiles.html

5. Unity Manual - Add tests to your package  
   https://docs.unity3d.com/6000.3/Documentation/Manual/cus-tests.html

### 実運用参考

6. Aximmetry - Setting Up Free-D Systems  
   https://aximmetry.com/learn/virtual-production-workflow/tracking/setting-up-specific-tracking-systems/setting-up-free-d-systems/

7. Aximmetry - PTZ Cameras  
   https://aximmetry.com/learn/virtual-production-workflow/tracking/advanced-information-and-features/ptz-cameras/

8. Pixotope Help - Calibrate delays  
   https://help.pixotope.com/phc/26.1/calibrate-delays

9. Unreal Engine - Live Link FreeD in Unreal Engine  
   https://dev.epicgames.com/documentation/en-us/unreal-engine/live-link-freed--in-unreal-engine
