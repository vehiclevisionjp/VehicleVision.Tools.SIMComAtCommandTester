# Release Guide

## Version policy

Semantic Versioningを使用します。実機対応範囲と公開APIが安定するまでは`0.y.z`とします。

- `0.1.0`: 最初の公開プレビュー
- `0.y.0`: 後方互換とは限らない機能追加、対応系列・ワークフロー追加
- `0.y.z`: 互換性を維持した不具合修正と小規模改善
- `1.0.0`: 対応範囲と公開APIを安定版として宣言するとき

## Create a release

1. 実機スモークテストとGitHub Actions CIの成功を確認します。
2. `Directory.Build.props`の`VersionPrefix`をリリース番号へ更新します。
3. `CHANGELOG.md`の`Unreleased`をバージョン番号と日付へ変更します。
4. 変更をコミットして`main`へpushします。
5. 同じ番号の注釈付きタグを作成してpushします。

```powershell
git tag -a v0.1.0 -m "Release v0.1.0"
git push origin main
git push origin v0.1.0
```

`v*.*.*`タグによりReleaseワークフローが起動します。タグと`VersionPrefix`が一致しない場合は失敗します。

## Release assets

- GUI: Windows x64 / Arm64、自己完結型single-file
- CLI: Windows x64 / Arm64、自己完結型single-file
- `SHA256SUMS.txt`: 全ZIPのSHA-256

`0.x`リリースはGitHub上でprereleaseとして公開されます。`1.0.0`以降は通常リリースになります。
