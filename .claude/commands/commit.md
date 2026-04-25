コミットを作成する。その前に以下を順番に確認・実行すること。

## 1. README の更新チェック

git diff (staged + unstaged) を確認し、以下に該当する変更があれば **README.md と README-JPN.md の両方**を更新してからコミットに含める。

- 設定項目の追加・変更・削除
- コンソールコマンドの追加・変更・削除
- 機能の追加・変更・削除
- バージョン番号の変更

README の更新が不要な例: バグ修正のみ、内部リファクタリング、コメント修正。

## 2. バージョン管理ルール

- バージョンは `BeginnerGuard.cs` の `[Info("Beginner Guard", "Mazurk4_", "x.y.z")]` が唯一の真実の源
- main マージ時に GitHub Actions が自動タグ付けするので、手動で `git tag` しない
- `Co-Authored-By: Claude` 行はコミットメッセージに含めない

## 3. コミット実行

上記を確認・対応したうえでコミットを作成する。
