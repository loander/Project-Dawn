******************************************
*             Voxel Play 2               *
* Copyright (C) Kronnect Technologies SL * 
*             README FILE                *
******************************************


What's Voxel Play?
--------------------

Voxel Play is a voxelized environment for your game. It aims to provide a complete solution for terrain, sky, water, UI, inventory and character interaction.


How to use this asset
---------------------
Firstly, you should run the Demo scenes to get an idea of the overall functionality.
Then, please take a look at the online documentation to learn how to use all the features that Voxel Play can offer.

Documentation/API reference
---------------------------
The user manual is available online:
https://kronnect.com/guides

You can find internal development notes in the Documentation folder.


Support
-------
Please read the documentation and browse/play with the demo scene and sample source code included before contacting us for support :-)

Have any question or issue?
* Support-Web: https://kronnect.com/support
* Support-Discord: https://discord.gg/EH2GMaM
* Email: contact@kronnect.com
* Twitter: @Kronnect



Future updates
--------------

All our assets follow an incremental development process by which a few beta releases are published on our support forum (kronnect.com).
We encourage you to signup and engage our forum. The forum is the primary support and feature discussions medium.

Of course, all updates of Voxel Play will be eventually available on the Asset Store.


Version history
---------------

Version 14.4
- Several internal minor optimizations
- Default camera fov in demo scenes and character controller is now 70 (from 60) which makes voxel look a bit smaller and improves immersion
- API: added ChunkHasCollider method
- [Fix] Fixed an issue which produced some invisible colliders when voxels collapse

Version 14.3
- API: added VoxelToCube()
- API: added OnChunkBeforeDetailGeneration event
- API: added UpdateVoxelDefinitionTextures(voxelDefinition)
- [Fix] Added depth texture compatibility to vegetation shaders

Version 14.2
- Added compatibility with screen space shadows in Unity 2022.3 URP (ie. Umbra Soft Shadows asset)
- API: added new GetVoxelIndices overloads to convert efficiently a list/array of Vector3d into a list/array of VoxelIndex structs
- Improved VR state detection

Version 14.1
- Added Global Specular option to Voxel Play Environment: improves lighting for regular opaque voxels
- Connected Voxels: expanded rules to support 26 neighbours
- Minor loop optimizations
- [Fix] Fixed speed of vegetation shadows animation
- [Fix] Fixed some tree leaves not receiving AO

Version 14.0
- Added support for URP Render Graph
- Prevents texture array packers to exceed system capacity when using texture variations or connected textures
- Ensures visible distance is always >= force chunk distance value

Version 13.8
- Multi-step Terrain Generator: added "Offset" parameter to texture sampler operators
- Voxel Play Environment inspector: added option to increase the number of different materials that can be used in a single chunk
- [Fix] Fixed import settings for tree textures of demo scene HQForest
- [Fix] Fixed regression bug that prevented rendering of normal/displacement maps on side faces

Version 13.7
- Texture alpha values are now ignored when loading a texture for opaque voxel definitions
- API: added ModelPlaceAlignment parameter to all ModelPlace methods which allows placing the model centered or not at the given position
- API: added additional GetVoxelColor / SetVoxelColor overloads
- [Fix] Fixed bug in GetVoxelNeighbourhood11 method which was not returning the correct voxel positions

Version 13.6
- Multi-Step Terrain Generator: added "Sample Heightmap From Unity Terrain" operator
- Unity Terrain Generator: Min Height field is now visible
- Terrain Generators: added "Add Water" option. Now you can ignore water in terrain generation using this checkbox.
- API: GetVoxelNeighbourhood now returns the world space position of the voxels
- API: Added SetVoxels(boxMin, boxMax, array) and SetVoxels(position, array)
- API: Added more ModelDefinition.Create overloads that take list of voxel definitions, voxels or colors
- API: ModelCreateGameObject now can use textures and normal maps
- [Fix] Fixed Randomization affecting app random state
- [Fix] Render queue of realistic water with no shadows shader has been changed to avoid sorting order issues with transparent voxels

