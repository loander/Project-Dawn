using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;

namespace VoxelPlay {

    public delegate void LoadGameEvent(string tag, byte[] contents);
    public delegate void SaveGameEvent(SaveGameCustomDataWriter writer);

    public partial class VoxelPlayEnvironment : MonoBehaviour {

        public event LoadGameEvent OnLoadCustomGameData;
        public event SaveGameEvent OnSaveCustomGameData;

        const string SAVEGAMEDATA_EXTENSION = ".bytes";

        /// <summary>
        /// True if the current game has been loaded from a savefile.
        /// </summary>
        [NonSerialized]
        public bool saveFileIsLoaded;


        const byte SAVE_FILE_CURRENT_FORMAT = 14;
        bool isLoadingGame;

        /// <summary>
        /// Loads the savegame file specified in the "saveFilename" property of Voxel Play Environment
        /// </summary>
        /// <param name="firstLoad">Pass true only if this is the first time the game is loaded</param>
        /// <param name="preservePlayerPosition">If set to <c>true</c> preserve player position.</param>
        /// <param name="fallbackVoxelDefinition">In case a voxel definition from the savegame file no longer exists, it will be replaced by this fallback voxel definition</param>
        /// <returns>true if the savegame was loaded correctly</returns>
        public bool LoadGameBinary(bool firstLoad, bool preservePlayerPosition = false, VoxelDefinition fallbackVoxelDefinition = null) {

            saveFileIsLoaded = false;

            if (firstLoad) {
                if (string.IsNullOrEmpty(saveFilename))
                    return false;
            } else {
                if (!CheckGameFilename())
                    return false;
            }

            bool result = true;
            try {
                byte[] saveGameData = GetSaveGameDataBinary();
                if (saveGameData == null) {
                    return false;
                }

                DestroyAllVoxels();

                // get version
                isLoadingGame = true;
                using (BinaryReader br = new BinaryReader(new MemoryStream(saveGameData, false), Encoding.UTF8)) {
                    int version = br.ReadByte();
#pragma warning disable 0429
#pragma warning disable 0162
                    if (CHUNK_SIZE != 16 && version <= 9) {
                        throw new ApplicationException("Saved game cannot be loaded. Chunk size does not match!");
                    }
                    if (version >= 10) {
                        int chunkSize = br.ReadByte();
                        if (CHUNK_SIZE != chunkSize) {
                            throw new ApplicationException("Saved game cannot be loaded. Saved chunk size (" + chunkSize + ") does not match current scene chunk size!");
                        }
                    }
#pragma warning restore 0162
#pragma warning restore 0429
                    switch (version) {
                        case 5:
                            LoadGameBinaryFileFormat_5(br, preservePlayerPosition);
                            break;
                        case 6:
                            LoadGameBinaryFileFormat_6(br, preservePlayerPosition);
                            break;
                        case 7:
                            LoadGameBinaryFileFormat_7(br, preservePlayerPosition);
                            break;
                        case 8:
                            LoadGameBinaryFileFormat_8(br, preservePlayerPosition);
                            break;
                        case 9:
                            LoadGameBinaryFileFormat_9(br, preservePlayerPosition);
                            break;
                        case 10:
                            LoadGameBinaryFileFormat_10(br, preservePlayerPosition);
                            break;
                        case 11:
                            LoadGameBinaryFileFormat_11(br, preservePlayerPosition);
                            break;
                        case 12:
                            LoadGameBinaryFileFormat_12(br, preservePlayerPosition);
                            break;
                        case 13:
                            LoadGameBinaryFileFormat_13(br, preservePlayerPosition);
                            break;
                        case 14:
                            LoadGameBinaryFileFormat_14(br, preservePlayerPosition, fallbackVoxelDefinition);
                            break;
                        default:
                            throw new ApplicationException("LoadGame() does not support this file format.");
                    }
                    br.Close();
                }
                isLoadingGame = false;
                saveFileIsLoaded = true;
                if (!firstLoad && VoxelPlayUI.instance != null) {
                    VoxelPlayUI.instance.ToggleConsoleVisibility(false);
                    ShowMessage("<color=green>Game loaded successfully!</color>");
                }
                if (OnGameLoaded != null) {
                    OnGameLoaded();
                }
            } catch (Exception ex) {
                ShowError("<color=red>Load error:</color> <color=orange>" + ex.Message + "</color><color=white>" + ex.StackTrace + "</color>");
                result = false;
            }

            isLoadingGame = false;
            shouldCheckChunksInFrustum = true;
            return result;
        }

