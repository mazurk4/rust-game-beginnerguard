# Contributing to BeginnerGuard

Contributions are welcome! Please read these guidelines before opening a PR.

## Before You Start

- Check [Issues](../../issues) to see if what you want to fix/add is already being tracked.
- For large changes, open an issue first to discuss the approach before writing code.

## Pull Request Guidelines

1. **Test your changes** on a local Rust server before submitting.
   Describe what you tested in the PR description (e.g., "tested private profile ban flow", "confirmed Japanese messages appear correctly").

2. **Keep PRs focused** — one fix or feature per PR is easier to review.

3. **Match the existing code style** — same indentation, naming conventions, and comment format as the existing code.

4. **Update documentation** if you add new config keys, permissions, or commands:
   - `README.md` / `README-JPN.md`
   - `config/BeginnerGuard.json.example`
   - `lang/en/BeginnerGuard.json` (and `lang/ja/` if translatable)

5. **Translation PRs are very welcome**, even without any code changes.

## Review Notes

I review PRs as time allows — please be patient.
I may ask questions or request changes before merging, especially for logic-heavy changes.
If a PR goes quiet for a while, feel free to ping via a comment.

## License

By submitting a pull request, you agree that your contribution will be licensed
under the same **MIT** license as this project.