Version 13.5
- Ability to use Voxel Play Environment without default UI or input controllers
- Added Demo Scene 6: creates a voxel gameobject from a model definition
- Added "NavMesh Resolution" option to Voxel Play Environment inspector
- API: Change: ModelCreateGameObject() is now a public static method
- [Fix] Some shaders won't render shadows correctly in builds in certain Unity version due to multi_compile changes in URP

Version 13.4
- Prefab Spawner: added "requireCollider" and "requireNavMesh" options
- Torch/smooth lighting is now compatible with custom voxels which use multiple mesh filters
- API: added OnWorldLoaded event
- Improved shadow rendering of cutout voxels
- [Fix] Fixes for server mode
- [Fix] API: fixes to VoxelOverlap method

Version 13.3
- Inspector can now select and edit multiple voxel definitions
- Unload Far Chunks: option to destroy far chunks but keep modified chunks
- [Fix] Fixed a rare chunk dictionary hash collision

Version 13.2
- Added "Voxel Padding" option under Voxel Environment inspector -> Voxel Generation section. Controls voxel mesh extra padding on/off which avoids gaps when using greedy meshing.
- Added custom post processing option to remove gap/white pixels between adjacen geometry when voxel padding is disabled
- Texture Variations: voxel rotation is now also used as seed for randomization

Version 13.1
- Added "Damage Particles" global option to Voxel Play Environment inspector (used to enable/disable particle effects globally)
- [Fix] Fixed particles not falling in Unity 2023 beta due to a Physics regression bug

Version 13.0
- Connected Voxels: option to apply rules during rendering (keeps voxel definition but renders a different voxel in its place).
- Texture Variations: workflow similar to Connected Textures to provide texture variations to any voxel definition. See: https://kronnect.freshdesk.com/support/solutions/articles/42000102856-texture-variations

Version 12.2.1
- Added support for Domain Reload options when Render In Editor mode is enabled

Version 12.2
- Fly mode implemented in third person controller (press F to enable Fly Mode, Q/E keys to move down/up)
- A warning is now shown in the inspector if Depth Priming Mode is enabled in URP
- API: added GetChunksBounds() - returns the bounds enclosing all generated chunks
- [Fix] Fixed a regression bug which resulted in textures not being saved when using the Export Chunks command

Version 12.1
- Added "Shadow Tint Color" option to World Definition (under Sky & Lighting section). Enable the feature in Voxel Play Environment ("Colored Shadows" checkbox)

Version 12.0.1
- [Fix] Vegetation now receives correct per-pixel lighting when URP native light option is enabled
- [Fix] Fixed an issue with initialization of virtual lights

Version 12.0
- Added Unity 2022/URP 14 Forward+ support, supporting +8 native lights: https://i.imgur.com/52Ovvygl.jpg
- GPU instancing now supports meshes with multiple submeshes and materials
- Added GPU instancing support to VPModelTextureAlpha & VPModelTextureAlphaDoubleSided shaders
- Added "Instancing Culling Mode" option to Voxel Play Environment. Aggresive is the default value: culls non visible voxels. Gentle allows some padding to keep shadows from invisible voxels. Disabled: renders all voxels, regardless of their positions vs camera.
- Added HDR support to color and emission color properties of VP materials
- Added "Fog Tint" option to Voxel Play Environment
- Added "VPModelTextureCuoutDoubleSided" shader
- Regrouped some properties in Voxel Play Environment inspector for better visibility
- [Fix] Fixed a realistic water shader glitch when viewed through tree leaves

Version 11.5.1
- API: added OnItemConsumed / OnItemsClear events to VoxelPlayPlayer API
- [Fix] Voxel Play Behaviour now applies lighting to objects that use multi-materials

