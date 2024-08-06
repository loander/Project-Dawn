using System.Threading.Tasks;
using UnityEngine;

namespace VoxelPlay {

    [DefaultExecutionOrder(1000)]
    public class TerrainFarChunks : MonoBehaviour {

        public bool enableShadows = true;
        [Range(0, 1)]
        public float shadowIntensity = 0.8f;

        public Color waterColor = new Color(0.247f, 0.396f, 0.745f, 0.87f);

        static class ShaderParams {
            public static int WaterLevel = Shader.PropertyToID("_WaterLevel");
            public static int WaterColor = Shader.PropertyToID("_WaterColor");
            public static int SnapshotData = Shader.PropertyToID("_SnapshotData");
            public static int TerrainFarChunksTex = Shader.PropertyToID("_TerrainFarChunksTex");
            public static int TerrainMaxAltitude = Shader.PropertyToID("_TerrainMaxAltitude");
            public static int ShadowIntensity = Shader.PropertyToID("_ShadowIntensity");

            public const string SKW_SHADOWS = "_SHADOWS";
        }

        const int TEXTURE_SIZE = 4096;
        const int MIN_DISTANCE_UPDATE_SQR = 256 * 256;

        VoxelPlayEnvironment env;
        Texture2D terrainTex;
        Color32[] terrainData;
        Material mat;
        Vector3d lastSnapshotPosition;
        bool capturing;
        int worldExtents;
        bool requestTextureUpdate;
        Camera cam;
        Vector3 boundsMin; // in world space
        VoxelPlayTerrainGenerator tg;
        bool abort;
        Renderer quadRenderer;
        int chunkRange = 128;

        void Start() {
            terrainTex = new Texture2D(TEXTURE_SIZE, TEXTURE_SIZE, TextureFormat.RGBA32, false);
            terrainData = new Color32[TEXTURE_SIZE * TEXTURE_SIZE];
            requestTextureUpdate = true;
            quadRenderer = GetComponent<Renderer>();
            env = VoxelPlayEnvironment.instance;
            lastSnapshotPosition.x = float.MinValue;
            if (env.initialized) {
                Init();
            } else { 
                env.OnInitialized += Init;
            }
        }

        private void OnValidate() {
            UpdateMaterialProperties();    
        }

        private void Init() {
            mat = quadRenderer.material;
            quadRenderer.enabled = true;
            cam = env.cameraMain;
            chunkRange = TEXTURE_SIZE / (VoxelPlayEnvironment.CHUNK_SIZE * 2);
            worldExtents = VoxelPlayEnvironment.CHUNK_SIZE * chunkRange;

            // disable fog as Terrain Far Chunk shader integrates a fog
            env.enableFogSkyBlending = false;
            env.UpdateMaterialProperties();

            // The terrain generators are not thread-safe so we instantiate a copy with same settings to be used in the background thread of this class
            tg = Instantiate(env.world.terrainGenerator);
            tg.Initialize();
        }

        private void OnDestroy() {
            abort = true;
            if (capturing) {
                System.Threading.Thread.Sleep(1000);
            }
            if (terrainTex != null) {
                DestroyImmediate(terrainTex);
            }
        }

        void LateUpdate() {

            if (!env.initialized) return;

            // Align the quad impostor for horizon to the camera
            AlignFarChunksQuad();

            // Update quad texture if we have data and submit to GPU
            if (requestTextureUpdate) {
                requestTextureUpdate = false;
                terrainTex.SetPixels32(terrainData);
                terrainTex.Apply();
                UpdateMaterialProperties();
            }

            // Needs new snapshot?
            if (capturing) return;
            Vector3d snapshotPosition = cam.transform.position;
            if (FastVector.SqrDistanceXZ(ref snapshotPosition, ref lastSnapshotPosition) < MIN_DISTANCE_UPDATE_SQR) return;

            // Start a new capture
            capturing = true;
            lastSnapshotPosition = snapshotPosition;
            boundsMin = snapshotPosition - new Vector3d(worldExtents, 0, worldExtents);
            Capture();
        }

        void UpdateMaterialProperties() {
            if (mat == null) return;
            mat.SetTexture(ShaderParams.TerrainFarChunksTex, terrainTex);
            mat.SetVector(ShaderParams.SnapshotData, new Vector4(boundsMin.x, boundsMin.z, TEXTURE_SIZE, env.visibleChunksDistance * VoxelPlayEnvironment.CHUNK_SIZE));
            mat.SetFloat(ShaderParams.WaterLevel, env.waterLevel + 0.8f);
            mat.SetColor(ShaderParams.WaterColor, waterColor);
            mat.SetFloat(ShaderParams.TerrainMaxAltitude, tg.maxHeight + 1);
            if (enableShadows) {
                mat.EnableKeyword(ShaderParams.SKW_SHADOWS);
                mat.SetFloat(ShaderParams.ShadowIntensity, 1f - shadowIntensity);
            } else {
                mat.DisableKeyword(ShaderParams.SKW_SHADOWS);
            }
        }

        void AlignFarChunksQuad() {
            float dist = cam.farClipPlane * 0.9999f;
            transform.position = cam.transform.position + cam.transform.forward * dist;
            transform.forward = cam.transform.forward;
            float h = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f) * dist * 2f;
            transform.localScale = new Vector3(h * cam.aspect, h, 1f);
        }

        async void Capture() {

            await Task.Run(() =>
            {
                int chunkAxis = chunkRange * 2;
                int numChunks = (int)Mathf.Pow(chunkAxis, 2);

                for (int i = 0; i < numChunks && !abort; i++) {
                    Vector3d curPos;
                    curPos.x = boundsMin.x + (i % chunkAxis) * VoxelPlayEnvironment.CHUNK_SIZE;
                    curPos.z = boundsMin.z + (i / chunkAxis) * VoxelPlayEnvironment.CHUNK_SIZE;
                    curPos.y = 0;
                    Snapshot(curPos);
                }
            });

            requestTextureUpdate = true;
            capturing = false;
        }


        void Snapshot(Vector3d curPos) {

            Color32 color = new Color32(255, 255, 255, 0);
            int offsetX = (int)(curPos.x - boundsMin.x);
            int offsetY = (int)(curPos.z - boundsMin.z);
            for (int z = 0; z < VoxelPlayEnvironment.CHUNK_SIZE; z++) {
                for (int x = 0; x < VoxelPlayEnvironment.CHUNK_SIZE; x++) {
                    tg.GetHeightAndMoisture(x + curPos.x, z + curPos.z, out float altitude, out float moisture);
                    BiomeDefinition biome = env.GetBiome(altitude, moisture);
                    color.a = (byte)(altitude * 255f);
                    Color c = biome.voxelTop.sampleColor;
                    float rn = 200 + 55 * WorldRand.GetValue();
                    color.r = (byte)(c.r * rn);
                    color.g = (byte)(c.g * rn);
                    color.b = (byte)(c.b * rn);

                    int tindex = (z + offsetY) * TEXTURE_SIZE + x + offsetX;
                    terrainData[tindex] = color;
                }
            }
        }
    }

}