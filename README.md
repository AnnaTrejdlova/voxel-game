<div align="center">
  <img src="Exports/icon.png" alt="Logo" width="80" height="80">

  <h1 align="center">Voxel Game</h1>

  <p align="center">
    A voxel game with procedurally generated terrain
  </p>
  <p align="center">
    (inspired by Minecraft)
  </p>
  <br>
</div>



## About The Project

![Voxel Game Screen Shot](Exports/Screenshot2.jpg)

This is a project I started to learn Unity game engine.
The goal was mainly to learn generating meshed, texturing, uv mapping and playing with world generation using Perlin noise.

What I achieved:
* Basic 3D player movement
* World generation (Perlin noise, multithreading)
* Chunk-based mesh generation


### Built With

* [Unity game engine](https://unity3d.com/)


## Getting Started

### Prerequisites

Install the Unity Editor version `2020.3.32f1`

### Installation

Clone the repo
   ```sh
   git clone https://github.com/AnnaTrejdlova/voxel-game.git
   ```


## Gameplay

* Movement: `W` `A` `S` `D`
* Camera: Mouse
* Jump: `Space`
* Fly mode: double press `Space`
* Fly up: `Space`
* Fly down: `Shift`
* Place block: `RMB`
* Destroy block: `LMB`


<!-- ROADMAP -->
## Roadmap

- [ ] UI - item bar, inventory, hand, F3 debug overlay, vignette, main menu (+ESC menu)
- [ ] Settings - render distance, world size (OR infinite generation)
- [ ] Buttons - ESC, F1, F2, F3, F11(Alt+Enter), E(TAB), C(zoom)
- [ ] Biome grass colors, render distance fog
- [ ] Terrain generation - 3 noise maps + biome map
