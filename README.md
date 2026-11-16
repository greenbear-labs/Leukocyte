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

## Controls

| Action | Key |
| --- | --- |
| Move | WASD |
| Sprint | Left Shift |
| Jump | Space |
| Fire Enzyme Rifle | Left mouse |
| Reload | R |
| Cytokine Burst (bouncing grenade, 2 charges, 12 s recharge) | G |
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

## Artery Defense rules

- Germs pour in from the **Wound Breach** at the far end of the artery.
- They hunt you when you're close; otherwise they float toward the three pink **Tissue Nodes**. Every germ that touches a node drains **Host Vitals**.
- Survive all five waves to win. You lose if Host Vitals hit 0 or you run out of reinforcements (3 lives).
- A gentle blood current pushes you back toward the Lymph Checkpoint, and red blood cells drift past for flow.

## Germs

| Germ | Behavior | HP |
| --- | --- | --- |
| Swarmer (green bacterium) | Fast, zig-zags in packs, melee bites for 8 | 30 |
| Spitter (violet virus) | Holds 12 to 20 m away, strafes, lobs dodgeable acid globs for 12 | 60 |
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
