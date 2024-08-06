using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

    public partial class VoxelPlayEnvironment : MonoBehaviour {


        static long[] distinctColors = {
            0xFF0000, 0x00FF00, 0x0000FF, 0xFFFF00, 0xFF00FF, 0x00FFFF, 0x000000,
            0x800000, 0x008000, 0x000080, 0x808000, 0x800080, 0x008080, 0x808080,
            0xC00000, 0x00C000, 0x0000C0, 0xC0C000, 0xC000C0, 0x00C0C0, 0xC0C0C0,
            0x400000, 0x004000, 0x000040, 0x404000, 0x400040, 0x004040, 0x404040,
            0x200000, 0x002000, 0x000020, 0x202000, 0x200020, 0x002020, 0x202020,
            0x600000, 0x006000, 0x000060, 0x606000, 0x600060, 0x006060, 0x606060,
            0xA00000, 0x00A000, 0x0000A0, 0xA0A000, 0xA000A0, 0x00A0A0, 0xA0A0A0,
            0xE00000, 0x00E000, 0x0000E0, 0xE0E000, 0xE000E0, 0x00E0E0, 0xE0E0E0
        };

        /// <summary>
        /// Dictionary lookup for the voxel definition by name
        /// </summary>
        Dictionary<string, VoxelDefinition> voxelDefinitionsDict;

        /// <summary>
        /// Set to true if the texture array needs to be recreated (ie. new voxel definitions have been added)
        /// </summary>
        bool requireTextureArrayUpdate;

        /// <summary>
        /// Temporary/session voxels added by users at runtime
        /// </summary>
        List<VoxelDefinition> sessionUserVoxels;
        int sessionUserVoxelsLastIndex;

        TextureArrayPacker mainTextureProvider;
        Dictionary<TextureProviderSettings, TextureArrayPacker> texturesProviders = new Dictionary<TextureProviderSettings, TextureArrayPacker>();

        void InitRenderingMaterials() {

            // Init system arrays and structures
            if (materialsDict == null) {
                materialsDict = new FastHashSet<Material[]>();
            } else {
                materialsDict.Clear();
            }

            // Assign materials to rendering buffers
            if (renderingMaterials == null || renderingMaterials.Length != MAX_MATERIALS_PER_CHUNK) {
                renderingMaterials = new RenderingMaterial[MAX_MATERIALS_PER_CHUNK];
            }
            lastRenderingMaterialIndex = -1;

            if (materialIndices == null) {
                materialIndices = new Dictionary<RenderingMaterialDescriptor, int>();
            } else {
                materialIndices.Clear();
            }

            if (mainTextureProvider == null) {
                TextureProviderSettings settings = TextureProviderSettings.Create(textureSize, textureScale: 1f, enableNormalMap, enableReliefMapping, this);
                mainTextureProvider = GetTextureArrayPacker(settings);
                mainTextureProvider.Clear();
            }

            // In this exact order
            RegisterRenderingMaterial(RenderType.Opaque.GetDefaultMaterial(this), RenderType.Opaque, mainTextureProvider);
            RegisterRenderingMaterial(RenderType.CutoutCross.GetDefaultMaterial(this), RenderType.CutoutCross, mainTextureProvider);
            RegisterRenderingMaterial(RenderType.Cutout.GetDefaultMaterial(this), RenderType.Cutout, mainTextureProvider);
            RegisterRenderingMaterial(RenderType.Water.GetDefaultMaterial(this), RenderType.Water, mainTextureProvider);
            RegisterRenderingMaterial(RenderType.Transp6tex.GetDefaultMaterial(this), RenderType.Transp6tex, mainTextureProvider);
            RegisterRenderingMaterial(RenderType.Cloud.GetDefaultMaterial(this), RenderType.Cloud, mainTextureProvider);
            RegisterRenderingMaterial(RenderType.OpaqueAnimated.GetDefaultMaterial(this), RenderType.OpaqueAnimated, mainTextureProvider);
            RegisterRenderingMaterial(RenderType.OpaqueNoAO.GetDefaultMaterial(this), RenderType.OpaqueNoAO, mainTextureProvider);
        }

        void DisposeTextures() {
            if (voxelDefinitions != null) {
                for (int k = 0; k < voxelDefinitionsCount; k++) {
                    VoxelDefinition vd = voxelDefinitions[k];
                    if (vd != null) {
                        if (vd.textureThumbnailBottom != null && vd.textureThumbnailTopInstanced) DestroyImmediate(vd.textureThumbnailBottom);
                        if (vd.textureThumbnailSide != null && vd.textureThumbnailSideInstanced) DestroyImmediate(vd.textureThumbnailSide);
                        if (vd.textureThumbnailTop != null && vd.textureThumbnailTopInstanced) DestroyImmediate(vd.textureThumbnailTop);
                    }
                }
            }
            if (modelHighlightMat != null) {
                DestroyImmediate(modelHighlightMat);
            }
            if (texturesProviders != null) {
                foreach (TextureArrayPacker tap in texturesProviders.Values) {
                    if (tap != null) tap.Clear();
                }
                texturesProviders.Clear();
            }
            mainTextureProvider = null;
        }


        /// <summary>
        /// Adds a voxel definition to the array. It doesn't do any safety check nor modifies the voxel definition except assigning an index
        /// </summary>
        bool AppendVoxelDefinition(VoxelDefinition vd) {

            if (vd == null)
                return false;

            if (vd.index > 0 && vd.index < voxelDefinitionsCount && voxelDefinitions[vd.index] == vd)
                return false; // already added

            // Resize voxel definitions array?
            if (voxelDefinitionsCount >= voxelDefinitions.Length) {
                voxelDefinitions = voxelDefinitions.Extend();
            }

            voxelDefinitions[voxelDefinitionsCount] = vd;
            vd.index = (ushort)voxelDefinitionsCount;
            voxelDefinitionsCount++;
            voxelDefinitionsDict[vd.name] = vd;

            return true;
        }


        /// <summary>
        /// Inserts an user voxel definition to the array. It doesn't do any safety check nor modifies the voxel definition except assigning an index
        /// </summary>
        bool InsertUserVoxelDefinition(VoxelDefinition vd) {

            if (vd == null || voxelDefinitions == null)
                return false;

            if (vd.index > 0 && vd.index < voxelDefinitionsCount && voxelDefinitions[vd.index] == vd)
                return false; // already added

            // Resize voxel definitions array?
            if (voxelDefinitionsCount >= voxelDefinitions.Length) {
                voxelDefinitions = voxelDefinitions.Extend();
            }

            // Make space
            for (int k = voxelDefinitionsCount - 1; k > sessionUserVoxelsLastIndex + 1; k--) {
                voxelDefinitions[k] = voxelDefinitions[k - 1];
                voxelDefinitions[k].index++;
            }
            sessionUserVoxelsLastIndex++;
            vd.index = (ushort)sessionUserVoxelsLastIndex;
            voxelDefinitions[sessionUserVoxelsLastIndex] = vd;
            voxelDefinitionsCount++;
            voxelDefinitionsDict[vd.name] = vd;

            sessionUserVoxels.Add(vd);

            return true;
        }

        void AddVoxelTextures(VoxelDefinition vd) {

            if (!AppendVoxelDefinition(vd)) {
                return;
            }

            LogMessage("Loading voxel definition: " + vd.name);

            // Autofix certain non supported properties
            if (vd.navigatable) {
                vd.navigatable = vd.renderType.supportsNavigation();
            }

            // Ensures opaque is set to 15 for solid voxels
            if (vd.renderType.isOpaque()) vd.opaque = FULL_OPAQUE;

            vd.isSolid = vd.navigatable || vd.opaque == FULL_OPAQUE;

            // Sets if this voxel should render an additional cutout cross to make the leaves denser
            vd.usesDenseLeaves = vd.denseLeaves && denseTrees;

            // Bevel compatibility check
            if (!vd.renderType.supportsBevel()) vd.supportsBevel = false;

            // Check if custom model has collider and proper materials
            vd.prefabUsesCollider = false;
            if (vd.renderType == RenderType.Custom) {
                AddVoxelTexturesCustom(vd);
            } else {
                AddVoxelTexturesNonCustom(vd);
            }

            GetVoxelThumbnails(vd);
        }

        static List<Renderer> tmpRenderers = new List<Renderer>();
        void AddVoxelTexturesCustom(VoxelDefinition vd) {
            if (vd.model == null) {
                // custom voxel is missing model so we assign a default cube
                vd.model = GetDefaultVoxelPrefab();
            }
            vd.prefab = vd.model;
            if (vd.model != null) {
                if (vd.prefabMaterial != CustomVoxelMaterial.PrefabMaterial) {
                    Material instancingMat = null;
                    switch (vd.prefabMaterial) {
                        case CustomVoxelMaterial.VertexLit: instancingMat = Resources.Load<Material>("VoxelPlay/Materials/VP Model VertexLit"); break;
                        case CustomVoxelMaterial.Texture: instancingMat = Resources.Load<Material>("VoxelPlay/Materials/VP Model Texture"); break;
                        case CustomVoxelMaterial.TextureBumpMap: instancingMat = Resources.Load<Material>("VoxelPlay/Materials/VP Model Texture BumpMap"); break;
                        case CustomVoxelMaterial.TextureAlpha: instancingMat = Resources.Load<Material>("VoxelPlay/Materials/VP Model Texture Alpha"); break;
                        case CustomVoxelMaterial.TextureAlphaDoubleSided: instancingMat = Resources.Load<Material>("VoxelPlay/Materials/VP Model Texture Alpha Double Sided"); break;
                        case CustomVoxelMaterial.TextureTriplanar: instancingMat = Resources.Load<Material>("VoxelPlay/Materials/VP Model Texture Triplanar"); break;
                        case CustomVoxelMaterial.TextureCutout: instancingMat = Resources.Load<Material>("VoxelPlay/Materials/VP Model Texture Cutout"); break;
                    }
                    if (instancingMat != null) {
                        instancingMat = Instantiate(instancingMat);
                        if (!vd.gpuInstancing) instancingMat.DisableKeyword(SKW_VOXELPLAY_GPU_INSTANCING);
                        vd.prefab = Instantiate(vd.model);
                        vd.prefab.SetActive(false);
                        vd.prefab.transform.SetParent(transform, false);
                        vd.prefab.GetComponentsInChildren(tmpRenderers);
                        int rrCount = tmpRenderers.Count;
                        for (int k = 0; k < rrCount; k++) {
                            Material refMat = tmpRenderers[k].sharedMaterial;
                            if (refMat != null) {
                                if (refMat.HasProperty(ShaderParams.Color) && instancingMat.HasProperty(ShaderParams.Color)) {
                                    instancingMat.SetColor(ShaderParams.Color, refMat.GetColor(ShaderParams.Color));
                                }
                                if (refMat.HasProperty(ShaderParams.MainTex) && instancingMat.HasProperty(ShaderParams.MainTex)) {
                                    instancingMat.SetTexture(ShaderParams.MainTex, refMat.GetTexture(ShaderParams.MainTex));
                                }
                                if (refMat.HasProperty(ShaderParams.BumpMap) && instancingMat.HasProperty(ShaderParams.BumpMap)) {
                                    instancingMat.SetTexture(ShaderParams.BumpMap, refMat.GetTexture(ShaderParams.BumpMap));
                                }
                            }
                            tmpRenderers[k].sharedMaterial = instancingMat;
                        }
                    }
                } else {
                    // disables instancing material to prevent the issue of toggling on / off GPU instancing at voxel definition level and leaving the material keyword set
                    if (!vd.gpuInstancing) {
                        Material mat = vd.material;
                        if (mat != null) {
                            mat.DisableKeyword(SKW_VOXELPLAY_GPU_INSTANCING);
                        }
                    }
                }

                // Override main texture?
                if (vd.overrideMainTexture && vd.textureSample != null) {
                    if (vd.prefab == vd.model) {
                        vd.prefab = Instantiate(vd.model, transform, false);
                        vd.prefab.SetActive(false);
                    }
                    vd.prefab.GetComponentsInChildren(tmpRenderers);
                    int rrCount = tmpRenderers.Count;
                    for (int k = 0; k < rrCount; k++) {
                        Material refMat = tmpRenderers[k].sharedMaterial;
                        if (refMat != null && refMat.HasProperty(ShaderParams.MainTex)) {
                            Material mat = Instantiate(refMat);
                            mat.mainTexture = vd.textureSample;
                            if (vd.overrideMainTextureOffset != Vector2.zero) {
                                mat.SetTextureOffset(ShaderParams.MainTex, vd.overrideMainTextureOffset);
                            }
                            tmpRenderers[k].sharedMaterial = mat;
                        }
                    }
                }

                // annotate if model has collider
                Collider prefabCollider = vd.prefab.GetComponentInChildren<Collider>();
                bool hasPrefabCollider = prefabCollider != null;
                if (vd.gpuInstancing) {
                    if (vd.createGameObject) {
                        vd.prefabUsesCollider = hasPrefabCollider;
                    }
                } else {
                    vd.prefabUsesCollider = hasPrefabCollider;
                }
                if (hasPrefabCollider && applicationIsPlaying && prefabCollider is BoxCollider) {
                    StartCoroutine(ComputePrefabBoxColliderBounds(vd));
                }

                // annotate if model has a rigidbody
                vd.prefabUsesRigidbody = vd.prefab.GetComponentInChildren<Rigidbody>() != null;

            }
            // Some material fixes. If compute shaders option is enabled, replace shaders with appropriate and also fix the Voxel Light value.
            Material prefabMat = vd.material;
            if (prefabMat != null) {
                if (prefabMat.HasProperty(ShaderParams.VoxelLight) && prefabMat.shader.name.Contains("/Model")) {
                    float voxelLight = prefabMat.GetFloat(ShaderParams.VoxelLight);
                    if (Mathf.Approximately(voxelLight, 1f)) {
                        prefabMat.SetFloat(ShaderParams.VoxelLight, 15);
                    }
                }
                if (useComputeBuffers) {
                    if (prefabMat.shader.name.Contains("Models/Texture/Opaque")) {
                        prefabMat = Instantiate(prefabMat);
                        prefabMat.shader = Shader.Find("Voxel Play/Models/GPU Instanced Indirect/Texture/Opaque");
                        vd.material = prefabMat;
                    } else
                    if (prefabMat.shader.name.Contains("Models/Texture/Alpha")) {
                        prefabMat = Instantiate(prefabMat);
                        prefabMat.shader = Shader.Find("Voxel Play/Models/GPU Instanced Indirect/Texture/Alpha");
                        vd.material = prefabMat;
                    } else
                    if (prefabMat.shader.name.Contains("Models/Texture/Cutout")) {
                        prefabMat = Instantiate(prefabMat);
                        prefabMat.shader = Shader.Find("Voxel Play/Models/GPU Instanced Indirect/Texture/Cutout");
                        vd.material = prefabMat;
                    } else
                    if (prefabMat.shader.name.Contains("Models/Texture/Triplanar")) {
                        prefabMat = Instantiate(prefabMat);
                        prefabMat.shader = Shader.Find("Voxel Play/Models/GPU Instanced Indirect/Texture/Triplanar");
                        vd.material = prefabMat;
                    } else
                    if (prefabMat.shader.name.Contains("Models/VertexLit")) {
                        prefabMat = Instantiate(prefabMat);
                        prefabMat.shader = Shader.Find("Voxel Play/Models/GPU Instanced Indirect/VertexLit");
                        vd.material = prefabMat;
                    }
                }
            }

            if (vd.textureSide == null) {
                // assign default texture sample for inventory icons
                Material modelMaterial = vd.material;
                if (vd.icon == null && modelMaterial != null && modelMaterial.mainTexture != null && modelMaterial.mainTexture is Texture2D) {
                    vd.icon = (Texture2D)modelMaterial.mainTexture;
                }
            } else {
                Color32[] colors = vd.textureSide.GetPixels32();
                vd.sampleColor = colors[Random.Range(0, colors.Length)];
            }
        }

        void AddVoxelTexturesNonCustom(VoxelDefinition vd) {

            // gpu instancing option only available for custom voxels
            if (vd.gpuInstancing) {
                vd.gpuInstancing = false;
            }
            // get default water voxel definition
            if (vd.renderType == RenderType.Water && currentWaterVoxelDefinition == null) {
                currentWaterVoxelDefinition = vd;
            }

            // Assign rendering material
            Material mat = vd.GetOverrideMaterial();
            if (vd.overrideMaterial && !vd.texturesByMaterial && mat != null && mat.HasProperty(ShaderParams.MainTex) && mat.mainTexture != null && mat.mainTexture.dimension != UnityEngine.Rendering.TextureDimension.Tex2DArray) {
                ShowError("Voxel definition " + vd.name + " overrides material but shader does not use texture array. Use a compatible shader or enable the 'Textures by Material' option and provide your own textures in the material itself. Using default material for its render type.");
                mat = null;
            }
            if (mat == null) {
                mat = vd.renderType.GetDefaultMaterial(this);
            }

            if (vd.textureSideIndices == null || vd.textureSideIndices.Length != 4) {
                vd.textureSideIndices = new TextureRotationIndices[4];
            }

            TextureArrayPacker textureArrayPacker = null;

            if (vd.overrideMaterial && vd.texturesByMaterial) {
                vd.materialBufferIndex = RegisterRenderingMaterialNoTextureArray(mat);
            } else {

                // Picks a texture packer
                textureArrayPacker = mainTextureProvider;
                if (vd.texturesCustomPacking) {
                    TextureProviderSettings settings = TextureProviderSettings.Create(vd.texturesPackingSize, vd.texturesPackingScale, vd.texturesPackingNormalMap, vd.texturesPackingReliefMap, this);
                    textureArrayPacker = GetTextureArrayPacker(settings);
                }

                if (textureArrayPacker == null) {
                    ShowError("Couldn't find a texture provider for voxel definition " + vd.name);
                    return;
                }

                // Is this texture provider provider full? Texture Arrays have a limit of 2048 slices on Shader Model 4.5 or later. Otherwise, the limit is 256.
                bool animated = vd.renderType.supportsTextureAnimation();
                int textureCount = 12;
                if (vd.connectedTextures != null) textureCount += vd.connectedTextures.config.Length * 2;
                if (vd.textureVariations != null) textureCount += vd.textureVariations.config.Length * 2;
                if (animated && vd.animationTextures != null) textureCount += vd.animationTextures.Length * 6;

                if (textureArrayPacker != null && !textureArrayPacker.HasAvailableTextureSlices(textureCount)) {
                    Material templateMat = mat;
                    // Look for another compatible texture array packer already registered
                    bool foundSuitableTextureArray = false;
                    for (int k = lastRenderingMaterialIndex; k >= 0; k--) {
                        Material rmTemplateMaterial = renderingMaterials[k].descriptor.templateMaterial;
                        TextureArrayPacker rmProvider = renderingMaterials[k].descriptor.textureProvider;
                        if (rmTemplateMaterial == templateMat && rmProvider != null && rmProvider.settings == textureArrayPacker.settings && rmProvider.HasAvailableTextureSlices(textureCount)) {
                            textureArrayPacker = rmProvider;
                            vd.materialBufferIndex = k;
                            foundSuitableTextureArray = true;
                            break;
                        }
                    }
                    if (!foundSuitableTextureArray) {
                        // There's no other available texture array with same settings for this material, so create a new one
                        textureArrayPacker = new TextureArrayPacker(textureArrayPacker.settings);
                        texturesProviders[textureArrayPacker.settings] = textureArrayPacker;
                        vd.materialBufferIndex = RegisterRenderingMaterial(mat, vd.renderType, textureArrayPacker, true);
                    }
                } else {
                    // Register the rendering material
                    vd.materialBufferIndex = RegisterRenderingMaterial(mat, vd.renderType, textureArrayPacker);
                }

                vd.textureArrayPacker = textureArrayPacker;

                bool supportsEmission = vd.renderType.supportsEmission();

                // Ensure transparent voxel definition has all textures set; assign a default transparent one if needed
                if (vd.renderType == RenderType.Transp6tex) {
                    if (vd.textureTop == null) vd.textureTop = textureArrayPacker.GetDefaultTransparentTexture();
                    if (vd.textureSide == null) vd.textureSide = textureArrayPacker.GetDefaultTransparentTexture();
                    if (vd.textureBottom == null) vd.textureBottom = textureArrayPacker.GetDefaultTransparentTexture();
                    if (vd.textureRight == null) vd.textureRight = textureArrayPacker.GetDefaultTransparentTexture();
                    if (vd.textureForward == null) vd.textureForward = textureArrayPacker.GetDefaultTransparentTexture();
                    if (vd.textureLeft == null) vd.textureLeft = textureArrayPacker.GetDefaultTransparentTexture();
                } else if (vd.textureTopNRM != null) {
                    // auto-fix side & bottom normal map assignment
                    if (vd.textureSideNRM == null && vd.textureSide == vd.textureTop) vd.textureSideNRM = vd.textureTopNRM;
                    if (vd.textureBottomNRM == null && vd.textureBottom == vd.textureTop) vd.textureBottomNRM = vd.textureTopNRM;
                }

                bool ignoresAlpha = vd.renderType.isOpaque();

                // Compute voxel definition texture indices including rotations
                // Note: when using normal and/or displacement maps, if the main texture is equal to a previous texture, the normal and/or displacement map of the first texture will be used.
                // This occur because the normal and displacement maps are interleaved in the texture array and textures can't be repeated in the array so normal and disp will be already next to the first texture added
                vd.textureIndexTop = textureArrayPacker.AddTexture(vd.textureTop, supportsEmission ? vd.textureTopEmission : null, vd.textureTopNRM, vd.textureTopDISP, !animated, ignoreAlpha: ignoresAlpha);
                if (animated) {
                    for (int k = 0; k < vd.animationTextures.Length; k++) {
                        textureArrayPacker.AddTexture(vd.animationTextures[k].textureTop != null ? vd.animationTextures[k].textureTop : vd.textureTop, null, null, null, false, ignoreAlpha: ignoresAlpha);
                    }
                }
                vd.textureIndexSide = textureArrayPacker.AddTexture(vd.textureSide, supportsEmission ? vd.textureSideEmission : null, vd.textureSideNRM, vd.textureSideDISP, !animated, ignoreAlpha: ignoresAlpha);
                if (animated) {
                    for (int k = 0; k < vd.animationTextures.Length; k++) {
                        textureArrayPacker.AddTexture(vd.animationTextures[k].textureSide != null ? vd.animationTextures[k].textureSide : vd.textureSide, null, null, null, false, ignoreAlpha: ignoresAlpha);
                    }
                }
                vd.textureIndexBottom = textureArrayPacker.AddTexture(vd.textureBottom, supportsEmission ? vd.textureBottomEmission : null, vd.textureBottomNRM, vd.textureBottomDISP, !animated, ignoreAlpha: ignoresAlpha);
                if (animated) {
                    for (int k = 0; k < vd.animationTextures.Length; k++) {
                        textureArrayPacker.AddTexture(vd.animationTextures[k].textureBottom != null ? vd.animationTextures[k].textureBottom : vd.textureBottom, null, null, null, false, ignoreAlpha: ignoresAlpha);
                    }
                }

                if (vd.renderType.numberOfTextures() == 6) {
                    if (vd.textureTopNRM != null) {
                        // auto-fix side normal map assignment
                        if (vd.textureRightNRM == null && vd.textureRight == vd.textureTop) vd.textureRightNRM = vd.textureTopNRM;
                        if (vd.textureForwardNRM == null && vd.textureForward == vd.textureTop) vd.textureForwardNRM = vd.textureTopNRM;
                        if (vd.textureLeftNRM == null && vd.textureLeft == vd.textureTop) vd.textureLeftNRM = vd.textureTopNRM;
                    }

                    int textureIndexRight = vd.textureIndexRight = textureArrayPacker.AddTexture(vd.textureRight, supportsEmission ? vd.textureRightEmission : null, vd.textureRightNRM, vd.textureRightDISP, ignoreAlpha: ignoresAlpha);
                    int textureIndexForward = vd.textureIndexForward = textureArrayPacker.AddTexture(vd.textureForward, supportsEmission ? vd.textureForwardEmission : null, vd.textureForwardNRM, vd.textureForwardDISP, ignoreAlpha: ignoresAlpha);
                    int textureIndexLeft = vd.textureIndexLeft = textureArrayPacker.AddTexture(vd.textureLeft, supportsEmission ? vd.textureLeftEmission : null, vd.textureLeftNRM, vd.textureLeftDISP, ignoreAlpha: ignoresAlpha);

                    vd.textureSideIndices[0] = new TextureRotationIndices {
                        forward = textureIndexForward,
                        right = textureIndexRight,
                        back = vd.textureIndexSide,
                        left = textureIndexLeft
                    };
                    vd.textureSideIndices[1] = new TextureRotationIndices {
                        forward = textureIndexLeft,
                        right = textureIndexForward,
                        back = textureIndexRight,
                        left = vd.textureIndexSide
                    };
                    vd.textureSideIndices[2] = new TextureRotationIndices {
                        forward = vd.textureIndexSide,
                        right = textureIndexLeft,
                        back = textureIndexForward,
                        left = textureIndexRight
                    };
                    vd.textureSideIndices[3] = new TextureRotationIndices {
                        forward = textureIndexRight,
                        right = vd.textureIndexSide,
                        back = textureIndexLeft,
                        left = textureIndexForward
                    };
                } else {
                    vd.textureIndexForward = vd.textureIndexLeft = vd.textureIndexRight = vd.textureIndexSide;
                    vd.textureSideIndices[0] = vd.textureSideIndices[1] = vd.textureSideIndices[2] = vd.textureSideIndices[3] = new TextureRotationIndices {
                        forward = vd.textureIndexSide,
                        right = vd.textureIndexSide,
                        back = vd.textureIndexSide,
                        left = vd.textureIndexSide
                    };
                }

                LoadConnectedTextures(vd);
                LoadTextureVariations(vd);
            }

            // Annotate voxel definition greedy mesher
            vd.greedyMesherLitAO = renderingMaterials[vd.materialBufferIndex].greedyMesherLitAO;
            vd.greedyMesherLit = renderingMaterials[vd.materialBufferIndex].greedyMesherLit;

            // Compute voxel sample color
            if (vd.renderType == RenderType.CutoutCross && vd.sampleColor.a == 0) {
                AnalyzeGrassTexture(vd, vd.textureSample != null ? vd.textureSample : vd.textureSide);
            } else {
                if (vd.textureSample != null) {
                    Color32[] colors = vd.textureSample.GetPixels32();
                    vd.sampleColor = colors[Random.Range(0, colors.Length)];
                } else if (vd.textureIndexSide > 0 && textureArrayPacker != null) {
                    Color32[] colors = textureArrayPacker.textures[vd.textureIndexSide].colorsAndEmission;
                    vd.sampleColor = colors[Random.Range(0, colors.Length)];
                } else if (vd.textureSide != null) {
                    Color32[] colors = vd.textureSide.GetPixels32();
                    vd.sampleColor = colors[Random.Range(0, colors.Length)];
                }
            }
        }


        TextureArrayPacker GetTextureArrayPacker(TextureProviderSettings settings) {
            if (!texturesProviders.TryGetValue(settings, out TextureArrayPacker provider)) {
                provider = new TextureArrayPacker(settings);
                texturesProviders[settings] = provider;
            }
            return provider;
        }

        IEnumerator ComputePrefabBoxColliderBounds(VoxelDefinition vd) {
            bool oldActiveState = vd.prefab.activeSelf;
            vd.prefab.SetActive(false);
            GameObject dummy = Instantiate(vd.prefab);
            // Disable all components to avoid undesired effects
            Component[] components = dummy.GetComponents<Component>();
            for (int k = 0; k < components.Length; k++) {
                MonoBehaviour mono = components[k] as MonoBehaviour;
                if (mono != null) {
                    mono.enabled = false;
                }
            }
            vd.prefab.SetActive(oldActiveState);
            dummy.hideFlags = HideFlags.HideAndDontSave;
            dummy.SetActive(true);
            dummy.transform.position = new Vector3(0, 10000, 10000);
            dummy.transform.rotation = Misc.quaternionZero;
            dummy.transform.localScale = Misc.vector3one;

            yield return new WaitForEndOfFrame();
            BoxCollider collider = dummy.GetComponentInChildren<BoxCollider>();
            Bounds bounds = collider.bounds;
            bounds.center -= dummy.transform.position;
            vd.prefabColliderBounds = bounds;
            Destroy(dummy);
        }



        void AnalyzeGrassTexture(VoxelDefinition vd, Texture2D tex) {
            if (tex == null) {
                Debug.Log("AnalyzeGrassTexture: texture not found for " + vd.name);
                return;
            }
            // get sample color (random pixel from texture raw data)
            Color[] colors = tex.GetPixels();
            int tw = tex.width;
            int th = tex.height;
            int pos = 4 * tw + tw * 3 / 4;
            if (pos >= colors.Length)
                pos = colors.Length - 1;
            for (int k = pos; k > 0; k--) {
                if (colors[k].a > 0.5f) {
                    vd.sampleColor = colors[k];
                    break;
                }
            }
            // get grass dimensions
            int xmin, xmax, ymin, ymax;
            xmin = tw;
            xmax = 0;
            ymin = th;
            ymax = 0;
            for (int y = 0; y < th; y++) {
                int yy = y * tw;
                for (int x = 0; x < tw; x++) {
                    if (colors[yy + x].a > 0.5f) {
                        if (x < xmin)
                            xmin = x;
                        if (x > xmax)
                            xmax = x;
                        if (y < ymin)
                            ymin = y;
                        if (y > ymax)
                            ymax = y;
                    }
                }
            }
            float w = (xmax - xmin + 1f) / tw;
            float h = (ymax - ymin + 1f) / th;
            vd.scale = new Vector3(w, h, w);
        }

        void GetVoxelThumbnails(VoxelDefinition vd) {

            // vegetation uses sample colors, not textures for particles
            if (vd.renderType == RenderType.CutoutCross || vd.renderType == RenderType.Invisible || !vd.canBeCollected) return;

            Texture2D top, side, bottom;
            top = side = bottom = null;
            if (vd.overrideMaterial && vd.texturesByMaterial) {
                Material mat = vd.overrideMaterialNonGeo;
                if (mat == null) {
                    ShowError($"Missing override material for {vd.name} in the voxel definition.");
                } else {
                    if (!mat.HasTexture(ShaderParams.MainTex) && !mat.HasTexture(ShaderParams.BaseMap)) {
                        top = side = bottom = Texture2D.blackTexture;
                    } else if (mat.mainTexture is Texture2D tex) {
#if UNITY_EDITOR
                    string path = UnityEditor.AssetDatabase.GetAssetPath(tex);
                    if (!string.IsNullOrEmpty(path)) {
                        UnityEditor.TextureImporter timp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
                        if (timp != null && !timp.isReadable) {
                            timp.isReadable = true;
                            timp.SaveAndReimport();
                        }
                    }
#endif
                        top = side = bottom = tex;
                    }
                }
            } else {
                if (vd.renderType == RenderType.Custom && vd.textureSample != null) {
                    top = side = bottom = vd.textureSample;
                } else {
                    top = vd.textureTop;
                    side = vd.textureSide;
                    bottom = vd.textureBottom;
                }
            }
            vd.textureThumbnailTopInstanced = vd.textureThumbnailSideInstanced = vd.textureThumbnailBottomInstanced = false;
            int res = world.dropVoxelTextureResolution;

            if (top != null) {
                if (res != 0 && (top.width != res || top.height != res)) {
                    vd.textureThumbnailTop = Instantiate(top);
                    vd.textureThumbnailTopInstanced = true;
                    vd.textureThumbnailTop.hideFlags = HideFlags.DontSave;
                    TextureTools.ScaleTexture(vd.textureThumbnailTop, res, res, FilterMode.Point);
                } else {
                    vd.textureThumbnailTop = top;
                }
            }
            if (side != null) {
                if (res != 0 && (side.width != res || side.height != res)) {
                    vd.textureThumbnailSide = Instantiate(side);
                    vd.textureThumbnailSideInstanced = true;
                    vd.textureThumbnailSide.hideFlags = HideFlags.DontSave;
                    TextureTools.ScaleTexture(vd.textureThumbnailSide, res, res, FilterMode.Point);
                } else {
                    vd.textureThumbnailSide = side;
                }
            }
            if (bottom != null) {
                if (res != 0 && (bottom.width != res || bottom.height != res)) {
                    vd.textureThumbnailBottom = Instantiate(bottom);
                    vd.textureThumbnailBottomInstanced = true;
                    vd.textureThumbnailBottom.hideFlags = HideFlags.DontSave;
                    TextureTools.ScaleTexture(vd.textureThumbnailBottom, res, res, FilterMode.Point);
                } else {
                    vd.textureThumbnailBottom = bottom;
                }
            }
        }


        void LoadWorldTextures() {

            requireTextureArrayUpdate = false;

            // Clear definitions
            if (voxelDefinitions != null) {
                // clear any previous voxel definition by resetting the index field
                for (int k = 0; k < voxelDefinitionsCount; k++) {
                    VoxelDefinition vd = voxelDefinitions[k];
                    if (vd != null) {
                        vd.Reset();
                    }
                }
            } else {
                voxelDefinitions = new VoxelDefinition[128];
            }
            voxelDefinitionsCount = 0;
            if (voxelDefinitionsDict == null) {
                voxelDefinitionsDict = new Dictionary<string, VoxelDefinition>();
            } else {
                voxelDefinitionsDict.Clear();
            }
            if (sessionUserVoxels == null) {
                sessionUserVoxels = new List<VoxelDefinition>();
            }

            InitConnectedTextures();

            InitTextureVariations();

            // The null voxel definition
            VoxelDefinition nullVoxelDefinition = ScriptableObject.CreateInstance<VoxelDefinition>();
            nullVoxelDefinition.name = "Null";
            nullVoxelDefinition.hidden = true;
            nullVoxelDefinition.canBeCollected = false;
            nullVoxelDefinition.ignoresRayCast = true;
            nullVoxelDefinition.renderType = RenderType.Invisible;
            nullVoxelDefinition.doNotSave = true;
            AddVoxelTextures(nullVoxelDefinition);

            // Add a reserved slot
            VoxelDefinition reserved = ScriptableObject.CreateInstance<VoxelDefinition>();
            reserved.name = "Reverved";
            reserved.hidden = true;
            reserved.canBeCollected = false;
            reserved.ignoresRayCast = true;
            reserved.renderType = RenderType.Invisible;
            reserved.doNotSave = true;
            AddVoxelTextures(reserved);

            // Add the default hole voxel definition
            VoxelDefinition hole = ScriptableObject.CreateInstance<VoxelDefinition>();
            hole.name = "DefaultHole";
            hole.hidden = true;
            hole.canBeCollected = false;
            hole.ignoresRayCast = true;
            hole.renderType = RenderType.Invisible;
            hole.doNotSave = true;
            AddVoxelTextures(hole);

            // Check default voxel
            if (defaultVoxel == null) {
                defaultVoxel = Resources.Load<VoxelDefinition>("VoxelPlay/Defaults/DefaultVoxel");
            }
            defaultVoxel.doNotSave = true;
            AddVoxelTextures(defaultVoxel);

            if (defaultWaterVoxel == null) {
                defaultWaterVoxel = Resources.Load<VoxelDefinition>("VoxelPlay/Defaults/Water/VoxelWaterSea");
            }
            currentWaterVoxelDefinition = defaultWaterVoxel;


            // Add all biome textures
            if (world.biomes != null) {
                for (int k = 0; k < world.biomes.Length; k++) {
                    BiomeDefinition biome = world.biomes[k];
                    if (biome == null)
                        continue;
                    if (biome.voxelTop != null) {
                        AddVoxelTextures(biome.voxelTop);
                        if (biome.voxelTop.biomeDirtCounterpart == null) {
                            biome.voxelTop.biomeDirtCounterpart = biome.voxelDirt;
                        }
                    }
                    AddVoxelTextures(biome.voxelDirt);
                    if (biome.vegetation != null) {
                        for (int v = 0; v < biome.vegetation.Length; v++) {
                            AddVoxelTextures(biome.vegetation[v].vegetation);
                        }
                    }
                    if (biome.underwaterVegetation != null) {
                        for (int v = 0; v < biome.underwaterVegetation.Length; v++) {
                            AddVoxelTextures(biome.underwaterVegetation[v].vegetation);
                        }
                    }
                    if (biome.trees != null) {
                        for (int t = 0; t < biome.trees.Length; t++) {
                            ModelDefinition tree = biome.trees[t].tree;
                            if (tree == null)
                                continue;
                            for (int b = 0; b < tree.bits.Length; b++) {
                                AddVoxelTextures(tree.bits[b].voxelDefinition);
                            }
                        }
                    }
                    if (biome.ores != null) {
                        for (int v = 0; v < biome.ores.Length; v++) {
                            // ensure proper size
                            if (biome.ores[v].veinMinSize == biome.ores[v].veinMaxSize && biome.ores[v].veinMaxSize == 0) {
                                biome.ores[v].veinMinSize = 2;
                                biome.ores[v].veinMaxSize = 6;
                                biome.ores[v].veinsCountMin = 1;
                                biome.ores[v].veinsCountMax = 2;
                            }
                            AddVoxelTextures(biome.ores[v].ore);
                        }
                    }
                }
            }

            // Special voxels
            if (enableClouds) {
                if (world.cloudVoxel == null) {
                    world.cloudVoxel = Resources.Load<VoxelDefinition>("VoxelPlay/Defaults/VoxelCloud");
                }
                AddVoxelTextures(world.cloudVoxel);
            }

            // Add additional world voxels
            if (world.moreVoxels != null) {
                for (int k = 0; k < world.moreVoxels.Length; k++) {
                    AddVoxelTextures(world.moreVoxels[k]);
                }
            }

            // Add all items' textures are available
            if (world.items != null) {
                int itemCount = world.items.Length;
                for (int k = 0; k < itemCount; k++) {
                    ItemDefinition item = world.items[k];
                    if (item != null && item.category == ItemCategory.Voxel) {
                        AddVoxelTextures(item.voxelType);
                    }
                }
            }

            // Add any other voxel found inside Defaults
            LogMessage("Loading all voxels in VoxelPlay/Defaults");
            VoxelDefinition[] vdd = Resources.LoadAll<VoxelDefinition>("VoxelPlay/Defaults");
            for (int k = 0; k < vdd.Length; k++) {
                AddVoxelTextures(vdd[k]);
            }

            // Add any other voxel found inside World directory
            if (!string.IsNullOrEmpty(world.name)) {
                LogMessage("Loading all voxels in Worlds/" + world.name);
                vdd = Resources.LoadAll<VoxelDefinition>("Worlds/" + world.name);
                for (int k = 0; k < vdd.Length; k++) {
                    AddVoxelTextures(vdd[k]);
                }

                LogMessage("Loading all voxels in World/" + world.name);
                vdd = Resources.LoadAll<VoxelDefinition>("World/" + world.name);
                for (int k = 0; k < vdd.Length; k++) {
                    AddVoxelTextures(vdd[k]);
                }

                // Add any other voxel found inside a resource directory with same name of world (if not placed into Worlds directory)
                LogMessage("Loading all voxels in Resources/" + world.name);
                vdd = Resources.LoadAll<VoxelDefinition>(world.name);
                for (int k = 0; k < vdd.Length; k++) {
                    AddVoxelTextures(vdd[k]);
                }

                // Add any model def voxels
                LogMessage("Loading all model definitions in Worlds/" + world.name); ;
                ModelDefinition[] md = Resources.LoadAll<ModelDefinition>("Worlds/" + world.name);
                for (int k = 0; k < md.Length; k++) {
                    ModelRegisterVoxelDefinitions(md[k]);
                }
                LogMessage("Loading all model definitions in World/" + world.name);
                md = Resources.LoadAll<ModelDefinition>("World/" + world.name);
                for (int k = 0; k < md.Length; k++) {
                    ModelRegisterVoxelDefinitions(md[k]);
                }
                LogMessage("Loading all model definitions in Resources/" + world.name);
                md = Resources.LoadAll<ModelDefinition>(world.name);
                for (int k = 0; k < md.Length; k++) {
                    ModelRegisterVoxelDefinitions(md[k]);
                }
            }

            // Add any other voxel found inside a resource directory under the world definition asset
            if (!string.IsNullOrEmpty(world.resourceLocation)) {
                LogMessage("Loading all voxels in " + world.resourceLocation);
                vdd = Resources.LoadAll<VoxelDefinition>(world.resourceLocation);
                for (int k = 0; k < vdd.Length; k++) {
                    AddVoxelTextures(vdd[k]);
                }
            }

            // Add user provided voxels during playtime
            int count = sessionUserVoxels.Count;
            for (int k = 0; k < count; k++) {
                AddVoxelTextures(sessionUserVoxels[k]);
            }
            sessionUserVoxelsLastIndex = voxelDefinitionsCount - 1;

            // Add transparent voxel definitions for the see-through effect
            if (seeThrough) {
                int lastOne = voxelDefinitionsCount; // this loop will add voxels so end at the last regular voxel definition (don't process see-through versions)
                for (int k = 0; k < lastOne; k++) {
                    VoxelDefinition vd = voxelDefinitions[k];
                    if (vd.renderType == RenderType.CutoutCross) {
                        vd.seeThroughMode = vd.seeThroughMode == SeeThroughMode.NotSupported ? SeeThroughMode.NotSupported : SeeThroughMode.FullyInvisible;
                    } else if (vd.seeThroughMode == SeeThroughMode.Transparency) {
                        if (vd.renderType.supportsAlphaSeeThrough()) {
                            vd.seeThroughVoxelTempTransp = CreateSeeThroughVoxelDefinition(vd);
                        } else {
                            vd.seeThroughMode = SeeThroughMode.FullyInvisible;
                        }
                    }
                }
            }

            // Assign textures to materials
            SetRenderingMaterialsTextures(createTextureArrays: true);

            if (modelHighlightMat == null) {
                modelHighlightMat = Instantiate(Resources.Load<Material>("VoxelPlay/Materials/VP Highlight Model")) as Material;
            }
            modelHighlightMat.SetTexture(ShaderParams.MainTex, mainTextureProvider.textureArray);

        }


        void SetRenderingMaterialsTextures(bool createTextureArrays) {
            if (renderingMaterials != null) {
                if (createTextureArrays) {
                    for (int k = 0; k < renderingMaterials.Length; k++) {
                        RenderingMaterial rm = renderingMaterials[k];
                        if (rm.descriptor.textureProvider != null) {
                            LogMessage($"Created texture array for {rm.material.name} with {rm.descriptor.textureProvider.texturesCount} textures (system max: {rm.descriptor.textureProvider.maxTextures}).");
                            rm.descriptor.textureProvider.CreateTextureArray();
                        }
                    }
                }
                for (int k = 0; k < renderingMaterials.Length; k++) {
                    RenderingMaterial rm = renderingMaterials[k];
                    if (rm.descriptor.textureProvider != null) {
                        Material mat = rm.material;
                        if (mat != null && mat.HasProperty(ShaderParams.MainTex)) {
                            mat.SetTexture(ShaderParams.MainTex, rm.descriptor.textureProvider.textureArray);
                        }
                    }
                }
            }

            matDynamicOpaque.SetTexture(ShaderParams.MainTex, mainTextureProvider.textureArray);
            matDynamicCutout.SetTexture(ShaderParams.MainTex, mainTextureProvider.textureArray);
        }


        /// <summary>
        /// Assigns a color to each biome.
        /// </summary>
        public void SetBiomeDefaultColors(bool force) {
            if (world != null) {
                if (world.biomes != null) {
                    for (int b = 0; b < world.biomes.Length; b++) {
                        BiomeDefinition biome = world.biomes[b];
                        if (biome == null || biome.zones == null)
                            continue;
                        if (force || biome.biomeMapColor.a == 0) {
                            long color = distinctColors[b % distinctColors.Length];
                            Color32 biomeColor = new Color32((byte)(color >> 16), (byte)((color >> 8) & 255), (byte)(color & 255), 255);
                            biome.biomeMapColor = biomeColor;
                        }
                    }
                }
            }
        }




    }


}
