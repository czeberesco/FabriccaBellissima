# R3 runtime assemblies

The binaries in this directory were restored from the following official NuGet packages for the R3 Unity integration:

- `R3` 1.3.1 — MIT license — https://github.com/Cysharp/R3
- `Microsoft.Bcl.TimeProvider` 8.0.0 — MIT license
- `Microsoft.Bcl.AsyncInterfaces` 6.0.0 — MIT license
- `System.Threading.Channels` 8.0.0 — MIT license

They use the `netstandard2.0` target assemblies. The matching R3 Unity adapter is pinned in `Packages/manifest.json` at tag `1.3.1`.
