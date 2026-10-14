# Building Event Horizon

The source targets the installed Risk of Rain 2 managed assemblies, BepInEx 5, Harmony, and .NET Standard 2.1 reference assemblies. Game assemblies are not redistributed.

Run `build.ps1` with `-GameDirectory`, `-BepInExDirectory`, and `-ReferenceDirectory` pointing to your local installation and .NET Standard 2.1 reference directory. The script uses the Windows .NET Framework C# compiler and produces `src/EventHorizon.dll`.

Two precompiled D3D11 shader bundles and the impact/music PCM resources are embedded in the DLL. Their historical `SurvivorDomains.*` resource names are retained because the renderer loads those exact identifiers; there is no dependency on the private Survivor Domains plugin.

`test.ps1` runs deterministic tests against the production combat, projectile, surface, and black-hole mesh code using modeled Unity APIs, plus compiled-assembly release checks. Its Cecil dependency is taken from the BepInEx installation. These checks are not a substitute for rendered multiplayer testing.

Run `package-release.py` with Python and Pillow to produce the Thunderstore ZIP. `prepare-preview.py` uses `imageio-ffmpeg` to generate the looping preview from the separately supplied gameplay video. Media is hosted as GitHub release assets and is not bundled inside the mod package.
