using UnityEngine;
using System;

namespace VoxelPlay {

	[Serializable]
	public partial class BiomeSurfaceVoxel {
		public VoxelDefinition voxelDefinition;
		[Range (0, 1)]
		public float probability;
	}

	[Serializable]
	public partial class BiomeUndergroundVoxel {
		public VoxelDefinition voxelDefinition;
		[Range (0, 1)]
		public float probability;
		public int altitudeMin, altitudeMax;
	}


	public partial class BiomeDefinition {

		const int minimumAltitude = -500;
		const int maximumAltitude = 500;

		[Header ("Extra Voxels")]
		
		[Note("Add any number of additional voxels for the SURFACE of this biome. The sum of all probabilities must be 1. If the sum is less than 1, the remaining probability will be used by the main voxelTop. For example, if the sum of probabilities for additional voxels is 0.6, the main voxel top will be used in 40% (0.4) of surface voxels in this biome.")]
		public InspectorNote noteTopAdditional;
		public BiomeSurfaceVoxel[] voxelTopAdditional;

		[Note("Add any number of additional voxels for the UNDERGROUND of this biome. The sum of all probabilities must be 1. If the sum is less than 1, the remaining probability will be used by the main voxelDirt. For example, if the sum of probabilities for additional voxels is 0.6, the main voxel dirt will be used in 40% (0.4) of underground voxels in this biome.", margin = 8)]
		public InspectorNote noteDirtAdditional;

		public BiomeUndergroundVoxel[] voxelDirtAdditional;

		[Tooltip("Optional voxel to be used at the bottom of ocean/lakes. If not assigned, the Voxel Dirt will be used instead.")]
		public VoxelDefinition voxelLakeBed;

		[Note("", margin = 8)]
		public InspectorNote separator;

		[Header("Underground Vegetation")]
		[Range(0, 1)]
		public float undergroundVegDensity = 0.05f;
		public BiomeVegetation[] undergroundVegetation;

		[Header("Underground Ceiling Vegetation")]
		[Range(0, 1)]
		public float undergroundCeilingVegDensity = 0.05f;
		public BiomeVegetation[] undergroundCeilingVegetation;


		VoxelDefinition[] allTopVoxels;
		VoxelDefinition[][] allDirtVoxels;

		/// <summary>
		/// Initialization function called by Terrain Generator
		/// </summary>
		public void InitMoreVoxels () {
			if (voxelLakeBed == null) {
				voxelLakeBed = voxelDirt;
			}
			if (undergroundVegetation == null) {
				undergroundVegetation = new BiomeVegetation[0];
			}
			if (undergroundCeilingVegetation == null) {
				undergroundCeilingVegetation = new BiomeVegetation[0];
			}
			VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
			if (env != null) {
				for (int v = 0; v < undergroundVegetation.Length; v++) {
					if (undergroundVegetation[v].vegetation == null) {
						undergroundVegetation[v].vegetation = env.defaultVoxel;
					} else {
						// ensure voxel definition is registered
						env.AddVoxelDefinition(undergroundVegetation[v].vegetation);
					}
				}
				for (int v = 0; v < undergroundCeilingVegetation.Length; v++) {
					if (undergroundCeilingVegetation[v].vegetation == null) {
						undergroundCeilingVegetation[v].vegetation = env.defaultVoxel;
					} else {
						// ensure voxel definition is registered
						env.AddVoxelDefinition(undergroundCeilingVegetation[v].vegetation);
					}
				}
			}

			// Consolidate all top/dirt voxel definitions in a single array
			DistributeSurfaceVoxels (voxelTop, voxelTopAdditional, ref allTopVoxels);
			DistributeUndergroundVoxels (voxelDirt, voxelDirtAdditional, ref allDirtVoxels);
		}

