using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace VoxelPlay {
	[ExecuteInEditMode]
	public class ExportGlobalSettings : MonoBehaviour {

		public int lightCount;
		public Vector4[] lightPosBuffer;
		public Vector4[] lightColorBuffer;
		public float emissionIntensity;
		public Color skyTint;
        public Color fogTint;
		public Color groundColor;
        public Vector4 fogData;
		public float fogAmount;
		public float exposure;
		public float ambientLight;
		public float daylightShadowAtten;
		public float globalSpecularIntensity;
		public bool enableFog;

		void OnEnable () {
			UpdateSettings ();
		}

		void OnValidate () {
			UpdateSettings ();
		}

		void UpdateSettings () {
			// Avoid interfering with Voxel Play environment.			
			if (VoxelPlayEnvironment.instance != null) {
				return;
			}
			if (lightPosBuffer != null && lightPosBuffer.Length > 0) {
				Shader.SetGlobalVectorArray ("_VPPointLightPosition", lightPosBuffer);
			}
			if (lightColorBuffer != null && lightColorBuffer.Length > 0) {
				Shader.SetGlobalVectorArray ("_VPPointLightColor", lightColorBuffer);
			}
			Shader.SetGlobalInt (GPULighting.VoxelPlayLightManager.ShaderParams.GlobalLightCount, lightCount);
			Shader.SetGlobalFloat (ShaderParams.VPEmissionIntensity, emissionIntensity);
			Shader.SetGlobalColor (ShaderParams.VPSkyTint, skyTint);
            Shader.SetGlobalColor(ShaderParams.VPGroundColor, groundColor);
            Shader.SetGlobalColor(ShaderParams.VPFogTint, fogTint);
            Shader.SetGlobalVector (ShaderParams.VPFogData, fogData);
			Shader.SetGlobalFloat (ShaderParams.VPFogAmount, fogAmount);
			Shader.SetGlobalFloat (ShaderParams.VPExposure, exposure);
			Shader.SetGlobalFloat (ShaderParams.VPAmbientLight, ambientLight);
			Shader.SetGlobalFloat (ShaderParams.VPDaylightShadowAtten, daylightShadowAtten);
			Shader.SetGlobalFloat(ShaderParams.VPGlobalSpecular, globalSpecularIntensity);
			if (enableFog) {
				Shader.EnableKeyword ("VOXELPLAY_GLOBAL_USE_FOG");
			} else {
				Shader.DisableKeyword ("VOXELPLAY_GLOBAL_USE_FOG");
			}
		}
	

	}

}