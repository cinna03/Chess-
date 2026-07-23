# AR Tabletop Chess

Hot-seat and vs-computer chess with an AR placement path for iPhone. Built with Unity 6, AR Foundation, and ARKit.

Play in the Editor (`ChessPlaytest`) or place a 3D board on a real table (AR). Legal moves, captures, check detection, turn UX, and a simple computer opponent.

**GitHub:** https://github.com/cinna03/Chess-

## Features

- **Hot-seat** — two players, one device; board flips to face whose turn it is
- **vs Computer** — you play White; **minimax AI** (depth 3, material + positional eval)
- **Game over** — checkmate / stalemate panel with rematch
- Polished **uGUI** with **Fredoka** (cute rounded font) + panel pop animations
- Legal-move markers (green) and capture rings (red)
- Piece move hop + capture tray
- Check detection + celebration burst on checkmate
- AR: tap a detected plane to place the board (`ARChess` / setup menu)

## Out of scope

- Online multiplayer
- Difficulty selector / grandmaster-level engine
- Android public build (iPhone AR path; itch build from ChessPlaytest)

## Contributors

| Name | Role | Contributions |
|------|------|----------------|
| **[Your Name] — Group Leader** | Lead developer | Game logic, UX, AI mode, AR placement, repo, build, video |
| **[Teammate Name]** | Collaborator | DevLog entries, documentation support, video / process contributions |

*(Update names before Canvas submit. Task allocation tracker: leader = edit, teammate = comment.)*

## Requirements

- Unity 6 (URP + AR Foundation 6.5 / ARKit)
- macOS + Xcode for iOS builds
- Editor playtest works without a headset/phone

## Project structure

```
Assets/
├── Scripts/Chess/
│   ├── Core/     # Board, legal moves, AI
│   ├── View/     # Board visuals, HUD, input, modes
│   └── AR/       # Plane placement
├── Scenes/
│   ├── ChessPlaytest.unity   # Desktop / Editor demo (use for itch build)
│   ├── ARChess.unity         # AR-enabled scene
│   └── SampleScene.unity     # Mobile AR template
Packages/
ProjectSettings/
```

## Setup

1. Clone this repository.
2. Open the folder in Unity Hub (Unity 6).
3. Let Unity import packages.

### Editor playtest (recommended for graders / itch)

4. Open `Assets/Scenes/ChessPlaytest.unity`.
5. Press **Play**.
6. Choose **Hot-seat** or **vs Computer**.
7. Tap pieces → glowing squares to move.

### AR on iPhone

4. Open `Assets/Scenes/SampleScene.unity` or `ARChess.unity`.
5. If needed: **Chess → Setup AR Chess In Open Scene**, then save as `ARChess.unity`.
6. Build Settings → iOS → Build & Run.
7. Scan a table → tap to place → play.

## Demo media

Add screenshots / GIFs here before submission:

- Mode select screen
- Legal move markers + capture
- Board flip (hot-seat)
- vs Computer reply
- Capture tray
- (Optional) AR board on table

## Links (fill before Canvas)

- **DevLog:** _[public Notion / Google Doc / Wiki]_
- **Public build (itch.io):** _[url]_
- **Video walkthrough:** _[YouTube / Drive public link]_
- **Task allocation tracker:** _[url]_
