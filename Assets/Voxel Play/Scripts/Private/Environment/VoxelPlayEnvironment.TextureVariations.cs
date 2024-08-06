using UnityEngine;

namespace VoxelPlay {

    public partial class VoxelPlayEnvironment : MonoBehaviour {

        void InitTextureVariations() {

            // Add texture variations
            TextureVariations[] tvv = Resources.LoadAll<TextureVariations>("");
            int tvCount = tvv.Length;
            LogMessage(tvCount + " texture variations found.");
            for (int k = 0; k < tvCount; k++) {
                TextureVariations tv = tvv[k];
                if (tv == null) continue;
                VoxelDefinition vd = tv.voxelDefinition;
                if (tv.voxelDefinition == null) {
                    LogMessage($"Texture variation {k + 1} / {tvCount} for {tv.name} ignore. Missing voxel definition.");
                    continue;
                }
                vd.textureVariations = tv;
            }
        }

        void LoadTextureVariations(VoxelDefinition vd) {

            TextureVariations tv = vd.textureVariations;
            if (tv == null) {
                return;
            }

            LogMessage($"Texture variation for {vd.name} loaded. Adding {tv.config.Length} textures.");
            for (int j = 0; j < tv.config.Length; j++) {
                Texture2D tex = tv.config[j].texture;
                tv.config[j].textureIndex = vd.textureArrayPacker.AddTexture(tex, null, tv.config[j].normalMap, null, ignoreAlpha: vd.renderType.isOpaque());
            }
            tv.Init();

        }

    }


}
