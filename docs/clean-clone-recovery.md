# Clean-clone recovery — 2026-10-22

Baseline: `ca4d595` (master). Unity `6000.0.72f1` / revision `b731fd3ae857`.

## Findings and applied corrections

- **High / confirmed:** build settings referenced absent MainMenu and Level scenes under `Assets/Packages/Synty/PolygonWestern/Scenes`. Neither file nor its meta is available. Build list is now intentionally empty until the standalone generator runs. Original scene GUIDs: `a6cfe7158e8640e439e80f83b781e51e`, `b02577a54950ef949a2fcd9d37a1d40c`. Importing the vendor pack does **not** recover the author's custom scene wiring.
- **High / confirmed:** no Packages manifest or lock is tracked, and `git log --all -- '**/manifest.json'` found no recovery candidate in the cloned history. Added a **reconstructed minimal manifest**, not a recovered original. Input System 1.17.0 is pinned (Unity 6000.0 supported; registry metadata confirms dependency on modules.uielements 1.0.0). uGUI 2.0.0 supplies UnityEngine.UI; engine modules cover audio, animation, physics, AI, IMGUI, UI and serialization. No use of Cinemachine, Addressables or AI Navigation package APIs was found. Existing NavMeshAgent uses the AI engine module; original baked navigation data is unavailable.
- The NuGet `packages/` ignore rule was made root-specific and explicit UPM exceptions added. `Assets/Packages/` is explicitly excluded for licensed local imports.
- **High / confirmed:** GraphicsSettings and both quality profiles referenced absent URP assets (`4b83569d67af61e458304325a23e5dfd`, `5e6cbd92db86f4b18aec3ed561671858`); global URP settings `18dc0cd2c080841dea60987a38ce93fa` were absent too. The standalone baseline now uses Built-in rendering. This is an intentional baseline change, not restoration of the original URP setup. Licensed restoration must restore/create URP settings and compatible materials separately.

## Standalone alternative

Run **WesternShooter > Create standalone smoke demo** after package resolution. The explicit editor command creates `Assets/StandaloneGenerated/StandaloneDemo.unity`, four original PCM sine-wave test clips, a plane-like cube floor, capsule player, camera/light and three cube targets. It saves the scene and registers it as the only build scene. It refuses to overwrite an existing generated directory. No automatic scene creation on import, no commercial assets, no reconstruction of missing GUIDs.

The new Standalone composition module references existing Player, Zombies and Audio modules. Existing eight modules and audio source files are unchanged. Movement uses PlayerMovementStateMachine, PlayerMotor and PlayerInputReader. Cube health uses ZombieHealthModel. Existing GameAudioManager receives the tracked GameAudioMixer, its actual snapshots and Music group, with generated clips for AdaptiveMusicController. Voice key simulates a snapshot request; it is not recorded dialogue. The demo's hitscan and controls are isolated smoke-test adapters, not replacements for the original projectile combat or GameFlow.

Controls: WASD/Shift movement; right mouse + left click on a cube applies 25 damage; four hits destroy each target. Static camera. H damages player; F invokes the existing death shortcut. 1/2 peaceful/wanted; 3/4 victory/defeat music; V toggles simulated voice request; Escape pauses movement and requests non-music mute; IMGUI master slider exercises existing mixer parameter handling. No zombie AI, NavMesh, location voice clips, full UI, original animations or automatic three-kill victory integration is claimed.

## Dependency audit

`python tools/audit_unity_references.py` produces `docs/unresolved-asset-guids.json`. It checks serialized GUIDs against local meta files, excludes built-in GUIDs, and does not resolve package GUIDs. Four existing files contain unresolved external object references: the three character/prop prefabs and Readme.asset. Mixer parameter GUIDs are internal identifiers, not missing assets; the scanner checks fileID/GUID object-reference pairs only. The missing original scenes cannot be audited transitively: their serialized content is unavailable. Prefabs include external mesh/material/avatar references and serialized skeleton data; copying them to a new directory does not make them independent or establish redistribution rights.

No existing prefabs, animations, recorded audio or Readme.asset are loaded by the standalone generator. Its only existing non-code asset dependency is GameAudioMixer.mixer, whose referenced objects are internal to that mixer.

## Content and redistribution decisions

| Content | Decision / evidence needed |
| --- | --- |
| Gameplay/audio source and new generator | Retain; README attributes code to repository author. No repository-wide LICENSE exists; this change does not grant a new license. |
| New primitives and generated tones | No imported commercial art or recordings used. Generated locally from code. |
| Existing mixer | Retain integration settings; no embedded audio recording. Authorship/license still belongs with project provenance. |
| Synty / wooden UI source assets | Do not import into Git. Use locally only under the applicable purchase/store/license terms. |
| Character/prop prefabs and animation clips already tracked | Origin and permission unresolved. Some prefabs serialize rig data and external art references. Retained to avoid destructive loss; **not certified for public redistribution**. |
| Existing music, SFX and voices | Credits/name alone do not establish source redistribution rights. Retained unchanged and unused by standalone; require source URL, author, exact license/version, purchase evidence where relevant, and permission for raw-file sharing. |

Synty's current one-time EULA prohibits sharing asset source outside the team; it explicitly excludes Unity Asset Store purchases, so the actual purchase channel and terms must be checked. Do not infer that owning a pack permits publishing raw files. Links: https://syntystore.com/pages/one-time-purchase-licence and https://syntystore.com/pages/licences-overview . No history rewrite or deletion of existing media was performed. If any tracked files prove restricted, remove them from the public tree and assess history cleanup separately. The repository as a whole is **not cleared for redistribution** by this patch.

## Validation status

**Checked statically:** project version; all eight original asmdef boundaries; external namespaces; missing scene paths; missing URP GUIDs; package history; registry metadata for Input System 1.17.0; mixer snapshot/group names; JSON parsing; GUID inventory; generated asset/meta coverage; ignore behavior; whitespace diff check. No Unity executable is installed here.

**Pending in Unity 6000.0.72f1:** resolve manifest (including editor-provided uGUI and engine modules), compile all assemblies, generate the scene, inspect console, verify movement/grounding/cube damage, mixer snapshots/crossfades/slider/voice release, pause behavior, exit/reenter Play Mode, then build and launch a desktop Player. Commit the **Unity-generated `Packages/packages-lock.json`** after successful resolution; no fabricated lock is supplied. Exact transitive reproducibility is pending until that lock is generated and a second clean clone resolves it successfully.

Batch preparation (not run here):

```sh
Unity -batchmode -quit -projectPath /absolute/path/WesternShooter/WesternShooter -executeMethod WesternShooter.Standalone.Editor.StandaloneSceneBuilder.Generate -logFile standalone-generation.log
```

Run against a disposable clean clone. Generated scene/assets may be committed after Unity QA with their generated meta files. Until then the repository supplies an explicit generation workflow, not a prevalidated playable build.

## Restoring the original privately

Recover the author's actual MainMenu/Level scene files and corresponding meta files from their working copy/backup. Obtain licensed packs through their purchase channels. Audit scene dependency closure locally, including embedded meshes, materials, textures, rig/animation data, UI and navigation. Move only confirmed author-owned scene content to a tracked scene directory, retain original metas where safe, and replace restricted embedded content before publication. Restore compatible URP package/assets based on the original local manifest/lock, not guessed scene GUIDs. Check scene names/indices used by MainMenuFlowController and GameManager before registering the original flow. Missing scenes, original package versions and license evidence cannot be recovered from the published files alone.