Version 11.5
- Added 'Unload Far Chunks Mode' option (visibility or destroy)
- API: added "ignoreFrustum" parameter to ChunkRedraw methods. This ensures the chunk will be rendered regardless of frustum visibility (or visible distance).
- [Fix] Fixed chunks not being generated when setting distanceAnchor property to a non-camera gameobject

Version 11.4
- Added "Climb Max Step Height" parameter to first person character controller. Determines the maximum height of a step to allow climbing.
- Connected textures resolver now takes into account player orientation
- [Fix] Internal: texture packer now can differentiate textures that use same diffuse texture but different normal maps

Version 11.3
- Removed "hasContent" field from Voxel structure, replaced by boolean property. This saves 2 words of memory per voxel.
- API: added ChunkRedrawNeighbours method.

Version 11.2
- Vegetation voxels can now be collected when destroyed (see "Can Be Collected" option in the voxel definition)
- Added "Drop Probability" property to voxel definition
- Particles and damage cracks are now influenced by torch lights

Version 11.1
- Render In Editor: added new detail level "Standard with no detail generators"
- [Fix] Fixes for the Unity Terrain to Voxel terrain generator

Version 11.0
- Added virtual point lights. See: https://kronnect.freshdesk.com/a/solutions/articles/42000084968
- Tweaked lighting equation so normal map are visible under shadows
- API: added "IncludeVoxelProperties" option to GetChunkRawData/SetChunkRawData methods
- API: added "cancel" parameter to event OnModelBuildStart
- [Fix] Fixed potential issue when reloading textures by refreshing certain properties in the inspector
- [Fix] Qubicle import fixes

Version 10.9.3
- [Fix] Fixed Light Manager issue not detecting player camera displacement correctly in URP

Version 10.9.1
- Improved URP shader support with the inclusion of specific DepthOnly and DepthNormals passes
- [Fix] Fixed random generation issue that affected placement of vegetation on certain platforms
- [Fix] Fixed some custom voxel properties not being loaded correctly from a savegame

Version 10.9
- Minimum Unity version 2020.3.16
- Added "Delayed Initialization" option to Voxel Play Environment inspector. You can initialize the engine calling the Init() or InitAndLoadSaveGame() methods instead
- Biome Explorer: added position to tooltip
- Voxel Definitions: texture sample field is now exposed in the inspector for all voxel definitions
- [Fix] Fixed voxel signature calculation which resulted in some collision mesh issues

Version 10.8
- Added "AllowNestedExecutions" to detail generator class
- Voxel Definition: added "Greedy Meshing" option to override materials
- [Fix] Model fitToTerrain property was being ignored when placing models with the default character controllers

Version 10.7.3
- [Fix] Fixed issue in Unity Editor when no camera is present in the scene and VP is initialized

Version 10.7.2
- [Fix] Fixed issue with building steps voxels disappearing when stacking other voxels nearby
- [Fix] Fixed issue with rendering materials that use different relief/normal map settings
- [Fix] Fixed water with no shadows shader render queue which resulted in overdraw with other transparent objects

Version 10.7.1
- Improvements to the Unity terrain generator
- Added a warning when water level is higher than maximum terrain height
- Reduced usage of global keywords
- [Fix] Fixed savannah tree 1
- [Fix] Fixed issue with warning when connected texture is not valid
- [Fix] Fixed native URP issue in builds using Unity 2020.3 or later

Version 10.7
- Custom voxels: added new properties when GPU instancing is enabled: GenerateCollider & GenerateNavMesh. See: https://kronnect.freshdesk.com/support/solutions/articles/42000049143-custom-voxel-types
- Custom voxels: added Occludes Forward/Back/Left/Right/Top/Bottom optimization options.
- Item.itemChunk & item.itemVoxelIndex are now generalized for all persistent items. Previously, only torches used those two fields of Item class

Version 10.6
- Connected textures: added slot for optional normal map
- API: added ModelWorldCapture(bounds). Captures a portion of the world into a Model Definition
- [Fix] Fixed potential memory leak with "Unload Far NavMesh" option
- [Fix] Fixed voxel highlight edges material leak when destroying a highlighted custom voxel

