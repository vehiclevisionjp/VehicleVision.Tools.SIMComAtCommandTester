# SIMCom AT Command Tester

2G、LPWA、LTE、5Gの幅広いSIMComモジュールをWindowsから操作・確認するためのATコマンドツールです。SIM7100Jx、SIM7600JC-H、SIM7312G-M.2、SIM8262E-M2を含む主要系列を対象に、GUIとメニュー式CLIを提供します。

## 主な機能

- モジュールの自動判定と系列別プロファイル
- COMポート検出とシリアル接続
- 定義済みコマンドと任意ATコマンドの実行
- 応答判定、タイムアウト、タイムスタンプ付き通信ログ
- 基本診断、SIM・登録状態・電波強度の確認
- APN、PDPタイプ、PAP/CHAP認証、PSアタッチの設定
- SMS送信（GSM/ASCIIおよび日本語UCS2）
- 系列別GNSS電源操作と測位情報取得
- HTTP GET/POST
- MQTT接続、publish、切断
- パスワード、SMS本文、HTTP/MQTTペイロードのログマスク

## 対応モジュール

| モジュール | 対応 | 備考 |
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

実際に利用できるコマンドはモジュールとファームウェアによって異なります。

## ダウンロード

[GitHub Releases](../../releases/latest)から、使用するCPUに合ったGUIまたはCLIのZIPをダウンロードして展開してください。

- `SIMComAt-GUI-v<version>-win-x64.zip`: 一般的なWindows PC向けGUI
- `SIMComAt-CLI-v<version>-win-x64.zip`: 一般的なWindows PC向けCLI
- `SIMComAt-GUI-v<version>-win-arm64.zip`: Windows on Arm向けGUI
- `SIMComAt-CLI-v<version>-win-arm64.zip`: Windows on Arm向けCLI

Release版は自己完結型のため、.NET Runtimeの別途インストールは不要です。公開前のソースから実行する場合は[開発者ガイド](docs/DEVELOPMENT.md)を参照してください。

## 動作環境

- Windows 10またはWindows 11
- 対象モジュール用USBドライバー
- SIMComモジュールのATコマンド用COMポート

デバイスマネージャーでATコマンド用ポートを確認してください。診断、NMEA、モデムなどの別ポートを選ぶと応答しません。

## クイックスタート

1. ReleaseのZIPを展開し、GUIまたはCLIの実行ファイルを起動します。
2. モジュールを接続し、ATコマンド用COMポートを確認します。
3. モデル、COMポート、ボーレートを選択します。機種が不明な場合は「自動検出 / 汎用SIMCom」を選びます。
4. 接続後、基本診断または`AT`を実行して応答を確認します。

標準的な接続設定は`115200 bps / 8-N-1 / フロー制御なし`です。モジュールやファームウェアの設定に応じて変更してください。

## GUIの使い方

接続後、定義済みコマンドをダブルクリックするか、入力欄から任意のATコマンドを送信できます。

- `APN / PDP`: CID、PDPタイプ、APN、認証方式、ユーザー名、パスワードを設定
- `SMS`: GSM/ASCIIまたはUCS2を選んでテキスト送信
- `GNSS`: 電源操作と測位情報取得
- `HTTP`: GET/POSTとレスポンス取得
- `MQTT`: 接続、publish、切断

日本語SMSにはUCS2を使用してください。目安はGSM/ASCIIが160文字、UCS2が70文字です。

## CLIの使い方

起動後にモデルとCOMポートを選び、番号メニューから操作します。定義済みコマンド、任意入力、一括診断、APN・認証設定、SMS、GNSS、HTTP、MQTTなどを利用できます。パスワード入力は画面に表示されません。

## 注意事項と制限

- 実機とファームウェアの組み合わせごとに動作確認が必要です。
- 全系列の全ATコマンドを一律に保証するものではありません。未収録のコマンドは任意AT入力から実行できます。
- HTTP/MQTTはSIMComの`HTTP*`、`CMQTT*`コマンド群を対象とし、ファームウェアによって未搭載または構文が異なる場合があります。
- TCP/UDPソケット、FTP、ファイルシステムの専用画面・メニューは未対応です。
- SMSの連結メッセージ、PDUモード、受信管理、配信レポートは未対応です。
- 書き込み系ATコマンドはモジュール設定や通信状態を変更します。内容を確認してから実行してください。
- Issueへログを添付する前に、電話番号、IMEI、IMSI、ICCID、APNなど実機由来の情報を除去してください。

## ドキュメント

- [変更履歴](CHANGELOG.md)
- [開発者ガイド](docs/DEVELOPMENT.md)
- [コントリビューションガイド](CONTRIBUTING.md)
- [セキュリティポリシー](SECURITY.md)

## ライセンス

[MIT License](LICENSE)
