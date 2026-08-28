# SIMCom AT Command Tester

SIM7100Jx、SIM7600JC-H、SIM7312G-M.2、SIM8262E-M2向けのATコマンド確認ツールです。通信・応答解析を共通コアに置き、WPF GUIとメニュー式CLIから同じ処理を利用します。

## 構成

- `SimComAt.Core`: シリアル通信、AT応答解析、機種プロファイル
- `SimComAt.Gui`: Windows WPF GUI
- `SimComAt.Cli`: 対話メニュー式CLI
- `SimComAt.Core.Tests`: 外部テストフレームワーク不要のコア動作確認

## 必要環境

- Windows 10/11
- .NET 8 SDK（実行だけなら .NET 8 Desktop Runtime）
- SIMComモジュールのATコマンド用COMポート

USBドライバーを導入し、デバイスマネージャーでATコマンド用ポートを確認してください。診断、NMEA、モデム等の別ポートを選ぶと応答しません。

## ビルドと実行

```powershell
dotnet restore
dotnet build -c Release
dotnet run --project src/SimComAt.Gui
dotnet run --project src/SimComAt.Cli
dotnet run --project tests/SimComAt.Core.Tests
```

通常は `115200 / 8-N-1 / フロー制御なし` で接続します。必要に応じて画面またはCLIでボーレートを変更してください。

## 使い方

GUIではモデル、COMポート、ボーレートを選択して接続します。左の定義済みコマンドをダブルクリックするか、上部に任意のATコマンドを入力して送信します。`AT`を省略して`+CSQ`のように入力した場合は自動で補完されます。

CLIでは起動後にモデルとポートを選択し、定義済みコマンド、任意入力、一括診断のいずれかを番号で選びます。

## 対応範囲と注意

初期実装には、疎通、端末情報、SIM、LTE/5G登録状態、電波強度、PDPコンテキスト等の4機種で共通して利用しやすい照会コマンドを収録しています。APN設定、ソケット、HTTP/MQTT、GNSS、音声、SMS、およびSIM8262E-M2固有の5Gコマンドは、ファームウェア版による差異があるため未収録です。実運用で追加する際は、対象モジュールとファームウェアに対応したSIMCom公式ATコマンドマニュアルで確認してください。

機種固有コマンドは `ModemProfiles.cs` のプロファイルに追加できます。通信方式をUSB以外へ差し替える場合は `IAtTransport` を実装します。
