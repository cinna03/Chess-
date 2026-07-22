# AR Tabletop Chess

Hot-seat chess in augmented reality for iPhone. Built with Unity 6, AR Foundation, and ARKit.

Players scan a flat surface, place a 3D chessboard in the real world, and take turns on one device with legal move validation.

## Features (MVP)

- Place a chessboard on a detected horizontal surface (AR)
- Full 32-piece setup with standard starting positions
- Tap to select a piece → legal-move highlights → tap to move
- Hot-seat turns (White / Black on one phone)
- Captures and basic check detection
- Reset game

## Out of scope (for now)

- AI opponent
- Online multiplayer
- Full checkmate / stalemate UI
- Android build (development target is iPhone)

## Requirements

- Unity 6 (project uses URP + AR Foundation 6.5 / ARKit)
- macOS + Xcode for iOS builds
- Physical iPhone for AR testing (Editor can test chess logic without AR)

## Project structure

```
Assets/
├── Scripts/Chess/          # Game logic + views (added as we build)
├── Scenes/                 # AR / gameplay scenes
├── MobileARTemplateAssets/ # Unity Mobile AR template
├── XR/ / XRI/              # XR / Interaction Toolkit setup
Packages/                   # Package manifest (AR Foundation, ARKit, …)
ProjectSettings/
```

## Setup

1. Clone this repository (Unity project root, e.g. `~/Chess`).
2. Open the folder in Unity Hub (Unity 6).
3. Let Unity import packages.

### Editor playtest (no AR / no phone)

4. Double-click `Assets/Scenes/ChessPlaytest.unity` **or** menu **Chess → Open Editor Playtest Scene**.
5. Press Play → click pieces (green = move, red = capture). Hot-seat: White then Black.

### AR on iPhone

4. Open `Assets/Scenes/SampleScene.unity` (Mobile AR template).
5. Menu **Chess → Setup AR Chess In Open Scene**.
6. **File → Save As…** → `Assets/Scenes/ARChess.unity`.
7. Build Settings → iOS → switch platform → Build & Run on a signed iPhone.
8. Scan a table → tap to place board → play hot-seat. Use **Replace Board** / **New Game** on the HUD.

Scripts: `Assets/Scripts/Chess/Core` (rules), `View` (board/input), `AR` (placement).


## Team

| Name | Role | Contributions |
|------|------|----------------|
| **Group leader (you)** | Lead developer | Design, Unity/AR, chess logic, build, docs, video |
| **Teammate** | Unavailable | No commits this submission (illness) |

Update names before Canvas submission.

## License / course use

Student portfolio project for course submission.
