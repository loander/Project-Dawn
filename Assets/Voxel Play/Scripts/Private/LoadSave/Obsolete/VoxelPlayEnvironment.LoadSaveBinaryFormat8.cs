using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Text;
using System.Globalization;

namespace VoxelPlay {

	public partial class VoxelPlayEnvironment : MonoBehaviour {

		void LoadGameBinaryFileFormat_8 (BinaryReader br, bool preservePlayerPosition = false) {
			// Character controller transform position & rotation
			Vector3 pos = DecodeVector3Binary (br);
			Vector3 characterRotationAngles = DecodeVector3Binary (br);
			Vector3 cameraLocalRotationAngles = DecodeVector3Binary (br);
			if (!preservePlayerPosition) {
				if ((UnityEngine.Object)characterController != null) {
					characterController.MoveTo(pos);
					characterController.transform.rotation = Quaternion.Euler (characterRotationAngles);
					cameraMain.transform.localRotation = Quaternion.Euler (cameraLocalRotationAngles);
					characterController.UpdateLook ();
				}
			}

			InitSaveGameStructs ();
			// Read voxel definition table
			int vdCount = br.ReadInt16 (); 
			for (int k = 0; k < vdCount; k++) {
				VoxelDefinition vd = GetVoxelDefinition(br.ReadString());
				saveVoxelDefinitionsList.Add(vd);
			}
			// Read item definition table
			int idCount = br.ReadInt16 (); 
			for (int k = 0; k < idCount; k++) {
				saveItemDefinitionsList.Add (br.ReadString ());
			}

			int numChunks = br.ReadInt32 ();
			VoxelDefinition voxelDefinition = defaultVoxel;
			int prevVdIndex = -1;
			Color32 voxelColor = Misc.color32White;
			for (int c = 0; c < numChunks; c++) {
				// Read chunks
				// Get chunk position
				Vector3d chunkPosition = DecodeVector3Binary (br).ToVector3d ();
				VoxelChunk chunk = GetChunkUnpopulated (chunkPosition);
				byte isAboveSurface = br.ReadByte ();
				chunk.isAboveSurface = isAboveSurface == 1;
				chunk.allowTrees = false;
				chunk.modified = true;
				chunk.isPopulated = true;
				chunk.voxelSignature = -1;
				chunk.renderState = ChunkRenderState.Pending;
				SetChunkOctreeIsDirty (chunkPosition, false);
				ChunkClearFast (chunk);
				// Read voxels
				int numWords = br.ReadInt16 ();
				for (int k = 0; k < numWords; k++) {
					// Voxel definition
					int vdIndex = br.ReadInt16 ();
					if (prevVdIndex != vdIndex) {
						if (vdIndex >= 0 && vdIndex < vdCount) {
							voxelDefinition = saveVoxelDefinitionsList [vdIndex];
							prevVdIndex = vdIndex;
						}
					}
					// RGB
					voxelColor.r = br.ReadByte ();
					voxelColor.g = br.ReadByte ();
					voxelColor.b = br.ReadByte ();
					// Voxel index
					int voxelIndex = br.ReadInt16 ();
					// Repetitions
					int repetitions = br.ReadInt16 ();

					byte flags = br.ReadByte ();
					byte hasCustomRotation = br.ReadByte ();

					if (voxelDefinition == null) {
						continue;
					}

					// Custom voxel flags
					if (voxelDefinition.renderType == RenderType.Custom) {
						//flags &= 0xF; // custom voxels do not store texture rotation; their transform has the final rotation
						if (hasCustomRotation == 1) {
							// custom rotation no longer saved in the file
							DecodeVector3Binary (br);
							//delayedVoxelCustomRotations.Add (GetVoxelPosition (chunkPosition, voxelIndex), voxelAngles);
						}
					}
					for (int i = 0; i < repetitions; i++) {
						chunk.voxels [voxelIndex + i].Set (voxelDefinition, voxelColor);
						if (voxelDefinition.renderType == RenderType.Water || voxelDefinition.renderType.supportsTextureRotation ()) {
							chunk.voxels [voxelIndex + i].SetFlags (flags);
						}
					}
				}
				// Read light sources
				int lightCount = br.ReadInt16 ();
				VoxelHitInfo hitInfo = new VoxelHitInfo ();
				for (int k = 0; k < lightCount; k++) {
					// Voxel index
					hitInfo.voxelIndex = br.ReadInt16 ();
					// Voxel center
					hitInfo.voxelCenter = GetVoxelPosition (chunkPosition, hitInfo.voxelIndex);
					// Normal
					hitInfo.normal = DecodeVector3Binary (br);
					hitInfo.chunk = chunk;
					// Item definition
					int itemIndex = br.ReadInt16 ();
					if (itemIndex < 0 || itemIndex >= idCount)
						continue;
					string itemDefinitionName = saveItemDefinitionsList [itemIndex];
					ItemDefinition itemDefinition = GetItemDefinition (itemDefinitionName);
					TorchAttach (hitInfo, itemDefinition);
				}
				// Read items
				int itemCount = br.ReadInt16 ();
				for (int k = 0; k < itemCount; k++) {
					// Voxel index
					int itemIndex = br.ReadInt16 ();
					if (itemIndex < 0 || itemIndex >= idCount)
						continue;
					string itemDefinitionName = saveItemDefinitionsList [itemIndex];
					Vector3d itemPosition = DecodeVector3Binary (br).ToVector3d();
					int quantity = br.ReadInt16 ();
					ItemSpawn (itemDefinitionName, itemPosition, quantity);
				}
			}
		}

	}



}