Version 10.5.3
- Added Bright Point Lights Max Distance option to Voxel Play Environment inspector
- Added more verbose messages during initialization
- [Fix] Fixed a missing foe prefab reference in demo scene 3

Version 10.5.2
- Added SendMessageOptions.DontRequireReceiver to SendMessage commands when loading/saving a scene to prevent console warnings
- Added "Can Climb" option to first person controller
- Added "Manage Voxel Rotation" to character controller
- [Fix] Save/load game fixes

Version 10.5.1
- API: added "fallbackVoxelDefinition" to load savegame methods (replaces a missing voxel definition from the savegame with an alternate voxel definition)
- Added an inspector error message if Enable URP Support is activated but Universal RP package is not present or configured
- Added support to origin shift to foes in demo scene flat terrain
- [Fix] Fixed character controller position not being applied correctly when loading a saved game
- [Fix] Fixed origin shift regression with first person character controller
- [Fix] Fixed dynamic voxel textures not reflecting all textures when rotating a 6-textured cube

Version 10.5
- Improvements to water placement/destruction in build mode
- Improvements to realistic water appearance on side faces
- [Fix] Fixed custom voxels visibility not being preserved when updating a chunk

Version 10.4
- API: added VoxelGetRotation methods
- Constructor: added tiny delay when returning to focus to prevent accidental clicks
- Constructor: improvements to "Save As New..." option
- [Fix] Constructor: voxel rotations are lost when using the Displace command
- [Fix] Constructor: voxels at z position=0 were not saved correctly
- [Fix] Fixed footfall sounds update failing when character is not grounded

Version 10.3.1
- API: added ChunkReset() method
- [Fix] Fixed water blocks rendering in black in URP when camera background is set to solid color

Version 10.3
- Custom voxels: added "Compute Lighting" option (experimental). This option bakes surrounding lighting and AO into the mesh vertex colors at runtime.
- Internal improvements related to multiple player instances
- DefaultCaveGenerator: added minLength / maxLength properties (length random range for tunnels)
- Improvements to terrain generator and caves
- Improved torch lighting falloff in linear color space
- [Fix] OnGameLoaded event not fired when calling LoadGameFromByteArray
- [Fix] Fixed transparent blocks rendering in black in URP when camera background is set to solid color
- [Fix] Fixed /teleport console command bug
- [Fix] Fixed an error when visible lights exceed 32
- [Fix] Fixed chunk rendering issue when pool is exhausted
- [Fix] Fixed texture bleeding for opaque side textures with solid colors

Version 10.2
- Added debug info when loading connected textures
- Added buoyance effect to particles when underwater (in practice, they fall slower underwater)
- Improved Connected Texture editor visuals
- Added helps section to Voxel Play Environment inspector
- Added menu links to online documentation, youtube tutorials and support forum
- API: improved transition between dynamic voxel to regular voxel using VoxelCancelDynamic method
- API: virtualized methods of character controllers for easier customization
- [Fix] Damage particles now use the textureSample field in voxel definition if present
- [Fix] Voxels were highlighted when highlighting is disabled when using the third person controller

Version 10.1
- Change: chunk.isAboveSurface now defaults to true
- Optimization of the voxel thumbnail generation. New "Drop Voxel Texture Resolution". See: https://kronnect.freshdesk.com/support/solutions/articles/42000001884-world-definition-fields
- UI: removed console message when crouching
- [Fix] First person character controller fixes
- [Fix] Fixes related to the water level transition
- [Fix] Fixed model colors imported with Qubicle when rendering in linear color space

Version 10.0 4/Aug/2021
- Support for URP native lights including point and spot lights with shadows
- Improved underwater effect (fog, caustics) and air to water transition
- Added /fps command to console to toggle fps display on/off
- [Fix] Fixed rogue white pixels on the edges of some voxels visible underground in very dark areas
- [Fix] Fixed an issue with collider rebuild which could led to player falling down
