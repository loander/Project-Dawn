using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace VoxelPlay {

    public partial class VoxelPlayEnvironment : MonoBehaviour {

        const string CHUNKS_ROOT = "Chunks Root";
        const string CHUNKS_EXPORT_ROOT = "Exported Chunks";

        // Optimization support
        VoxelChunk lastChunkFetch;
        int lastChunkFetchX, lastChunkFetchY, lastChunkFetchZ;
        readonly object lockLastChunkFetch = new object();

        #region Chunk functions

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        int GetChunkHash(int chunkX, int chunkY, int chunkZ) {
            int x00 = WORLD_SIZE_DEPTH * WORLD_SIZE_HEIGHT * (chunkX + WORLD_SIZE_WIDTH);
            int y00 = WORLD_SIZE_DEPTH * (chunkY + WORLD_SIZE_HEIGHT);
            return x00 + y00 + chunkZ;
        }

        /// <summary>
        /// Gets the chunk if exits or create it if forceCreation is set to true.
        /// </summary>
        /// <returns><c>true</c>, if chunk fast was gotten, <c>false</c> otherwise.</returns>
        /// <param name="chunkX">Chunk x.</param>
        /// <param name="chunkY">Chunk y.</param>
        /// <param name="chunkZ">Chunk z.</param>
        /// <param name="createIfNotAvailable">If set to <c>true</c> force creation if chunk doesn't exist.</param>
		bool GetChunkFast(int chunkX, int chunkY, int chunkZ, out VoxelChunk chunk, bool createIfNotAvailable = false) {
            lock (lockLastChunkFetch) {
                if (lastChunkFetchX == chunkX && lastChunkFetchY == chunkY && lastChunkFetchZ == chunkZ && (object)lastChunkFetch != null) {
                    chunk = lastChunkFetch;
                    return true;
                }
            }
            int hash = GetChunkHash(chunkX, chunkY, chunkZ);
            STAGE = 501;
            bool exists = cachedChunks.TryGetValue(hash, out CachedChunk cachedChunk);
            chunk = exists ? cachedChunk.chunk : null;

            if (createIfNotAvailable) {
                if (!exists) {
                    STAGE = 502;
                    // not yet created, create it
                    chunk = CreateChunk(hash, chunkX, chunkY, chunkZ, createEmptyChunk: false);
                    exists = true;
                }
                if ((object)chunk == null) { // chunk is really empty, create it with empty space
                    STAGE = 503;
                    chunk = CreateChunk(hash, chunkX, chunkY, chunkZ, createEmptyChunk: true);
                }
            }
            STAGE = 0;
            if (exists) {
                lock (lockLastChunkFetch) {
                    lastChunkFetchX = chunkX;
                    lastChunkFetchY = chunkY;
                    lastChunkFetchZ = chunkZ;
                    lastChunkFetch = chunk;
                }
                return (object)chunk != null;
            }
            chunk = null;
            return false;
        }


        bool GetChunkOrCreate(Vector3d position, out VoxelChunk chunk) {
            FastMath.FloorToInt(position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out int x, out int y, out int z);
            return GetChunkFast(x, y, z, out chunk, createIfNotAvailable: true);
        }


        bool GetChunkOrCreate(int chunkX, int chunkY, int chunkZ, out VoxelChunk chunk) {
            return GetChunkFast(chunkX, chunkY, chunkZ, out chunk, createIfNotAvailable: true);
        }

        VoxelChunk GetChunkIfExists(int hash) {
            if (cachedChunks.TryGetValue(hash, out CachedChunk cachedChunk)) {
                return cachedChunk.chunk;
            }
            return null;
        }


        /// <summary>
        /// Creates the chunk.
        /// </summary>
        /// <returns>The chunk.</returns>
        /// <param name="hash">Hash.</param>
        /// <param name="chunkX">Chunk x.</param>
        /// <param name="chunkY">Chunk y.</param>
        /// <param name="chunkZ">Chunk z.</param>
        /// <param name="createEmptyChunk">If set to <c>true</c> create empty chunk.</param>
        /// <param name="complete">If set to <c>true</c> detail generators will fire as well as OnChunkCreated event. Chunk will be marked as populated and a refresh will be triggered if within view distance.</param>
        VoxelChunk CreateChunk(int hash, int chunkX, int chunkY, int chunkZ, bool createEmptyChunk, bool complete = true) {

            STAGE = 101;
            Vector3d position;
            position.x = chunkX * CHUNK_SIZE + CHUNK_HALF_SIZE;
            position.y = chunkY * CHUNK_SIZE + CHUNK_HALF_SIZE;
            position.z = chunkZ * CHUNK_SIZE + CHUNK_HALF_SIZE;

            STAGE = 102;
            // Create entry in the dictionary
            if (!cachedChunks.TryGetValue(hash, out CachedChunk cachedChunk)) {
                cachedChunk = new CachedChunk();
                cachedChunks[hash] = cachedChunk;
            }

            STAGE = 103;
            VoxelChunk chunk;
            if ((object)cachedChunk.chunk == null) {
                // Fetch a new entry in the chunks pool
                if (chunksPoolFetchNew) {
                    chunksPoolFetchNew = false;
                    FetchNewChunkIndex(position);
                }
                chunk = chunksPool[chunksPoolCurrentIndex];
            } else {
                chunk = cachedChunk.chunk;
            }

            // Paint voxels
            bool chunkHasContents = false;
            chunk.position = position;

            STAGE = 104;
            if (createEmptyChunk) {
                if (OnChunkBeforeCreate != null) {
                    // allows a external function to fill the contents of this new chunk
                    OnChunkBeforeCreate(position, out chunkHasContents, chunk, out bool isAboveSurface);
                    chunk.isAboveSurface = isAboveSurface;
                } else {
                    chunk.isAboveSurface = CheckIfChunkAboveTerrain(position);
                }
            } else {
                if (world.infinite || (position.x >= -world.extents.x && position.x <= world.extents.x && position.y >= -world.extents.y && position.y <= world.extents.y && position.z >= -world.extents.z && position.z <= world.extents.z)) {
                    if (OnChunkBeforeCreate != null) {
                        // allows a external function to fill the contents of this new chunk
                        OnChunkBeforeCreate(position, out chunkHasContents, chunk, out bool isAboveSurface);
                        chunk.isAboveSurface = isAboveSurface;
                    }
                    if (!chunkHasContents) {
                        if (!chunk.isCloud) {
                            chunkHasContents = world.terrainGenerator.PaintChunk(chunk);
                        }
                        chunk.isAboveSurface |= !chunkHasContents;
                    }
                }
            }

            STAGE = 105;

            if (chunkHasContents || createEmptyChunk) {

                chunk.ComputeNeighbours();

                if (effectiveGlobalIllumination) {
                    // Ensure that the chunk lightmap is clear; the chunk lightmap might be computed early if the chunk was obtained using GetChunkUnpopulated method
                    // We ensure it remains cleared and is rebuilt when terrain paints this chunk
                    if (chunk.isDirty) {
                        chunk.isDirty = false;
                        chunk.ClearLightmap(FULL_DARK);
                    }
                    // rebuild lightmap for chunks got with GetChunkUnpopulated (ie. from a saved game file)
                    chunk.needsLightmapRebuild = true;
                } else {
                    // lit chunk if not global illumination
                    chunk.ClearLightmap(FULL_LIGHT);
                }

                chunksPoolFetchNew = true;
                chunksCreated++;

                cachedChunk.chunk = chunk;

                if (complete) {
                    chunk.isPopulated = true;
                    // rebuild lightmap as this chunk is fully populated and has been modified by a detail generator
                    chunk.needsLightmapRebuild = true;

                    // Check for detail generators
                    bool useDetailGenerators = worldHasDetailGenerators && enableDetailGenerators;
#if UNITY_EDITOR
                    if (renderInEditorDetail == EditorRenderDetail.StandardNoDetailGenerators && !applicationIsPlaying) {
                        useDetailGenerators = false;
                    }
#endif
                    if (useDetailGenerators) {
                        bool prevCaptureEvents = captureEvents;
                        captureEvents = false;
                        // detail generators shouldn't trigger events for performance reasons. Also a detail generator works same on all clients in multiplayer environment
                        // so no need to propagate these changes as every client will execute the same logic.
                        int detailGeneratorsCount = world.detailGenerators.Length;
                        for (int d = 0; d < detailGeneratorsCount; d++) {
                            VoxelPlayDetailGenerator gen = world.detailGenerators[d];
                            if (gen.enabled) {
                                if (gen.allowNestedExecutions || !gen.busy) {
                                    if (OnChunkBeforeDetailGeneration != null) {
                                        OnChunkBeforeDetailGeneration(chunk, gen, out bool cancelDetailGeneration);
                                        if (cancelDetailGeneration) continue;
                                    }
                                    gen.busy = true;
                                    gen.AddDetail(chunk);
                                }
                            }
                            gen.busy = false;
                        }
                        captureEvents = prevCaptureEvents;
                    }

                    if (chunkHasContents) {
                        // if chunk is near camera, request a render refresh
                        bool sendRefresh = (chunkX >= visible_xmin && chunkX <= visible_xmax && chunkZ >= visible_zmin && chunkZ <= visible_zmax && chunkY >= visible_ymin && chunkY <= visible_ymax);
                        if (sendRefresh) {
                            ChunkRequestRefresh(chunk, clearLightmap: false, refreshMesh: true);
                        }
                    } else {
                        chunk.renderState = ChunkRenderState.RenderingComplete;
                    }

                    if (OnChunkAfterCreate != null) {
                        OnChunkAfterCreate(chunk);
                    }
                }

                STAGE = 0;
                return chunk;
            }
            chunk.renderState = ChunkRenderState.RenderingComplete;
            STAGE = 0;
            return null;
        }

        bool CheckIfChunkAboveTerrain(Vector3d position) {

            position.y += (CHUNK_HALF_SIZE - 1);
            if (position.y < waterLevel) {
                return false;
            }

            position.x -= CHUNK_HALF_SIZE;
            position.z -= CHUNK_HALF_SIZE;
            Vector3d pos = position;

            for (int z = 0; z < CHUNK_SIZE; z++) {
                pos.z = position.z + z;
                for (int x = 0; x < CHUNK_SIZE; x++) {
                    pos.x = position.x + x;
                    float groundLevel = GetHeightMapInfoFast(pos.x, pos.z).groundLevel;
                    float surfaceLevel = waterLevel > groundLevel ? waterLevel : groundLevel;
                    if (position.y >= surfaceLevel) {
                        // chunk is above terrain or water
                        return true;
                    }
                }
            }

            return false;
        }


        void RefreshNeighbourhood(VoxelChunk chunk, bool forceMeshRefresh = false, bool clearLightMap = true, bool excludeCenterChunk = false, bool ignoreFrustum = false) {
            if ((object)chunk == null)
                return;

            FastMath.FloorToInt(chunk.position.x / CHUNK_SIZE, chunk.position.y / CHUNK_SIZE, chunk.position.z / CHUNK_SIZE, out int chunkX, out int chunkY, out int chunkZ);

            for (int y = -1; y <= 1; y++) {
                for (int z = -1; z <= 1; z++) {
                    for (int x = -1; x <= 1; x++) {
                        if (excludeCenterChunk && y == 0 && z == 0 && x == 0) continue;
                        GetChunkFast(chunkX + x, chunkY + y, chunkZ + z, out VoxelChunk neighbour);
                        if ((object)neighbour != null) {
                            ChunkRequestRefresh(neighbour, clearLightMap, forceMeshRefresh, ignoreFrustum);
                        }
                    }
                }
            }
        }

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        void RebuildNeighboursIfNeeded(VoxelChunk chunk, int voxelIndex) {
            int bx = voxelIndex & VOXELINDEX_X_EDGE_BITWISE;
            int bz = voxelIndex & VOXELINDEX_Z_EDGE_BITWISE;
            int by = voxelIndex & VOXELINDEX_Y_EDGE_BITWISE;

            if (bx == 0)
                ChunkRequestRefresh(chunk.left, clearLightmap: false, refreshMesh: true);
            else if (bx == VOXELINDEX_X_EDGE_BITWISE)
                ChunkRequestRefresh(chunk.right, clearLightmap: false, refreshMesh: true);

            if (by == 0)
                ChunkRequestRefresh(chunk.bottom, clearLightmap: false, refreshMesh: true);
            else if (by == VOXELINDEX_Y_EDGE_BITWISE)
                ChunkRequestRefresh(chunk.top, clearLightmap: false, refreshMesh: true);

            if (bz == 0)
                ChunkRequestRefresh(chunk.back, clearLightmap: false, refreshMesh: true);
            else if (bz == VOXELINDEX_Z_EDGE_BITWISE)
                ChunkRequestRefresh(chunk.forward, clearLightmap: false, refreshMesh: true);
        }


        void RebuildNeighbours(VoxelChunk chunk) {

            if ((object)chunk.left != null) {
                ChunkRequestRefresh(chunk.left, clearLightmap: false, refreshMesh: true);
            }
            if ((object)chunk.right != null) {
                ChunkRequestRefresh(chunk.right, clearLightmap: false, refreshMesh: true);
            }
            if ((object)chunk.top != null) {
                ChunkRequestRefresh(chunk.top, clearLightmap: false, refreshMesh: true);
            }
            if ((object)chunk.bottom != null) {
                ChunkRequestRefresh(chunk.bottom, clearLightmap: false, refreshMesh: true);
            }
            if ((object)chunk.forward != null) {
                ChunkRequestRefresh(chunk.forward, clearLightmap: false, refreshMesh: true);
            }
            if ((object)chunk.back != null) {
                ChunkRequestRefresh(chunk.back, clearLightmap: false, refreshMesh: true);
            }
        }


        /// <summary>
        /// Clears a chunk
        /// </summary>
        void ChunkClearFast(VoxelChunk chunk) {
            chunk.ClearVoxels(noLightValue);
        }

        public void ChunksExportAll() {
            if (cachedChunks == null) {
                return;
            }
            GameObject exportRoot = GameObject.Find(CHUNKS_EXPORT_ROOT);
            if (exportRoot != null) {
                DestroyImmediate(exportRoot);
            }
            exportRoot = new GameObject(CHUNKS_EXPORT_ROOT);
            exportRoot.transform.position = Misc.vector3zero;

            ExportGlobalSettings settings = exportRoot.AddComponent<ExportGlobalSettings>();
            settings.lightPosBuffer = Shader.GetGlobalVectorArray(VoxelPlay.GPULighting.VoxelPlayLightManager.ShaderParams.GlobalLightPositionsArray);
            settings.lightColorBuffer = Shader.GetGlobalVectorArray(VoxelPlay.GPULighting.VoxelPlayLightManager.ShaderParams.GlobalLightColorsArray);
            settings.lightCount = Shader.GetGlobalInt(VoxelPlay.GPULighting.VoxelPlayLightManager.ShaderParams.GlobalLightCount);
            settings.emissionIntensity = Shader.GetGlobalFloat(ShaderParams.VPEmissionIntensity);
            settings.skyTint = Shader.GetGlobalColor(ShaderParams.VPSkyTint);
            settings.groundColor = Shader.GetGlobalColor(ShaderParams.VPGroundColor);
            settings.fogTint = Shader.GetGlobalColor(ShaderParams.VPFogTint);
            settings.fogData = Shader.GetGlobalVector(ShaderParams.VPFogData);
            settings.fogAmount = Shader.GetGlobalFloat(ShaderParams.VPFogAmount);
            settings.exposure = Shader.GetGlobalFloat(ShaderParams.VPExposure);
            settings.ambientLight = Shader.GetGlobalFloat(ShaderParams.VPAmbientLight);
            settings.daylightShadowAtten = Shader.GetGlobalFloat(ShaderParams.VPDaylightShadowAtten);
            settings.enableFog = Shader.IsKeywordEnabled(SKW_VOXELPLAY_GLOBAL_USE_FOG);
            settings.globalSpecularIntensity = Shader.GetGlobalFloat(ShaderParams.VPGlobalSpecular);

            foreach (KeyValuePair<int, CachedChunk> kv in cachedChunks) {
                if (kv.Value == null)
                    continue;
                VoxelChunk chunk = kv.Value.chunk;
                if ((object)chunk == null)
                    continue;
                if (chunk.mf.sharedMesh != null) {
                    chunk.gameObject.hideFlags = 0;
                    chunk.mf.sharedMesh.hideFlags = 0;
                    if (chunk.mc != null && chunk.mc.sharedMesh != null) {
                        chunk.mc.sharedMesh.hideFlags = 0;
                    }
                    chunk.transform.SetParent(exportRoot.transform, true);
                }
            }
            cachedChunks.Clear();

#if UNITY_EDITOR
            // Mark scene as modified
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif

        }

        #endregion

    }



}
