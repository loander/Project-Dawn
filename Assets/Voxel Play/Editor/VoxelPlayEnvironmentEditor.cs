﻿//#define USES_URP
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;
using System.Reflection;
using UnityEngine.Rendering;
#if USES_URP
using UnityEngine.Rendering.Universal;
#endif

namespace VoxelPlay {

    [CustomEditor(typeof(VoxelPlayEnvironment))]
    public class VoxelPlayEnvironmentEditor : Editor {
        SerializedProperty enableURP, debugLevel, enableGeneration;
        SerializedProperty world, enableBuildMode, buildMode, welcomeMessage, welcomeMessageDuration, renderInEditor, renderInEditorLowPriority, renderInEditorDetail, renderInEditorAreaCenter, renderInEditorAreaSize;
        SerializedProperty enableConsole, showConsole, enableInventory, enableStatusBar, enableLoadingPanel, loadingText, initialWaitTime, initialWaitText, loadSavedGame, saveFilename, enableDebugWindow, showFPS;
        SerializedProperty globalIllumination, ambientLight, daylightShadowAtten, enableSmoothLighting, enableFogSkyBlending, textureSize, enableShadows, obscuranceMode, obscuranceIntensity;
        SerializedProperty shadowsOnWater, realisticWater;
        SerializedProperty enableTinting, enableColoredShadows, enableBevel, enableOutline, outlineColor, outlineThreshold, enableCurvature;
        SerializedProperty seeThrough, seeThroughTarget, seeThroughRadius, seeThroughHeightOffset, seeThroughAlpha;
        SerializedProperty useOriginShift, originShiftDistanceThreshold;
        SerializedProperty enableReliefMapping, reliefStrength, reliefMaxDistance, reliefIterations, reliefIterationsBinarySearch;
        SerializedProperty enableBrightPointLights, enableURPNativeLights, brightPointsMaxDistance;
        SerializedProperty enableNormalMap, usePixelLights, enableFresnel, fresnelExponent, fresnelIntensity, fresnelColor;
        SerializedProperty enableGlobalSpecular, globalSpecularIntensity;
        SerializedProperty hqFiltering, mipMapBias, filterMode, doubleSidedGlass, transparentBling, damageParticles, useComputeBuffers, usePostProcessing;
        SerializedProperty maxChunks, prewarmChunksInEditor, visibleChunksDistance, distanceAnchor, unloadFarChunks, unloadFarChunksMode, unloadFarNavMesh;
        SerializedProperty adjustCameraFarClip, forceChunkDistance, maxCPUTimePerFrame, maxChunksPerFrame, maxTreesPerFrame, maxBushesPerFrame, lowMemoryMode, delayedInitialization, onlyRenderInFrustum;
#if !UNITY_WEBGL
        SerializedProperty multiThreadGeneration;
#endif
        SerializedProperty serverMode, enableColliders, enableTrees, denseTrees, enableVegetation, enableDetailGenerators, enableNavMesh, navMeshResolution, hideChunksInHierarchy;
        SerializedProperty sun, fogAmount, fogDistance, fogDistanceAuto, fogFallOff, fogTint, enableClouds;
        SerializedProperty uiCanvasPrefab, inputControllerPC, inputControllerMobile, crosshairPrefab, crosshairTexture, consoleBackgroundColor, statusBarBackgroundColor;
        SerializedProperty defaultBuildSound, defaultPickupSound, defaultImpactSound, defaultDestructionSound, defaultVoxel, defaultWaterVoxel;
        SerializedProperty layerParticles, layerVoxels, layerClouds, particlePoolSize;
        SerializedProperty previewTouchUIinEditor;
        SerializedProperty instancingCullingMode, instancingCullingPadding;

        VoxelPlayEnvironment env;
        WorldDefinition cachedWorld;
        VoxelPlayTerrainGenerator cachedTerrainGenerator;
        Editor cachedWorldEditor, cachedTerrainGeneratorEditor;
        static GUIStyle titleLabelStyle, boxStyle;
        static bool worldExpand, terrainGeneratorExpand;
        static int cookieIndex = -1;
        Color titleColor;
        static GUIStyle sectionHeaderStyle;
        static bool expandQualitySection, expandRenderingSection, expandStatsSection, expandVoxelGenerationSection, expandSkySection, expandInGameSection, expandDefaultsSection, expandAdvancedSection;
        bool enableCurvatureFromShader;
        string[] chunkSizeOptions;
        int[] chunkSizeValues;
        int chunkNewSize;
        string curvatureAmount;
        bool voxelPadding;
        int maxMaterialsPerChunk;

        const string VP_SECTION_QUALITY = "VoxelPlayExpandQualitySection";
        const string VP_SECTION_RENDERING = "VoxelPlayExpandRenderingSection";
        const string VP_SECTION_STATS = "VoxelPlayVoxelStatsSection";
        const string VP_SECTION_GENERATION = "VoxelPlayVoxelGenerationSection";
        const string VP_SECTION_SKY = "VoxelPlaySkySection";
        const string VP_SECTION_GAME_FEATURES = "VoxelPlayInGameSection";
        const string VP_SECTION_DEFAULTS = "VoxelPlayDefaultsSection";
        const string VP_SECTION_ADVANCED = "VoxelPlayAdvancedSection";


        [MenuItem("Assets/Create/Voxel Play/Online Documentation", false, 2001)]
        public static void ShowDocs() {
            Application.OpenURL("https://kronnect.freshdesk.com/support/home");
        }

        [MenuItem("Assets/Create/Voxel Play/Tutorials", false, 2002)]
        public static void ShowTutorials() {
            Application.OpenURL("https://youtube.com/playlist?list=PLqzCcLYG3btgt5wsMqTf7ANjrwacdjfr8");
        }

        [MenuItem("Assets/Create/Voxel Play/Support Forum", false, 2003)]
        public static void ShowSupport() {
            Application.OpenURL("https://kronnect.com/support");
        }


