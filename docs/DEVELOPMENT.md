# Development Guide

この文書は、SIMCom AT Command Testerをソースからビルド、テスト、拡張する開発者向けです。利用方法は[README](../README.md)を参照してください。

## 前提環境

- Windows 10またはWindows 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PowerShell
- 任意: Visual Studio Code、C# Dev Kit、C#拡張
- 実機確認時: 対象モジュール用USBドライバーとATコマンド用COMポート

## セットアップとビルド

リポジトリのルートで実行します。

```powershell
dotnet restore
dotnet build VehicleVision.Tools.SIMComAtCommandTester.sln -c Release --no-restore
```

GUIとCLIは次のコマンドで起動できます。

```powershell
dotnet run --project src/SimComAt.Gui
dotnet run --project src/SimComAt.Cli
```

## テスト

テストプロジェクトは外部テストフレームワークに依存しない実行形式です。

```powershell
dotnet run --project tests/SimComAt.Core.Tests -c Release
```

ビルド済みの場合は`--no-build`を追加できます。Pull Requestと`main`ブランチへのpushでは、GitHub Actionsが.NET 10のReleaseビルドとテストを実行します。

## Visual Studio Code

推奨拡張機能はワークスペースを開いた際に案内されます。

- `Ctrl+Shift+B`: Debug構成でソリューションをビルド
- `Terminal` → `Run Task`: restore、Releaseビルド、テスト、GUI/CLI起動
- `Run and Debug`: GUI、CLI、コアテストをデバッガーから起動

Explorer、検索、ファイル監視では`bin`、`obj`、`.vs`を除外しています。これらは`.gitignore`でも追跡対象外です。

## プロジェクト構成

```text
src/
  SimComAt.Core/       シリアル通信、AT応答解析、機種プロファイル、各種サービス
  SimComAt.Gui/        Windows WPF GUI
  SimComAt.Cli/        対話型CLI
tests/
  SimComAt.Core.Tests/ 共通コアの動作確認
```

GUIとCLIは`SimComAt.Core`を共有します。通信、応答解析、機種差分、ワークフローをフロントエンドへ重複実装しないでください。

## 主な拡張ポイント

- `ModemProfiles.cs`: 対応型番、系列判定、系列固有機能
- `AtCommandCatalog.cs`: 定義済みATコマンド
- `IAtTransport.cs`: シリアル以外を含む通信方式の抽象化
- `AtCommandClient.cs`: AT送受信、応答判定、対話処理
- 各サービス: APN/PDP、SMS、GNSS、HTTP、MQTTの再利用可能なワークフロー

新しいモジュール系列を追加する場合は、自動判定に使う型番、対応機能、系列固有コマンドをプロファイルへ追加し、未知型番が汎用プロファイルへフォールバックする動作を維持してください。

ATコマンドを追加する際は、対象モジュールとファームウェアに対応するSIMCom公式マニュアルを確認してください。複数系列で共通化できない構文は、プロファイルまたはサービス側で明示的に分岐します。

## 実装上の方針

- Nullable参照型を維持し、コンパイラー警告を残さない
- 実機を必要としない処理は疑似`IAtTransport`でテストする
- 認証情報やペイロードをログへ出力しない
- テストデータへ電話番号、IMEI、IMSI、ICCIDなど実機由来の情報を含めない
- 設定変更、再起動、回線接続を行う操作は副作用を明示する

## バージョンとリリース

Semantic Versioningを採用し、初期の公開版は`0.y.z`とします。タグからWindows x64/Arm64向けの自己完結型GUI・CLI、ZIP、SHA-256チェックサムを生成します。

具体的な公開手順は[Release Guide](RELEASING.md)、リリース内容は[CHANGELOG](../CHANGELOG.md)を参照してください。
