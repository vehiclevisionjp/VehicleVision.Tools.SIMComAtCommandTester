# SIMCom AT Command Tester

SIM7100Jx、SIM7600JC-H、SIM7312G-M.2、SIM8262E-M2向けの、Windows用ATコマンド確認ツールです。

シリアル通信とAT応答解析を共通コアに集約し、WPF GUIと対話型CLIの2種類のフロントエンドから同じ処理を利用します。

## 主な機能

- 対象SIMComモジュールのプロファイル選択
- 利用可能なCOMポートの検出
- ボーレートを指定したシリアル接続
- 定義済みATコマンドの実行
- 任意ATコマンドの直接入力（`AT`プレフィックスの自動補完）
- 送受信ログとタイムスタンプの表示
- `OK`、`ERROR`、`+CME ERROR`、`+CMS ERROR`、タイムアウトの判定
- CLIからの基本診断コマンド一括実行

## 対応モジュール

| モジュール | プロファイル | 備考 |
|---|---:|---|
| SIM7100Jx | 対応 | 共通照会コマンド |
| SIM7600JC-H | 対応 | 共通照会コマンド |
| SIM7312G-M.2 | 対応 | 共通照会コマンド |
| SIM8262E-M2 | 対応 | 共通照会コマンド |

疎通、端末情報、SIM状態、LTE/5G登録状態、電波強度、PDPコンテキストなど、各機種で共通して利用しやすい照会コマンドを収録しています。

## 動作環境

- Windows 10またはWindows 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- 対象モジュール用USBドライバー
- SIMComモジュールのATコマンド用COMポート

実行ファイルをビルドせず利用する場合も、.NET 10 Desktop Runtimeが必要です。デバイスマネージャーでATコマンド用ポートを確認してください。診断、NMEA、モデムなどの別ポートを選ぶと応答しません。

## ビルド

リポジトリのルートで次を実行します。

```powershell
dotnet restore
dotnet build VehicleVision.Tools.SIMComAtCommandTester.sln -c Release --no-restore
```

### GUI

```powershell
dotnet run --project src/SimComAt.Gui
```

モデル、COMポート、ボーレートを選択して接続します。定義済みコマンドをダブルクリックするか、上部の入力欄から任意のATコマンドを送信します。

### CLI

```powershell
dotnet run --project src/SimComAt.Cli
```

起動後にモデルとCOMポートを選び、番号メニューから定義済みコマンド、任意入力、一括診断のいずれかを選択します。

通常の接続設定は `115200 bps / 8-N-1 / フロー制御なし` です。利用するモジュールやファームウェアに応じて変更してください。

## テスト

テストプロジェクトは外部テストフレームワークに依存しない実行形式です。

```powershell
dotnet run --project tests/SimComAt.Core.Tests -c Release
```

プルリクエストと`main`ブランチへのpushでは、GitHub Actionsが.NET 10のReleaseビルドとテストを実行します。

## プロジェクト構成

```text
src/
  SimComAt.Core/       シリアル通信、AT応答解析、機種プロファイル
  SimComAt.Gui/        Windows WPF GUI
  SimComAt.Cli/        対話型CLI
tests/
  SimComAt.Core.Tests/ 共通コアの動作確認
```

機種固有コマンドは `src/SimComAt.Core/ModemProfiles.cs` に追加できます。USBシリアル以外の通信方式へ差し替える場合は `IAtTransport` を実装してください。

## 制限事項

- 実機とファームウェアの組み合わせによる動作確認が必要です。
- APN設定、ソケット、HTTP/MQTT、GNSS、音声、SMS、SIM8262E-M2固有の5Gコマンドは未収録です。
- コマンドを追加する際は、対象モジュールとファームウェアに対応したSIMCom公式ATコマンドマニュアルを確認してください。
- 書き込み系ATコマンドはモジュール設定や通信状態を変更する可能性があります。内容を確認してから実行してください。

## コントリビューション

IssueやPull Requestを歓迎します。開発手順と注意事項は[CONTRIBUTING.md](CONTRIBUTING.md)を参照してください。脆弱性に関する報告は[SECURITY.md](SECURITY.md)に従ってください。

## ライセンス

このプロジェクトは[MIT License](LICENSE)で公開されています。
