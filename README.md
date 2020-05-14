# 3d-Sandbox-voxel-block-game
Minecraft-like prototype with procedurally generated terrain and different types of placeable / destructible blocks.

![thumbnail](/Images/thumbnail.png)

[Youtube showcase](https://www.youtube.com/watch?v=Aml4akSJuFk)

● opaque / transparent blocks

● procedural terrain using 2 layers of perlin noise as a heightmap

● caves using a 3d perlin noise

● optimized chunk meshes, only visible faces are drawn

● flowing fluids

● interactable blocks

● runtime chunk generation

There is still a lot of room for optimization such as combining adjacent block faces into a quad or using threading for chunk / mesh generation which I plan to do in the future.
