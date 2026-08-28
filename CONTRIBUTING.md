# Contributing

コントリビューションを歓迎します。

## 開発手順

1. リポジトリをforkし、作業ブランチを作成します。
2. 変更内容に対応するテストを追加または更新します。
3. Releaseビルドとテストを実行します。
4. 変更理由、確認方法、対象モジュールをPull Requestに記載します。

```powershell
dotnet restore
dotnet build VehicleVision.Tools.SIMComAtCommandTester.sln -c Release --no-restore
dotnet run --project tests/SimComAt.Core.Tests -c Release --no-build
```

## ATコマンドを追加する場合

- 対象モジュールとファームウェアのバージョンを明記してください。
- 参照したSIMCom公式ATコマンドマニュアルの文書名と版を記載してください。
- 複数機種で共通か、特定機種専用かを明確にしてください。
- 設定変更、再起動、回線接続などの副作用があるコマンドには説明を付けてください。
- 認証情報、電話番号、IMSI、ICCIDなど、実機由来の機密情報をログやテストデータへ含めないでください。

## コーディング方針

- 共通処理は`SimComAt.Core`へ置き、GUIとCLIへ重複実装しないでください。
- Nullable参照型を維持し、コンパイラー警告を残さないでください。
- 実機を必要としない処理は疑似`IAtTransport`でテストしてください。
