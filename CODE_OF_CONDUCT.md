# Code of Conduct

## Our Pledge

We are committed to making participation in this project a harassment-free experience for everyone,
regardless of age, body size, disability, ethnicity, gender identity and expression, level of experience,
nationality, personal appearance, race, religion, or sexual identity and orientation.

## Our Standards

### Community Behavior

Expected behavior:

- Using welcoming and inclusive language
- Being respectful of differing viewpoints and experiences
- Gracefully accepting constructive criticism
- Focusing on what is best for the community
- Showing empathy towards other community members

Unacceptable behavior:

- Trolling, insulting or derogatory comments, and personal or political attacks
- Public or private harassment
- Publishing others' private information (physical or electronic address) without explicit permission
- Other conduct which could reasonably be considered inappropriate in a professional setting

### Technical Standards

All code contributions must comply with the **[uMod Approval Guidelines](https://umod.org/guides/development/approval-guide)**.

This is the shared technical baseline for all code submitted to this project, whether or not the
contributor intends to submit directly to uMod. The requirements cover:

- Valid plugin attributes (`[Info]`, `[Description]`)
- Lang API usage for all player-facing messages
- No `System.Reflection` usage
- Permissible open-source license
- Performance-conscious code (avoid slow lookups, excessive allocations, frequent saves)
- Proper cleanup of static state on plugin `Unload()`

See [CONTRIBUTING.md](CONTRIBUTING.md) for the full checklist and how to run the automated check.

## Enforcement

Instances of abusive, harassing, or otherwise unacceptable behavior may be reported by opening an issue
or contacting the maintainer directly. All complaints will be reviewed and investigated and will result in
a response that is deemed necessary and appropriate to the circumstances.

The project maintainer is obligated to maintain confidentiality with regard to the reporter of an incident.

## Attribution

This Code of Conduct is adapted from the [Contributor Covenant](https://www.contributor-covenant.org), version 2.1,
available at https://www.contributor-covenant.org/version/2/1/code_of_conduct.html.
