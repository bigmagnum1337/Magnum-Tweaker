# Magnum Tweaker contributor and agent guide

These rules apply across the repository. Read the README and the affected code before making changes. Preserve user work and keep unrelated changes out of the diff.

## Git workflow

- Treat `main` as the integration branch. Branch from the latest local `main` for independent work. If a task extends an unmerged feature, branch from that feature and identify the dependency in the PR.
- Use short English branch names such as `feat/startup-scan`, `fix/sidebar-scroll`, or `chore/refresh-docs`. If the current task branch already fits the request, continue on it.
- Write concise English imperative commit subjects such as `Fix sidebar scrolling` or `Document the gaming preset`. Group related code, tests, and docs in one reviewable commit. Before committing, inspect the staged diff and run the relevant checks.
- A PR explains the change, includes verification commands and their results, and flags compatibility, UI, or rollback changes. PRs into `main` should pass both **Build** and **Run Test**. Keep merges, pushes, releases, and history rewrites under the maintainer's control.
- Commit source assets needed by the application. Keep EXEs, test results, SDK downloads, workstation files, and personal settings out of Git.

## Application-wide design

- The current design uses a near-black base, white accents, gray secondary text, rounded corners, subtle glass surfaces, and restrained glow. `App.xaml` holds shared WPF resources and templates. Add reusable styles there or in shared resource dictionaries when several windows use them.
- A request to change the design globally applies to **every window in the application**. Review the splash, language and Windows selector, terms, mandatory startup scan, main catalog, settings overlay, and reference guide. Check chrome, button hover and focus, icons, scrollbars, labels, and contrast in each.
- Preserve semantic status colors for applied, pending, unsupported, and failed checks. Pair color with a text label and retain a visible keyboard focus indicator. Use shared assets for the brand and Windows icon.
- Keep animation short and purposeful. Respect the Windows animation preference when adding transitions. Avoid effects or layout churn on the scroll path. Preserve virtualization for long lists, and verify rapid repeated interaction.
- Provide Russian and English for user-facing UI changes. Use `UiLanguage.cs` and existing localized tweak fields. Check that both languages fit at minimum window size and common Windows scaling levels.

## Tweaks and ownership

- Keep system writes inside service code. Treat scanning as read-only. Filter by actual OS, edition, hardware, or available software when the tweak requires it.
- Each action should have a stable ID, concise bilingual description, scope, actual setting, and source when available. Distinguish a verified setting from an expected performance outcome.
- Save the original value before changing a setting. Preserve the first snapshot across retries and verify the setting after applying. Retain the snapshot on restore failure. File replacements should be atomic when possible.
- Use explicit confirmation for options with machine-wide impact. Keep the gaming preset limited to reversible settings that do not overclock hardware. Preserve read-only RP service checks and the GTA V Legacy GPU-preservation behavior.
- Use comments for constraints and reasons that aren't obvious from the code. Prefer descriptive names and small, readable changes. Update the README if categories, counts, packaging, or contributor commands change.

## Verification

- Run `./pipelines/Build.ps1` and `./pipelines/Run-Test.ps1` on Windows with the .NET 8 SDK. GitHub Actions wrappers live in `.github/workflows/` and invoke the scripts in `pipelines/`.
- Cover changed behavior with meaningful normal, edge, and failure cases when relevant. Use temporary directories, isolated volatile HKCU keys, and in-memory doubles in automated tests. Leave live Windows and game settings alone.
- For UI changes, verify both languages and run the relevant items in [the manual smoke checklist](docs/TESTING.md). For real side-effect verification, use a disposable VM or separately authorized test machine and report what was actually verified.
- The distributable is the single EXE in `dist/`. Use `Publish.ps1` or the Build pipeline so previous output is replaced only after a successful publish.
