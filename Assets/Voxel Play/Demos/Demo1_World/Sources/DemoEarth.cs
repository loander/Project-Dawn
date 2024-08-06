using UnityEngine;
using VoxelPlay;

namespace VoxelPlayDemos {

    public class DemoEarth : MonoBehaviour {

        public GameObject deerPrefab;
        public GameObject bouncingSpherePrefab;
        VoxelPlayEnvironment env;

        public Texture2D tex;

        void Start() {
            env = VoxelPlayEnvironment.instance;

            // when Voxel Play is ready, do some stuff...
            env.OnInitialized += OnInitialized;

            // when the world has finished generating and rendering
            env.OnWorldLoaded += OnWorldLoaded;

            // Get notified if player is damaged
            VoxelPlayPlayer.instance.OnPlayerGetDamage += OnPlayerGetDamage;

            // Get notified if player is killed
            VoxelPlayPlayer.instance.OnPlayerIsKilled += OnPlayerIsKilled;

        }

        void OnWorldLoaded() {
            // This event triggers when Voxel Play is initialized and the world has finished rendering
            Debug.Log("World fully loaded " + Time.time);
        }

        void OnInitialized() {

            Debug.Log("Voxel Play initialized.");

            // Item definitions are stored in Items folder within the world name folder


            // Add 3 torches to initial player inventory
            VoxelPlayPlayer.instance.AddInventoryItem(env.GetItemDefinition("Torch"), 3);
            VoxelPlayPlayer.instance.AddInventoryItem(env.GetItemDefinition("Torch Red"), 2);

            // Add a shovel (no need to specify quantity it's 1 unit)
            VoxelPlayPlayer.instance.AddInventoryItem(env.GetItemDefinition("Shovel"));

            // Add a sword 
            VoxelPlayPlayer.instance.AddInventoryItem(env.GetItemDefinition("Sword"));

            // Add 20 grenades
            VoxelPlayPlayer.instance.AddInventoryItem(env.GetItemDefinition("Grenade"), 200);

            // Add special instructions after 4 seconds of game running
            Invoke("SpecialKeys", 4);
        }

        void OnPlayerGetDamage(ref int damage, int remainingLifePoints) {
            Debug.Log("Player gets " + damage + " damage points (" + remainingLifePoints + " life points left)");
        }


        void OnPlayerIsKilled() {
            Debug.Log("Player is dead!");
        }


        void SpecialKeys() {
            env.ShowMessage("<color=green>Press <color=yellow>O</color> to throw a ball, <color=yellow>Y</color> to summon a deer, <color=yellow>X</color> to place a brick, <color=yellow>M</color> to levitate a voxel or <color=yellow>R</color> to rotate a voxel :)</color>", 20, true);
        }

        void Update() {
            // If Voxel Play is not yet initialized OR console is visible, do not react to normal player input
            if (!env.initialized || VoxelPlayUI.instance.IsConsoleVisible)
                return;
            if (Input.GetKeyDown(KeyCode.O)) {
                ThrowBall();
            }
            if (Input.GetKeyDown(KeyCode.Y)) {
                SummonDeer();
            }
            if (Input.GetKeyDown(KeyCode.X)) {
                PlaceBrick();
            }
            if (Input.GetKeyDown(KeyCode.M)) {
                LevitateVoxel();
            }
            if (Input.GetKeyDown(KeyCode.R)) {
                //RotateVoxel();
                VoxelDefinition vd;

                vd = env.GetVoxelDefinition("BlueBricks");

                vd.textureSide = tex;
                env.UpdateVoxelDefinitionTextures(vd);

            }
        }


        /// <summary>
        /// Summons a ball that interacts with voxel environment. It can be launched entering in the console "Invoke Demo Ball"
        /// </summary>
        void ThrowBall() {
            GameObject ball = Instantiate(bouncingSpherePrefab);
            ball.transform.position = Camera.main.transform.position + Camera.main.transform.forward;
            ball.GetComponent<Renderer>().material.color = new Color(Random.value * 0.5f + 0.5f, Random.value * 0.5f + 0.5f, Random.value * 0.5f + 0.5f);

            // Throw it! :)
            ball.GetComponent<Rigidbody>().velocity = Camera.main.transform.forward * 10f;
        }

        /// <summary>
        /// Summons a deer prefab
        /// </summary>
        void SummonDeer() {
            VoxelHitInfo hitInfo;
            if (env.RayCast(Camera.main.transform.position, Camera.main.transform.forward, out hitInfo)) {
                // Instantiate deer
                GameObject deer = Instantiate(deerPrefab);
                // Position it on ground
                deer.transform.position = hitInfo.point;
                // Important: instantiate material so different deers can have different colors and smooth lighting; we do it by assigning a random color to provide variation
                deer.GetComponent<MeshRenderer>().material.color = new Color(Random.value * 0.1f + 0.9f, Random.value * 0.1f + 0.9f, 1f);
            }
        }


        /// <summary>
        /// Places a brickWall voxel in front of player. Can be executed in game entering in the console "Invoke Demo PlaceBrick"
        /// </summary>
        void PlaceBrick() {
            // Instead of using Raycast like in the SummonDeer function, will reuse the crosshair data (just another way of doing the same)
            VoxelPlayFirstPersonController fpsController = VoxelPlayFirstPersonController.instance;
            if (fpsController.crosshairOnBlock) {
                Vector3d pos = fpsController.crosshairHitInfo.voxelCenter + fpsController.crosshairHitInfo.normal;
                VoxelDefinition brickWall = env.GetVoxelDefinition("VoxelBrickWall");
                env.VoxelPlace(pos, brickWall);
            }
        }

        /// <summary>
        /// Converts voxel on the crosshair into a dynamic gameobject
        /// </summary>
        void LevitateVoxel() {
            VoxelPlayFirstPersonController fpsController = VoxelPlayFirstPersonController.instance;
            if (fpsController.crosshairOnBlock) {
                VoxelChunk chunk = fpsController.crosshairHitInfo.chunk;
                int voxelIndex = fpsController.crosshairHitInfo.voxelIndex;
                VoxelDefinition type = chunk.voxels[voxelIndex].type;
                if (!type.renderType.supportsDynamic()) {
                    env.ShowError("The voxel type " + type.name + " can't be levitated.");
                    return;
                }
                GameObject obj = env.VoxelGetDynamic(chunk, voxelIndex, true);
                if (obj != null) {
                    Rigidbody rb = obj.GetComponent<Rigidbody>();
                    rb.AddForce(Vector3.up * 500f);
                }
            }
        }


        /// <summary>
        /// Rotates voxel on the crosshair
        /// </summary>
        void RotateVoxel() {
            VoxelPlayFirstPersonController fpsController = VoxelPlayFirstPersonController.instance;
            if (fpsController.crosshairOnBlock) {
                env.VoxelRotate(env.lastHighlightInfo.center, 0f, 15f, 0);
                // You could also call env.VoxelRotateTextures method which switches the textures around the sides. VoxelRotateTextures accepts "turns" of 1, 2, 3, etc. each one meaning a 90º rotation.
            }
        }
    }

}