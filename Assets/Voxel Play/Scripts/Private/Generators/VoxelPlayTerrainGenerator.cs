using System;
using UnityEngine;

namespace VoxelPlay {

	public abstract class VoxelPlayTerrainGenerator : ScriptableObject { 

        protected const int ONE_Y_ROW = VoxelPlayEnvironment.ONE_Y_ROW;
        protected const int ONE_Z_ROW = VoxelPlayEnvironment.ONE_Z_ROW;

        [Header ("Terrain Parameters")]
		[Tooltip("The maximum height allowed by the terrain generator (usually equals to 255). The altitude returned by the terrain generators are in the 0-1 range and multiplied by this value to produce the actual terrain altitude for each position in the world.")]
		public float maxHeight = 255;

        [Tooltip("The mininmum height for the world. This value is used by some terrain generators to limit the depth of the terrain or place a bedrock voxel.")]
		public float minHeight = -32;

		[Tooltip("Disable to avoid rendering water")]
		public bool addWater = true;

        [Tooltip("Water level (water altitude). Set this value to 0 if your world doesn't use water like lakes or seas.")]
		public int waterLevel = 25;

		/// <summary>
        /// Set this to false if the terrain generator doesn't use heightmaps nor moisture. This is useful if you write custom contents in the PaintChunk method that do not rely on heightmaps exposed by the terrain generator.
        /// </summary>
		[NonSerialized]
		public bool usesHeightAndMoisture = true;

		[NonSerialized]
		protected VoxelPlayEnvironment env;

		[NonSerialized]
		protected WorldDefinition world;

		/// <summary>
		/// Resets any cached data and reload info. This method is optional.
		/// </summary>
		protected virtual void Init() { }

		/// <summary>
		/// Gets the altitude and moisture (0..1 range). This method is optional.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="z">The z coordinate.</param>
		/// <param name="altitude">Altitude (0..1 range).</param>
		/// <param name="moisture">Moisture (0..1 range).</param>
		public virtual void GetHeightAndMoisture (double x, double z, out float altitude, out float moisture) {
			usesHeightAndMoisture = false;
			altitude = 0;
			moisture = 0;
        }

		/// <summary>
		/// Paints the terrain inside the chunk defined by its central "position".
		/// </summary>
		/// <returns><c>true</c>, if terrain was painted, <c>false</c> otherwise.</returns>
		public abstract bool PaintChunk (VoxelChunk chunk);

		/// <summary>
		/// Returns true if the terrain generator is ready to be used. Call Initialize() otherwise.
		/// </summary>
		[NonSerialized]
		public bool isInitialized;



		/// <summary>
		/// Use this method to initialize the terrain generator
		/// </summary>
		public void Initialize () {
			env = VoxelPlayEnvironment.instance;
			if (env == null)
				return;
			world = env.world;
			if (addWater) {
				if (waterLevel > maxHeight) {
					Debug.LogWarning("Water level is higher than terrain maximum height. Check terrain settings.");
				}
				env.waterLevel = waterLevel;
				env.hasWater = true;
			} else {
				env.hasWater = false;
			}
			Init ();
			if (world == null)
				return;
			isInitialized = true;
		}

	}

}