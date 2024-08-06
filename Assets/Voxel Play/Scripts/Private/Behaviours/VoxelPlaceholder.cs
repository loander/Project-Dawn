using System;
using System.Runtime.CompilerServices;
using System.Collections;
using UnityEngine;

namespace VoxelPlay {

    public class VoxelPlaceholder : MonoBehaviour {

        [NonSerialized]
        public int resistancePointsLeft;

        [NonSerialized]
        public Renderer damageIndicator;

        [NonSerialized]
        public VoxelChunk chunk;

        [NonSerialized]
        public int voxelIndex;

        [NonSerialized]
        public GameObject modelTemplate;

        [NonSerialized]
        public GameObject modelInstance;

        /// <summary>
        /// Default bounds when the placeholder is created
        /// </summary>
        [NonSerialized]
        public Bounds bounds;

        /// <summary>
        /// Stores the rotation when the placeholder was created. Used to detect if the voxel has been rotated and update the placeholder accordingly
        /// </summary>
        [NonSerialized]
        public float currentRotationDegrees;


        /// <summary>
        /// Current bounds in world space, taking into account that the renderer might have changed position or scale by some script
        /// </summary>
        public Bounds GetWorldSpaceBounds() {
            Vector3 center, size;
            if (modelMeshRenderers != null && modelMeshRenderers.Length > 0 && modelMeshRenderers[0] != null) {
                center = modelMeshRenderers[0].bounds.center;
                size = modelMeshRenderers[0].bounds.size;
            } else {
                center = transform.position + bounds.center;
                size = bounds.size;
            }
            return new Bounds(center, size);
        }

        public struct ModelParts {
            public MeshFilter meshFilter;
            /// <summary>
            /// Reference to original mesh of the custom voxel prefab in case it's instanced to adjust its colors due to smooth lighting
            /// </summary>
            public Mesh originalMesh;
            /// <summary>
            /// Last computed tint color when rendered a miv in this position
            /// </summary>
            public Color32 lastMivTintColor; 
        }

        [NonSerialized]
        public ModelParts[] parts;


        public void ResetLastMivTintColors() {
            if (parts == null) return;
            int partsCount = parts.Length;
            for (int k=0;k<partsCount;k++) {
                parts[k].lastMivTintColor = Misc.color32White;
            }
        }

        /// <summary>
        /// Returns a reference to the first mesh filter of this instance
        /// </summary>
        public MeshFilter modelMeshFilter {
            get {
                if (parts == null) return null;
                return parts[0].meshFilter;
            }
        }

        /// <summary>
        /// Returns a reference to the first mesh renderer of this instance
        /// </summary>
        public MeshRenderer modelMeshRenderer {
            get {
                if (modelMeshRenderers == null) return null;
                return modelMeshRenderers[0];
            }
        }

        [NonSerialized]
        public MeshRenderer[] modelMeshRenderers;

        [NonSerialized]
        public Rigidbody rb;

        public Material damageIndicatorMaterial {
            get {
                if (_damageIndicatorMaterial == null && damageIndicator != null) {
                    _damageIndicatorMaterial = Instantiate(damageIndicator.sharedMaterial);
                    damageIndicator.sharedMaterial = _damageIndicatorMaterial;
                }
                return _damageIndicatorMaterial;
            }
        }


        float recoveryTime;
        Material _damageIndicatorMaterial;
        bool[] prevEnableState;

        void OnDestroy() {
            CancelInvoke(nameof(Recover));
            StopAllCoroutines();
        }

        public void StartHealthRecovery(float damageDuration) {
            recoveryTime = Time.time + damageDuration;
            CancelInvoke(nameof(Recover));
            Invoke(nameof(Recover), damageDuration + 0.1f);
        }

        void Recover() {
            float time = Time.time;
            if (time >= recoveryTime) {
                if (chunk != null && chunk.voxels[voxelIndex].typeIndex != 0) {
                    resistancePointsLeft = chunk.voxels[voxelIndex].type.resistancePoints;
                }
                if (damageIndicator != null) {
                    damageIndicator.enabled = false;
                }
            }
        }


        public void SetAutoCancelDynamic(float delay) {
            Invoke(nameof(CancelDynamic), delay + UnityEngine.Random.value);
        }

        public void CancelDynamic() {
            if (this != null && isActiveAndEnabled) {
                StartCoroutine(Consolidate());
            }
        }

        public void CancelDynamicNow() {
            VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
            if (env == null || gameObject.Equals(null))
                return;
            env.VoxelCancelDynamic(this);
        }

        IEnumerator Consolidate() {
            VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
            if (env == null || gameObject.Equals(null))
                yield break;
            if (env.GetChunk(transform.position, out VoxelChunk targetChunk, false)) {
                const float maxDist = 100 * 100;
                if (env == null || gameObject.Equals(null) || env.cameraMain == null)
                    yield break;
                while (env.cameraMain != null && FastVector.SqrDistanceByValue((Vector3)targetChunk.position, env.cameraMain.transform.position) < maxDist && env.ChunkIsInFrustum(targetChunk)) {
                    yield return Misc.waitForOneSecond;
                }
                env.VoxelCancelDynamic(this);
            }
        }

        /// <summary>
        /// Toggles renderers visibility preserving visibility set by other systems
        /// </summary>
        /// <param name="enabled"></param>
        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        public void ToggleRenderers(bool enabled) {
            if (modelMeshRenderers == null) return;

            int renderersCount = modelMeshRenderers.Length;
            if (prevEnableState == null || prevEnableState.Length < renderersCount) {
                prevEnableState = new bool[renderersCount];
            }
            for (int j = 0; j < renderersCount; j++) {
                if (modelMeshRenderers[j] != null) {
                    if (enabled) {
                        modelMeshRenderers[j].enabled = prevEnableState[j];
                    } else {
                        prevEnableState[j] = modelMeshRenderers[j].enabled;
                        modelMeshRenderers[j].enabled = false;
                    }
                }
            }
        }


    }
}