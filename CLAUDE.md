# Project rules for Claude

## What this is

OneClickShutdown is a console application that shuts Windows down immediately. The whole program is
one method: it sets the console title, prints two German lines, starts `shutdown /s /t 0` and waits
for a key press. There is no library, no NuGet package to publish and no test project. The
deliverable is an Inno Setup installer that is committed into this repository.

One solution `src/OneClickShutdown.sln` with exactly one project:

- `src/OneClickShutdown/OneClickShutdown.csproj`, `OutputType` `Exe`, target framework
  `net10.0-windows`.

Layout inside `src/OneClickShutdown`:

- `Program.cs`: the static class `Program` with the single method `Main`. That is the entire
  application logic.
- `GlobalUsings.cs`: all usings of the project (`System.Diagnostics`, `System.Reflection`).
- `License.txt`: a byte identical copy of the `License.txt` in the repository root. It is copied to
  the output directory with `CopyToOutputDirectory=Always` and the Inno Setup script points its
  `LicenseFile` at it. Change one of the two and change the other as well.
- `Shutdown.ico`: `ApplicationIcon` of the project and `SetupIconFile` of the installer.

`Setup` folder:

- `OneClickShutdown-Setup.iss`: the Inno Setup script. Encoded UTF-8 **with** BOM.
- `build-setup-files.bat`: deletes every `bin` and `obj` below `src`, publishes self contained for
  `win-x64` into `src/OneClickShutdown/bin/publish` and removes the `*.pdb` files. It does **not**
  compile the installer, that is a separate `ISCC.exe` call.
- `OneClickShutdown-Setup.exe`: the built installer, tracked in git.

Repository root: `README.md` (the only user documentation, uppercase, unlike the sibling
repositories that use `Readme.md`), `Changelog.md`, `License.txt` (MIT), `.gitignore` and
`.gitattributes`. There is no `Updating.md`, no `HowToUse.md`, no screenshots and no `.github`
folder.

## Never run this program

`OneClickShutdown.exe` shuts down the machine it is started on, without a delay and without a
confirmation prompt. Never start the built executable, never start the installed program, and never
let the installer run it through its `[Run]` section (an unattended `/SILENT` install would do
exactly that). "I ran it to see whether it works" costs the user their session and every unsaved
file.

Verify a change in this order instead:

1. `dotnet build src/OneClickShutdown.sln -c Release`, zero warnings.
2. Read the published output, `runtimeconfig.json` and the file version of the exe.
3. Compile the installer to a scratch directory and read its version resource, do not execute it.
4. For behaviour that really needs a run, build a copy of `Program.cs` in a scratch project with
   the `Process.Start` line removed, and run that.

## Build

```powershell
dotnet build src/OneClickShutdown.sln -c Release
```

- Single target framework `net10.0-windows`, no multi-targeting. `RuntimeIdentifiers` is `win-x64`.
- There are no tests. `dotnet test` finds no test project, do not claim a test run.
- All build properties live directly in `src/OneClickShutdown/OneClickShutdown.csproj`. There is
  **no** `Directory.Build.props` in this repository.
- `TreatWarningsAsErrors` is enabled, so every warning breaks the build, NuGet warnings (`NU****`)
  from restore included. A clean build reports zero warnings, keep it that way.
- `NU1803` (HTTP source usage during restore) is the one warning suppressed via `NoWarn`. Fix
  warnings instead of extending that list. `NuGetAudit` and `NuGetAuditMode=all` are on, so a
  vulnerable transitive package fails the build too.
- The only package reference is `GitVersion.MsBuild`. Versions come from it out of the git tags, for
  example `1.0.8-1` for the first commit after tag `1.0.8`. Never edit a version property or an
  assembly version by hand.
- Restore needs nuget.org. If a private feed is configured globally on the machine and answers 404
  for public packages, restore fails with `NU1301`. Then build with an explicit source:
  `dotnet build src/OneClickShutdown.sln --source https://api.nuget.org/v3/index.json`.

## Code conventions

Follow the surrounding code:

- File header comment block with `<copyright file="..." company="Hämmer Electronics">` and a
  `<summary>`, then the file-scoped namespace.
- XML doc comments on every type and every member, private members included, no exceptions.
- `Nullable`, `ImplicitUsings` and `LangVersion latest` are enabled.
- New `using` directives go into `GlobalUsings.cs`, inside the existing `#pragma warning disable
  IDE0065` block, never at the top of a file. The editorconfig requires usings inside the namespace
  (`csharp_using_directive_placement=inside_namespace:warning`), which global usings cannot satisfy,
  that is what the pragma is for. Do not add other pragmas. The comment text in that block is German
  because Visual Studio generated it, leave it alone.
- Fields, properties, methods and events are always accessed with `this.` qualification
  (`dotnet_style_qualification_for_*` at severity `warning`). `Program` is static, so nothing in the
  current code needs it.
- `src/.editorconfig` also enforces braces everywhere, no multiple blank lines, four spaces, CRLF,
  UTF-8, file scoped namespaces, `System` usings sorted first and `IDE0005` as warning. Analyzer
  warnings are fixed, not silenced.

## Known quirks

Do not silently "clean up" these, they are existing behaviour:

