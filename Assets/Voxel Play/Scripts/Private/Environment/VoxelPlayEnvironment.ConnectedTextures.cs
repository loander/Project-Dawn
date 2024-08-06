using UnityEngine;

namespace VoxelPlay {

    public partial class VoxelPlayEnvironment : MonoBehaviour {

        void InitConnectedTextures() {

            // Find connected textures
            ConnectedTexture[] ctt = Resources.LoadAll<ConnectedTexture>("");
            int cttCount = ctt.Length;
            LogMessage($"{cttCount} connected textures rules found.");
            for (int k = 0; k < cttCount; k++) {
                ConnectedTexture ct = ctt[k];
                if (ct == null) continue;
                if (ct.voxelDefinition == null) {
                    LogMessage($"Connected texture {k + 1} / {cttCount} for {ct.name} ignored. Missing voxel definition.");
                    continue;
                }
                ct.voxelDefinition.connectedTextures = ct;
            }
        }


        void LoadConnectedTextures(VoxelDefinition vd) {

            ConnectedTexture ct = vd.connectedTextures;
            if (ct == null) {
                return;
            }

            LogMessage($"Connected texture for {vd.name} loaded. Adding {ct.config.Length} textures.");
            for (int j = 0; j < ct.config.Length; j++) {
                ct.config[j].textureIndex = vd.textureArrayPacker.AddTexture(ct.config[j].texture, null, ct.config[j].normalMap, null);
            }
            ct.Init();
        }
    }

}
