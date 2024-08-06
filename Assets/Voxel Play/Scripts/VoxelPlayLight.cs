using System;
using UnityEngine;
using VoxelPlay.GPULighting;

namespace VoxelPlay {

    [HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000084968-how-point-lights-work-in-voxel-play-")]
    [ExecuteInEditMode]
    public class VoxelPlayLight : MonoBehaviour {

        [NonSerialized] public Light pointLight;

        public bool virtualLight;
        [ColorUsage(showAlpha: false, hdr: true)]
        public Color lightColor = Color.white;
        public float lightIntensity = 1f;
        public float lightRange = 10f;

        public void OnEnable() {
            if (TryGetComponent(out pointLight)) {
                VoxelPlayLightManager.RegisterLight(this);
            }
        }

        private void OnValidate() {
            lightIntensity = Mathf.Max(0, lightIntensity);
            lightRange = Mathf.Max(0, lightRange);
            if (virtualLight || pointLight != null) {
                VoxelPlayLightManager.RegisterLight(this);
            }
        }

        public void OnDisable() {
            VoxelPlayLightManager.UnregisterLight(this);
        }




    }
}