- **`OutputType` must stay `Exe`.** This is a console application. With `WinExe` no console is
  allocated on a double click, and then the very first line of `Main` throws
  `IOException: Das Handle ist ungültig` from the `Console.Title` setter, so `Process.Start` is
  never reached and the program does nothing at all. That is not theory, it is the history of this
  repository: `ff9a450` (2021-01-14) fixed `WinExe` to `Exe` on purpose,
  `216a2de "Updated editorconfig."` (2022-10-30) silently reverted it, and the releases 1.0.5, 1.0.6
  and 1.0.7 shipped broken. Fixed again in 1.0.8. If a tool or template offers to "correct"
  `OutputType` for a Windows target, decline.
- **The `-windows` in the target framework.** `net10.0-windows` without `UseWindowsForms` or
  `UseWPF`. It only narrows the API surface to Windows, which is honest here because the program
  calls `shutdown.exe`. It does not make this a GUI application, see the point above.
- **German output in an otherwise English project.** `Program.cs` prints `Windows wird
  heruntergefahren...` and `Beliebige Taste zum Beenden drücken...`. There is no resource file and
  no language switch, unlike the sibling repository `512kbChecker`. Keep the umlauts as real
  characters, the file is UTF-8.
- **`Console.ReadKey` after the shutdown call.** In the normal case the machine goes down and the
  key is never pressed. The call earns its place when the shutdown fails or is blocked by a policy,
  because it keeps the window open long enough to read the message. Do not "optimize" it away.
- **`Console.Title` shows the GitVersion assembly version.** It reads the executing assembly name
  and version, so an untagged build shows something like `OneClickShutdown 1.0.8.0`. That is the
  four part assembly version, not the informational version.
- **The installer is tracked although `.gitignore` excludes `*.exe`.** `Setup/OneClickShutdown-Setup.exe`
  was added with `git add -f` and every new installer needs `git add -f` again. The repository
  therefore grows by the full installer size with every release.
- **The `.iss` needs its BOM.** Inno Setup 6 reads a script as UTF-8 only if a BOM is present,
  otherwise it falls back to the system code page. The file contains exactly one non-ASCII
  character, the `ä` in `Hämmer Electronics`. Without the BOM the installer says
  `HÃ¤mmer Electronics` on any machine whose code page is not Windows-1252. The file was converted
  to UTF-8 with BOM in 1.0.8, keep it that way and keep CRLF.
- **Quick launch icon for Windows 7 and older.** The `quicklaunchicon` task in the `.iss` is limited
  by `OnlyBelowVersion: 0,6.1` and therefore never fires. It is what makes `ISCC.exe` warn about
  `PrivilegesRequired=admin` together with `{userappdata}`. Removing the line would make the compile
  warning free.
- **AppVeyor badge without CI in the repository.** `README.md` links an AppVeyor build that is
  configured outside of this repository. There is no pipeline file here.
- **`src/OneClickShutdown.sln.DotSettings`** is tracked and holds nothing but a ReSharper user
  dictionary (`Beenden`, `Beliebige`, `dr_00FCcken`, `heruntergefahren`, `H_00E4mmer`, `wird`).
  Leave it alone.
- **`.gitattributes` sets `* text=auto`**, every rule of the Visual Studio template below it is
  commented out. Any binary file that must not be normalized needs its own rule.

## Releasing

1. Make the change.
2. Add an entry at the top of `Changelog.md` in the existing format:
   `* **Version 1.0.8.0 (2026-08-15)** : Short description.`
3. Set `MyAppVersion` in `Setup/OneClickShutdown-Setup.iss` to the same four part version. Keep the
   BOM and CRLF.
4. Commit everything.
5. Tag that commit with the plain version number, no `v` prefix (`1.0.8`, `1.0.7`, ...). The
   existing tags are lightweight tags, create new ones the same way.
6. **Then** build the installer, in this order:
   - `Setup/build-setup-files.bat` (publishes self contained and removes the `*.pdb`).
   - `"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" Setup\OneClickShutdown-Setup.iss`.
7. `git add -f Setup/OneClickShutdown-Setup.exe`, commit as `Updated setup.`.
8. Push the commits and the tag.

The tag comes **before** the installer build on purpose. GitVersion takes the assembly version from
the tag, so building first burns a prerelease version like `1.0.8-1+Branch.master.Sha...` into the
shipped executable. The history shows the same order: tag `1.0.7` sits on `7a2130f`, the setup
commit `53fd2eb` comes after it.

The version in `Changelog.md` and in the `.iss` has four parts (`1.0.8.0`), the tag has three
(`1.0.8`).

Note on the batch file: if `NoDefaultCurrentDirectoryInExePath` is set in the environment, cmd does
not search the current directory. Call it as `call .\build-setup-files.bat` after `cd /d` into the
`Setup` folder, because the `cd ..\src` inside the batch is relative to the start directory.

## Git

- **Never amend a commit.** No `git commit --amend`, not for a typo in the message, not to add a
  forgotten file, not even when the commit is still local. Write a follow-up commit instead. The
  release versions come from tags on exact commits, an amended commit leaves its tag pointing at a
  commit that no longer exists in the branch.

## Writing style

- Commit messages are written **in English only**: short, precise subject line, explanatory body
  when needed.
- Code comments and comments in project files such as `.csproj` are **always English**, regardless
  of the language used in the conversation. The two console output strings are the documented
  exception, they are German.
- **No em dashes or en dashes** (`—`, `–`), neither in prose, commit messages, code comments nor
  documentation. Use a regular hyphen, comma, colon, parentheses or a separate sentence.
- German texts (documentation, chat replies) always use real umlauts and ß, never ASCII
  transliterations such as `ae`, `oe`, `ue` or `ss`. Identifiers, file names and configuration keys
  stay unchanged where umlauts are technically undesirable.
