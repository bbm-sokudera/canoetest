# RuntimeParameterController - 使用ガイド

## 概要

`RuntimeParameterController`は、Unity実行時に深度カメラの検知パラメータやOSC通信設定をGUI上でリアルタイムに調整できるツールです。

## 機能

### 1. 深度設定 (Depth Settings)

| パラメータ | 説明 | 範囲 |
|:---------|:-----|:-----|
| **Depth Scale** | 深度値を実際の距離に変換するスケール係数 | 1000 - 5000 |
| **Min Depth Threshold** | 検知する最小深度（メートル） | 0.1 - 2.0m |
| **Max Depth Threshold** | 検知する最大深度（メートル） | 0.5 - 3.0m |

### 2. 検知設定 (Detection Settings)

| パラメータ | 説明 | 範囲 |
|:---------|:-----|:-----|
| **Min Pixels to Trigger** | 物体検知と判定する最小ピクセル数 | 10 - 200 |
| **Sequence Max Time Gap** | シーケンス検知の最大時間差（秒） | 0.1 - 2.0秒 |

### 3. 領域設定 (Region Settings)

4つの検知領域（Region 0〜3）の位置とサイズを個別に調整できます。

- **X**: 領域の左上X座標（ピクセル） - 範囲: 0-640
- **Y**: 領域の左上Y座標（ピクセル） - 範囲: 0-480
- **Width**: 領域の幅（ピクセル） - 範囲: 10-300
- **Height**: 領域の高さ（ピクセル） - 範囲: 10-300

### 4. OSC設定 (OSC Settings)

| パラメータ | 説明 |
|:---------|:-----|
| **Remote Host** | OSCメッセージの送信先IPアドレス |
| **Remote Port** | OSCメッセージの送信先ポート番号 |
| **Enable Rate Limiter** | レート制限機能のオン/オフ |
| **Limit Time** | 制限時間（秒） - 範囲: 0.1-3.0秒 |
| **Limit Count** | 制限時間内の最大送信回数 - 範囲: 1-10回 |

## 使い方

### 基本操作

1. **Unity Editorでシーンを開く**
   - `Assets/Orbbec/Scenes/DepthViewer 1.unity` を開く

2. **Play モードに入る**
   - 画面左側にGUIパネルが表示されます

3. **各設定を調整**
   - スライダーをドラッグして値を変更
   - 変更は即座に反映されます

4. **GUIの表示/非表示**
   - キーボードの **H キー** を押すとGUIの表示/非表示を切り替えられます

### OSC設定の変更手順

1. **Remote Host を変更**:
   - テキストフィールドに新しいIPアドレスを入力
   - 「適用」ボタンをクリック

2. **Remote Port を変更**:
   - テキストフィールドに新しいポート番号を入力
   - 「適用」ボタンをクリック

### 領域設定の調整

1. 「▼ 領域設定 (Region Settings)」をクリックして展開
2. 編集したい領域ボタン（Region 0〜3）をクリック
3. スライダーでX, Y, Width, Heightを調整
4. Sceneビューで変更をリアルタイムプレビュー（Gizmosで表示）

## セットアップ

### DepthViewer 1シーンの構成

このシーンには以下のGameObjectが配置されています：

- **RuntimeController** - このGUIコントローラー
  - `detector`: NormLowSequenceDetectorへの参照
  - `oscManager`: OSCManagerへの参照
  - `showGUI`: 初期表示状態（デフォルト: true）
  - `guiScale`: GUIのスケール（デフォルト: 1.0）

### 他のシーンに追加する場合

1. 空のGameObjectを作成
2. `RuntimeParameterController` スクリプトをアタッチ
3. Inspectorで以下を設定：
   - **Detector**: `NormLowSequenceDetector` コンポーネントへの参照
   - **Osc Manager**: `OSCManager` コンポーネントへの参照

## トラブルシューティング

### GUIが表示されない

- **H キー** を押してGUIが非表示になっていないか確認
- `RuntimeController` オブジェクトがアクティブか確認
- `showGUI` フィールドが `true` になっているか確認

### パラメータの変更が反映されない

- Inspectorで `detector` と `oscManager` の参照が正しく設定されているか確認
- コンソールにエラーメッセージが出ていないか確認

### OSC送信先が変更できない

- 「適用」ボタンを押し忘れていないか確認
- IPアドレスとポート番号が正しい形式か確認

## ヒント

- **調整中のリアルタイムフィードバック**:
  - Scene ViewでGizmosをオンにすると、領域の変更をリアルタイムで確認できます

- **最適な設定の探し方**:
  1. まず深度範囲を調整して、手が検知される範囲を決定
  2. 次に検知ピクセル数を調整して、誤検知を減らす
  3. 領域の位置とサイズを調整して、ジェスチャーを正確に捉える
  4. シーケンス時間を調整して、操作しやすい速度に設定

- **パフォーマンス向上**:
  - レート制限を有効にして、不要なOSC送信を削減
  - 検知ピクセル数を増やして、ノイズによる誤検知を防ぐ

## 関連ファイル

- `Assets/Scripts/RuntimeParameterController.cs` - メインスクリプト
- `Assets/Scripts/OSCManager.cs` - OSC通信管理（プロパティ追加済み）
- `Assets/Scripts/RegionDetector.cs` (NormLowSequenceDetector) - ジェスチャー検知
- `Assets/Orbbec/Scenes/DepthViewer 1.unity` - 実装済みシーン
