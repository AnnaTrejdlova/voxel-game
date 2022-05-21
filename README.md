<div id="top"></div>

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <img src="Exports/NoiseMap3.png" alt="Logo" width="80" height="80">

  <h1 align="center">Voxel Game</h1>

  <p align="center">
    A voxel game with procedurally generated terrain (inspired by Minecraft)
  </p>
</div>



<!-- ABOUT THE PROJECT -->
## About The Project

[![Voxel Game Screen Shot][product-screenshot]](Exports/Screenshot2.jpg)

This is a project I started to learn Unity game engine.
The goal was mainly to learn generating meshed, texturing, uv mapping and playing with world generation using Perlin noise.

What I achieved:
* Basic 3D player movement
* World generation (Perlin noise, multithreading)
* Chunk-based mesh generation


### Built With

* [Unity game engine](https://unity3d.com/)


<!-- GETTING STARTED -->
## Getting Started

This is an example of how you may give instructions on setting up your project locally.
To get a local copy up and running follow these simple example steps.

### Prerequisites

Install the Unity Editor version `2020.3.32f1`

### Installation

Clone the repo
   ```sh
   git clone https://github.com/michaltrejdl/voxel-game.git
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