        string GetFullFilename() {
#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(world);
            path = Path.GetDirectoryName(path) + "/SavedGames";
            Directory.CreateDirectory(path);
            path += "/" + saveFilename + SAVEGAMEDATA_EXTENSION;
            return path;
#else
												string path = Application.persistentDataPath + "/VoxelPlay";
												Directory.CreateDirectory (path);
			string fullName = path + "/" + saveFilename + SAVEGAMEDATA_EXTENSION;
												return fullName;
#endif
        }


        byte[] GetSaveGameDataBinary() {

#if UNITY_EDITOR
            // In Editor, always load saved game from Resources/Worlds/<name of world>/SavedGames folder
            string path = AssetDatabase.GetAssetPath(world);
            path = Path.GetDirectoryName(path) + "/SavedGames/" + saveFilename + SAVEGAMEDATA_EXTENSION;
            if (File.Exists(path)) {
                return File.ReadAllBytes(path);
            }
            return null;

#else
												// In Build, try to load the saved game from application data path. If there's none, try to load a default saved game from Resources.
			string path = Application.persistentDataPath + "/VoxelPlay/" + saveFilename + SAVEGAMEDATA_EXTENSION;
												if (File.Exists(path)) {
			return File.ReadAllBytes (path);

												} else {
																string resource = "Worlds/" + world.name + "/SavedGames/" + saveFilename;
												TextAsset ta = Resources.Load<TextAsset>(resource);
												if (ta!=null) {
												return ta.bytes;
												} else {
												return null;
												}
												}
#endif
        }


        bool CheckGameFilename() {
            if (string.IsNullOrEmpty(saveFilename)) {
                ShowMessage("<color=orange>Set a file name for the game to load/save first.</color>");
                return false;
            }
            return true;
        }

        public bool SaveGameBinary() {
            if (!CheckGameFilename())
                return false;

            bool success = true;
            try {
                string filename = GetFullFilename();
                FileStream fs = new FileStream(filename, FileMode.Create);
                BinaryWriter bw = new BinaryWriter(fs, Encoding.UTF8);
                SaveGameBinaryFormat(bw);
                bw.Close();
                fs.Close();
                ShowMessage("<color=green>Game saved successfully in </color><color=yellow>" + filename + "</color>");
            } catch (Exception ex) {
                ShowError("<color=red>Error:</color> <color=orange>" + ex.Message + "</color>");
                success = false;
            }
            return success;
        }

        /// <summary>
        /// Returns the world encoded in a string
        /// </summary>
        /// <returns>The game to text.</returns>
        public byte[] SaveGameToByteArray() {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms, Encoding.UTF8);
            SaveGameBinaryFormat(bw);
            bw.Close();
            return ms.ToArray();
        }

        /// <summary>
        /// Returns the world encoded in base 64 format
        /// </summary>
        public string SaveGameToBase64() {
            return Convert.ToBase64String(SaveGameToByteArray());
        }

        /// <summary>
        /// Loads game world from a string
        /// </summary>
        /// <returns>True if saveGameData was loaded successfully.</returns>
        /// <param name="preservePlayerPosition">If set to <c>true</c> preserve player position.</param>
        /// <param name="clearScene">If set to <c>true</c> existing chunks will be cleared before loading the game. If set to false, only chunks from the saved game will be replaced.</param>
        /// <param name="fallbackVoxelDefinition">In case a voxel definition from the savegame file no longer exists, it will be replaced by this fallback voxel definition</param>
        public bool LoadGameFromBase64(string saveGameDataBase64string, bool preservePlayerPosition, bool clearScene = true, VoxelDefinition fallbackVoxelDefinition = null) {
            byte[] saveGameData = System.Convert.FromBase64String(saveGameDataBase64string);
            return LoadGameFromByteArray(saveGameData, preservePlayerPosition, clearScene, fallbackVoxelDefinition);
        }

