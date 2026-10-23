DupClean Desktop App — Windows x64

This is a native Windows desktop app project (C# WinForms), not a website or Python script.
The self-contained build creates DupClean.exe and bundles the .NET runtime, so the target
Windows PC does not need Python or a separate .NET installation. The finished app is designed
to work offline.

BUILT-IN LANGUAGES
- English
- 简体中文 (Simplified Chinese)
- Bahasa Melayu
- Español
- Français
- Deutsch
- 日本語
- العربية
- हिन्दी
- Português (Brasil)

BUILD THE APP
The actual Windows EXE must be compiled on a Windows runner. This environment cannot run a
Windows compiler, so the included GitHub Actions workflow builds it for you:
1. Create a GitHub repository.
2. Upload DupClean.csproj, Program.cs, app.manifest, and .github/workflows/build.yml.
3. Open Actions and run "Build DupClean Desktop App" (or push to main).
4. Download the "DupClean-Windows-x64" artifact from the completed workflow.
5. Extract DupClean.exe and double-click it. No Python needed; the app runs offline.

MORE LANGUAGES
The interface ships with common languages above. Adding every language on earth with good-quality
translations needs a localization catalogue. No live translation service is used, because the app
must work offline. Future language packs can be added without changing the file-scanning logic.

SAFETY
- Check the paths in results before deleting.
- Recycle Bin deletion is usually restorable and normally does not free disk space until the bin is emptied.
- Permanent deletion bypasses the Recycle Bin and may not be recoverable.
- Estimated savings assumes one copy per duplicate group is retained.
