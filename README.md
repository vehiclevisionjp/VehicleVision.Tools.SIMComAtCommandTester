# SIMCom AT Command Tester

2G、LPWA、LTE、5Gの幅広いSIMComモジュール向けWindows用ATコマンド制御・確認ツールです。SIM7100Jx、SIM7600JC-H、SIM7312G-M.2、SIM8262E-M2を含む主要系列を対象にしています。

シリアル通信とAT応答解析を共通コアに集約し、WPF GUIと対話型CLIの2種類のフロントエンドから同じ処理を利用します。

## 主な機能

- 対象SIMComモジュールのプロファイル選択
- `CGMI` / `CGMM` / `CGMR`による機種・ファームウェア自動判定
- 利用可能なCOMポートの検出
- ボーレートを指定したシリアル接続
- 定義済みATコマンドの実行
- 任意ATコマンドの直接入力（`AT`プレフィックスの自動補完）
- 送受信ログとタイムスタンプの表示
- `OK`、`ERROR`、`+CME ERROR`、`+CMS ERROR`、タイムアウトの判定
- CLIからの基本診断コマンド一括実行
- APN、PDPタイプ、PAP/CHAP認証の設定
- PSアタッチとPDPコンテキストの有効化・無効化
- 認証ユーザー名・パスワードの通信ログ自動マスク
- SMSテキスト送信（GSM/ASCIIおよび日本語UCS2）
- 系列別GNSS電源操作・測位情報取得
- `>`プロンプト、Ctrl+Z終端、秘匿ペイロード送信に対応する共通対話エンジン

## 対応モジュール

| モジュール | プロファイル | 備考 |
|---|---:|---|
| SIM800 / SIM900系 | 対応 | 2G |
| SIM7000系 | 対応 | LTE-M / NB-IoT / GSM |
| SIM7020 / SIM7022系 | 対応 | NB-IoT |
| SIM7070 / SIM7080 / SIM7090系 | 対応 | LTE-M / NB-IoT |
| SIM7100系 | 対応 | LTE / 3G / 2G |
| SIM7500 / SIM7600 / SIM7800系 | 対応 | LTE、SIM7600JC-Hを含む |
| A76xx / SIM76xx系 | 対応 | LTE Cat.1 |
| SIM73xx系 | 対応 | LTE、SIM7312G-M.2を含む |
| SIM82xx / SIM83xx系 | 対応 | 5G、SIM8262E-M2を含む |
| 未知のSIMCom型番 | 汎用対応 | 基本AT・SIM・ネットワーク・PDP操作 |

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

モデル、COMポート、ボーレートを選択して接続します。「自動検出 / 汎用SIMCom」を選ぶと接続後に機種判定を行います。定義済みコマンドをダブルクリックするか、上部の入力欄から任意のATコマンドを送信します。

`APN / PDP`タブではCID、PDPタイプ、APN、認証方式、ユーザー名、パスワードを入力し、`AT+CGDCONT`と`AT+CGAUTH`を設定できます。資格情報は保存せず、通信ログでは`***`に置き換えます。設定後、必要に応じてPSアタッチとPDP有効化を実行してください。

`SMS`タブではGSM/ASCII（最大160文字）またはUCS2（最大70文字）を選択して送信できます。日本語を送る場合はUCS2を選択してください。SMS本文は通信ログへ出力しません。

`GNSS`タブでは選択・検出された系列に応じて、`CGNS`、`CGNSS`、`CGPS`系のコマンドを切り替えて電源操作と測位情報取得を行います。

### CLI

```powershell
dotnet run --project src/SimComAt.Cli
```

起動後にモデルとCOMポートを選び、番号メニューから定義済みコマンド、任意入力、一括診断、APN・認証設定、PSアタッチ、PDP有効化、SMS送信、GNSS操作などを選択します。CLIのパスワード入力は画面に表示されません。

通常の接続設定は `115200 bps / 8-N-1 / フロー制御なし` です。利用するモジュールやファームウェアに応じて変更してください。

## テスト

テストプロジェクトは外部テストフレームワークに依存しない実行形式です。

```powershell
dotnet run --project tests/SimComAt.Core.Tests -c Release
```

プルリクエストと`main`ブランチへのpushでは、GitHub Actionsが.NET 10のReleaseビルドとテストを実行します。

## Visual Studio Code

[C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)とC#拡張を推奨しています。リポジトリをVS Codeで開くと、推奨拡張機能のインストール案内が表示されます。

- `Ctrl+Shift+B`: Debug構成でソリューションをビルド
- `Terminal` → `Run Task`: restore、Releaseビルド、テスト、GUI/CLI起動
- `Run and Debug`: GUI、CLI、コアテストをブレークポイント付きで実行

VS CodeのExplorer、検索、ファイル監視から`bin`、`obj`、`.vs`は除外されます。Gitの追跡対象からも`.gitignore`で除外されています。

## バージョンとリリース

[Semantic Versioning 2.0.0](https://semver.org/)を採用し、最初の公開プレビューを`0.1.0`とします。`v0.1.0`のようなタグをpushすると、GitHub ActionsがWindows x64/Arm64向けの自己完結型GUI・CLI、ZIP、SHA-256チェックサム、リリースノートを生成します。

詳細な公開手順は[Release Guide](docs/RELEASING.md)、変更履歴は[CHANGELOG.md](CHANGELOG.md)を参照してください。

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
- 基本、SIM、ネットワーク、PDP、SMS、GNSS、HTTP/MQTT等の代表的なコマンドを収録していますが、全系列の全コマンドを一律に保証するものではありません。
- HTTP/MQTT、ソケット、ファイルシステム等は系列ごとに構文と動作が異なります。現在は照会・直接実行が中心で、共通対話エンジンを使った専用ワークフローは順次対応です。
- SMSの連結メッセージ、PDUモード、受信管理、配信レポートは未対応です。
- コマンドを追加する際は、対象モジュールとファームウェアに対応したSIMCom公式ATコマンドマニュアルを確認してください。
- 書き込み系ATコマンドはモジュール設定や通信状態を変更する可能性があります。内容を確認してから実行してください。

## コントリビューション

IssueやPull Requestを歓迎します。開発手順と注意事項は[CONTRIBUTING.md](CONTRIBUTING.md)を参照してください。脆弱性に関する報告は[SECURITY.md](SECURITY.md)に従ってください。

## ライセンス

このプロジェクトは[MIT License](LICENSE)で公開されています。
