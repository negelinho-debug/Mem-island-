# MEM ISLAND — Unity source (first playable graybox)

Independent humorous first-person 3D Unity game prototype, original geometry and synthesized sounds.

## Current scripted route

Spawn -> Kotost shop and Melstroy cage close to the spawn -> forest with common
meme enemies -> road pursuit (original synthesized *placeholder* tune) -> icy mountain
with Luk Chesnok (ice cube with eyes) -> jumps/platforms -> Vozdukhan arena
(10 slipper hits, grounded vulnerability only, 50% HP super form) ->
15th Telifon spawns after victory -> ending island with Kotost and mini credits.

At start: 3 hearts and flashlight. The fourteen Telifons are spiderlike, not phones.
Other joke characters are moving enemies. Placeholder 3D silhouettes and positions
are NOT approved final meme models or final level layout.

## Unity editor

Unity 2022.3.62f1 (built-in rendering, legacy Input Manager): open this folder,
then open `Assets/Scenes/MemIsland.unity`, Play.
You can regenerate a starter scene from `MEM ISLAND > Создать игровую сцену`.

Desktop: WASD + right mouse drag to look; Space = jump; F = slap; E = talk.
Mobile touch: lower-left virtual stick, drag upper right to look;
onscreen buttons for jump, slap, and talk. Landscape orientation.

## CI / GitHub

Included `.github/workflows/unity-webgl.yml` builds WebGL through GameCI,
**only after valid Unity license secrets have been configured**.
Unity Personal license activation in headless CI currently has limitations:
https://game.ci/docs/github/activation/

Unity iPhone builds also need Unity iOS export, Xcode, then packaging and signing.
There is NO prebuilt IPA in this archive and CI is NOT verified to complete.
The enclosed workflow is WebGL-only, NOT an IPA builder.

## Limitations

- Procedural test level and deliberately simple prototype art, not final models.
- Voice lines are on-screen text only, not sound recordings.
- Copyrighted `Run From Your Demons...` music is NOT packaged; chase uses an
  original placeholder synth loop.
- NPC look, map placements, difficulty, parkour layout and sounds await direction.
- No guarantee of error-free compilation because a Unity Editor is not installed
  in the current authoring environment.

All labels, mechanics, text and temporary art can be changed later.

## User-directed map update

Near spawn: Kotost shop and Melstroy cage, both safe NPCs. In front of them:
a small forest containing normal hostile meme enemies. Then the chase road.
This update changes test layout only; final assets and positions are not settled.
