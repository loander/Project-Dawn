﻿using UnityEngine;

namespace VoxelPlay {
    public partial class VoxelPlayEnvironment : MonoBehaviour {

        [HideInInspector]
        public Transform fxRoot;

        struct ParticlePoolEntry {
            public bool used;
            public Renderer renderer;
            public Rigidbody rigidBody;
            public BoxCollider collider;
            public Item item;
            public float creationTime, destructionTime;
            public int lastX, lastY, lastZ;
            public float startScale, endScale;
            public float buoyancy;
        }


        public int particlePoolSize = 1000;
        const string VM_FX_ROOT = "VMFX Root";
        const string DAMAGE_INDICATOR = "DamageIndicator";
        GameObject damagedVoxelPrefab, damageParticlePrefab;

        ParticlePoolEntry[] particlePool;
        bool shouldUpdateParticlesLighting;
        int lastParticleIndexUsed;

        void InitParticles() {

            Transform t = transform.Find(VM_FX_ROOT);
            if (t != null) {
                DestroyImmediate(t.gameObject);
            }

            GameObject fx = new GameObject(VM_FX_ROOT);
            fx.hideFlags = HideFlags.DontSave;
            fxRoot = fx.transform;
            fxRoot.hierarchyCapacity = 100;
            fxRoot.SetParent(worldRoot, false);

            if (world.damageParticle == null) {
                world.damageParticle = Resources.Load<GameObject>("VoxelPlay/Prefabs/DamageParticle");
            }

            damageParticlePrefab = world.damageParticle;

            if (particlePool == null) {
                particlePool = new ParticlePoolEntry[particlePoolSize];
                for (int k = 0; k < particlePoolSize; k++) {
                    int i = GetParticleFromPool();
                    ReleaseParticle(i);
                }
            }
            for (int k = 0; k < particlePoolSize; k++) {
                particlePool[k].used = false;
            }
            lastParticleIndexUsed = -1;
            Physics.IgnoreLayerCollision(layerParticles, layerParticles);
        }

        void DestroyParticles() {
            if (fxRoot != null) {
                DestroyImmediate(fxRoot.gameObject);
                fxRoot = null;
            }
        }


        void UpdateParticles() {
            if (particlePool == null)
                return;

            VoxelChunk chunk;
            int voxelIndex;
            int lastCX = -1;
            int lastCY = -1;
            int lastCZ = -1;
            int lastLight = -1;
            float now = Time.time;
            Vector3 scale;
            bool noMoreLightingChecks = false;
            float lastBuoyancy = 0;

            for (int k = 0; k <= lastParticleIndexUsed; k++) {
                if (!particlePool[k].used)
                    continue;
                Renderer renderer = particlePool[k].renderer;
                if (now > particlePool[k].destructionTime || renderer == null) {
                    ReleaseParticle(k);
                    continue;
                }
                Transform particleTransform = renderer.transform;
                if (particlePool[k].endScale > 0) {
                    float t = now - particlePool[k].creationTime;
                    t *= 7f;
                    if (t > 1f) t = 1f;
                    // particle scale is homogeneous in x,y,z so we only compute x
                    float sc = particlePool[k].startScale * (1f - t) + particlePool[k].endScale * t;
                    scale.x = scale.y = scale.z = sc;
                    particleTransform.localScale = scale;
                }
                if (noMoreLightingChecks || !effectiveGlobalIllumination)
                    continue;
                Vector3d currentPos = particleTransform.position;
                int cx = (int)currentPos.x;
                int cy = (int)currentPos.y;
                int cz = (int)currentPos.z;
                if (shouldUpdateParticlesLighting || cx != particlePool[k].lastX || cy != particlePool[k].lastY || cz != particlePool[k].lastZ) {
                    int voxelLight = lastLight;
                    if (lastCX != cx || lastCY != cy || lastCZ != cz) {
                        voxelLight = GetVoxelLightPacked(currentPos, out chunk, out voxelIndex);
                        if ((object)chunk == null || chunk.needsLightmapRebuild) {
                            shouldUpdateParticlesLighting = true;
                            noMoreLightingChecks = true;
                            continue;
                        }
                        lastCX = cx;
                        lastCY = cy;
                        lastCZ = cz;
                        lastLight = voxelLight;
                        lastBuoyancy = chunk.voxels[voxelIndex].GetWaterLevel() >= 10 ? 400f : 0f;
                    }
                    particlePool[k].buoyancy = lastBuoyancy;
                    renderer.sharedMaterial.SetInt(ShaderParams.VoxelLight, voxelLight);
                    particlePool[k].lastX = cx;
                    particlePool[k].lastY = cy;
                    particlePool[k].lastZ = cz;
                }
            }
            shouldUpdateParticlesLighting = false;
        }