		/// <summary>
		/// For optimization purposes this function precomputes random values and distributes voxels in an array
		/// </summary>
		void DistributeSurfaceVoxels (VoxelDefinition mainVoxel, BiomeSurfaceVoxel[] additionalVoxels, ref VoxelDefinition[] voxelsArray) {

			VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
			if (env != null) {
				if (mainVoxel == null) {
					mainVoxel = env.defaultVoxel;
				}
				// Ensure textures are added to the engine
				if (additionalVoxels != null) {
					for (int k = 0; k < additionalVoxels.Length; k++) {
						env.AddVoxelDefinition(additionalVoxels[k].voxelDefinition);
					}
				}
			}

			voxelsArray = new VoxelDefinition[100];
			float acumProb = 0;
			int currentIndex = 0;
			if (additionalVoxels != null) {
				for (int k = 0; k < additionalVoxels.Length; k++) {
					if (additionalVoxels[k].voxelDefinition == null)
						continue;
					acumProb += additionalVoxels[k].probability;
					acumProb = Mathf.Clamp01(acumProb);
					int nextProb = (int)(acumProb * 100);
					if (currentIndex < nextProb) {
						VoxelDefinition vd = additionalVoxels[k].voxelDefinition;
						do {
							voxelsArray[currentIndex++] = vd;
						} while (currentIndex < nextProb);
					}
				}
			}
			while (currentIndex < 100) {
				voxelsArray [currentIndex++] = mainVoxel;
			}
		}

		/// <summary>
		/// For optimization purposes this function precomputes random values and distributes voxels in an array
		/// </summary>
		void DistributeUndergroundVoxels (VoxelDefinition mainVoxel, BiomeUndergroundVoxel[] additionalVoxels, ref VoxelDefinition[][] voxelsArray) {

			VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
			if (env != null) {
				if (mainVoxel == null) {
					mainVoxel = env.defaultVoxel;
				}
				// Ensure textures are added to the engine
				if (additionalVoxels != null) {
					for (int k = 0; k < additionalVoxels.Length; k++) {
						env.AddVoxelDefinition(additionalVoxels[k].voxelDefinition);
					}
				}
			}

			voxelsArray = new VoxelDefinition[1000][];
			for (int altitude = minimumAltitude; altitude < maximumAltitude; altitude++) {
				VoxelDefinition[] voxelsThisAltitude = new VoxelDefinition[100];
				int altitudeIndex = altitude - minimumAltitude;
				voxelsArray [altitudeIndex] = voxelsThisAltitude;
				float acumProb = 0;
				int currentIndex = 0;
				if (additionalVoxels != null) {
					for (int k = 0; k < additionalVoxels.Length; k++) {
						if (additionalVoxels[k].voxelDefinition == null)
							continue;
						if (additionalVoxels[k].altitudeMin != 0 && additionalVoxels[k].altitudeMin > altitude)
							continue;
						if (additionalVoxels[k].altitudeMax != 0 && additionalVoxels[k].altitudeMax < altitude)
						continue;
	
					acumProb += additionalVoxels [k].probability;
					acumProb = Mathf.Clamp01 (acumProb);
						int nextProb = (int)(acumProb * 100);
						if (currentIndex < nextProb) {
							VoxelDefinition vd = additionalVoxels[k].voxelDefinition;
							do {
								voxelsThisAltitude[currentIndex++] = vd;
							} while (currentIndex < nextProb);
						}
					}
				}
				while (currentIndex < 100) {
					voxelsThisAltitude [currentIndex++] = mainVoxel;
				}
			}
		}

		/// <summary>
		/// Returns a random voxel for the surface at the given position
		/// </summary>
		public VoxelDefinition GetVoxelTop (Vector3 position) {
			float rand = WorldRand.GetValue (position);
			int index = (int)(rand * 100);
			return allTopVoxels [index];
		}

		/// <summary>
		/// Returns a random voxel for the underground at the given position
		/// </summary>
		public VoxelDefinition GetVoxelDirt (Vector3 position) {
			position.y -= minimumAltitude;
			float rand = WorldRand.GetValue (position);
			int altitude;
			if (position.y < 0) {
				altitude = 0;
			} else if (position.y > 999) {
				altitude = 999;
			} else {
				altitude = (int)position.y;
			}
			int index = (int)(rand * 100);
			VoxelDefinition vd = allDirtVoxels[altitude][index];
			return vd;
		}

	}

}