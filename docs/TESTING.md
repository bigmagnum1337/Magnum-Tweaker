# Testing Magnum Tweaker

Run from the repository root on Windows.

```powershell
.\pipelines\Build.ps1
.\pipelines\Run-Test.ps1
```

The automated suite checks the catalog, Windows-profile filtering, read-only RP checks, card actions, cancellation and scan errors, preference persistence, backup integrity, typed registry rollback GTA V XML replacement, gaming-preset membership and counts, and embedded guide and audio integrity. Registry cases use a unique volatile current-user key per test and delete it in a finally block. File cases use temporary copies. A green CI result verifies behavior of those paths, not in-game performance or compatibility across all driver versions.

## Manual smoke checklist

1. Launch the portable EXE. Use Russian and English in separate runs. Verify startup, Windows selection, terms with keyboard navigation, the mandatory scan, and entry into the catalog.
2. Check the settings overlay, language and Windows profile switching, sound volume at 0% and an audible level, and the reference guide.
3. Resize to the minimum window size. Scroll the catalog, switch the last category to the first, collapse and expand the sidebar, and toggle several descriptions. Confirm that selected icons, focus, and status labels remain legible and that inactive actions are hidden.
4. Verify the preset counts after a scan. On a disposable Windows test machine, apply one reversible tweak and restore it. With a disposable GTA V Legacy profile, verify that the preset preserves the original GPU name and that restore recovers the original file.
5. If reporting performance, compare identical in-game scenes over repeated runs and report FPS, frame times, or network latency separately from whether the feature writes the expected value.