        /// <summary>
        /// Loads game world from a string
        /// </summary>
        /// <returns>True if saveGameData was loaded successfully.</returns>
        /// <param name="preservePlayerPosition">If set to <c>true</c> preserve player position.</param>
        /// <param name="clearScene">If set to <c>true</c> existing chunks will be cleared before loading the game. If set to false, only chunks from the saved game will be replaced.</param>
        /// <param name="fallbackVoxelDefinition">In case a voxel definition from the savegame file no longer exists, it will be replaced by this fallback voxel definition</param>
        public bool LoadGameFromByteArray(byte[] saveGameData, bool preservePlayerPosition, bool clearScene = true, VoxelDefinition fallbackVoxelDefinition = null) {
            if (clearScene) {
                DestroyAllVoxels();
            } else {
                // Remove all modified chunks to ensure only loaded chunks are the modified ones
                List<VoxelChunk> tempChunks = BufferPool<VoxelChunk>.Get();
                GetChunks(tempChunks, ChunkModifiedFilter.OnlyModified);
                int count = tempChunks.Count;
                for (int k = 0; k < count; k++) {
                    VoxelChunk chunk = tempChunks[k];
                    if (chunk != null && chunk.modified) {
                        // Restore original contents
                        world.terrainGenerator.PaintChunk(chunk);
                        ChunkRequestRefresh(chunk, true, true);
                        chunk.modified = false;
                    }
                }
                BufferPool<VoxelChunk>.Release(tempChunks);
            }


            bool result;
            try {
                if (saveGameData == null) {
                    return false;
                }

                // get version
                isLoadingGame = true;
                using (BinaryReader br = new BinaryReader(new MemoryStream(saveGameData), Encoding.UTF8)) {
                    byte version = br.ReadByte();
#pragma warning disable 0429
#pragma warning disable 0162
                    if (CHUNK_SIZE != 16 && version <= 9) {
                        throw new ApplicationException("Saved game cannot be loaded. Chunk size does not match!");
                    }
                    if (version >= 10) {
                        int chunkSize = br.ReadByte();
                        if (CHUNK_SIZE != chunkSize) {
                            throw new ApplicationException("Saved game cannot be loaded. Saved chunk size (" + chunkSize + ") does not match current scene chunk size!");
                        }
                    }
#pragma warning restore 0162
#pragma warning restore 0429
                    switch (version) {
                        case 5:
                            LoadGameBinaryFileFormat_5(br, preservePlayerPosition);
                            break;
                        case 6:
                            LoadGameBinaryFileFormat_6(br, preservePlayerPosition);
                            break;
                        case 7:
                            LoadGameBinaryFileFormat_7(br, preservePlayerPosition);
                            break;
                        case 8:
                            LoadGameBinaryFileFormat_8(br, preservePlayerPosition);
                            break;
                        case 9:
                            LoadGameBinaryFileFormat_9(br, preservePlayerPosition);
                            break;
                        case 10:
                            LoadGameBinaryFileFormat_10(br, preservePlayerPosition);
                            break;
                        case 11:
                            LoadGameBinaryFileFormat_11(br, preservePlayerPosition);
                            break;
                        case 12:
                            LoadGameBinaryFileFormat_12(br, preservePlayerPosition);
                            break;
                        case 13:
                            LoadGameBinaryFileFormat_13(br, preservePlayerPosition);
                            break;
                        case 14:
                            LoadGameBinaryFileFormat_14(br, preservePlayerPosition, fallbackVoxelDefinition);
                            break;
                        default:
                            throw new ApplicationException("LoadGameFromArray() does not support this file format.");
                    }
                    br.Close();
                }
                isLoadingGame = false;
                saveFileIsLoaded = true;
                if (OnGameLoaded != null) {
                    OnGameLoaded();
                }
                result = true;
            } catch (Exception ex) {
                Debug.LogError("Voxel Play: " + ex.Message);
                result = false;
            }

            isLoadingGame = false;
            shouldCheckChunksInFrustum = true;
            return result;

        }


    }



}
