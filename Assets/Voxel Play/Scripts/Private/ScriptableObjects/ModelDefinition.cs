using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

    [Serializable]
    public struct ModelBit {
        public int voxelIndex;
        public VoxelDefinition voxelDefinition;
        [Tooltip("Explicitly declares an empty position. When this model definition is placed, empty positions will clear any previously existing voxel in the world on that position.")]
        public bool isEmpty;
        public Color32 color;
        [Tooltip("The rotation for this voxels. Allowed rotations are 0, 90, 180 or 270 degrees.")]
        public float rotation;

        /// <summary>
        /// The final color combining bit tint color and voxel definition tint color
        /// </summary>
        [NonSerialized]
        public Color32 finalColor;
    }


    [Serializable]
    public struct TorchBit {
        public int voxelIndex;
        public ItemDefinition itemDefinition;
        public Vector3 normal;
    }

    [CreateAssetMenu(menuName = "Voxel Play/Model Definition", fileName = "ModelDefinition", order = 102)]
    [HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000033382-model-definitions")]
    public partial class ModelDefinition : ScriptableObject {

        [Tooltip("Size of the model (axis X)")]
        public int sizeX = VoxelPlayEnvironment.CHUNK_SIZE;

        [Tooltip("Size of the model (axis Y)")]
        public int sizeY = VoxelPlayEnvironment.CHUNK_SIZE;

        [Tooltip("Size of the model (axis Z)")]
        public int sizeZ = VoxelPlayEnvironment.CHUNK_SIZE;

        [Tooltip("Offset of the model with respect to the placement position (axis X)")]
        public int offsetX;

        [Tooltip("Offset of the model with respect to the placement position (axis Y)")]
        public int offsetY;

        [Tooltip("Offset of the model with respect to the placement position (axis Z)")]
        public int offsetZ;

        [Tooltip("The duration of the build in seconds")]
        public float buildDuration = 3f;

        [Tooltip("if this model is a tree, no more trees will be allowed in the same chunk")]
        public bool exclusiveTree;

        [Tooltip("Extends bottom voxels if needed to fill empty space under the model and until the terrain surface.")]
        public bool fitToTerrain;

        /// <summary>
        /// Array of model bits.
        /// </summary>
        public ModelBit[] bits;

        /// <summary>
        /// Array of torch data
        /// </summary>
        public TorchBit[] torches;

        /// <summary>
        /// Used temporarily to cache the gameobject generated from the model definition
        /// </summary>
        [NonSerialized, HideInInspector]
        public GameObject modelGameObject;

        /// <summary>
        /// Returns a new model definition
        /// </summary>
        public static ModelDefinition Create(int sizeX, int sizeY, int sizeZ) {
            ModelDefinition md = CreateInstance<ModelDefinition>();
            md.sizeX = sizeX;
            md.sizeY = sizeY;
            md.sizeZ = sizeZ;
            return md;
        }

        /// <summary>
        /// Utility method that creates a new model definition from a list of voxel definitions
        /// </summary>
        public static ModelDefinition Create(int sizeX, int sizeY, int sizeZ, List<VoxelDefinition> voxelDefinitions) {
            if (voxelDefinitions == null) {
                Debug.LogError("ModelDefinition.Create: voxelDefinitions is null");
                return null;
            }
            int totalLength = sizeX * sizeY * sizeZ;
            if (voxelDefinitions.Count < totalLength) {
                Debug.LogError("ModelDefinition.Create: voxelDefinitions does not have enough entries");
                return null;
            }
            ModelDefinition md = Create(sizeX, sizeY, sizeZ);
            md.bits = new ModelBit[totalLength];
            ModelBit bit = new ModelBit();
            int c = 0;
            for (int k = 0; k < totalLength; k++) {
                VoxelDefinition vd = voxelDefinitions[k];
                if (vd == null) continue;
                bit.color = vd.tintColor;
                bit.voxelDefinition = vd;
                bit.voxelIndex = k;
                md.bits[c++] = bit;
            }
            md.ComputeBounds();
            md.ComputeFinalColors();
            return md;
        }

        /// <summary>
        /// Utility method that creates a new model definition from a list of voxels
        /// </summary>
        public static ModelDefinition Create(int sizeX, int sizeY, int sizeZ, List<Voxel> voxels) {
            if (voxels == null) {
                Debug.LogError("ModelDefinition.Create: voxels is null");
                return null;
            }
            int totalLength = sizeX * sizeY * sizeZ;
            if (voxels.Count < totalLength) {
                Debug.LogError("ModelDefinition.Create: voxels does not have enough entries");
                return null;
            }
            ModelDefinition md = Create(sizeX, sizeY, sizeZ);
            md.bits = new ModelBit[totalLength];
            ModelBit bit = new ModelBit();
            int c = 0;
            for (int k = 0; k < totalLength; k++) {
                bit.color = voxels[k].color;
                bit.voxelDefinition = voxels[k].type;
                bit.voxelIndex = k;
                md.bits[c++] = bit;
            }
            md.ComputeBounds();
            md.ComputeFinalColors();
            return md;
        }

        /// <summary>
        /// Preferable method to use when assigning whole new bits. This method calls ComputeBounds() and ComputeFinalColors() as well.
        /// </summary>
        public void SetBits(ModelBit[] bits) {
            this.bits = bits;
            ComputeBounds();
            ComputeFinalColors();
        }

        /// <summary>
        /// Utility method that creates a new model definition from a list of colors using the same voxel definition. If voxel definition is not provided, the default voxel definition will be used
        /// </summary>
        public static ModelDefinition Create(int sizeX, int sizeY, int sizeZ, VoxelDefinition voxelDefinition, List<Color> colors) {
            if (colors == null) {
                Debug.LogError("ModelDefinition.Create: colors is null");
                return null;
            }
            int totalLength = sizeX * sizeY * sizeZ;
            if (colors.Count < totalLength) {
                Debug.LogError("ModelDefinition.Create: colors does not have enough entries");
                return null;
            }
            if (voxelDefinition == null) {
                voxelDefinition = VoxelPlayEnvironment.instance.defaultVoxel;
            }
            ModelDefinition md = Create(sizeX, sizeY, sizeZ);
            md.bits = new ModelBit[totalLength];
            ModelBit bit = new ModelBit();
            int c = 0;
            for (int k = 0; k < totalLength; k++) {
                bit.color = colors[k];
                bit.voxelDefinition = voxelDefinition;
                bit.voxelIndex = k;
                md.bits[c++] = bit;
            }
            md.ComputeBounds();
            md.ComputeFinalColors();
            return md;
        }

        /// <summary>
        /// Returns the voxel index inside this model definition according to the model size
        /// </summary>
        public int GetVoxelIndex(int x, int y, int z) {
            return y * (sizeZ * sizeX) + z * sizeX + x;
        }

        public Vector3 size {
            get {
                return new Vector3(sizeX, sizeY, sizeZ);
            }
        }

        public Vector3 offset {
            get {
                return new Vector3(offsetX, offsetY, offsetZ);
            }
        }

        Bounds _bounds;

        /// <summary>
        /// The real boundary of visible voxels within the model definition
        /// </summary>
        public Bounds bounds {
            get {
                return _bounds;
            }
        }


        int _xMin, _yMin, _zMin;
        int _xMax, _yMax, _zMax;
        public int xMin { get { return _xMin; } }
        public int xMax { get { return _xMax; } }
        public int yMin { get { return _yMin; } }
        public int yMax { get { return _yMax; } }
        public int zMin { get { return _zMin; } }
        public int zMax { get { return _zMax; } }

        void OnEnable() {
            ComputeFinalColors();
            ComputeBounds();
        }


        public void ComputeFinalColors() {
            if (bits == null) return;
            for (int k = 0; k < bits.Length; k++) {
                Color32 color = bits[k].color;
                if (color.r == 0 && color.g == 0 && color.b == 0) {
                    color = Misc.color32White;
                }
                if (bits[k].voxelDefinition != null) {
                    color = color.MultiplyRGB(bits[k].voxelDefinition.tintColor);
                }
                bits[k].finalColor = color;
            }
        }


        public void ComputeBounds() {
            if (bits == null) return;
            _xMin = _zMin = _yMin = int.MaxValue;
            _xMax = _zMax = _yMax = int.MinValue;

            int modelOneYRow = sizeZ * sizeX;
            int modelOneZRow = sizeX;

            for (int b = 0; b < bits.Length; b++) {
                if (bits[b].isEmpty) continue;
                int bitIndex = bits[b].voxelIndex;
                int py = bitIndex / modelOneYRow;
                int remy = bitIndex - py * modelOneYRow;
                int pz = remy / modelOneZRow;
                int px = remy - pz * modelOneZRow;

                if (px < _xMin) _xMin = px;
                if (px > _xMax) _xMax = px;
                if (py < _yMin) _yMin = py;
                if (py > _yMax) _yMax = py;
                if (pz < _zMin) _zMin = pz;
                if (pz > _zMax) _zMax = pz;
            }

            Vector3 size = new Vector3(_xMax - _xMin + 1, _yMax - _yMin + 1, _zMax - _zMin + 1);
            Vector3 center = new Vector3((_xMax + _xMin) * 0.5f, (_yMax + _yMin) * 0.5f, (_zMax + _zMin) * 0.5f);
            _bounds = new Bounds(center, size);
        }
    }
}