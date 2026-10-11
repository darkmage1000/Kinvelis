# Kinvelis

A mobile 3D creature-collecting survival game for Android, built in Unity 6. Explore Rimewild Isle, bond with 206 kin, build a camp they help you run, and travel land by land to the eight Wardens who hold the locks on a storm.

Everything in the game is built in code: the world, the creatures, their animation, the interface, even the sound effects and music. All names, creatures and the world are original.

**Player guide (wiki):** https://darkmage1000.github.io/Kinvelis/

## What's in it

- An 8 km world of eight lands, each with its own kin, weather, night rules and hazards
- 206 kin to catch, level, bond with, evolve and put to work, including themed characters with a signature skill each
- Camp building with worker kin, crafting stations, research, outposts and automation
- A main story, camp and kin quest lines, rival tamers, village requests, legend hunts and ten boss fights
- Melee and ranged weapons with their own moves, lock-on targeting, mounts, gliding and surfing
- A "Little Explorer" mode for young children and a "Toy Box" of live rule switches

## Running it

1. Open the folder with **Unity 6000.0.62f1** (URP, Android build support).
2. Open `Assets/Scenes/Main.unity` and press Play. If materials or content are missing, run **Wildbond > Setup Project** from the menu bar.
3. To build for a phone: **Wildbond > Build Android APK**, then `bash Tools/deploy.sh` (wireless adb).

Keyboard in the editor: WASD move, right mouse look, F or click attack, G seed, E use, Space jump, Q roll, Shift sprint, R kin, T kin skill, B build, Tab menu.

## Project notes

`CLAUDE.md` is the running design and architecture log: how each system works, where it lives, and why it was built that way. Inside the code the game still goes by its working title, "Wildbond" (namespace, editor menu, package id).

`docs/` holds the player wiki, published with GitHub Pages. Its data is exported from the game by **Wildbond > Export Wiki Data** (in Play mode).
