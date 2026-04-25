# Contributing to BeginnerGuard

Contributions are welcome! Please read these guidelines before opening a PR.

## Before You Start

- Check [Issues](../../issues) to see if what you want to fix/add is already being tracked.
- For large changes, open an issue first to discuss the approach before writing code.
- **Always work on a dedicated branch** — never commit directly to `main`.

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

## uMod Approval Requirements

This plugin targets the [uMod platform](https://umod.org). All code contributions must comply with the
[uMod Approval Guide](https://umod.org/guides/development/approval-guide).

The following checklist summarises the requirements. Items marked **Must Fix** are blockers; a PR that fails
them will not be merged.

### Must Fix

- [ ] `[Info("Beginner Guard", "Mazurk4_", "x.y.z")]` attribute is present and valid (formatted title, author username, semver version).
- [ ] `[Description("...")]` attribute is present with a meaningful description (more than a couple of words, does not merely restate "it is a plugin").
- [ ] **No `System.Reflection` usage** anywhere in the plugin.
- [ ] A permissible open-source license is attached (LICENSE file at the project root).
- [ ] The main class name (`BeginnerGuard`) and filename (`BeginnerGuard.cs`) are valid and match each other.
- [ ] All player-facing messages use the **Lang API** (`lang.GetMessage` / `GetMessage`). No hardcoded message strings sent to players.

### Should Fix

- [ ] Configurable parameters are not hard-coded — use the configuration file for values that differ between server installations.
- [ ] Static fields/instances are nullified or cleared in `Unload()` to avoid memory leaks on reload.
- [ ] Dependency checks are performed in `OnServerInitialized()` or later — not in `Init()`.
- [ ] If another plugin is explicitly required, `// Requires: PluginName` is declared at the top of the file.
- [ ] `FindObjectsOfType` (and other slow Unity search functions) is not used.

### May Fix

- [ ] LINQ (`.Where(`, `.Select(`, `.FirstOrDefault(`, etc.) is not used in frequently-called hooks such as `OnTick`.
- [ ] Frequently accessed objects are cached rather than looked up on every call.
- [ ] `SaveData()` is not called on every hook invocation — batch or debounce data saves.

> Run `/umod-check` in Claude Code to get an automated report against these rules before submitting.

## Review Notes

I review PRs as time allows — please be patient.
I may ask questions or request changes before merging, especially for logic-heavy changes.
If a PR goes quiet for a while, feel free to ping via a comment.

## License

By submitting a pull request, you agree that your contribution will be licensed
under the same **MIT** license as this project.