        void OnEnable() {
            titleColor = EditorGUIUtility.isProSkin ? new Color(0.52f, 0.66f, 0.9f) : new Color(0.12f, 0.16f, 0.4f);
            enableURP = serializedObject.FindProperty("enableURP");
            debugLevel = serializedObject.FindProperty("debugLevel");
            world = serializedObject.FindProperty("world");
            enableGeneration = serializedObject.FindProperty("enableGeneration");
            enableBuildMode = serializedObject.FindProperty("enableBuildMode");
            buildMode = serializedObject.FindProperty("buildMode");
            welcomeMessage = serializedObject.FindProperty("welcomeMessage");
            welcomeMessageDuration = serializedObject.FindProperty("welcomeMessageDuration");
            renderInEditor = serializedObject.FindProperty("renderInEditor");
            renderInEditorLowPriority = serializedObject.FindProperty("renderInEditorLowPriority");
            renderInEditorDetail = serializedObject.FindProperty("renderInEditorDetail");
            renderInEditorAreaCenter = serializedObject.FindProperty("renderInEditorAreaCenter");
            renderInEditorAreaSize = serializedObject.FindProperty("renderInEditorAreaSize");
            enableConsole = serializedObject.FindProperty("enableConsole");
            consoleBackgroundColor = serializedObject.FindProperty("consoleBackgroundColor");
            showConsole = serializedObject.FindProperty("showConsole");
            enableInventory = serializedObject.FindProperty("enableInventory");
            prewarmChunksInEditor = serializedObject.FindProperty("prewarmChunksInEditor");
            enableLoadingPanel = serializedObject.FindProperty("enableLoadingPanel");
            loadingText = serializedObject.FindProperty("loadingText");
            initialWaitTime = serializedObject.FindProperty("initialWaitTime");
            initialWaitText = serializedObject.FindProperty("initialWaitText");
            loadSavedGame = serializedObject.FindProperty("loadSavedGame");
            saveFilename = serializedObject.FindProperty("saveFilename");
            enableDebugWindow = serializedObject.FindProperty("enableDebugWindow");
            showFPS = serializedObject.FindProperty("showFPS");

            globalIllumination = serializedObject.FindProperty("globalIllumination");
            ambientLight = serializedObject.FindProperty("ambientLight");
            daylightShadowAtten = serializedObject.FindProperty("daylightShadowAtten");
            enableSmoothLighting = serializedObject.FindProperty("enableSmoothLighting");
            obscuranceMode = serializedObject.FindProperty("obscuranceMode");
            obscuranceIntensity = serializedObject.FindProperty("obscuranceIntensity");

            enableReliefMapping = serializedObject.FindProperty("enableReliefMapping");
            reliefStrength = serializedObject.FindProperty("reliefStrength");
            reliefMaxDistance = serializedObject.FindProperty("reliefMaxDistance");
            reliefIterations = serializedObject.FindProperty("reliefIterations");
            reliefIterationsBinarySearch = serializedObject.FindProperty("reliefIterationsBinarySearch");

            enableNormalMap = serializedObject.FindProperty("enableNormalMap");
            usePixelLights = serializedObject.FindProperty("usePixelLights");
            enableBevel = serializedObject.FindProperty("enableBevel");

            enableFresnel = serializedObject.FindProperty("enableFresnel");
            fresnelExponent = serializedObject.FindProperty("fresnelExponent");
            fresnelIntensity = serializedObject.FindProperty("fresnelIntensity");
            fresnelColor = serializedObject.FindProperty("fresnelColor");

            enableGlobalSpecular = serializedObject.FindProperty("enableGlobalSpecular");
            globalSpecularIntensity = serializedObject.FindProperty("globalSpecularIntensity");

            enableBrightPointLights = serializedObject.FindProperty("enableBrightPointLights");
            enableURPNativeLights = serializedObject.FindProperty("enableURPNativeLights");
            brightPointsMaxDistance = serializedObject.FindProperty("brightPointsMaxDistance");

            enableFogSkyBlending = serializedObject.FindProperty("enableFogSkyBlending");
            textureSize = serializedObject.FindProperty("textureSize");
            realisticWater = serializedObject.FindProperty("realisticWater");
            shadowsOnWater = serializedObject.FindProperty("shadowsOnWater");
            enableShadows = serializedObject.FindProperty("enableShadows");
            enableTinting = serializedObject.FindProperty("enableTinting");
            enableColoredShadows = serializedObject.FindProperty("enableColoredShadows");
            enableCurvature = serializedObject.FindProperty("enableCurvature");
            enableOutline = serializedObject.FindProperty("enableOutline");
            outlineColor = serializedObject.FindProperty("outlineColor");
            outlineThreshold = serializedObject.FindProperty("outlineThreshold");
            doubleSidedGlass = serializedObject.FindProperty("doubleSidedGlass");
            transparentBling = serializedObject.FindProperty("transparentBling");
            damageParticles = serializedObject.FindProperty("damageParticles");
            hqFiltering = serializedObject.FindProperty("hqFiltering");
            mipMapBias = serializedObject.FindProperty("mipMapBias");
            filterMode = serializedObject.FindProperty("filterMode");
            useComputeBuffers = serializedObject.FindProperty("useComputeBuffers");
            usePostProcessing = serializedObject.FindProperty("usePostProcessing");

            seeThrough = serializedObject.FindProperty("seeThrough");
            seeThroughTarget = serializedObject.FindProperty("seeThroughTarget");
            seeThroughRadius = serializedObject.FindProperty("seeThroughRadius");
            seeThroughHeightOffset = serializedObject.FindProperty("_seeThroughHeightOffset");
            seeThroughAlpha = serializedObject.FindProperty("seeThroughAlpha");

            useOriginShift = serializedObject.FindProperty("useOriginShift");
            originShiftDistanceThreshold = serializedObject.FindProperty("originShiftDistanceThreshold");

            maxChunks = serializedObject.FindProperty("maxChunks");
            visibleChunksDistance = serializedObject.FindProperty("_visibleChunksDistance");
            distanceAnchor = serializedObject.FindProperty("distanceAnchor");
            unloadFarChunks = serializedObject.FindProperty("unloadFarChunks");
            unloadFarChunksMode = serializedObject.FindProperty("unloadFarChunksMode");
            unloadFarNavMesh = serializedObject.FindProperty("unloadFarNavMesh");
            adjustCameraFarClip = serializedObject.FindProperty("adjustCameraFarClip");

            forceChunkDistance = serializedObject.FindProperty("forceChunkDistance");
            maxCPUTimePerFrame = serializedObject.FindProperty("maxCPUTimePerFrame");
            maxChunksPerFrame = serializedObject.FindProperty("maxChunksPerFrame");
            maxTreesPerFrame = serializedObject.FindProperty("maxTreesPerFrame");
            maxBushesPerFrame = serializedObject.FindProperty("maxBushesPerFrame");
#if !UNITY_WEBGL
            multiThreadGeneration = serializedObject.FindProperty("multiThreadGeneration");
#endif
            lowMemoryMode = serializedObject.FindProperty("lowMemoryMode");
            delayedInitialization = serializedObject.FindProperty("delayedInitialization");
            onlyRenderInFrustum = serializedObject.FindProperty("onlyRenderInFrustum");
            serverMode = serializedObject.FindProperty("serverMode");
            enableColliders = serializedObject.FindProperty("enableColliders");
            enableNavMesh = serializedObject.FindProperty("enableNavMesh");
            navMeshResolution = serializedObject.FindProperty("navMeshResolution");
            hideChunksInHierarchy = serializedObject.FindProperty("hideChunksInHierarchy");
            enableTrees = serializedObject.FindProperty("enableTrees");
            denseTrees = serializedObject.FindProperty("denseTrees");
            enableVegetation = serializedObject.FindProperty("enableVegetation");
            enableDetailGenerators = serializedObject.FindProperty("enableDetailGenerators");

            sun = serializedObject.FindProperty("sun");
            fogAmount = serializedObject.FindProperty("fogAmount");
            fogDistance = serializedObject.FindProperty("fogDistance");
            fogDistanceAuto = serializedObject.FindProperty("fogDistanceAuto");
            fogFallOff = serializedObject.FindProperty("fogFallOff");
            fogTint = serializedObject.FindProperty("fogTint");

            enableClouds = serializedObject.FindProperty("enableClouds");

            uiCanvasPrefab = serializedObject.FindProperty("UICanvasPrefab");
            inputControllerPC = serializedObject.FindProperty("inputControllerPCPrefab");
            inputControllerMobile = serializedObject.FindProperty("inputControllerMobilePrefab");
            crosshairPrefab = serializedObject.FindProperty("crosshairPrefab");
            crosshairTexture = serializedObject.FindProperty("crosshairTexture");

            enableStatusBar = serializedObject.FindProperty("enableStatusBar");
            statusBarBackgroundColor = serializedObject.FindProperty("statusBarBackgroundColor");

            layerParticles = serializedObject.FindProperty("layerParticles");
            particlePoolSize = serializedObject.FindProperty("particlePoolSize");
            layerVoxels = serializedObject.FindProperty("layerVoxels");
            layerClouds = serializedObject.FindProperty("layerClouds");

            defaultBuildSound = serializedObject.FindProperty("defaultBuildSound");
            defaultPickupSound = serializedObject.FindProperty("defaultPickupSound");
            defaultImpactSound = serializedObject.FindProperty("defaultImpactSound");
            defaultDestructionSound = serializedObject.FindProperty("defaultDestructionSound");
            defaultVoxel = serializedObject.FindProperty("defaultVoxel");
            defaultWaterVoxel = serializedObject.FindProperty("defaultWaterVoxel");

            env = (VoxelPlayEnvironment)target;
            if (!Application.isPlaying) {
                if (!env.initialized && env.gameObject.activeInHierarchy) {
                    env.Init();
                }
                env.WantRepaintInspector += this.Repaint;
            }

            worldExpand = EditorPrefs.GetBool("VoxelPlayWorldSection", worldExpand);
            terrainGeneratorExpand = EditorPrefs.GetBool("VoxelPlayTerrainGeneratorSection", terrainGeneratorExpand);

            expandQualitySection = EditorPrefs.GetBool(VP_SECTION_QUALITY, false);
            expandRenderingSection = EditorPrefs.GetBool(VP_SECTION_RENDERING, false);
            expandStatsSection = EditorPrefs.GetBool(VP_SECTION_STATS, false);
            expandVoxelGenerationSection = EditorPrefs.GetBool(VP_SECTION_GENERATION, false);
            expandSkySection = EditorPrefs.GetBool(VP_SECTION_SKY, false);
            expandInGameSection = EditorPrefs.GetBool(VP_SECTION_GAME_FEATURES, false);
            expandDefaultsSection = EditorPrefs.GetBool(VP_SECTION_DEFAULTS, false);
            expandAdvancedSection = EditorPrefs.GetBool(VP_SECTION_ADVANCED, true);

            enableCurvatureFromShader = "1".Equals(GetShaderOptionValue("VOXELPLAY_CURVATURE", "VPCommonVertexModifier.cginc"));
            curvatureAmount = GetShaderOptionValue("VOXELPLAY_CURVATURE_AMOUNT", "VPCommonVertexModifier.cginc");

            previewTouchUIinEditor = serializedObject.FindProperty("previewTouchUIinEditor");

            instancingCullingMode = serializedObject.FindProperty("instancingCullingMode");
            instancingCullingPadding = serializedObject.FindProperty("instancingCullingPadding");

            chunkSizeOptions = new string[] { "16", "32" };
            chunkSizeValues = new int[] { 16, 32 };
            chunkNewSize = VoxelPlayEnvironment.CHUNK_SIZE;
            maxMaterialsPerChunk = VoxelPlayEnvironment.MAX_MATERIALS_PER_CHUNK;
            voxelPadding = VoxelPlayGreedyCommon.PADDING != 0;
        }

        void OnDisable() {
            if (env != null) {
                env.WantRepaintInspector -= this.Repaint;
            }
            EditorPrefs.SetBool(VP_SECTION_QUALITY, expandQualitySection);
            EditorPrefs.SetBool(VP_SECTION_RENDERING, expandRenderingSection);
            EditorPrefs.SetBool(VP_SECTION_STATS, expandStatsSection);
            EditorPrefs.SetBool(VP_SECTION_GENERATION, expandVoxelGenerationSection);
            EditorPrefs.SetBool(VP_SECTION_SKY, expandSkySection);
            EditorPrefs.SetBool(VP_SECTION_GAME_FEATURES, expandInGameSection);
            EditorPrefs.SetBool(VP_SECTION_DEFAULTS, expandDefaultsSection);
            EditorPrefs.SetBool(VP_SECTION_ADVANCED, expandAdvancedSection);
        }


