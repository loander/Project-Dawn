using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VoxelPlay {

    public delegate void VoxelHitEvent(VoxelChunk chunk, int voxelIndex, ref int damage);
    public delegate void VoxelHitInfoEvent(VoxelHitInfo hitInfo, ref int damage);
    public delegate void VoxelHitAfterEvent(VoxelChunk chunk, int voxelIndex, int damage);
    public delegate void VoxelHitInfoAfterEvent(VoxelHitInfo hitInfo, int damage);
    public delegate void VoxelHitsEvent(List<VoxelIndex> voxelIndices);
    public delegate void VoxelEvent(VoxelChunk chunk, int voxelIndex);
    public delegate void VoxelPositionEvent(Vector3d position, VoxelChunk chunk, int voxelIndex);
    public delegate void VoxelPlaceEvent(Vector3d position, VoxelChunk chunk, int voxelIndex, ref VoxelDefinition voxelDefinition, ref Color32 tintColor);
    public delegate void VoxelDropItemEvent(VoxelChunk chunk, VoxelHitInfo hitInfo, out bool canBeCollected);
    public delegate void VoxelClickEvent(VoxelChunk chunk, int voxelIndex, int buttonIndex);
    public delegate void VoxelChunkBeforeCreationEvent(Vector3d chunkCenter, out bool overrideDefaultContents, VoxelChunk chunk, out bool isAboveSurface);
    public delegate void VoxelChunkBeforeDetailGenerationEvent(VoxelChunk chunk, VoxelPlayDetailGenerator generator, out bool cancelDetailGeneration);
    public delegate void VoxelChunkEvent(VoxelChunk chunk);
    public delegate void VoxelChunkUnloadEvent(VoxelChunk chunk, out bool canReuse);
    public delegate void VoxelTorchEvent(VoxelChunk chunk, LightSource lightSource);
    public delegate void VoxelPlayEvent();
    public delegate void VoxelLightRefreshEvent();
    public delegate void RepaintActionEvent();
    public delegate void VoxelModelBuildStartEvent(ModelDefinition model, Vector3d position, out bool cancel);
    public delegate void VoxelModelBuildEndEvent(ModelDefinition model, Vector3d position);
    public delegate void VoxelCollapseEvent(List<VoxelIndex> indices);


    [ExecuteInEditMode]
    public partial class VoxelPlayEnvironment : MonoBehaviour {

        public delegate void AfterInitCallback();

        [NonSerialized]
        public bool initialized, applicationIsPlaying;

#if UNITY_EDITOR
        public event RepaintActionEvent WantRepaintInspector;

        long lastCameraMoveTime, lastInspectorUpdateTime;
#endif


        const string VOXELPLAY_WORLD_ROOT = "Voxel Play World";

        // Shader keywords
        public const string SKW_VOXELPLAY_GPU_INSTANCING = "VOXELPLAY_GPU_INSTANCING";
        public const string SKW_VOXELPLAY_USE_ROTATION = "VOXELPLAY_USE_ROTATION";
        public const string SKW_VOXELPLAY_USE_NORMAL = "VOXELPLAY_USE_NORMAL";
        const string SKW_VOXELPLAY_USE_OUTLINE = "VOXELPLAY_USE_OUTLINE";
        const string SKW_VOXELPLAY_USE_PARALLAX = "VOXELPLAY_USE_PARALLAX";
        const string SKW_VOXELPLAY_GLOBAL_USE_FOG = "VOXELPLAY_GLOBAL_USE_FOG";
        const string SKW_VOXELPLAY_AA_TEXELS = "VOXELPLAY_USE_AA";
        const string SKW_VOXELPLAY_USE_PIXEL_LIGHTS = "VOXELPLAY_PIXEL_LIGHTS";
        const string SKW_VOXELPLAY_TRANSP_BLING = "VOXELPLAY_TRANSP_BLING";

        [NonSerialized]
        public System.Diagnostics.Stopwatch stopWatch;
        Vector3 lastCamPos, currentCamPos;
        Vector3d lastAnchorPos;

        [NonSerialized] public Vector3d currentAnchorPos;

        /// <summary>
        /// The currentAnchorPos in world space (it's same than currentAnchorPos when origin shift is disabled)
        /// </summary>
        [NonSerialized] public Vector3 currentAnchorPosWS;

        float lastCamOrthoSize;
        Quaternion lastCamRot, currentCamRot;
        bool shouldCheckChunksInFrustum;
        Material skyboxEarth, skyboxEarthSimplified, skyboxSpace, skyboxEarthNightCube, skyboxEarthDayNightCube, skyboxMaterial;
        Camera sceneCam;
        Collider characterControllerCollider;
        Material modelHighlightMat;

        /// <summary>
        /// The transform of the world root where all objects created by Voxel Play are placed
        /// </summary>
        [NonSerialized]
        public Transform worldRoot;

        /// <summary>
        /// Stores the last message send to ShowMessage() method
        /// </summary>
        [NonSerialized]
        public string lastMessage = "";

        [NonSerialized]
        public bool isMobilePlatform;

        [NonSerialized]
        internal bool draftModeActive;

        [NonSerialized] public int STAGE;

        Collider[] tempColliders;

        readonly int[] neighbourOffsets = {
            0, 1, 0,
            1, 0, 0,
            -1, 0, 0,
            0, 0, 1,
            0, 0, -1,
            0, -1, 0
        };

        /// <summary>
        /// Used to tag and group several modifications of a chunk in a loop
        /// </summary>
        int modificationTag;

        #region Gameloop events

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init() {
            VoxelPlayEnvironment env = instance;
            if (env != null) {
                env.DisposeAll();
            }
        }

        void OnEnable() {
            applicationIsPlaying = Application.isPlaying;
            VRCheck.Init();

            if (delayedInitialization && applicationIsPlaying) return;
            OnEnableInternal();
        }

        void OnEnableInternal() {
            if (!initialized) {
#if UNITY_EDITOR
                CheckSpecialFeaturesScriptingSupport();
#endif
                InitAndLoadSaveGame();
            }
        }

        void OnValidate() {
            visibleChunksDistance = Mathf.Max(visibleChunksDistance, forceChunkDistance);
        }


        void LateUpdate() {
            if (applicationIsPlaying && initialized) {
                DoWork();
                if (useOriginShift) {
                    OriginShiftUpdate();
                }
                ProcessThreadMessages();
            }
        }

        void FixedUpdate() {
            if (applicationIsPlaying && initialized) {
                UpdateParticlesForces();
            }
        }

        void OnDisable() {
            StopGenerationThreads();
        }

        void OnDestroy() {
#if UNITY_EDITOR
            EditorApplication.update -= UpdateInEditor;
#endif
            DisposeAll();
        }

        #endregion

        #region Initialization and disposal

        public void AssignDefaultAssets() {
            UICanvasPrefab = Resources.Load<GameObject>("VoxelPlay/UI/Voxel Play UI Canvas");
            inputControllerPCPrefab = Resources.Load<GameObject>("VoxelPlay/InputControllers/PC/Voxel Play PC Input Controller");
            inputControllerMobilePrefab = Resources.Load<GameObject>("VoxelPlay/InputControllers/Mobile/Voxel Play Mobile Input Controller");
        }

        /// <summary>
        /// If true, Voxel Play won't release the textures arrays so they can be used in exported chunks
        /// </summary>
        [NonSerialized]
        public bool keepTexturesOnDestroy;

        /// <summary>
        /// Initializes the engine and loads any save game specified in the saveGameFile property.
        /// </summary>
        public void InitAndLoadSaveGame() {
            Init(MayLoadSaveGame);
        }

        void MayLoadSaveGame() {
            if (applicationIsPlaying || (!applicationIsPlaying && renderInEditor)) {
                if (loadSavedGame) {
                    LoadGameBinary(true);
                }
            }
        }


        /// <summary>
        /// Initilizes the engine. Optionally call the callback function when initalization ends.
        /// </summary>
        /// <param name="callback">Callback function to be called when initialization ends.</param>
        public void Init(AfterInitCallback callback = null) {
            StartCoroutine(InitBackground(callback));
        }

        IEnumerator InitBackground(AfterInitCallback callback = null) {
            LogMessage("Init started...", true);

            initialized = false;
            sceneCam = null;
            tempColliders = new Collider[1];
            InitMainThreading();

#if UNITY_ANDROID || UNITY_IOS
			isMobilePlatform = true;
#elif UNITY_WEBGL
			isMobilePlatform = false;
#if UNITY_EDITOR
			if (PlayerSettings.WebGL.memorySize<2000) PlayerSettings.WebGL.memorySize = 2000;
#endif
#else
            isMobilePlatform = Application.isMobilePlatform;
#endif

#if UNITY_WEBGL
            effectiveMultithreadGeneration = false;
#else
            effectiveMultithreadGeneration = multiThreadGeneration;
#endif

#if !UNITY_EDITOR
            if (isMobilePlatform) {
                Application.lowMemory += Application_lowMemory;
            }
#endif

            // Init camera and Sun references
            WaitForSeconds w = new WaitForSeconds(0.5f);
            while (cameraMain == null) {
                if (cameraMain == null || !cameraMain.isActiveAndEnabled) {
                    cameraMain = Camera.main;
                    if (cameraMain == null || !cameraMain.isActiveAndEnabled) {
                        cameraMain = Misc.FindObjectOfType<Camera>();
                    }
                    if (cameraMain == null) {
                        LogMessage("Waiting for camera...");
                        if (Application.isPlaying) {
                            yield return w;
                        } else {
                            break;
                        }
                    }
                }
            }

            // Default distance anchor
            if ((distanceAnchor == null || !distanceAnchor.gameObject.activeSelf) && cameraMain != null) {
                distanceAnchor = cameraMain.transform;
            }

            // Cache player collider
            if ((UnityEngine.Object)characterController == null) {
                characterController = Misc.FindObjectOfType<VoxelPlayCharacterControllerBase>();
            }
            if ((UnityEngine.Object)characterController != null) {
                characterControllerCollider = characterController.GetComponentInChildren<CharacterController>();
                if (characterControllerCollider == null) {
                    characterControllerCollider = characterController.GetComponentInChildren<Collider>();
                }
            }

#if UNITY_EDITOR
            if (cameraMain != null && cameraMain.actualRenderingPath != RenderingPath.Forward) {
                Debug.LogWarning("Voxel Play works better with Forward Rendering path.");
            }
#if !UNITY_2019_1_OR_NEWER
            if (!isMobilePlatform && QualitySettings.antiAliasing < 2) {
                Debug.LogWarning ("Voxel Play looks better with MSAA enabled (x2 minimum to enable crosshair).");
            }
#endif
#endif

            if (isMobilePlatform && applicationIsPlaying && adjustCameraFarClip) {
                cameraMain.farClipPlane = Mathf.Min(400, _visibleChunksDistance * CHUNK_SIZE);
            }
            VoxelPlayUI oldUISystem = GetComponent<VoxelPlayUI>();
            if (oldUISystem != null) {
                DestroyImmediate(oldUISystem);
                oldUISystem = null;
            }

            // Default values
            if (crosshairPrefab == null) {
                crosshairPrefab = Resources.Load<GameObject>("VoxelPlay/UI/crosshair");
            }
            if (crosshairTexture == null) {
                crosshairTexture = Resources.Load<Texture2D>("VoxelPlay/UI/crosshairTexture");
            }

            if (applicationIsPlaying) {
                // UI
                VoxelPlayUI.Init();
                // Init Input

                GameObject inputPrefab = null;
#if UNITY_EDITOR
                if (isMobilePlatform && previewTouchUIinEditor) {
                    inputPrefab = inputControllerMobilePrefab;
                } else {
                    inputPrefab = inputControllerPCPrefab;
                }
#else
                if (isMobilePlatform) {
                    inputPrefab = inputControllerMobilePrefab;
                } else {
                    inputPrefab = inputControllerPCPrefab;
                }
#endif

                if (inputPrefab != null) {
                    GameObject inputGO = Instantiate(inputPrefab);
                    inputGO.name = inputPrefab.name;
                    inputGO.SetActive(false);
                    input = inputGO.GetComponent<VoxelPlayInputController>();
                }
            }

            stopWatch = new System.Diagnostics.Stopwatch();

#if UNITY_EDITOR
            lastInspectorUpdateTime = 0;
            lastCameraMoveTime = 0;
#endif

            if (!enableBuildMode)
                buildMode = false;

            if (applicationIsPlaying || (!applicationIsPlaying && renderInEditor)) {
                stopWatch.Start();
                if (cachedChunks == null || chunksPool == null) {
                    LoadWorldInt();
                    if (applicationIsPlaying) {
                        StartCoroutine(WarmChunks(callback));
                    } else {
                        WarmChunksEditor(callback);
                    }
                }
            } else {
                UpdateAmbientProperties();
            }

        }

        private void Application_lowMemory() {
            // Low memory warning on mobile
            Resources.UnloadUnusedAssets();
        }

        IEnumerator WarmChunks(AfterInitCallback callback) {
            WaitForEndOfFrame w = new WaitForEndOfFrame();
            yield return w;

            int required = maxChunks;
#if UNITY_EDITOR
            required = prewarmChunksInEditor;
#endif

            LogMessage("Warming " + maxChunks + " chunks...", true);
            if (world != null) {
                while (chunksPoolLoadIndex < maxChunks) {
                    try {
                        for (int k = 0; k < 100; k++) {
                            ReserveChunkMemory();
                        }
                    } catch (Exception ex) {
                        ShowExceptionMessage(ex);
                        break;
                    }
                    if (enableLoadingPanel) {
                        VoxelPlayUI ui = VoxelPlayUI.instance;
                        if (ui != null) {
                            float progress = (float)(chunksPoolLoadIndex + 1) / required;
                            ui.ToggleInitializationPanel(true, loadingText, progress);
                        }
                    }
                    yield return w;
                    if (chunksPoolLoadIndex > required)
                        break;
                }
            }
            LogMessage("Chunks pool initialized.");

            InitEnd(callback);
        }

        void WarmChunksEditor(AfterInitCallback callback) {
            int required = 1000;
            if (world != null) {
                int numberOfChunks = Mathf.Min(maxChunks, required);
                LogMessage("Warming " + numberOfChunks + " chunks...", true);
                while (chunksPoolLoadIndex < numberOfChunks) {
                    for (int k = 0; k < 100; k++) {
                        ReserveChunkMemory();
                    }
                }
                LogMessage("Chunks pool initialized.");
            }
            InitEnd(callback);
        }



        void InitEnd(AfterInitCallback callback) {
            if (applicationIsPlaying) {
                GC.Collect();
            }

            SetInitialized();
            LogMessage("All systems ready.", true);

            if (input != null) {
                input.gameObject.SetActive(true);
                input.Init();
            }

            ComputeFirstReusableChunk();

            if (applicationIsPlaying) {
                if (OnInitialized != null) {
                    OnInitialized();
                }
            }

#if UNITY_EDITOR
            EditorApplication.update -= UpdateInEditor;
            if (renderInEditor && !applicationIsPlaying) {
                EditorApplication.update += UpdateInEditor;
            }
#endif

            if (callback != null) {
                callback();
            }

            if (initialWaitTime > 0 && applicationIsPlaying) {
                if (input != null) {
                    input.enabled = false;
                }
                StartCoroutine(DoWaitTime());
            } else {
                EndWaitTime();
            }
        }

        IEnumerator DoWaitTime() {
            LogMessage("Initial wait time of " + initialWaitTime + " seconds...");
            WaitForSeconds w = new WaitForSeconds(0.2f);
            float start = Time.time;
            float progress = 0;
            while (progress < 1f || !canHideInitialLoadingScreen) {
                progress = (Time.time - start) / initialWaitTime;
                if (progress > 1f) {
                    progress = 1f;
                }
                VoxelPlayUI ui = VoxelPlayUI.instance;
                if (ui != null) {
                    ui.ToggleInitializationPanel(true, initialWaitText, progress);
                }
                yield return w;
            }
            EndWaitTime();
        }

        void EndWaitTime() {
            if (input != null) {
                input.enabled = true;
            }
            VoxelPlayUI ui = VoxelPlayUI.instance;
            if (ui != null) {
                ui.ToggleInitializationPanel(false);
            }
            if (!string.IsNullOrEmpty(welcomeMessage)) {
                ShowMessage(welcomeMessage, welcomeMessageDuration, true);
            }
        }

        /// <summary>
        /// Destroyes everything and initializes world
        /// </summary>
        void LoadWorldInt() {
            LogMessage("Releasing previous world...", true);
            DisposeAll();

            if (world == null) {
                if (Application.isPlaying) {
                    world = ScriptableObject.CreateInstance<WorldDefinition>();
                    Debug.LogWarning("World Definition asset missing in Voxel Play Environment. Assigning a temporary asset.");
                } else {
                    return;
                }
            }

            // Create world root
            if (worldRoot == null) {
                GameObject wr = GameObject.Find(VOXELPLAY_WORLD_ROOT);
                if (wr == null) {
                    wr = new GameObject(VOXELPLAY_WORLD_ROOT);
                    wr.transform.position = Misc.vector3zero;
                }
                worldRoot = wr.transform;
            }

            LogMessage("Initializing world systems...", true);

            WorldRand.Randomize(world.seed);
            Physics.gravity = new Vector3(0, world.gravity, 0);
            InitOriginShift();
            InitSaveGameStructs();
            InitNotificationManager();
            InitWater();
            InitSky();
            InitRenderer();
            InitTrees();
            InitVegetation();
            LoadWorldTextures();
            InitConnectedVoxels();
            InitItems();
            InitNavMesh();
            InitChunkManager();
            NotifyCameraMove(); // forces check chunks in frustum
            InitClouds();
            InitPhysics();
            InitParticles();
            UpdateMaterialProperties();
            SetBuildMode(buildMode);

            LogMessage("World systems initialized.");

            if (OnWorldLoaded != null) {
                StartCoroutine(WaitUntilWorldIsLoaded());
            }
        }


        void SetInitialized() {
            initialized = true;
            InitTime();
        }

        #endregion

        #region Master rendering

        void DoWork() {
            if (!enableGeneration) return;
            try {
                STAGE = 1;
                CheckCamera();

#if UNITY_EDITOR
                if (WantRepaintInspector != null) { // update inspector stats
                    long elapsed = stopWatch.ElapsedMilliseconds - lastInspectorUpdateTime;
                    if (elapsed > 1000) {
                        WantRepaintInspector();
                        lastInspectorUpdateTime = stopWatch.ElapsedMilliseconds;
                    }
                }
                if (!applicationIsPlaying) {
                    if (renderInEditorLowPriority) {
                        if (cameraHasMoved) {
                            lastCameraMoveTime = stopWatch.ElapsedMilliseconds;
                        }
                        long elapsed = stopWatch.ElapsedMilliseconds - lastCameraMoveTime;
                        if (elapsed < 1000) {
                            return;
                        }
                        // After 1 second of inactivity, rendering resumes but we need to inform that the camera probably has moved so the frustum planes get recalculated
                        NotifyCameraMove();
                        shouldCheckChunksInFrustum = true;
                    }
                }
#endif
                if (cameraHasMoved && seeThrough) {
                    ManageSeeThrough();
                }

                // main world creation & rendering cycle
                long currentTime = stopWatch.ElapsedMilliseconds;
#if UNITY_EDITOR
                long availableTime = (!applicationIsPlaying && renderInEditorLowPriority) ? (long)(maxCPUTimePerFrame * 0.7f) : maxCPUTimePerFrame;
#else
                long availableTime = maxCPUTimePerFrame;
#endif
                long creationMaxTime = currentTime + (long)(availableTime * 0.8f); // Max 80% of frame time to populate stuff
                long maxFrameTime = currentTime + availableTime;

                if (applicationIsPlaying) {
                    CheckChunksVisibleDistance(maxFrameTime);
                    STAGE = 10;
                    UpdateNavMesh();
                    DoLightEffects();
                    CheckSystemKeys();
                }
                CheckSunRotation();

                // Content generators - only process new chunks if there's no GPU upload or collider creation jobs
                if (meshingIdle > 0) {
                    bool captureChunkChangeEventsState = captureChunkChanges;
                    captureChunkChanges = false;

                    STAGE = 3;
                    CheckChunksInRange(creationMaxTime);
                    STAGE = 4;
                    CheckTreeRequests(creationMaxTime);
                    STAGE = 5;
                    CheckVegetationRequests(creationMaxTime);
                    STAGE = 21;
                    DoDetailWork(creationMaxTime);

                    captureChunkChanges = captureChunkChangeEventsState;
                }

                STAGE = 23;
                UpdateWaterFlood();

                // Check which chunks need to be refreshed (either lightmap or content)
                STAGE = 9;
                CheckRenderChunkQueue(maxFrameTime);

                ProcessLightmapUpdates();

                if (!effectiveMultithreadGeneration) {
                    STAGE = 22;
                    GenerateChunkMeshDataInMainThread(maxFrameTime);
                }

                if (requireTextureArrayUpdate) {
                    STAGE = 11;
                    LoadWorldTextures();
                    UpdateRenderingMaterialsProperties();
                } else {
                    STAGE = 31;
                    // Signal which chunk meshes need to be rebuilt, send them to generation thread and upload any ready mesh
                    UpdateMeshAndNotifyChunkChanges(maxFrameTime);
                }

                // After chunk mesh upload has completed, process any pending light update for particles
                UpdateParticles();

                STAGE = 41;
                // Render instanced objects
                instancedRenderer.Render(currentCamPos, _visibleChunksDistance, frustumPlanesNormals, frustumPlanesDistances);

                // Send queued event notifications
                NotificationManagerSend();

                // end cycle
            } catch (Exception ex) {
                ShowExceptionMessage(ex);
            }
            STAGE = 0;
        }

        IEnumerator WaitUntilWorldIsLoaded() {
            while (meshingIdle < 5) {
                yield return null;
            }
            if (OnWorldLoaded != null) {
                OnWorldLoaded();
            }
        }

        void ShowExceptionMessage(Exception ex) {
            string msg = "<color=yellow>Critical (STAGE=" + STAGE + ")</color>: " + ex.Message + "\n" + ex.StackTrace;
            ShowError(msg);
            Debug.LogError(msg);
        }

        /// <summary>
        /// Returns current camera used by the renderer. In Editor mode (not playmode), it's the SceneView camera
        /// </summary>
        /// <value>The current camera.</value>
        public Camera currentCamera {
            get {
                if (applicationIsPlaying) {
                    return cameraMain;
                }

#if UNITY_EDITOR
                // In Editor camera (SceneCam)
                if (sceneCam == null) {
                    Camera[] cam = SceneView.GetAllSceneCameras();
                    if (cam != null) {
                        for (int k = 0; k < cam.Length; k++) {
                            if (cam[k] == Camera.current) {
                                sceneCam = Camera.current;
                                break;
                            }
                        }
                    }
                }

#endif

                if (Camera.current == null) {
                    if (sceneCam == null) {
                        return cameraMain;
                    }
                    return sceneCam;
                }
                return Camera.current;
            }
        }

        void CheckCamera() {

            Camera cam = currentCamera;

            if (cam == null)
                return;

            if (distanceAnchor == null) {
                distanceAnchor = cam.transform;
            } else {
                currentAnchorPosWS = distanceAnchor.transform.position;
            }

#if UNITY_EDITOR
            if (cam.cameraType == CameraType.SceneView && !applicationIsPlaying) {
                currentAnchorPosWS = cam.transform.position;
            }
#endif

            currentAnchorPos = currentAnchorPosWS;
            Vector3 prevCamPos = cam.transform.position;
            cam.transform.position = new Vector3((float)(prevCamPos.x - worldPivot.x), prevCamPos.y, (float)(prevCamPos.z - worldPivot.z)); // cam position needs to be changed to compute frustum corners below

            _cameraHasMoved = false;
            currentCamPos = cam.transform.position;
            currentCamRot = cam.transform.rotation;
            if (_notifyCameraMove || lastCamPos != currentCamPos || lastCamRot != currentCamRot || lastCamOrthoSize != cam.orthographicSize || lastAnchorPos != currentAnchorPos) {
                _notifyCameraMove = false;
                _cameraHasMoved = true;
            }
            if (_cameraHasMoved) {
                lastAnchorPos = currentAnchorPos;
                lastCamOrthoSize = cam.orthographicSize;
                lastCamPos = currentCamPos;
                lastCamRot = currentCamRot;
                GeometryUtility.CalculateFrustumPlanes(cam.projectionMatrix * cam.worldToCameraMatrix, frustumPlanes);

                for (int k = 0; k < 6; k++) {
                    frustumPlanesDistances[k] = frustumPlanes[k].distance;
                    frustumPlanesNormals[k] = frustumPlanes[k].normal;
                }

                cam.CalculateFrustumCorners(Misc.rectFullViewport, cam.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
                for (int i = 0; i < 4; i++) {
                    frustumCorners[i] = currentCamPos + cam.transform.TransformVector(frustumCorners[i]);
                }
                shouldCheckChunksInFrustum = true;
            }

            cam.transform.position = prevCamPos;
        }

        void DoLightEffects() {

            Shader.SetGlobalFloat(ShaderParams.VPEmissionIntensity, world.emissionMinIntensity + Mathf.PingPong(Time.time * world.emissionAnimationSpeed, (world.emissionMaxIntensity - world.emissionMinIntensity)));
        }

        void CheckSunRotation() {
            if (sun == null)
                return;
            Transform t = sun.transform;
            if (constructorMode) {
                t.rotation = Quaternion.Euler(30, 30, 0);
            } else if (world.dayCycleSpeed != 0) {
                UpdateSunRotation();
            }
        }

        public void UpdateInEditor() {
            if (!applicationIsPlaying) {
                if (!initialized) {
                    InitAndLoadSaveGame();
                } else {
                    DoWork();
                }
            }
        }

        #endregion

        #region Special keys handling

        void CheckSystemKeys() {
            if (enableConsole) {
                if (Input.GetKey(KeyCode.LeftControl)) {
                    if (Input.GetKeyDown(KeyCode.F3)) {
                        if (LoadGameBinary(false)) {
                            ShowMessage("<color=green>Game loaded successfully.</color>");
                        }
                    } else if (Input.GetKeyDown(KeyCode.F4)) {
                        SaveGameBinary();
                        ShowMessage("<color=green>Game saved. Press <color=yellow>Control + F3</color> to load.</color>");
                    }
                }
            }
        }

        #endregion

        #region Editor helpers

#if UNITY_EDITOR
        void CheckEditorTintColor() {
            if (!enableTinting) {
                Debug.Log("Option enableTinting is disabled. To use colored voxels, please enable the option in the VoxelPlayEnvironment component inspector.");
            }
        }

        void CheckSpecialFeaturesScriptingSupport() {
            if (Application.isPlaying || !gameObject.activeInHierarchy)
                return;

            // Do not execute if gameobject is prefab
            if (PrefabUtility.GetPrefabInstanceStatus(gameObject) == PrefabInstanceStatus.Connected)
                return;

            if ((Voxel.supportsTinting != enableTinting) || (supportsSeeThrough != seeThrough) || supportsBrightPointLights != enableBrightPointLights || supportsFresnel != enableFresnel || supportsURPNativeLights != enableURPNativeLights || supportsGlobalSpecular != enableGlobalSpecular) {
                UpdateSpecialFeaturesCodeMacro();
            }
        }

        public void SetShaderOptionValue(string option, string file, bool state) {
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
                    if (state) {
                        code[k] = "#define " + option;
                    } else {
                        code[k] = "//#define " + option;
                    }
                    File.WriteAllLines(path, code, System.Text.Encoding.UTF8);
                    break;
                }
            }
        }

        public void SetShaderOptionValue(string option, string file, string value) {
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


        public void UpdateSpecialFeaturesCodeMacro() {
            if (enableURP) {
                SetCommonURPFile("VPCommonURP_Pipeline.cginc");
            } else {
                SetCommonURPFile("VPCommonURP_Fallback.cginc");
            }
            SetShaderOptionValue("USES_URP", "VoxelPlayEnvironmentEditor.cs", enableURP);
            SetShaderOptionValue("USES_TINTING", "Voxel.cs", enableTinting);
            SetShaderOptionValue("USES_TINTING", "MeshingThreadTriangle.cs", enableTinting);
            SetShaderOptionValue("USES_TINTING", "VoxelPlayGreedySliceLitAO.cs", enableTinting);
            SetShaderOptionValue("USES_TINTING", "VoxelPlayGreedySliceLit.cs", enableTinting);
            SetShaderOptionValue("USES_TINTING", "VPCommonOptions.cginc", enableTinting);
            SetShaderOptionValue("USES_FRESNEL", "VPCommonOptions.cginc", enableFresnel && usePixelLights);
            SetShaderOptionValue("USES_FRESNEL", "VoxelPlayEnvironment.Renderer.cs", enableFresnel && usePixelLights);
            SetShaderOptionValue("USES_GLOBAL_SPECULAR", "VPCommonOptions.cginc", enableGlobalSpecular && usePixelLights);
            SetShaderOptionValue("USES_GLOBAL_SPECULAR", "VoxelPlayEnvironment.Renderer.cs", enableGlobalSpecular && usePixelLights);
            SetShaderOptionValue("USES_SEE_THROUGH", "VoxelPlayEnvironment.Renderer.cs", seeThrough);
            SetShaderOptionValue("USES_SEE_THROUGH", "VPCommonOptions.cginc", seeThrough);
            SetShaderOptionValue("USES_BRIGHT_POINT_LIGHTS", "VoxelPlayEnvironment.Renderer.cs", enableBrightPointLights);
            SetShaderOptionValue("USES_BRIGHT_POINT_LIGHTS", "VPCommonOptions.cginc", enableBrightPointLights);
            SetShaderOptionValue("USES_URP_NATIVE_LIGHTS", "VoxelPlayEnvironment.Renderer.cs", enableURPNativeLights);
            SetShaderOptionValue("USES_URP_NATIVE_LIGHTS", "VPCommonOptions.cginc", enableURPNativeLights);
            SetShaderOptionValue("USES_BEVEL", "VPCommonOptions.cginc", enableBevel && usePixelLights);
            SetShaderOptionValue("USES_BEVEL", "MeshingThreadTriangle.cs", enableBevel && usePixelLights);
            SetShaderOptionValue("USES_COLORED_SHADOWS", "VPCommonOptions.cginc", enableColoredShadows);
            switch (obscuranceMode) {
                case ObscuranceMode.Faster: SetShaderOptionValue("AO_FUNCTION", "VPCommonOptions.cginc", "ao = 1.05-(1.0-ao)*(1.0-ao)"); break;
                case ObscuranceMode.Custom: SetShaderOptionValue("AO_FUNCTION", "VPCommonOptions.cginc", "ao = pow(ao, _VPObscuranceIntensity - ao)"); break;
            }
            switch (filterMode) {
                case FilterMode.Trilinear: SetShaderOptionValue("FILTER_MODE", "VPCommonOptions.cginc", "3"); break;
                case FilterMode.Bilinear: SetShaderOptionValue("FILTER_MODE", "VPCommonOptions.cginc", "2"); break;
                default: SetShaderOptionValue("FILTER_MODE", "VPCommonOptions.cginc", "1"); break;
            }
            AssetDatabase.Refresh();
        }

        void SetCommonURPFile(string filename) {
            string[] res = Directory.GetFiles(Application.dataPath, filename, SearchOption.AllDirectories);
            for (int k = 0; k < res.Length; k++) {
                if (res[k].Contains(filename)) {
                    string dir = Path.GetDirectoryName(res[k]);
                    string source = dir + "/" + filename;
                    string dest = dir + "/VPCommonURP.cginc";
                    if (File.Exists(source) && File.Exists(dest)) {
                        if (new FileInfo(source).Length != new FileInfo(dest).Length) {
                            File.Copy(source, dest, true);
                        }
                    }
                    break;
                }
            }
        }

        public void SetConstValue(string option, string file, string value) {
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
            string searchToken = option + " = ";
            for (int k = 0; k < code.Length; k++) {
                if (code[k].Contains(searchToken)) {
                    code[k] = searchToken + value + ";";
                    File.WriteAllLines(path, code, System.Text.Encoding.UTF8);
                    break;
                }
            }
        }

        public void UpdateChunkSizeInCode(int newSize) {
            SetConstValue("public const int CHUNK_SIZE", "VoxelPlayEnvironment.cs", newSize.ToString());
        }

        public void UpdateMaxMaterialsPerChunk(int newMax) {
            SetConstValue("public const int MAX_MATERIALS_PER_CHUNK", "VoxelPlayEnvironment.Renderer.cs", newMax.ToString());
        }

        public void UpdateVoxelPadding(bool usePadding) {
            SetConstValue("public const float PADDING", "VoxelPlayGreedyCommon.cs", usePadding ? "0.001f" : "0");
        }


#endif

        #endregion

    }




}
