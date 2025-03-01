﻿using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay {

    public partial class VoxelPlayEnvironment : MonoBehaviour {

        const string TORCH_NAME = "Torch";

        /// <summary>
        /// Dictionary lookup for the voxel definition by name
        /// </summary>
        Dictionary<string, ItemDefinition> itemDefinitionsDict;


        /// <summary>
        /// Initializes the array of available items "allItems" with items defined at world level plus all the terrain voxels
        /// </summary>
        void InitItems() {

            int worldItemsCount = world.items != null ? world.items.Length : 0;
            if (allItems == null) {
                allItems = new List<InventoryItem>(voxelDefinitionsCount + worldItemsCount);
            } else {
                allItems.Clear();
            }
            // Init item definitions dictionary
            if (itemDefinitionsDict == null) {
                itemDefinitionsDict = new Dictionary<string, ItemDefinition>();
            } else {
                itemDefinitionsDict.Clear();
            }

            // Add world items
            for (int k = 0; k < worldItemsCount; k++) {
                ItemDefinition id = world.items[k];
                AddItemDefinition(id);
            }

            // Add any player item that's not listed in world items
            IVoxelPlayPlayer player = VoxelPlayPlayer.instance;
            if (player != null && player.items != null) {
                List<InventoryItem> playerItems = player.GetPlayerItems();
                int playerItemCount = playerItems.Count;
                for (int k = 0; k < playerItemCount; k++) {
                    InventoryItem it = playerItems[k];
                    AddItemDefinition(it.item);
                }
            }

            // Add any other item definition found inside Defaults
            ItemDefinition[] ids = Resources.LoadAll<ItemDefinition>("VoxelPlay/Defaults");
            for (int k = 0; k < ids.Length; k++) {
                AddItemDefinition(ids[k]);
            }

            // Add any other item definition inside a resource directory with same name of world
            ids = Resources.LoadAll<ItemDefinition>("Worlds/" + world.name);
            for (int k = 0; k < ids.Length; k++) {
                AddItemDefinition(ids[k]);
            }

            // Add any other item definition inside a resource directory with same name of world (if not placed into Worlds directory)
            ids = Resources.LoadAll<ItemDefinition>(world.name);
            for (int k = 0; k < ids.Length; k++) {
                AddItemDefinition(ids[k]);
            }

            // Add voxel definitions as inventory items
            for (int k = 0; k < voxelDefinitionsCount; k++) {
                if (voxelDefinitions[k].hidden)
                    continue;
                AddItemDefinition(voxelDefinitions[k]);
            }

        }

        /// <summary>
        /// Adds an item definition
        /// </summary>
        /// <returns><c>true</c>, if item definition was added, <c>false</c> otherwise.</returns>
        public bool AddItemDefinition(ItemDefinition itemDefinition) {
            if (itemDefinition == null || itemDefinitionsDict.ContainsKey(itemDefinition.name))
                return false;

            InventoryItem item = new InventoryItem();
            item.item = itemDefinition;
            item.quantity = 999999;
            allItems.Add(item);

            itemDefinitionsDict[itemDefinition.name] = itemDefinition;
            return true;
        }


        /// <summary>
        /// Adds an item definition created from a voxel definition
        /// </summary>
        public bool AddItemDefinition(VoxelDefinition voxelDefinition) {
            if (voxelDefinition == null || itemDefinitionsDict.ContainsKey(voxelDefinition.name)) return false;

            ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.category = ItemCategory.Voxel;
            item.icon = voxelDefinition.GetIcon();
            item.color = voxelDefinition.tintColor;
            item.title = voxelDefinition.title;
            item.voxelType = voxelDefinition;
            item.name = voxelDefinition.name;
            if (string.IsNullOrEmpty(item.title)) item.title = item.name;
            item.pickupSound = voxelDefinition.pickupSound;

            InventoryItem inventoryItem = new InventoryItem();
            inventoryItem.item = item;
            inventoryItem.quantity = 999999;
            allItems.Add(inventoryItem);

            itemDefinitionsDict[item.name] = item;
            return true;
        }

        /// <summary>
        /// Adds a torch.
        /// </summary>
        GameObject TorchAttachInt(VoxelHitInfo hitInfo, ItemDefinition torchDefinition = null, bool refreshChunks = true) {

            // Make sure the voxel exists (has not been removed just before this call) and is solid 
            if (hitInfo.chunk.voxels[hitInfo.voxelIndex].isEmpty || hitInfo.chunk.voxels[hitInfo.voxelIndex].opaque < FULL_OPAQUE) {
                return null;
            }

            // Placeholder for attaching the torch
            VoxelPlaceholder placeholder = GetVoxelPlaceholder(hitInfo.chunk, hitInfo.voxelIndex, true);
            if (placeholder == null) {
                return null;
            }

            // Position of the voxel containing the "light" of the torch
            Vector3d voxelLightPosition = hitInfo.voxelCenter + hitInfo.normal;

            if (!GetVoxelIndex(voxelLightPosition, out VoxelChunk chunk, out int voxelIndex)) {
                return null;
            }

            //  Make sure it's empty where the light gameobject will be placed
            if (chunk.voxels[voxelIndex].opaque >= 2) {
                return null;
            }

            // Updates current chunk
            if (chunk.lightSources != null) {
                foreach (var x in chunk.lightSources) {
                    if (x.voxelIndex == voxelIndex) {
                        // Restriction 2: no second torch on the same voxel index
                        return null;
                    }
                }
            }

            if (torchDefinition == null) {
                // Get an inventory item with name Torch
                int itemCount = allItems.Count;
                for (int k = 0; k < itemCount; k++) {
                    if (allItems[k].item.category == ItemCategory.Torch) {
                        torchDefinition = allItems[k].item;
                        break;
                    }
                }
            }
            if (torchDefinition == null) {
                return null;
            }

            // Instantiate torch prefab and set its parent
            Transform placeholderTransform = placeholder.transform;
            GameObject torch = Instantiate(torchDefinition.prefab, placeholderTransform, false);
            torch.name = TORCH_NAME;

            // Position torch on the wall
            Vector3 torchPos = placeholderTransform.position + hitInfo.normal * 0.5f;
            torch.transform.position = torchPos;

            // Rotate torch according to wall normal
            if (hitInfo.normal.y == -1) { // downwards
                torch.transform.Rotate(180f, 0, 0, Space.World);
            } else if (hitInfo.normal.y == 0) { // side wall
                torch.transform.Rotate(hitInfo.normal.z * 40f, 0, hitInfo.normal.x * -40f, Space.World);
            }

            // Create the item info and attach to the torch gameobject (Item class stores data about the chunk and other properties related to placed items)
            if (!torch.TryGetComponent(out Item itemInfo)) {
                itemInfo = torch.AddComponent<Item>();
            }
            itemInfo.itemDefinition = torchDefinition;
            itemInfo.canPickOnApproach = false;
            itemInfo.canBeDestroyed = true;
            itemInfo.itemChunk = chunk;
            itemInfo.itemVoxelIndex = voxelIndex;

            // Add light source to chunk
            LightSource lightSource = new LightSource();
            lightSource.gameObject = torch;
            lightSource.voxelIndex = voxelIndex;
            lightSource.itemDefinition = torchDefinition;
            lightSource.hitInfo = hitInfo;
            lightSource.lightIntensity = torchDefinition.lightIntensity;
            chunk.AddLightSource(lightSource);

            // Add script to remove light source from chunk when torch or voxel is destroyed
            LightSourceRemoval sr = torch.AddComponent<LightSourceRemoval>();
            sr.env = this;
            sr.chunk = chunk;

            Light pointLight = torch.GetComponentInChildren<Light>();
            if (pointLight != null) {
                pointLight.enabled = true;
            }

            // Make torch collider ignore player's collider to avoid collisions
            if (characterControllerCollider != null) {
                if (torch.TryGetComponent(out Collider collider)) {
                    Physics.IgnoreCollision(collider, characterControllerCollider);
                }
            }

            // Recompute lightmap
            if (refreshChunks) {
                SetTorchLightmap(chunk, voxelIndex, torchDefinition.lightIntensity);
            }

            // Trigger torch event
            if (!isLoadingGame && captureEvents && OnTorchAttached != null) {
                OnTorchAttached(chunk, lightSource);
            }

            RegisterChunkChanges(chunk);

            return torch;
        }

        void TorchDetachInt(VoxelChunk chunk, GameObject gameObject) {
            if ((object)chunk == null || chunk.lightSources == null || cachedChunks == null)
                return;
            int count = chunk.lightSources.Count;
            for (int k = 0; k < count; k++) {
                LightSource ls = chunk.lightSources[k];
                if (ls.gameObject == gameObject) {
                    if (captureEvents && OnTorchDetached != null) {
                        OnTorchDetached(chunk, ls);
                    }
                    chunk.lightSources.RemoveAt(k);
                    RegisterChunkChanges(chunk);

                    // Update lighting and neighbours
                    ClearTorchLightmap(chunk, ls.voxelIndex);

                    return;
                }
            }
        }



        GameObject CreateRecoverableItem(Vector3d position, ItemDefinition itemDefinition, int quantity = 1) {

            if (itemDefinition == null || itemDefinition.prefab == null)
                return null;

            GameObject obj = Instantiate(itemDefinition.prefab, worldRoot, false);
            Item item = obj.AddComponent<Item>();
            item.canPickOnApproach = itemDefinition.canBePicked && itemDefinition.pickMode == PickingMode.PickOnApproach;
            item.quantity = quantity;
            item.itemDefinition = itemDefinition;
            item.creationTime = Time.time;
            item.persistentItem = true;

            if (characterController != null) {
                if (obj.TryGetComponent(out Collider collider)) { 
                    Physics.IgnoreCollision(collider, characterControllerCollider);
                }
            }

            // Set position
            Vector3d pos = position + Random.insideUnitSphere * 0.25f;
            obj.transform.position = pos;

            // If there's no chunk rendered at the position, disable any rigidBody until it's loaded
            if (obj.TryGetComponent(out Rigidbody rb)) {
                rb.useGravity = false;
            }
            return obj;
        }


    }

}