        void UpdateParticlesForces() {
            if (particlePool == null)
                return;

            for (int k = 0; k <= lastParticleIndexUsed; k++) {
                if (!particlePool[k].used)
                    continue;
                if (particlePool[k].buoyancy > 0) {
                    particlePool[k].rigidBody.AddForce(0, particlePool[k].buoyancy * Time.fixedDeltaTime, 0);

                }
            }
        }


        void ReleaseParticle(int k) {
            particlePool[k].used = false;
            if (particlePool[k].renderer != null) {
                particlePool[k].rigidBody.isKinematic = true;
                particlePool[k].renderer.enabled = false;
                particlePool[k].item.enabled = false;
                particlePool[k].renderer.transform.position += new Vector3(1000, 1000, 1000);
            }
            particlePool[k].lastX = int.MinValue;
            if (k == lastParticleIndexUsed) {
                for (int j = k - 1; j >= 0; j--) {
                    if (particlePool[j].used) {
                        lastParticleIndexUsed = j;
                        return;
                    }
                }
                lastParticleIndexUsed = -1;
            }
        }


        public GameObject CreateRecoverableVoxel(Vector3d position, VoxelDefinition voxelType, Color32 color) {

            // Set item info
            ItemDefinition dropItem = voxelType.dropItem;
            if (dropItem == null) {
                dropItem = GetItemDefinition(ItemCategory.Voxel, voxelType);
                if (dropItem == null)
                    return null;
            }

            int ppeIndex = GetParticleFromPool();
            if (ppeIndex < 0)
                return null;

            // Set collider size
            particlePool[ppeIndex].collider.size = new Vector3(2f, 2f, 2f); // make voxel float on top of other voxels
            particlePool[ppeIndex].endScale = 0;

            // Set rigidbody behaviour
            particlePool[ppeIndex].rigidBody.freezeRotation = true;

            // Set position & scale
            Renderer particleRenderer = particlePool[ppeIndex].renderer;
            Vector3d particlePosition = position + Random.insideUnitSphere * 0.25f;
            particleRenderer.transform.position = particlePosition;
            particleRenderer.transform.localScale = new Vector3(voxelType.dropItemScale, voxelType.dropItemScale, voxelType.dropItemScale);

            float now = Time.time;

            particlePool[ppeIndex].item.itemDefinition = dropItem;
            particlePool[ppeIndex].item.canPickOnApproach = true;
            particlePool[ppeIndex].item.rb = particlePool[ppeIndex].rigidBody;
            particlePool[ppeIndex].item.creationTime = now;
            particlePool[ppeIndex].item.quantity = voxelType.renderType == RenderType.Water ? GetVoxel(particlePosition, false).GetWaterLevel() / 15f : 1f;

            // Set particle texture
            Material instanceMat = particleRenderer.sharedMaterial;
            switch (dropItem.category) {
                case ItemCategory.Voxel:
                    VoxelDefinition dropVoxelType = dropItem.voxelType;
                    if (dropVoxelType == null) {
                        dropVoxelType = voxelType;
                    }
                    SetParticleMaterialTextures(instanceMat, dropVoxelType, color, false);
                    break;
                default:
                    SetRecoverableVoxelMaterialTextures(instanceMat, dropItem.icon);
                    break;
            }
            instanceMat.mainTextureOffset = Misc.vector2zero;
            instanceMat.mainTextureScale = Misc.vector2one;
            instanceMat.SetInt(ShaderParams.VoxelLight, GetVoxelLightPacked(particlePosition));
            instanceMat.SetFloat(ShaderParams.FlashDelay, 5f);

            // Self-destruct
            particlePool[ppeIndex].creationTime = now;
            particlePool[ppeIndex].destructionTime = now + voxelType.dropItemLifeTime;

            return particlePool[ppeIndex].renderer.gameObject;
        }

