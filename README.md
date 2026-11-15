# Leukocyte

A first-person shooter where you play a humanoid white blood cell in the Host's Immune Corps, defending a living body from monstrous germs. Built in Unity.

This branch holds the **greybox prototype**: one level (the Femoral Artery), one weapon, two germ types and a five-wave defense. Everything is built from code at runtime out of Unity primitives, so there are no art assets yet.

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
| Release mouse | Esc (click to recapture) |
| Redeploy after win/loss | Enter |

## How a match works

- Germs pour in from the **Wound Breach** at the far end of the artery.
- They hunt you when you're close; otherwise they float toward the three pink **Tissue Nodes**. Every germ that touches a node drains **Host Vitals**.
- Survive all five waves to win. You lose if Host Vitals hit 0 or you run out of reinforcements (3 lives).
- A gentle blood current pushes you back toward the Lymph Checkpoint, and red blood cells drift past for flow.

## Germs

| Germ | Behavior | HP |
| --- | --- | --- |
| Swarmer (green bacterium) | Fast, zig-zags in packs, melee bites for 8 | 30 |
| Spitter (violet virus) | Holds 12 to 20 m away, strafes, lobs dodgeable acid globs for 12 | 60 |

## Code layout

```
Assets/Scripts/
  Core/     GameBootstrap, LevelBuilder, GameManager, WaveManager, TissueNode, Health, helpers
  Player/   PlayerController (CharacterController FPS), EnzymeRifle (hitscan weapon)
  Germs/    Germ base AI, Swarmer, Spitter, AcidGlob, GermFactory (greybox models)
  UI/       Hud (IMGUI crosshair, bars, wave info, end screens)
  Editor/   First-open scene setup and the Leukocyte menu
```

Tuning lives in public fields: wave sizes in `WaveManager.waves`, weapon stats on `EnzymeRifle`, germ stats in each germ's `Awake`.

## Next up

Abilities (Phagocyte Grab, Antibody Tag, Cytokine Burst), the Burrower and Spore Mother germs, AI squadmates, then an art and audio pass.
