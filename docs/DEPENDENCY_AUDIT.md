# Dependency audit — 2026-10-17

Package feeds were queried before this prerelease. Both plugins remain net6.0 to match the BepInEx runtime.

| Package | Previous | Release |
| --- | --- | --- |
| Costura.Fody | 5.8.0-alpha0098 / framework 5.7.0 | 6.2.0 |
| GitInfo | 3.3.3 | 3.6.1 |
| JetBrains.Annotations | 2023.2.0 previews | 2026.2.0 |
| YamlDotNet | 13.1.0 / framework 12.3.1 | 18.1.0 |
| Samboy063.Cpp2IL.Core | 2022.1.0-pre-release.19 | 2022.1.0-pre-release.21 |
| Pastel (framework) | 4.1.0 | 5.2.1 |
| BepInEx.IL2CPP.MSBuild (framework) | 2.0.1 | 2.1.0-rc.1 |
| Microsoft.CodeAnalysis.CSharp | 4.0.1 | 4.11.0 |
| Microsoft.CodeAnalysis.Analyzers | 3.3.2 | 3.11.0 |

Retained compatibility constraints:

- BepInEx.Unity.IL2CPP remains 6.0.0-be.735. Testing 6.0.0-be.752 and 6.0.0-be.789 failed in the latest available IL2CPP.MSBuild task: its generator expects CecilMetadataAccess, absent from the selected Il2CppInterop.Generator 1.5.x assemblies. The shipped loader and compile-time loader API remain aligned. A loader migration needs a corresponding build-tool migration and runtime tests.
- Pastel 6.0.1, 7.0.0 and 8.0.0 select .NET Framework assets on net6.0, producing NU1701. Version 5.2.1 provides a native net6.0 target.
- Roslyn 4.11 matches the compiler shipped in SDK 8.0.425. The newer 5.9 analyzer/compiler APIs require a newer compiler host; they are not loaded into the SDK 8 compiler.
- Game references remain 2026.9.29, matching Among Us v19 rather than selecting a mismatched game API.

Validation: both Release builds, 13 networking/utility checks, and 6 asset/YAML checks pass. YAML tests use the real upgraded serializer; graphics tests use Unity boundary stubs. Runtime loader/gameplay validation is still pending. Existing compiler warnings remain.

Graphify produced a partial local map of 884 detected code files and seven documents. Fifty images were not processed after semantic workers hit usage limits. Its diagnostic identified unresolved endpoints and collapsed edges, and some name-based links resolve external methods to test stubs. The graph guided inspection; source code and tests determine correctness. No claim of exhaustive bug coverage or measured FPS improvement is made.
