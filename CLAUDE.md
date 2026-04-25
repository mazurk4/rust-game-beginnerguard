# BeginnerGuard — Claude Instructions

## Branch Policy

**ファイルに変更を加える前に必ず新しいブランチを作成すること。**

`main` や既存のフィーチャーブランチへの直接コミットは禁止。

ブランチ名の形式: `type/short-description`

| type | 用途 |
|------|------|
| `fix/` | バグ修正 |
| `feat/` | 新機能 |
| `docs/` | ドキュメントのみの変更 |
| `refactor/` | リファクタリング |
| `ci/` | CI / GitHub Actions の変更 |

## Commit Rules

コミット前に [.claude/commands/commit.md](.claude/commands/commit.md) のチェックリストに従うこと。

## uMod Compliance

`BeginnerGuard.cs` に変更を加えた場合は、コミット前に `/umod-check` を実行して
[uMod Approval Guide](https://umod.org/guides/development/approval-guide) への準拠を確認すること。

## Version

バージョンの唯一の真実の源は `BeginnerGuard.cs` の `[Info(...)]` 属性。
`main` マージ時に GitHub Actions が自動タグ付けするので、手動で `git tag` しないこと。
