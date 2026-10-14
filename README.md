# Event Horizon

**Void Fiend's domain expansion.** Tear the entire stage into a blue-violet cosmic void beneath a physical black hole. Deal damage to charge the meter, then press **T** to unfold Event Horizon instantly for **22 seconds**.

[![Event Horizon gameplay preview](https://github.com/WreckzOG/EventHorizon/releases/download/v1.0.0/event-horizon-preview.gif)](https://github.com/WreckzOG/EventHorizon/releases/download/v1.0.0/event-horizon-demo.mp4)

**[Watch / download the gameplay demo](https://github.com/WreckzOG/EventHorizon/releases/download/v1.0.0/event-horizon-demo.mp4)**

The demo was recorded during development and shows the old duration and test button. This release lasts **22 seconds** and has **no test key or free-cast button**.

## Inside the domain

- **Open, stage-wide transformation:** animated nebulae, stars, faded scenery, dark terrain, and a fixed black hole with a glowing accretion disk. No spherical boundary.
- **Incapacitate and pull:** hostiles throughout the stage are incapacitated and slowly drawn toward the black hole. Native collision and immovable-body constraints remain in effect.
- **Stacking fire near the core:** every continuous second within 12 meters of the black hole's surface adds a native Burn stack lasting 4 seconds. Leaving resets the partial interval; existing burns can outlast the domain.
- **Projectile distortion:** supported hostile projectiles curve, slow, and can be absorbed. Friendly shots are preserved; custom projectile movers are conservatively excluded.
- **Singularity Attunement:** Void Fiend gains 20% movement speed and a gentler fall-speed cap while the domain is active.
- **Opening impact and soundtrack:** the supplied impact plays immediately, with music during the domain. The presentation dissolves after expiry and restores the stage.

Your normal skills and corruption system remain available. This package contains **only Void Fiend's Event Horizon**.

## Earn each cast

Deal damage to enemies to fill the on-screen meter. The default requirement is **100 times your level-scaled base damage**. Charge persists on your character master between stages and does not build while the domain is active. Casting spends the entire meter. Press **T** when ready; the activation key is configurable.

## Installation

Install with **r2modman** or **Thunderstore Mod Manager** and launch modded. BepInExPack is the only required mod dependency. For manual installation, copy `plugins/EventHorizon` into `BepInEx/plugins` after installing BepInExPack. Restart the game after installing.

Do not enable this alongside the private **Survivor Domains** development pack, which already contains Event Horizon.

## Configuration

After the first launch, edit `BepInEx/config/wreckzog.eventhorizon.cfg` with the game closed. Configure the activation key, charge requirement, duration, pull, incapacitation, burns, projectile distortion, visuals, and audio. The default activation has **no wind-up delay** and a rapid 0.3-second reveal included within the 22-second active lifetime.

For sound, use **[Audio] Enabled** and **Volume**. The custom soundtrack uses Windows audio output and is independent of the game's music/SFX sliders; gain changes apply on the next cast.

## Compatibility

- Built against the current installed Windows version of Risk of Rain 2. Void Fiend requires the corresponding game content and survivor unlock.
- The full environmental projection uses **Direct3D 11**. Other renderers can fall back to geometric effects without the projected environment.
- Every co-op participant should install the **same version**. Gameplay is server-authoritative; a fresh multiplayer playtest of this standalone release is pending.
- Custom soundtrack playback currently targets **Windows**.

Made with Codex / ChatGPT. The thumbnail is edited artwork based on the in-game black hole; the linked video is gameplay.
