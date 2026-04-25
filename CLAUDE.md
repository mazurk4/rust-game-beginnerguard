# BeginnerGuard — Claude Code 作業ガイド

## コミット前のチェックリスト

コミットを作成する前に、以下を確認すること。

### README の更新
変更内容が以下のいずれかに該当する場合は、**README.md と README-JPN.md の両方**を更新してからコミットする。

- 設定項目の追加・変更・削除
- コンソールコマンドの追加・変更・削除
- 機能の追加・変更・削除
- バージョン番号の変更

更新が不要な例: バグ修正のみ、内部リファクタリング、コメント修正。

### バージョン管理
- バージョンは `BeginnerGuard.cs` の `[Info("Beginner Guard", "Mazurk4_", "x.y.z")]` が唯一の真実の源
- main へのマージ時に GitHub Actions (`.github/workflows/tag-release.yml`) が `[Info]` バージョンを読み取り、自動でタグ付けする
- 手動で `git tag` しない
- `Co-Authored-By: Claude` 行はコミットメッセージに含めない
