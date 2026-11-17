# Leukocyte

A first-person shooter where you play a humanoid white blood cell in the Host's Immune Corps, defending a living body from monstrous germs. Built in Unity.

This branch holds the **greybox prototype**. A main menu offers two modes, and everything is built from code at runtime out of Unity primitives, so there are no art assets yet.

- **Artery Defense:** solo. Hold the femoral artery against five escalating germ waves.
- **Tissue War:** a squad battle on the Alveoli Front, a wide lung battlefield. Six outposts start split 3 and 3 between the Immune Corps and the infection. Capture them with your AI troopers and drain the enemy's reinforcements.

## Running it

1. Install Unity **2022.3 LTS or Unity 6** through Unity Hub.
2. In Unity Hub choose **Add > Add project from disk** and pick this folder.
3. On first open the project creates `Assets/Scenes/FemoralArtery.unity` and opens it. If it doesn't, use the menu **Leukocyte > Create Femoral Artery Scene**.
4. Press **Play**, then click the Game view to deploy.

The prototype uses the classic Input Manager. If you see input errors, set **Edit > Project Settings > Player > Active Input Handling** to **Input Manager (Old)** or **Both**.

You can also drop the `GameBootstrap` component on an empty GameObject in any empty scene and press Play.

## Web, phones and the Green Bear Arcade

**Leukocyte > Build WebGL (for the Arcade)** (or `-executeMethod Leukocyte.EditorTools.BuildScript.BuildWebGL` in batch mode) writes `Builds/WebGL/Build/WebGL.*`. Copy the four files into `games/leukocyte/Build` in the [Arcade](https://github.com/greenbear-labs/Arcade) repo.

On phones and tablets the game switches to touch controls: drag on the left to move (push to the edge to sprint), drag on the right to look, and hold FIRE while dragging to aim. There are buttons for jump, reload, the class ability, grab, squad and zoom, plus a pause button. All screens scale to fit the display, and shadows and unit counts are reduced on mobile.

## Controls

| Action | Key |
| --- | --- |
| Move | WASD |
| Sprint | Left Shift |
| Jump | Space |
| Fire Enzyme Rifle | Left mouse |
| Reload | R |
| Class ability: Cytokine Burst grenade, or Antibody Tag for the T-Cell | G |
| Zoom (T-Cell) | Right mouse |
| Squad order: call nearby troopers to follow, or send them back to the objectives (Tissue War) | Q |
| Change class (pause or respawn screen) | Tab or click |
| Phagocyte Grab (engulf a weakened germ to heal 30, or punch a healthy one) | F |
| Release mouse | Esc (click to recapture) |
| Choose spawn point (Tissue War, while down) | 1-9 |
| Main menu (paused or after a match) | M |
| Play again after win/loss | Enter |

## Tissue War rules

- Each side starts with 150 reinforcement tickets. Every death costs its side one ticket.
- Stand in an outpost's ring to capture it. More troops capture faster (up to 3x). Nothing moves while both sides are inside. A post goes neutral first, then flips.
- The side holding more outposts drains the other side's tickets every 3 seconds. Holding all six drains them fast.
- A side loses when its tickets reach 0.
- Up to 11 allied troopers and 18 germs fight at once. They push the nearest outposts they don't hold, and about a quarter of them stay back to defend.
- When you die, pick your HQ or any outpost you hold that isn't contested. By default you spawn at the front line.
- Held outposts heal their own side (HQs heal twice as fast). Wounded troopers fall back to heal, then rejoin.
- Press Q to have up to 4 nearby troopers follow you in formation; press it again to send them back to the objectives.

## Artery Defense rules

- Germs pour in from the **Wound Breach** at the far end of the artery.
- They hunt you when you're close; otherwise they float toward the three pink **Tissue Nodes**. Every germ that touches a node drains **Host Vitals**.
- Survive all six waves, including the Spore Mother boss wave, to win. You lose if Host Vitals hit 0 or you run out of reinforcements (3 lives).
- A gentle blood current pushes you back toward the Lymph Checkpoint, and red blood cells drift past for flow.

## Classes

Pick on the main menu, or press Tab on the pause or respawn screen. A change applies right away if you haven't fired or been hit since deploying, otherwise at your next deploy.

| Class | Role | HP | Speed | Weapon | G ability |
| --- | --- | --- | --- | --- | --- |
| Neutrophil | Assault | 100 | 105% | Enzyme Rifle: automatic, 12 dmg, 30 rounds | Cytokine Burst x3 |
| Macrophage | Heavy | 170 | 85% | Lysosome Scattergun: 9 pellets x 10 dmg, 6 shells | Cytokine Burst x2, bigger Grab heals |
| T-Cell | Marksman | 85 | 110% | Perforin Lance: 55 dmg, 8 rounds, zoom | Antibody Tag x2: marks enemies in a cone for 10 s; they take +50% damage and show through walls |

## Germs

| Germ | Behavior | HP |
| --- | --- | --- |
| Swarmer (green bacterium) | Fast, zig-zags in packs, melee bites for 8 | 30 |
| Spitter (violet virus) | Holds 12 to 20 m away, strafes, lobs dodgeable acid globs for 12 | 60 |
| Spore Mother (boss) | Huge drifting pod. Births Swarmers, fires 5-glob spore volleys, and shockwaves anyone close once below half health. Final wave of Artery Defense; in Tissue War she leaves the hive when the infection drops to 60 tickets, and killing her costs them 25 more | 2200 / 3000 |
| Brute (amber fungal hulk) | Slow tank. Swells up, then slams the ground for up to 30 in a 4.5 m radius | 400 |

## Feedback and audio

All sounds are synthesized in code (`Core/Sfx.cs`) as placeholders. The HUD has a kill feed with score, a red marker showing where damage came from, and ability cooldowns. The camera shakes on hits and explosions and widens slightly while sprinting.

## Code layout

```
Assets/Scripts/
  Core/     GameBootstrap (main menu), LevelBuilder (artery map), Combatant (teams), GameManager, WaveManager, TissueNode, Health, helpers
  Player/   PlayerController (CharacterController FPS), EnzymeRifle (hitscan weapon)
  Germs/    Germ base AI, Swarmer, Spitter, AcidGlob, GermFactory (greybox models)
  Conquest/ ConquestBuilder (Alveoli Front map), ConquestManager (tickets, spawns, objectives), Outpost, ImmuneBot (allied AI)
  UI/       HudBase plus the Artery Defense and Tissue War HUDs (IMGUI)
  Editor/   First-open scene setup and the Leukocyte menu
```

Tuning lives in public fields: wave sizes in `WaveManager.waves`, weapon stats on `EnzymeRifle`, germ stats in each germ's `Awake`.

## Next up

Abilities (Phagocyte Grab, Antibody Tag, Cytokine Burst), the Burrower and Spore Mother germs, AI squadmates, then an art and audio pass.
