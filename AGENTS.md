# AGENTS.md

Downloader for Chinese national textbook PDFs from `*.ykt.cbern.com.cn`. .NET 10 solution, no tests, no CI, no README.

## Layout
- `TextbookSpiderBase/` — class library. Folder name is `...Base` but its **`RootNamespace` is `TextbookSpider`** (`TextbookSpiderBase.csproj:7`). All core code lives in namespace `TextbookSpider`.
- `TextbookSpiderCli/` — console frontend, namespace `TextbookSpiderCli`. Interactive folder picker + downloader.
- `TextbookSpiderWpf/` — WinExe/WPF frontend (`net10.0-windows`), namespace `TextbookSpiderWpf`. Windows-only; cannot run headless.

## Commands
- Build: `dotnet build TextbookSpider.sln` (requires .NET 10 SDK; `net10.0` is intentional).
- Run CLI: `dotnet run --project TextbookSpiderCli`.
- Run WPF: `dotnet run --project TextbookSpiderWpf` (Windows GUI only).
- No lint/format/test tooling is configured; do not invent one.

## Conventions
- Functional error handling via `CSharpFunctionalExtensions`: `Result`/`Maybe`, `Result.Try`, `.Bind`, `.Match`, `.GetValueOrDefault`. New parsing/IO code should return `Result` and follow this style rather than throwing.
- MVVM uses `CommunityToolkit.Mvvm` source generators: `[ObservableProperty]` on `_camelCase` fields, `[RelayCommand]` on private methods. `MainViewmodel.cs` actually contains both `FoldersViewmodel` and `MainViewmodel` — filename is misleading.
- `DownloadMethods` is a `record struct` built by `DownloadMethods.ConstructDownloadMethods(auth)`; it carries the `GetStringAsync`/`GetStreamAsync` delegates. All HTTP goes through it; auth is sent as the `x-nd-auth` header. Endpoints and the header name are hardcoded.
- Auth is a required token: CLI prompts for it; WPF builds `DownloadMethods` with `string.Empty` (catalog fetch works unauthenticated, downloads will fail without it).

## Gotchas
- CLI writes PDFs to the current working directory; WPF writes under `./Textbooks` (`MainViewmodel.cs:53`). Both log to `log.txt` in the working directory.
- `Parse.ParseBookInfos` scans only `part_101.json`..`part_103.json` (`Fetch.cs:30`); new catalog parts need code changes.
- `Folder.MapTree` filters books by tag-subset match against the tag tree; titles with no `global_title.zh-CN` are dropped.
