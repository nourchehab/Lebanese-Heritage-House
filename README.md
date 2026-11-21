# Lebanese Heritage House

A first-person exploration game set in a traditional Beirut house, built in Unity as part of the AUB UNESCO cultural-heritage digitalization project.

The player arrives at the house of Assem Salam and has one hour to find the key and leave. Along the way they discover heritage items that teach them about Beirut, the architecture of the house, and its owner.

## Current content

- **Courtyard**: a 3D scan of the courtyard, with looping ambience and a fountain whose water and sound start when you walk close.
- **Connecting room**: textured room model, reached through the courtyard door. The ambience drops to 30% inside.
- **Gatekeeper**: a three-line opening dialogue. After it, the gatekeeper walks away.
- **Item bar**: five slots at the bottom of the screen. The last slot is the key.
- **Item 1, Carved stone panel**: found at the fountain from the door side. It plays an animated reveal, then shows a short info card about Ottoman-style stone blocks.

## Controls

| Key | Action |
| --- | --- |
| W A S D | Move |
| Shift | Run |
| Mouse | Look |
| E | Interact / continue dialogue |
| Esc | Free the cursor |

## Opening the project

1. Install **Unity 6000.4.7f1** (Unity 6.4) through Unity Hub.
2. In Unity Hub, choose **Add → Add project from disk** and select this folder.
3. Open `Assets/Scenes/SampleScene.unity` and press Play.

If the scene ever needs rebuilding, use the menu **Heritage → Set Up Playable Scene**.

## Project layout

```
Assets/
  Audio/      courtyard ambience
  Editor/     HeritageSceneSetup (scene builder menu)
  Fountain/   water particle prefab, material, water loop sound
  Items/      carved stone panel sprite and "item found" sound
  Models/     courtyard.fbx, room_v5.fbx, room textures and materials
  Resources/  item bar box images
  Scenes/     SampleScene (+ Backups/)
  Scripts/    player, interaction, dialogue, doors, audio, item bar, item reveal
Packages/
ProjectSettings/
```

The `Library/`, `Temp/`, `Logs/` and `UserSettings/` folders are intentionally not in the repository. Unity regenerates them the first time the project opens.