        public override void OnInspectorGUI() {
            serializedObject.UpdateIfRequiredOrScript();
            if (boxStyle == null) {
                boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.padding = new RectOffset(15, 10, 5, 5);
            }
            if (titleLabelStyle == null) {
                titleLabelStyle = new GUIStyle(EditorStyles.label);
            }
            titleLabelStyle.normal.textColor = titleColor;
            titleLabelStyle.fontStyle = FontStyle.Bold;
            if (sectionHeaderStyle == null) {
                sectionHeaderStyle = new GUIStyle(EditorStyles.foldout);
            }
            sectionHeaderStyle.SetFoldoutColor();

            if (cookieIndex >= 0) {
                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Help & Tutorials", titleLabelStyle);
                EditorGUILayout.HelpBox("To learn more about a property in this inspector move the mouse over the label for a quick description (tooltip).", MessageType.Info);
                if (GUILayout.Button("Online Documentation")) {
                    Application.OpenURL("https://kronnect.freshdesk.com/support/home");
                }
                if (GUILayout.Button("Tutorials")) {
                    Application.OpenURL("https://youtube.com/playlist?list=PLqzCcLYG3btgt5wsMqTf7ANjrwacdjfr8");
                }
                if (GUILayout.Button("Support Forum")) {
                    Application.OpenURL("https://kronnect.com/support");

                }
                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Random Tip", titleLabelStyle);
                EditorGUILayout.HelpBox(VoxelPlayCookie.GetCookie(cookieIndex), MessageType.Info);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("  ");
                ShowHelpButtons(true);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("General Settings", titleLabelStyle);
            if (cookieIndex < 0)
                ShowHelpButtons(false);
            EditorGUILayout.EndHorizontal();

            bool rebuildWorld = false;
            bool refreshChunks = false;
            bool reloadWorldTextures = false;
            bool updateSpecialFeaturesMacro = false;
            bool updateCurvatureMacro = false;
            bool prevBool;

            // General settings
#if UNITY_2019_3_OR_NEWER
            prevBool = env.enableURP;
            EditorGUILayout.PropertyField(enableURP, new GUIContent("Enable URP Support", "Enables Universal Rendering Pipeline support."));
            if (prevBool != enableURP.boolValue) {
                refreshChunks = true;
                updateSpecialFeaturesMacro = true;
            }
            if (!enableURP.boolValue && UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null) {
                EditorGUILayout.HelpBox("A rendering pipeline is in use. Activate the 'Enable URP Support' to complete setup.", MessageType.Warning);
                EditorGUILayout.Separator();
            } else if (enableURP.boolValue && UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null) {
                EditorGUILayout.HelpBox("URP pipeline not detected. Please ensure you have Universal RP package installed from the Package Manager and URP asset is assigned in Project Settings / Graphics.", MessageType.Error);
                EditorGUILayout.Separator();
            }

#if USES_URP
            UniversalRenderPipelineAsset pipe = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipe != null) {
                if (!pipe.supportsCameraDepthTexture) {
                    EditorGUILayout.HelpBox("Depth Texture option is required in Universal Rendering Pipeline asset!", MessageType.Error);
                    if (GUILayout.Button("Go to Universal Rendering Pipeline Asset")) {
                        Selection.activeObject = pipe;
                    }
                    EditorGUILayout.Separator();
                    GUI.enabled = false;
                }
                if (realisticWater.boolValue && pipe.msaaSampleCount > 1) {
                    EditorGUILayout.HelpBox("MSAA should be turned off when using realistic water option. Disable MSAA in URP asset.", MessageType.Warning);
                    if (GUILayout.Button("Go to Universal Rendering Pipeline Asset")) {
                        Selection.activeObject = pipe;
                    }
                    EditorGUILayout.Separator();
                }
            }

            CheckDepthPrimingMode();
#endif

#endif

            EditorGUILayout.BeginHorizontal();
            WorldDefinition wd = (WorldDefinition)world.objectReferenceValue;
            EditorGUILayout.PropertyField(world, new GUIContent("World", "The world definition asset. This asset contains the definition of biomes, voxels, items and other world-specific options."));
            if (wd != world.objectReferenceValue)
                rebuildWorld = true;
            if (GUILayout.Button("Create", GUILayout.Width(50))) {
                CreateWorldDefinition();
            }
            if (GUILayout.Button("Locate", GUILayout.Width(50))) {
                Selection.activeObject = world.objectReferenceValue;
            }
            EditorGUILayout.EndHorizontal();
            if (world.objectReferenceValue == null) {
                EditorGUILayout.HelpBox("Create or assign a World Definition asset.", MessageType.Warning);
            }

            if (world.objectReferenceValue != null) {
                if (GUILayout.Button("Expand/Collapse World Settings")) {
                    worldExpand = !worldExpand;
                    EditorPrefs.SetBool("VoxelPlayWorldSection", worldExpand);
                }
                if (worldExpand) {
                    if (cachedWorld != world.objectReferenceValue) {
                        cachedWorldEditor = null;
                    }
                    if (cachedWorldEditor == null) {
                        cachedWorld = (WorldDefinition)world.objectReferenceValue;
                        cachedWorldEditor = Editor.CreateEditor(world.objectReferenceValue);
                    }

                    // Drawing the world editor
                    EditorGUILayout.BeginVertical(boxStyle);
                    EditorGUI.BeginChangeCheck();
                    cachedWorldEditor.OnInspectorGUI();
                    if (EditorGUI.EndChangeCheck()) {
                        env.UpdateMaterialProperties();
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Separator();
                }

                VoxelPlayTerrainGenerator terrainGenerator = (VoxelPlayTerrainGenerator)((WorldDefinition)world.objectReferenceValue).terrainGenerator;
                if (terrainGenerator != null) {
                    if (GUILayout.Button("Expand/Collapse Terrain Settings")) {
                        terrainGeneratorExpand = !terrainGeneratorExpand;
                        EditorPrefs.SetBool("VoxelPlayTerrainGeneratorSection", terrainGeneratorExpand);
                    }
                    if (terrainGeneratorExpand) {
                        if (terrainGenerator != cachedTerrainGenerator) {
                            cachedTerrainGeneratorEditor = null;
                        }
                        if (cachedTerrainGeneratorEditor == null) {
                            cachedTerrainGenerator = terrainGenerator;
                            cachedTerrainGeneratorEditor = Editor.CreateEditor(terrainGenerator);
                        }

                        // Drawing the world editor
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.BeginVertical(boxStyle);
                        cachedTerrainGeneratorEditor.OnInspectorGUI();
                        EditorGUILayout.EndVertical();
                        if (EditorGUI.EndChangeCheck()) {
                            env.NotifyTerrainGeneratorConfigurationChanged();
                            VoxelPlayBiomeExplorer.requestRefresh = true;
                            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                        }
                        EditorGUILayout.Separator();
                    }
                }

                if (GUILayout.Button("Open Biome Map Explorer")) {
                    VoxelPlayBiomeExplorer.ShowWindow();
                }

                EditorGUILayout.Separator();
                EditorGUILayout.BeginVertical(boxStyle);
                float half = EditorGUIUtility.currentViewWidth * 0.4f;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();
                if (GUILayout.Button("Toggle Chunks", GUILayout.Width(half))) {
                    env.ChunksToggle();
                    SceneView.RepaintAll();
                }
                if (GUILayout.Button("Delete Chunks", GUILayout.Width(half))) {
                    renderInEditor.boolValue = false;
                    env.DisposeAll();
                }
                EditorGUILayout.Space();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();
                if (GUILayout.Button("Regenerate Terrain", GUILayout.Width(half))) {
                    renderInEditor.boolValue = true;
                    rebuildWorld = true;
                }
                GUI.enabled = env.chunksCreated > 0;
                if (GUILayout.Button("Export Chunks", GUILayout.Width(half))) {
                    env.ChunksExport();
                    EditorUtility.DisplayDialog("Export Chunks", "Chunks now available under 'Exported Chunks' node in hierarchy as regular gameobjects. Materials, textures and meshes are now part of the scene.\n\nThe 'ExportGlobalSettings' behaviour has been attached to 'Exported Chunks' root gameobject to keep global shader values.\nVoxel Play Environment has been removed.", "Ok");
                    GUIUtility.ExitGUI();
                    return;
                }
                GUI.enabled = true;
                EditorGUILayout.Space();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Separator();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(renderInEditor, new GUIContent("Render In Editor", "Enable world rendering in Editor. If disabled, world will only be visible during play mode."));
                if (EditorGUI.EndChangeCheck()) {
                    if (renderInEditor.boolValue) {
                        rebuildWorld = true;
                    }
                }
                if (!renderInEditor.boolValue)
                    GUI.enabled = false;
                EditorGUILayout.PropertyField(renderInEditorLowPriority, new GUIContent("   Low Priority", "When enabled, rendering in editor will only execute when scene camera is static."));
                if (wd != world.objectReferenceValue) {
                    rebuildWorld = true;
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(renderInEditorDetail, new GUIContent("   Render Detail", "Select the amount of detail to be rendered in Editor time."));
                if (EditorGUI.EndChangeCheck()) {
                    rebuildWorld = true;
                }
                GUI.enabled = true;

                if (renderInEditor.boolValue) {

                    if (env.cameraMain != null) {
                        env.cameraMain.transform.position = EditorGUILayout.Vector3Field("Main Cam Pos", env.cameraMain.transform.position);
                    }
                    if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null) {
                        EditorGUILayout.Vector3Field("Scene Cam Pos", SceneView.lastActiveSceneView.camera.transform.position);
                    }
                    EditorGUILayout.BeginHorizontal();
                    if (env.cameraMain != null) {
                        if (SceneView.lastActiveSceneView != null) {
                            if (GUILayout.Button("Scene Cam To Surface")) {
                                Vector3 pos = Misc.vector3zero;
                                pos = env.cameraMain.transform.position;
                                pos.y = env.GetTerrainHeight(Vector3.zero, true);
                                SceneView.lastActiveSceneView.LookAt(pos + new Vector3(50, 50, 50));
                            }
                            if (GUILayout.Button("Find Main Cam")) {
                                Vector3 pos = env.cameraMain.transform.position + new Vector3(50, 50, 50);
                                Vector3 fwd = (env.cameraMain.transform.position - pos).normalized;
                                SceneView.lastActiveSceneView.LookAt(pos, Quaternion.LookRotation(fwd));
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Separator();
                    EditorGUILayout.LabelField("Generate Area");
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(renderInEditorAreaCenter, new GUIContent("Center"));
                    EditorGUILayout.PropertyField(renderInEditorAreaSize, new GUIContent("Size"));
                    if (GUILayout.Button("Generate Chunks In Area")) {
                        GenerateEditorArea();
                        GUIUtility.ExitGUI();
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();

            } else {
                renderInEditor.boolValue = false;
            }

            // Quality and effects
            EditorGUILayout.Separator();
            expandQualitySection = EditorGUILayout.Foldout(expandQualitySection, "Quality And Effects", sectionHeaderStyle);
            if (expandQualitySection) {

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Preset", GUILayout.Width(120));
                if (GUILayout.Button(new GUIContent("All Features", "Enables all engine visual features available for the active platform."))) {
                    globalIllumination.boolValue = true;
                    enableShadows.boolValue = true;
                    shadowsOnWater.boolValue = true;
                    enableSmoothLighting.boolValue = true;
                    enableFogSkyBlending.boolValue = true;
                    denseTrees.boolValue = true;
                    hqFiltering.boolValue = true;
                    usePixelLights.boolValue = true;
                    enableBevel.boolValue = true;
                    enableFresnel.boolValue = true;
                    enableBrightPointLights.boolValue = true;
                    doubleSidedGlass.boolValue = true;
                    transparentBling.boolValue = true;
                    if (VoxelPlayFirstPersonController.instance != null) {
                        VoxelPlayFirstPersonController.instance.autoInvertColors = true;
                    }
                    rebuildWorld = true;
                }
                if (GUILayout.Button(new GUIContent("Medium", "Disables shadows to improve performance but keeps global illumination."))) {
                    globalIllumination.boolValue = true;
                    enableShadows.boolValue = false;
                    shadowsOnWater.boolValue = false;
                    enableSmoothLighting.boolValue = true;
                    enableFogSkyBlending.boolValue = true;
                    enableReliefMapping.boolValue = false;
                    usePixelLights.boolValue = true;
                    enableBevel.boolValue = false;
                    enableFresnel.boolValue = false;
                    enableBrightPointLights.boolValue = false;
                    denseTrees.boolValue = true;
                    hqFiltering.boolValue = true;
                    doubleSidedGlass.boolValue = true;
                    transparentBling.boolValue = true;
                    if (VoxelPlayFirstPersonController.instance != null) {
                        VoxelPlayFirstPersonController.instance.autoInvertColors = true;
                    }
                    rebuildWorld = true;
                }
                if (GUILayout.Button(new GUIContent("Fastest", "Disables all effects to improve performance."))) {
                    globalIllumination.boolValue = false;
                    enableShadows.boolValue = false;
                    shadowsOnWater.boolValue = false;
                    enableSmoothLighting.boolValue = false;
                    obscuranceMode.intValue = (int)ObscuranceMode.Faster;
                    enableFogSkyBlending.boolValue = false;
                    enableReliefMapping.boolValue = false;
                    enableNormalMap.boolValue = false;
                    usePixelLights.boolValue = false;
                    enableFresnel.boolValue = false;
                    enableBevel.boolValue = false;
                    enableBrightPointLights.boolValue = false;
                    denseTrees.boolValue = false;
                    hqFiltering.boolValue = false;
                    doubleSidedGlass.boolValue = false;
                    onlyRenderInFrustum.boolValue = true;
                    transparentBling.boolValue = false;
                    if (VoxelPlayFirstPersonController.instance != null) {
                        VoxelPlayFirstPersonController.instance.autoInvertColors = false;
                    }
                    if (visibleChunksDistance.intValue > 6) {
                        visibleChunksDistance.intValue = 6;
                    }
                    if (forceChunkDistance.intValue > 2) {
                        forceChunkDistance.intValue = 2;
                    }
                    if (maxChunks.intValue > 5000) {
                        maxChunks.intValue = 5000;
                    }
                    rebuildWorld = true;
                }
                EditorGUILayout.EndHorizontal();

                prevBool = globalIllumination.boolValue;
                EditorGUILayout.PropertyField(globalIllumination, new GUIContent("Global Illumination", "Enables Voxel Play's own lightmap computation. This option adds smooth shading and lighting in combination with Unity shadow system."));
                if (globalIllumination.boolValue != prevBool)
                    refreshChunks = true;

                prevBool = enableSmoothLighting.boolValue;
                EditorGUILayout.PropertyField(enableSmoothLighting, new GUIContent("Smooth Lighting", "Interpolates lighting between voxel vertices. Also includes ambient occlusion."));
                if (enableSmoothLighting.boolValue != prevBool)
                    refreshChunks = true;

                GUI.enabled = enableSmoothLighting.boolValue;
                int prevInt = obscuranceMode.intValue;
                EditorGUILayout.PropertyField(obscuranceMode, new GUIContent("Obscurance Mode", "Changes shader obscurance function. Requires smooth lighting."));
                if (obscuranceMode.intValue != prevInt) {
                    updateSpecialFeaturesMacro = true;
                }
                if (obscuranceMode.intValue == (int)ObscuranceMode.Custom) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(obscuranceIntensity, new GUIContent("Intensity", "AO intensity."));
                    EditorGUI.indentLevel--;
                }
                GUI.enabled = true;

                EditorGUILayout.PropertyField(ambientLight, new GUIContent("Ambient Light", "Minimum amount of light in the scene affecting the voxels."));

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(enableShadows, new GUIContent("Shadows", "Turns on/off shadow casting and receiving on voxels."));
                if (EditorGUI.EndChangeCheck()) {
                    rebuildWorld = true;
                }
                if (!enableShadows.boolValue) {
                    CheckMainLightShadows();
                }
                EditorGUI.BeginChangeCheck();
                if (!enableURP.boolValue) {
                    EditorGUILayout.PropertyField(shadowsOnWater, new GUIContent("Shadows On Water", "Enables shadow receiving on water surface."));
                } else if (shadowsOnWater.boolValue) {
                    shadowsOnWater.boolValue = false;
                }
                EditorGUILayout.PropertyField(realisticWater, new GUIContent("Realistic Water", "Uses a realistic water shader."));
                if (EditorGUI.EndChangeCheck()) {
                    rebuildWorld = true;
                }

                EditorGUILayout.PropertyField(daylightShadowAtten, new GUIContent("Daylight Shadow Atten", "Shadow attenuation factor when Sun is high. Set this value to 0 to preserve standard shadow intensity. A value of 1 will make shadows disappear when Sun is on top. A middle value will make shadows more intense when Sun is low in the sky and more subtle when Sun is high."));

                prevBool = enableNormalMap.boolValue;
                EditorGUILayout.PropertyField(enableNormalMap, new GUIContent("Normal Mapping", "Enables use of normal maps."));
                if (prevBool != enableNormalMap.boolValue) {
                    refreshChunks = true;
                    reloadWorldTextures = true;
                }
                prevBool = enableReliefMapping.boolValue;
                EditorGUILayout.PropertyField(enableReliefMapping, new GUIContent("Relief Mapping", "Enables parallax occlusion/relief mapping."));
                if (prevBool != enableReliefMapping.boolValue) {
                    refreshChunks = true;
                    reloadWorldTextures = true;
                }
                if (enableReliefMapping.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(reliefStrength, new GUIContent("Strength", "Strength of the parallax effect."));
                    EditorGUILayout.PropertyField(reliefMaxDistance, new GUIContent("Max Distance", "Maximum visible distance for the parallax effect."));
                    EditorGUILayout.PropertyField(reliefIterations, new GUIContent("Iterations", "Max number of ray-marching steps."));
                    EditorGUILayout.PropertyField(reliefIterationsBinarySearch, new GUIContent("Binary Search Iterations", "Max number of binary search iterations to precisely find the intersection point."));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.PropertyField(textureSize, new GUIContent("Texture Size", "Texture size should be a multiple of 2 (eg. 16, 32, 64, 128)"));


                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(enableTinting, new GUIContent("Enable Tinting", "Enables individual voxel tint color."));
                EditorGUILayout.PropertyField(enableColoredShadows, new GUIContent("Colored Shadows", "When enabled, customize shadow tint color in the world definition."));
                if (EditorGUI.EndChangeCheck()) {
                    refreshChunks = true;
                    updateSpecialFeaturesMacro = true;
                }

                EditorGUILayout.PropertyField(enableOutline, new GUIContent("Outline", "Enables outline effect on solid voxels."));
                if (enableOutline.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(outlineColor, new GUIContent("Color", "Outline color and alpha."));
                    EditorGUILayout.PropertyField(outlineThreshold, new GUIContent("Threshold", "Controls outline width."));
                    EditorGUI.indentLevel--;
                }

                GUI.enabled = usePixelLights.boolValue;
                prevBool = enableBevel.boolValue;
                EditorGUILayout.PropertyField(enableBevel, new GUIContent("Bevel", "Enables bevel effect by tweaking top face normals. Looks better from a third-person view."));
                if (prevBool != enableBevel.boolValue && !Application.isPlaying) {
                    refreshChunks = true;
                    updateSpecialFeaturesMacro = true;
                }
                GUI.enabled = true;

                GUI.enabled = usePixelLights.boolValue;
                prevBool = enableFresnel.boolValue;
                EditorGUILayout.PropertyField(enableFresnel, new GUIContent("Fresnel", "Enables fresnel effect."));
                if (enableFresnel.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(fresnelExponent, new GUIContent("Exponent"));
                    EditorGUILayout.PropertyField(fresnelIntensity, new GUIContent("Intensity"));
                    EditorGUILayout.PropertyField(fresnelColor, new GUIContent("Color"));
                    EditorGUI.indentLevel--;
                }
                if (prevBool != enableFresnel.boolValue && !Application.isPlaying) {
                    refreshChunks = true;
                    updateSpecialFeaturesMacro = true;
                }

                prevBool = enableGlobalSpecular.boolValue;
                EditorGUILayout.PropertyField(enableGlobalSpecular, new GUIContent("Global Specular", "Enables specular for regular opaque voxels. Makes these voxels more shiny when facing the directional light."));
                if (enableGlobalSpecular.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(globalSpecularIntensity, new GUIContent("Intensity"));
                    EditorGUI.indentLevel--;
                }
                if (prevBool != enableGlobalSpecular.boolValue && !Application.isPlaying) {
                    refreshChunks = true;
                    updateSpecialFeaturesMacro = true;
                }

                GUI.enabled = true;


                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(doubleSidedGlass, new GUIContent("Double Sided Glass", "Renders both sides of transparent voxels."));
                EditorGUILayout.PropertyField(transparentBling, new GUIContent("Transparent Bling", "Enables shining effect on transparent voxels."));
                if (EditorGUI.EndChangeCheck()) {
                    rebuildWorld = true;
                }

                EditorGUILayout.PropertyField(damageParticles);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(enableBrightPointLights, new GUIContent("Bright Point Lights", "Improves appearance of point lights."));
                if (enableBrightPointLights.boolValue) {
                    EditorGUI.indentLevel++;
                    if (enableURP.boolValue) {
                        EditorGUILayout.PropertyField(enableURPNativeLights, new GUIContent("Enable URP Native Lights", "Adds support for native URP point and spot lights with shadows. Make sure additional lights and shadows are enabled in the URP asset used in Project Settings/Quality or Project Settings/Graphics."));
                    }
                }

                if (EditorGUI.EndChangeCheck()) {
                    refreshChunks = true;
                    updateSpecialFeaturesMacro = true;
                }

                if (enableBrightPointLights.boolValue) {
                    EditorGUILayout.PropertyField(brightPointsMaxDistance, new GUIContent("Max Distance", "Max distance to render bright point lights."));
                    EditorGUI.indentLevel--;
                }

                GUI.enabled = !Application.isPlaying;
                EditorGUI.BeginChangeCheck();
                enableCurvatureFromShader = EditorGUILayout.Toggle(new GUIContent("Curvature", "Enables curvature vertex modifier in VoxelPlay shaders."), enableCurvatureFromShader);
                enableCurvature.boolValue = enableCurvatureFromShader;
                if (EditorGUI.EndChangeCheck()) {
                    updateCurvatureMacro = true;
                    rebuildWorld = true;
                }
                if (enableCurvatureFromShader) {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.indentLevel++;
                    curvatureAmount = EditorGUILayout.TextField(new GUIContent("Amount", "Vertex shift amount multiplier."), curvatureAmount);
                    if (GUILayout.Button("Update", GUILayout.Width(65))) {
                        updateCurvatureMacro = true;
                        rebuildWorld = true;
                    }
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndHorizontal();
                }
                GUI.enabled = true;

                prevBool = seeThrough.boolValue;
                EditorGUILayout.PropertyField(seeThrough, new GUIContent("See Through", "Hides voxels between camera and desired target. This option is designed for third person perspective."));
                if (prevBool != seeThrough.boolValue) {
                    updateSpecialFeaturesMacro = true;
                }
                if (seeThrough.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(seeThroughTarget, new GUIContent("Target", "The target gameobject. Usually this is the character controller or player gameobject."));
                    EditorGUILayout.PropertyField(seeThroughRadius, new GUIContent("Radius", "Radius of effect. No voxels will be visible within this distance to the target."));
                    EditorGUILayout.PropertyField(seeThroughHeightOffset, new GUIContent("Height Offset", "Voxels below target plus this height offset won't be hidden. This option avoids hiding the ground."));
                    EditorGUILayout.PropertyField(seeThroughAlpha, new GUIContent("Alpha", "The alpha value used for occluded voxels with see-through mode set to transparency."));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(useOriginShift, new GUIContent("Origin Shift", "Shift player to origin when its position passes beyond a threshold. This is called origin shift and is necessary to avoid floating point issues."));
                if (useOriginShift.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(originShiftDistanceThreshold, new GUIContent("Distance Threshold", "The distance at which the origin shift occurs."));
                    EditorGUI.indentLevel--;
                }

            }


            // Rendering
            EditorGUILayout.Separator();
            expandRenderingSection = EditorGUILayout.Foldout(expandRenderingSection, "Rendering Options", sectionHeaderStyle);
            if (expandRenderingSection) {

                EditorGUI.BeginChangeCheck();
                GUI.enabled = SystemInfo.supportsComputeShaders;
                GUIContent computeGUIContent = new GUIContent("Compute Buffers", "Enables compute buffers for custom voxels. This option requires GPU capable of Shader Model 4.5 so it will restrict the amount of potential mobile devices that can run your game. Performance benefits vs regular GPU instancing in custom voxels may vary depending on platform, amount of voxels, etc. Do a benchmark before using this option.");
                if (!GUI.enabled) {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(computeGUIContent, GUILayout.Width(EditorGUIUtility.labelWidth));
                    EditorGUILayout.LabelField("(Unsupported platform or graphics API)");
                    EditorGUILayout.EndHorizontal();
                } else {
                    EditorGUILayout.PropertyField(useComputeBuffers, computeGUIContent);
                }
                if (EditorGUI.EndChangeCheck()) {
                    rebuildWorld = true;
                }
                GUI.enabled = true;

                EditorGUILayout.PropertyField(instancingCullingMode, new GUIContent("Instancing Culling Mode", "Aggresive is the default value: culls non visible voxels. Gentle allows some padding to keep shadows from invisible voxels. Disabled: renders all voxels, regardless of their positions vs camera."));
                if ((InstancingCullingMode)instancingCullingMode.intValue == InstancingCullingMode.Gentle) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(instancingCullingPadding);
                    EditorGUI.indentLevel--;
                }

                GUI.enabled = !Application.isPlaying;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(filterMode, new GUIContent("Texture Sampling", "Choose the texture sampling filter mode."));
                if (EditorGUI.EndChangeCheck()) {
                    refreshChunks = true;
                    reloadWorldTextures = true;
                    updateSpecialFeaturesMacro = true;
                }

                GUI.enabled = true;
                if (filterMode.intValue == (int)FilterMode.Point) {
                    GUI.enabled = !enableReliefMapping.boolValue;
                    EditorGUILayout.PropertyField(hqFiltering, new GUIContent("HQ Point Filter", "Enables mipmapping and intergrated texel antialiasing."));
                    if (prevBool != hqFiltering.boolValue) {
                        refreshChunks = true;
                        reloadWorldTextures = true;
                    }
                    if (hqFiltering.boolValue) {
                        EditorGUI.indentLevel++;
                        float prevFloat = mipMapBias.floatValue;
                        EditorGUILayout.PropertyField(mipMapBias, new GUIContent("MipMap Bias", "Increase to reduce texture blurring."));
                        if (mipMapBias.floatValue != prevFloat) {
                            refreshChunks = true;
                            reloadWorldTextures = true;
                        }
                        EditorGUI.indentLevel--;
                    }
                    GUI.enabled = true;
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(usePixelLights, new GUIContent("Per-Pixel Lighting", "If disabled, lighting will be calculated per-vertex."));
                if (EditorGUI.EndChangeCheck()) {
                    refreshChunks = true;
                    reloadWorldTextures = true;
                    updateSpecialFeaturesMacro = true;
                }

                EditorGUILayout.LabelField("Gaps/White Pixels Removal Methods", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                voxelPadding = EditorGUILayout.Toggle(new GUIContent("Voxel Padding", "Enlarges voxels a bit to prevent gaps (white pixels) in adjacent edges due to greedy meshing."), voxelPadding);
                if (EditorGUI.EndChangeCheck()) {
                    ChangeVoxelPadding();
                }
                EditorGUILayout.PropertyField(usePostProcessing, new GUIContent("Post Processing", "Uses a custom post processing effect to detect and remove white pixels."));
                if (usePostProcessing.boolValue && !VoxelPlayPostProcessing.isActive) {
                    EditorGUILayout.HelpBox("Additional steps are required:\nIn built-in pipeline, Voxel Play Post Processing script must be added to your camera.\nIn URP, add the Voxel Play Post Processing render feature to the URP Universal Renderer.", MessageType.Warning);
                }
                if (voxelPadding && usePostProcessing.boolValue) {
                    EditorGUILayout.HelpBox("Only one method to remove white pixels should be used.", MessageType.Warning);
                }
                EditorGUI.indentLevel--;
            }


            // Stats
            EditorGUILayout.Separator();
            expandStatsSection = EditorGUILayout.Foldout(expandStatsSection, "Stats", sectionHeaderStyle);
            if (expandStatsSection) {
                ShowProgressBar("Chunk Rendering: Pending (" + env.chunksInRenderQueueCount + ") / Drawn (" + env.chunksDrawn + ")", (env.chunksDrawn + 1f) / (env.chunksDrawn + env.chunksInRenderQueueCount + 1f));
                if (env.enableTrees) {
                    ShowProgressBar("Tree Creation: Pending (" + env.treesInCreationQueueCount + ") / Created (" + env.treesCreated + ")", (env.treesCreated + 1f) / (env.treesCreated + env.treesInCreationQueueCount + 1f));
                } else {
                    ShowProgressBar("Tree Creation: ---", 1f);
                }
                if (env.enableVegetation) {
                    ShowProgressBar("Bush Creation: Pending (" + env.vegetationInCreationQueueCount + ") / Created (" + env.vegetationCreated + ")", (env.vegetationCreated + 1f) / (env.vegetationCreated + env.vegetationInCreationQueueCount + 1f));
                } else {
                    ShowProgressBar("Bush Creation: ---", 1f);
                }
                EditorGUILayout.LabelField(new GUIContent("Total Chunks Created", "Increases when the chunk contents are generated which occurs when a chunk is created for the first time or when a chunk is reused and its contents replaced."), new GUIContent(env.chunksCreated.ToString()));
                EditorGUILayout.LabelField("Chunks Pool Usage", env.chunksUsed + " of " + maxChunks.intValue + " (" + (env.chunksUsed * 100f / env.maxChunks).ToString("F1") + "%)");
                EditorGUILayout.LabelField(new GUIContent("Total Voxels Created", "Number of voxels that contribute to mesh generation. Fully surrounded voxels are hidden and are not included."), new GUIContent(env.voxelsCreatedCount.ToString()));
            }

            // Voxel Generation
            EditorGUILayout.Separator();
            expandVoxelGenerationSection = EditorGUILayout.Foldout(expandVoxelGenerationSection, "Voxel Generation", sectionHeaderStyle);
            if (expandVoxelGenerationSection) {
                EditorGUILayout.PropertyField(enableGeneration, new GUIContent("Enable Generation", "Enables/disables world/voxel generation updates."));
                EditorGUILayout.PropertyField(maxChunks, new GUIContent("Chunks Pool Size", "Number of total chunks allowed in memory."));
                EditorGUILayout.LabelField("   Recommended >=", env.maxChunksRecommended.ToString());
                EditorGUILayout.IntSlider(prewarmChunksInEditor, 1000, maxChunks.intValue, new GUIContent("   Prewarm In Editor", "Number of chunks that will be reserved during start in Unity Editor before game starts. In the final build, all chunks are reserved before game starts to provide a smooth gameplay experience."));
                EditorGUILayout.BeginHorizontal();
                chunkNewSize = EditorGUILayout.IntPopup("Chunk Size", chunkNewSize, chunkSizeOptions, chunkSizeValues);
                GUI.enabled = chunkNewSize != VoxelPlayEnvironment.CHUNK_SIZE;
                if (GUILayout.Button("Change", GUILayout.Width(80))) {
                    ChangeChunkSize();
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(onlyRenderInFrustum, new GUIContent("Only Render In Frustum", "When enabled, only chunks inside the camera frustum will be rendered."));
#if UNITY_WEBGL
				GUI.enabled = false;
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Multi Thread Generation", GUILayout.Width (EditorGUIUtility.labelWidth));
				EditorGUILayout.LabelField ("(Unsupported platform)");
				EditorGUILayout.EndHorizontal ();
				GUI.enabled = true;
#else
                EditorGUILayout.PropertyField(multiThreadGeneration, new GUIContent("Multi Thread Generation", "When enabled, uses a dedicated background thread for chunk generation (only in build, deactivated while running inside Unity Editor)."));
#endif
                EditorGUILayout.PropertyField(visibleChunksDistance, new GUIContent("Visible Chunk Distance", "Measured in number of chunks."));
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(adjustCameraFarClip, new GUIContent("Adjust Cam Far Clip", "Adjusts camera's far clipping plane to visible chunk distance automatically."));
                EditorGUILayout.PropertyField(distanceAnchor, new GUIContent("Distance Anchor", "Where the distance is computed from. Usually this is the camera (in first person view) or the character (in third person view)."));
                EditorGUILayout.PropertyField(unloadFarChunks, new GUIContent("Unload Far Chunks", "Disable or destroy chunk gameobject when it's out of visible distance. Enable/create it again when it enters the visible distance."));
                if (unloadFarChunks.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(unloadFarChunksMode, new GUIContent("Mode", "Select the action when a chunk is unloaded. 'Toggle visibility' will just hide/show the chunks based on the visible distance parameter. 'Destroy' will actually destroy and release memory of the chunk mesh as well as its collider and NavMesh (if present)."));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.PropertyField(unloadFarNavMesh, new GUIContent("Unload Far NavMesh", "Allows reusing a chunk NavMesh when it's out of visible distance. Note: NavMeshes are linked to chunks so when chunk pool is exhausted, NavMeshes will be reused automatically for new chunk requests. This option just releases the chunk NavMesh earlier when chunk is out of visible distance, without waiting for the pool to be depleted."));
                EditorGUI.indentLevel--;
                EditorGUILayout.PropertyField(forceChunkDistance, new GUIContent("Force Chunk Distance", "Distance measured in chunks that will be rendered completely before starting the game."));
                EditorGUILayout.PropertyField(maxCPUTimePerFrame, new GUIContent("Max CPU Time Per Frame", "Maximum milliseconds that can be used by the CPU per frame to generate the world."));
                EditorGUILayout.PropertyField(maxChunksPerFrame, new GUIContent("Max Chunks Per Frame", "Maximum number of chunks that can be generated in a single frame (0 = unlimited (limited only by the maxCPUTimePerFrame value)"));
                EditorGUILayout.PropertyField(maxTreesPerFrame, new GUIContent("Max Trees Per Frame", "Maximum number of trees that can be generated in a single frame  (0 = unlimited (limited only by the maxCPUTimePerFrame value)"));
                EditorGUILayout.PropertyField(maxBushesPerFrame, new GUIContent("Max Bushes Per Frame", "Maximum number of bushes that can be generated in a single frame  (0 = unlimited (limited only by the maxCPUTimePerFrame value)"));
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(enableColliders, new GUIContent("Colliders", "Enables/disables collider generation for opaque voxels."));
                EditorGUILayout.PropertyField(enableNavMesh, new GUIContent("NavMesh", "Enables/disables NavMesh generation."));
                if (enableNavMesh.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(navMeshResolution, new GUIContent("Resolution", "Detail of the generated navMesh. Use a higher resolution if you need navMesh to be created on single voxels or Default every two voxels."));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.PropertyField(hideChunksInHierarchy, new GUIContent("Hide Chunks In Hierarchy", "Do not show chunks in hierarchy (this option has no effect in a build)"));
                EditorGUILayout.PropertyField(enableTrees, new GUIContent("Trees", "Enables/disables tree generation."));
                if (EditorGUI.EndChangeCheck()) {
                    rebuildWorld = true;
                }
                if (enableTrees.boolValue) {
                    prevBool = denseTrees.boolValue;
                    EditorGUILayout.PropertyField(denseTrees, new GUIContent("   Dense Trees", "If enabled, disables adjacent voxel occlusion making tree leaves cutout denser."));
                    if (denseTrees.boolValue != prevBool)
                        refreshChunks = true;
                }
                prevBool = enableVegetation.boolValue;
                EditorGUILayout.PropertyField(enableVegetation, new GUIContent("Vegetation", "Enables/disables bush generation."));
                if (enableVegetation.boolValue != prevBool)
                    rebuildWorld = true;
                EditorGUILayout.PropertyField(enableDetailGenerators, new GUIContent("Detail Generators", "Enables/disables world detail generators."));
                EditorGUILayout.PropertyField(particlePoolSize, new GUIContent("Particle Pool Size", "Maximum number of active particles, including recoverable voxels"));
                layerParticles.intValue = EditorGUILayout.LayerField(new GUIContent("Particles Layer", "The layer used for particles. Used to optimize physics and avoid particle collision between them."), layerParticles.intValue);
                layerVoxels.intValue = EditorGUILayout.LayerField(new GUIContent("Voxels Layer", "The layer used for voxels. Used to optimize physics and avoid voxels collision between them."), layerVoxels.intValue);
                layerClouds.intValue = EditorGUILayout.LayerField(new GUIContent("Clouds Layer", "The layer used for cloud voxels. Can be used to ignore cloud chunks if using a top-down camera or for other purposes."), layerClouds.intValue);
            }

            // Sky Properties
            EditorGUILayout.Separator();
            expandSkySection = EditorGUILayout.Foldout(expandSkySection, "Sky Properties", sectionHeaderStyle);
            if (expandSkySection) {
                EditorGUILayout.PropertyField(sun, new GUIContent("Sun", "Assigns the directional light used as the Sun."));
                EditorGUILayout.PropertyField(enableFogSkyBlending, new GUIContent("Enable Fog", "Enabled fog/sky blending."));
                if (enableFogSkyBlending.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(fogAmount, new GUIContent("Fog Height", "Amount of fog."));
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(fogDistanceAuto, new GUIContent("Auto Distance", "Adjust fog distance to match camera's far clipping plane or visible chunk distance if unload chunks is enabled (the lower distance)."));
                    if (env.cameraMain != null) {
                        EditorGUILayout.LabelField("(Currently: " + env.GetFogAutoDistance() + ")");
                    }
                    EditorGUILayout.EndHorizontal();
                    if (!fogDistanceAuto.boolValue) {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(fogDistance, new GUIContent("Fog Distance", "Fog's distance factor"));
                        EditorGUI.indentLevel--;
                    }
                    EditorGUILayout.PropertyField(fogFallOff, new GUIContent("Fog Fall Off", "Fog's fall off factor"));
                    EditorGUILayout.PropertyField(fogTint, new GUIContent("Fog Tint", "Fog's tint color"));
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.PropertyField(enableClouds, new GUIContent("Enable Clouds", "Clouds generation on/off"));
            }

            EditorGUILayout.Separator();
            expandInGameSection = EditorGUILayout.Foldout(expandInGameSection, "Optional Game Features", sectionHeaderStyle);
            if (expandInGameSection) {
                EditorGUILayout.PropertyField(enableLoadingPanel, new GUIContent("Loading Screen", "Shows a loading panel during start up while chunks are being reserved."));
                if (enableLoadingPanel.boolValue) {
                    EditorGUILayout.PropertyField(loadingText, new GUIContent("   Text", "Text to show while initializing the engine."));
                }
                EditorGUILayout.PropertyField(initialWaitTime, new GUIContent("Initial Wait Time", "Additional seconds to wait before loading screen is removed."));
                if (initialWaitTime.floatValue > 0) {
                    EditorGUILayout.PropertyField(initialWaitText, new GUIContent("   Text", "Text to show diring the additional wait time."));
                }
                GUI.enabled = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android || EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS;
                EditorGUILayout.PropertyField(previewTouchUIinEditor, new GUIContent("Preview Mobile UI in Editor", "Shows mobile UI in Editor when targeting a mobile platform."));
                GUI.enabled = true;
                EditorGUILayout.PropertyField(enableBuildMode, new GUIContent("Enable Build Mode", "Enables entering Build Mode by pressing key B. In build mode, all world items are available in the inventory in unlimited amount and anything can be destroyed with a single hit. Player is also indestructible."));
                if (enableBuildMode.boolValue) {
                    EditorGUILayout.PropertyField(buildMode, new GUIContent("   Build Mode ON", "Activates build mode."));
                }
                EditorGUILayout.PropertyField(enableConsole, new GUIContent("Enable Console", "Enables console system. Shows when pressing F1."));
                if (enableConsole.boolValue) {
                    EditorGUILayout.PropertyField(showConsole, new GUIContent("   Visible", "Toggles console visibility on/off. The console shows useful data for debugging purposes."));
                    EditorGUILayout.PropertyField(consoleBackgroundColor, new GUIContent("   Background Color"));
                }
                EditorGUILayout.PropertyField(enableStatusBar, new GUIContent("Enable Status Bar"));
                if (enableStatusBar.boolValue) {
                    EditorGUILayout.PropertyField(statusBarBackgroundColor, new GUIContent("   Status Bar Color"));
                }
                EditorGUILayout.PropertyField(enableInventory, new GUIContent("Enable Inventory", "Enables inventory UI when pressing Tab. Disable if you wish to provide your own interface."));
                EditorGUILayout.PropertyField(enableDebugWindow, new GUIContent("Enable Debug Window", "Enables debug window toggling using F2."));
                EditorGUILayout.PropertyField(showFPS, new GUIContent("Show FPS", "Shows FPS on top/right screen corner."));
                EditorGUILayout.PropertyField(loadSavedGame, new GUIContent("Load Saved Game", "If Voxel Play should load a previously saved game at start up. Specify name of saved game in 'Save Filename' field."));
                if (loadSavedGame.boolValue) {
                    EditorGUILayout.PropertyField(saveFilename, new GUIContent("   Filename", "The current name for the saved game file. Used at runtime when pressing F3 to load or F4 to save. You can set a different save filename at runtime to support multiple save slots."));
                }
            }

            EditorGUILayout.Separator();
            expandDefaultsSection = EditorGUILayout.Foldout(expandDefaultsSection, "Default Assets", sectionHeaderStyle);
            if (expandDefaultsSection) {
                EditorGUILayout.PropertyField(defaultBuildSound, new GUIContent("Build Sound", "Default sound played when an item or voxel is placed in the scene."));
                EditorGUILayout.PropertyField(defaultPickupSound, new GUIContent("Pick Up Sound", "Default sound played when an item is collected."));
                EditorGUILayout.PropertyField(defaultImpactSound, new GUIContent("Impact Sound", "Default sound played when a voxel is hit."));
                EditorGUILayout.PropertyField(defaultDestructionSound, new GUIContent("Destruction Sound", "Default sound played when a voxel is destroyed."));
                EditorGUILayout.PropertyField(defaultVoxel, new GUIContent("Default Voxel", "Assumed voxel when the voxel definition is missing or placing colors directly on the positions."));
                EditorGUILayout.PropertyField(defaultWaterVoxel, new GUIContent("Default Water Voxel", "Default water voxel in case the terrain generator doesn't assign one."));

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(inputControllerPC, new GUIContent("Input Prefab (PC)", "The prefab that contains the input controller script for PC."));
                if (GUILayout.Button("Load Default", GUILayout.Width(120))) {
                    inputControllerPC.objectReferenceValue = Resources.Load<GameObject>("VoxelPlay/InputControllers/PC/Voxel Play PC Input Controller");
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(inputControllerMobile, new GUIContent("Input Prefab (Mobile)", "The prefab that contains the input controller script for mobile."));
                if (GUILayout.Button("Load Default", GUILayout.Width(120))) {
                    inputControllerMobile.objectReferenceValue = Resources.Load<GameObject>("VoxelPlay/InputControllers/Mobile/Voxel Play Mobile Input Controller");
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(uiCanvasPrefab, new GUIContent("UI Prefab", "The canvas prefab used for the game main interface. This interface has elements for inventory, selected item, crosshair and other information."));
                if (GUILayout.Button("Load Default", GUILayout.Width(120))) {
                    uiCanvasPrefab.objectReferenceValue = Resources.Load<GameObject>("VoxelPlay/UI/Voxel Play UI Canvas");
                }
                EditorGUILayout.EndHorizontal();

                if (uiCanvasPrefab.objectReferenceValue != null) {
                    EditorGUILayout.PropertyField(welcomeMessage, new GUIContent("Welcome Text", "Optional message shown when game starts"));
                    EditorGUILayout.PropertyField(welcomeMessageDuration, new GUIContent("Welcome Duration", "Duration for the welcome text"));
                }

                EditorGUILayout.PropertyField(crosshairPrefab, new GUIContent("Crosshair Prefab", "The prefab used for the crosshair."));
                EditorGUILayout.PropertyField(crosshairTexture, new GUIContent("Crosshair Texture", "The texture used for the crosshair."));
            }

            EditorGUILayout.Separator();

            expandAdvancedSection = EditorGUILayout.Foldout(expandAdvancedSection, "Advanced", sectionHeaderStyle);
            if (expandAdvancedSection) {
                EditorGUILayout.PropertyField(debugLevel);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serverMode, new GUIContent("Server Mode", "In server mode, Voxel Play doesn't render voxels to reduce memory usage and improve performance of system when running on an unattended server."));
                if (EditorGUI.EndChangeCheck() && serverMode.boolValue) {
                    lowMemoryMode.boolValue = true;
                }
                EditorGUILayout.PropertyField(lowMemoryMode, new GUIContent("Low Memory Mode", "When enabled, internal rendering buffers are not pre-allocated during start up. Memory allocation occur when needed only. Enable this option to reduce memory pressure warnings on mobile devices or on dedicated servers with low memory. Some memory allocation spike can occur when a buffer needs resizing."));
                EditorGUILayout.PropertyField(delayedInitialization, new GUIContent("Delayed Initialization", "When enabled, Voxel Play won't initialize until you call the Init() method."));
                EditorGUILayout.BeginHorizontal();
                maxMaterialsPerChunk = EditorGUILayout.IntField(new GUIContent("Max Materials Per Chunk", "The number of different materials that can be used in a single chunk. Please note that this number should be kept low to reduce memory usage and improve performance."), maxMaterialsPerChunk);
                GUI.enabled = maxMaterialsPerChunk != VoxelPlayEnvironment.MAX_MATERIALS_PER_CHUNK;
                if (GUILayout.Button("Change", GUILayout.Width(80))) {
                    ChangeMaxMaterialsPerChunk();
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Separator();

            if (GUILayout.Button("Import Models...")) {
                VoxelPlayImportTools.ShowWindow();
            }

            EditorGUILayout.Separator();

            if (serializedObject.ApplyModifiedProperties() || rebuildWorld || (Event.current.type == EventType.ExecuteCommand &&
                Event.current.commandName == "UndoRedoPerformed")) {
                if (updateSpecialFeaturesMacro) {
                    Debug.Log("Optimization: modifying scripts/shaders macros to reflect special feature change...");
                    env.UpdateSpecialFeaturesCodeMacro();
                    GUIUtility.ExitGUI();
                    return;
                }
                if (updateCurvatureMacro) {
                    UpdateCurvatureMacro();
                    GUIUtility.ExitGUI();
                    return;
                }
                if (env.gameObject.activeInHierarchy) {
                    if (Application.isPlaying || env.renderInEditor) {
                        if (rebuildWorld) {
                            rebuildWorld = false;
                            env.ReloadWorld();

                            // Check if scene camera is under terrain
                            if (!Application.isPlaying && env.renderInEditor && SceneView.lastActiveSceneView != null) {
                                Camera cam = SceneView.lastActiveSceneView.camera;
                                if (cam != null) {
                                    Vector3 camPos = SceneView.lastActiveSceneView.pivot;
                                    float h = env.GetTerrainHeight(camPos, true);
                                    if (camPos.y < h + 2) {
                                        camPos.y = h + 2;
                                    } else if (camPos.y > h + 100) {
                                        camPos.y = h + 50f;
                                    }
                                    SceneView.lastActiveSceneView.LookAt(camPos);
                                }
                            }
                        } else if (refreshChunks) {
                            refreshChunks = false;
                            env.Redraw(reloadWorldTextures);
                        }
                        env.UpdateMaterialProperties();
                    }
                }

                EditorApplication.update -= env.UpdateInEditor;

                if (renderInEditor.boolValue) {
                    EditorApplication.update += env.UpdateInEditor;
                }
            }
        }

        void ShowHelpButtons(bool showHideButton) {
            if (showHideButton) {
                if (GUILayout.Button("New Tip", GUILayout.Width(90))) {
                    cookieIndex++;
                }
                if (GUILayout.Button("Hide Help Section", GUILayout.Width(130))) {
                    cookieIndex = -1;
                    GUIUtility.ExitGUI();
                }
            } else if (GUILayout.Button("Help & Tutorials", GUILayout.Width(130))) {
                cookieIndex++;
            }
        }


        void ShowProgressBar(string text, float progress) {
            Rect r = EditorGUILayout.BeginVertical();
            EditorGUI.ProgressBar(r, progress, text);
            GUILayout.Space(18);
            EditorGUILayout.EndVertical();
        }


        void CreateWorldDefinition() {
            WorldDefinition wd = ScriptableObject.CreateInstance<WorldDefinition>();
            wd.name = "New World Definition";
            AssetDatabase.CreateAsset(wd, "Assets/" + wd.name + ".asset");
            AssetDatabase.SaveAssets();
            world.objectReferenceValue = wd;
            EditorGUIUtility.PingObject(wd);
        }


        string GetShaderOptionValue(string option, string file) {
            string[] res = Directory.GetFiles(Application.dataPath, file, SearchOption.AllDirectories);
            string path = null;
            for (int k = 0; k < res.Length; k++) {
                if (res[k].Contains("Voxel Play")) {
                    path = res[k];
                    break;
                }
            }
            if (path == null) {
                Debug.LogError(file + " could not be found!");
                return "";
            }

            string[] code = File.ReadAllLines(path, System.Text.Encoding.UTF8);
            string searchToken = "#define " + option;
            for (int k = 0; k < code.Length; k++) {
                if (code[k].Contains(searchToken)) {
                    string[] values = code[k].Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (values.Length == 3) {
                        return values[2];
                    }
                    break;
                }
            }
            return "";
        }

        void SetShaderOptionValue(string option, string file, string value) {
            string[] res = Directory.GetFiles(Application.dataPath, file, SearchOption.AllDirectories);
            string path = null;
            for (int k = 0; k < res.Length; k++) {
                if (res[k].Contains("Voxel Play")) {
                    path = res[k];
                    break;
                }
            }
            if (path == null) {
                Debug.LogError(file + " could not be found!");
                return;
            }

            string[] code = File.ReadAllLines(path, System.Text.Encoding.UTF8);
            string searchToken = "#define " + option;
            for (int k = 0; k < code.Length; k++) {
                if (code[k].Contains(searchToken)) {
                    code[k] = "#define " + option + " " + value;
                    File.WriteAllLines(path, code, System.Text.Encoding.UTF8);
                    break;
                }
            }
        }

        public void UpdateCurvatureMacro() {
            env.SetShaderOptionValue("VOXELPLAY_CURVATURE", "VPCommonVertexModifier.cginc", enableCurvature.boolValue ? "1" : "0");
            env.SetShaderOptionValue("VOXELPLAY_CURVATURE_AMOUNT", "VPCommonVertexModifier.cginc", curvatureAmount);
            Debug.Log("Voxel Play shaders updated.");
            AssetDatabase.Refresh();
        }

        void CheckMainLightShadows() {
            Light[] lights = Misc.FindObjectsOfType<Light>();
            for (int k = 0; k < lights.Length; k++) {
                if (lights[k].isActiveAndEnabled && lights[k].shadows != LightShadows.None) {
                    EditorGUILayout.HelpBox("Light '" + lights[k].name + "' currently is configured to cast shadows. Consider disabling shadows on your lights as well to improve performance.", MessageType.Info);
                }
            }
        }

        void ChangeChunkSize() {
            if (!EditorUtility.DisplayDialog("Change Chunk Size", "Please note that saved games with different chunk sizes cannot be loaded.\nThe view distance and chunk pool size will be adjusted to reflect the new chunk size.\n\nDo you want to change the chunk size? (it won't modify any saved game).", "Yes", "No")) {
                return;
            }
            int newVisibleDistance = visibleChunksDistance.intValue * VoxelPlayEnvironment.CHUNK_SIZE / chunkNewSize;
            newVisibleDistance = Mathf.Clamp(newVisibleDistance, 1, 25);
            visibleChunksDistance.intValue = newVisibleDistance;
            maxChunks.intValue = env.maxChunksRecommended;
            serializedObject.ApplyModifiedProperties();
            env.UpdateChunkSizeInCode(chunkNewSize);
            Debug.Log("New chunk size updated.");
            AssetDatabase.Refresh();
            GUIUtility.ExitGUI();
        }


        void ChangeMaxMaterialsPerChunk() {
            if (maxMaterialsPerChunk < 16) {
                EditorUtility.DisplayDialog("Change Max Materials Per Chunk", "Minimum material count is 16.", "Ok");
                return;
            }
            if (!EditorUtility.DisplayDialog("Change Max Materials Per Chunk", "Please note that increasing the number of materials per chunk increases memory usage and can affect performance..\n\nDo you want to change the maximum material count per chunk?", "Yes", "No")) {
                return;
            }
            env.UpdateMaxMaterialsPerChunk(maxMaterialsPerChunk);
            Debug.Log("Max Materials Per Chunk updated.");
            AssetDatabase.Refresh();
            GUIUtility.ExitGUI();
        }

        

        void ChangeVoxelPadding() {
            env.UpdateVoxelPadding(voxelPadding);
            Debug.Log("Voxel Padding updated.");
            AssetDatabase.Refresh();
            GUIUtility.ExitGUI();
        }


        void GenerateEditorArea() {
            if (!EditorUtility.DisplayDialog("Generate Chunks In Area", "Warning: chunks in area of size " + renderInEditorAreaSize.vector3Value + " with center at " + renderInEditorAreaCenter.vector3Value + " will be generated now.\n\nConfirm?", "Yes", "Cancel")) return;

            Vector3 sizeInChunks = renderInEditorAreaSize.vector3Value / VoxelPlayEnvironment.CHUNK_SIZE;
            int totalChunks = (int)(sizeInChunks.x * sizeInChunks.y * sizeInChunks.z);
            if (totalChunks > env.maxChunks) {
                EditorUtility.DisplayDialog("Max Chunks Exceeded!", "Total chunks to be generated (" + totalChunks + ") exceeds current pool size. Increase pool size or reduce size of area to be generated.", "Ok");
                return;
            }
            env.ChunkCheckArea(renderInEditorAreaCenter.vector3Value, sizeInChunks / 2f, true);
        }

#if USES_URP

        #region SRP utils

        void CheckDepthPrimingMode() {
            RenderPipelineAsset pipe = GraphicsSettings.currentRenderPipeline;
            if (pipe == null) return;
            // Check depth priming mode
            FieldInfo renderers = pipe.GetType().GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance);
            if (renderers == null) return;
            foreach (var renderer in (object[])renderers.GetValue(pipe)) {
                if (renderer == null) continue;
                FieldInfo depthPrimingModeField = renderer.GetType().GetField("m_DepthPrimingMode", BindingFlags.NonPublic | BindingFlags.Instance);
                int depthPrimingMode = -1;
                if (depthPrimingModeField != null) {
                    depthPrimingMode = (int)depthPrimingModeField.GetValue(renderer);
                }

                FieldInfo renderingModeField = renderer.GetType().GetField("m_RenderingMode", BindingFlags.NonPublic | BindingFlags.Instance);
                int renderingMode = -1;
                if (renderingModeField != null) {
                    renderingMode = (int)renderingModeField.GetValue(renderer);
                }

                if (depthPrimingMode > 0 && renderingMode != 1) {
                    EditorGUILayout.HelpBox("Depth Priming Mode in URP asset must be disabled.", MessageType.Warning);
                    if (GUILayout.Button("Show Pipeline Asset")) {
                        Selection.activeObject = (UnityEngine.Object)renderer;
                        GUIUtility.ExitGUI();
                    }
                    EditorGUILayout.Separator();
                }
            }
        }
#endregion

#endif

    }

}
