# Word Bubble Pop (Unity, C#)

A spelling game for kids: look at the picture, pop the letter bubbles in the right order, and finish the word within 20 seconds.
Every object on screen is a normal Unity object you can edit in the Inspector, and all numbers, words, colours, music and sounds are in one Game Settings file.

## Run it in Unity

1. Unzip this file. You get a folder called `WordBubblePop`.
2. Open **Unity Hub** → **Projects** → **Add** → **Add project from disk** → select the `WordBubblePop` folder → **Open**.
3. Click the project in the list to open it with Unity 6.6. The first open takes a few minutes.
4. The game scene is built and opened automatically (`Assets/Scenes/WordBubblePop.unity`).
5. In the **Game** tab, switch to **Simulator** and choose a phone.
6. Press ▶ **Play**. A mouse click works like a tap.

On a Mac, the Editor also speaks the words using the built-in "say" voice.

## Editing the game (no code needed)

Everything is a normal Unity object or asset, so you can edit it in the Inspector.

### 1. Objects on screen → Hierarchy

Open `Assets/Scenes/WordBubblePop.unity`. In the **Hierarchy** under **Canvas**:

| Object | What you can change |
|---|---|
| `Background` | Sky colour |
| `Game Column/Top Bar` | Level pill, stars pill, sound button: size, colour, text, font |
| `Game Column/Word Card` | Card colour and size, picture circle, texts ("What is this?") |
| `Word Card/Letter Boxes/Letter Box (template)` | Look of every letter box (size, colour, font) |
| `Word Card/Timer Track`, `Timer Fill`, `Timer Text` | Timer bar look |
| `Word Card/Progress Dots` | Dot size and spacing |
| `Game Column/River` | River shape and colours, lily pads, `Water` gradient image |
| `Start Screen`, `Level Complete Screen`, `Times Up Screen` | Popups: titles, steps, buttons, colours |

The three popup screens are switched off in the editor so they don't cover the game.
Tick the checkbox next to a screen's name in the Inspector to see and edit it, then untick it again.
Objects with "(template)" in the name are copied by the game for every letter or word: edit the template.

Tip: changes you make while ▶ Play is on are lost when you stop. Stop the game first, then edit.

### 2. Bubble look → Prefab

Double-click `Assets/Prefabs/Bubble` (or menu **Word Bubble Pop → Open Bubble Prefab**).
Change the shine, ring, shadow, letter font or colour, hint glow. On the Bubble component:
`Lightness` (how pale the colours are), `Letter Scale`, `Sway`, `Pop Time`.

### 3. Numbers, words, colours, music → Game Settings

Select `Assets/Settings/GameSettings` (or menu **Word Bubble Pop → Open Game Settings**):

| Section | Settings |
|---|---|
| Timer and levels | Seconds per word, words per level |
| Bubbles | Speed, how often they appear, how many, size, colours, when the hint glow appears |
| Letter boxes | Empty / filled / border colours |
| Timer bar | Normal and warning colour, warning seconds |
| Music and sounds | Background music, pop, wrong, tick, win, game over sounds, and their volumes |
| Voice | Speak words on/off, voice speed |
| Words | The word list: change a word, its picture, add (+) or remove (−) words |

**Change the music:** drag your own `.mp3`, `.wav` or `.ogg` file into `Assets/Audio`, then drag it onto **Music** in Game Settings.
**Change a sound:** same way, onto Pop Sound, Win Sound and so on.
**Add a word:** put its picture (PNG) in `Assets/Art/Pictures`, press **+** under Words, type the word in capitals and drag the picture in.

### Start again from a clean scene

Menu **Word Bubble Pop → Rebuild Scene** makes a fresh scene. Game Settings and the Bubble prefab are kept.

## Files

| File | Purpose |
|---|---|
| `Scenes/WordBubblePop.unity` | The game scene (created on first open) |
| `Settings/GameSettings.asset` | All numbers, words, colours, music and sounds (created on first open) |
| `Prefabs/Bubble.prefab` | Look of one letter bubble (created on first open) |
| `Scripts/GameManager.cs` | Game rules: timer, levels, stars, game over, saving progress |
| `Scripts/GameView.cs` | Links the game rules to the objects in the scene |
| `Scripts/GameSettings.cs` | Defines the Game Settings fields |
| `Scripts/Bubble.cs` | Bubble movement, tap, pop and shake |
| `Scripts/WordData.cs` | Picks the words for each level |
| `Scripts/Speech.cs` | Speaks words: Android TextToSpeech, iOS AVSpeechSynthesizer |
| `Scripts/UIKit.cs`, `Scripts/Palette.cs` | UI helpers and default colours |
| `Plugins/iOS/WBPSpeech.mm` | Small native plugin for the iOS voice |
| `Editor/SceneBuilder.cs` | Builds the scene, prefab and settings (menu: Word Bubble Pop) |
| `Editor/ProjectSetup.cs` | Sets app name, ID, portrait mode and icon |
| `Editor/AndroidTtsManifest.cs` | Adds the manifest entry Android 11+ needs for text-to-speech |
| `Art/Pictures/*.png` | Word pictures |
| `Art/UI/*.png` | Shapes used by the UI (circle, rounded box, star, water) |
| `Audio/*.wav` | Background music and sound effects |
| `Resources/Fonts/` | Baloo 2 font |

## Android build

1. **File → Build Profiles** (Unity 6) or **Build Settings** → **Android** → **Switch Platform**
   (install the *Android Build Support* module from Unity Hub the first time).
2. Connect an Android phone over USB (Developer options → USB debugging ON) → **Build And Run**.
   For just the APK file, press **Build**.

## iOS build

1. **Build Profiles / Build Settings** → **iOS** → **Switch Platform**
   (requires the *iOS Build Support* module from Unity Hub) → **Build** → choose a folder.
2. Open the generated `Unity-iPhone.xcodeproj` in Xcode → choose your Team under Signing → ▶ Run.
   To run in the Simulator: Player Settings → iOS → Target SDK = Simulator SDK.