        /// <summary>
        /// Sets particle materials
        /// </summary>
        /// <param name="isDamageParticle">If true, it will honor the textureSample property.</param>
        void SetParticleMaterialTextures(Material mat, VoxelDefinition voxelType, Color32 color, bool isDamageParticle) {
            Texture mainTexture, sideTexture, bottomTexture;

            if (isDamageParticle && voxelType.textureSample != null) {
                mainTexture = sideTexture = bottomTexture = voxelType.textureSample;
            } else if (voxelType.renderType == RenderType.CutoutCross) {
                // vegetation only uses sample colors
                mainTexture = sideTexture = bottomTexture = Texture2D.whiteTexture;
                float r = 0.8f + Random.value * 0.4f; // color variation
                color = new Color(voxelType.sampleColor.r * r, voxelType.sampleColor.g * r, voxelType.sampleColor.b * r, 1f);
            } else if (voxelType.renderType == RenderType.Custom && voxelType.material != null && voxelType.material.HasProperty(ShaderParams.MainTex) && voxelType.material.mainTexture != null && voxelType.material.mainTexture.dimension == UnityEngine.Rendering.TextureDimension.Tex2D) {
                mainTexture = sideTexture = bottomTexture = voxelType.material.mainTexture;
                color = Misc.colorWhite;
            } else {
                mainTexture = voxelType.textureThumbnailTop;
                sideTexture = voxelType.textureThumbnailSide;
                bottomTexture = voxelType.textureThumbnailBottom;
            }

            mat.mainTexture = mainTexture;
            mat.SetTexture(ShaderParams.TexSides, sideTexture);
            mat.SetTexture(ShaderParams.TexBottom, bottomTexture);
            mat.SetColor(ShaderParams.Color, color);
        }

        void SetRecoverableVoxelMaterialTextures(Material mat, Texture2D texture) {
            mat.SetTexture(ShaderParams.TexSides, texture);
            mat.SetTexture(ShaderParams.TexBottom, texture);
            mat.SetColor(ShaderParams.Color, Misc.colorWhite);
        }


        int GetParticleFromPool() {
            int count = particlePool.Length;
            int index = -1;
            int particlePoolCurrentIndex = lastParticleIndexUsed;
            for (int k = 0; k < count; k++) {
                if (++particlePoolCurrentIndex >= particlePool.Length) {
                    particlePoolCurrentIndex = 0;
                }
                if (!particlePool[particlePoolCurrentIndex].used) {
                    index = particlePoolCurrentIndex;
                    break;
                }
            }
            if (index < 0)
                return -1;

            if (index > lastParticleIndexUsed) {
                lastParticleIndexUsed = index;
            }

            Renderer particleRenderer;
            if (particlePool[index].renderer == null) {
                GameObject particle = Instantiate(damageParticlePrefab, fxRoot);
                particle.hideFlags = HideFlags.DontSave;
#if UNITY_EDITOR
			if (hideChunksInHierarchy) {
				particle.hideFlags |= HideFlags.HideInHierarchy;
			}
#endif
                particleRenderer = particle.GetComponent<Renderer>();
                particleRenderer.sharedMaterial = Instantiate(particleRenderer.sharedMaterial, fxRoot);
                particleRenderer.sharedMaterial.SetFloat(ShaderParams.AnimSeed, Random.value * Mathf.PI);
                particlePool[index].renderer = particleRenderer;
                particlePool[index].rigidBody = particleRenderer.GetComponent<Rigidbody>();
                particlePool[index].collider = particleRenderer.GetComponent<BoxCollider>();
                particlePool[index].item = particleRenderer.GetComponent<Item>();
                particlePool[index].renderer.gameObject.layer = layerParticles;
                // Ignore collisions with player
                if (characterControllerCollider != null) {
                    Physics.IgnoreCollision(particlePool[index].collider, characterControllerCollider);
                }
            } else {
                particleRenderer = particlePool[index].renderer;
                particlePool[index].rigidBody.isKinematic = false;
                particlePool[index].item.enabled = true;
                particleRenderer.enabled = true;
            }
            particlePool[index].rigidBody.freezeRotation = false;
            particlePool[index].rigidBody.constraints = RigidbodyConstraints.None;
            particlePool[index].rigidBody.velocity = Misc.vector3zero;
            particlePool[index].rigidBody.angularVelocity = Misc.vector3zero;
            particlePool[index].collider.size = Misc.vector3one;
            particlePool[index].used = true;
            particlePool[index].item.itemDefinition = null;
            particlePool[index].item.canPickOnApproach = false;
            particlePool[index].item.pickingUp = false;
            return index;
        }



    }
}
