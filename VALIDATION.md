# Release validation: 1.0.0

- Compiled against installed Risk of Rain 2 assemblies.
- 74 deterministic production combat/projectile assertions, including instant activation through 21.999 seconds and expiry at 22 seconds.
- 40 production surface lifecycle checks, including restoration after interrupted camera scopes.
- 21 physical singularity geometry checks, including fixed world anchoring and visible material-pass selection.
- Compiled binary checks: only Void Fiend, no test config/key/button/network flag, server ownership and charge checks, symmetric messages, two shader bundles and two audio resources.
- Isolated headless native-game probe: content initialization, domain attachment, refusal of an empty-charge cast, earned charged activation, remaining active beyond the old 12-second boundary, and expiry after 22 seconds all passed.

The headless probe uses a separate test-only plugin which is excluded from the release. The artificial respawn produced native inventory/passive-skill and headless input warnings; this was not a clean rendered playthrough. Fresh rendered multiplayer validation remains pending. Visual design has been iterated with the author's supplied gameplay captures.
