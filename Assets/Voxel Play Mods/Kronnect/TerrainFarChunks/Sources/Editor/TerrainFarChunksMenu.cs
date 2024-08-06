using UnityEngine;
using UnityEditor;

namespace VoxelPlay {

    public class TerrainFarChunksMenu : MonoBehaviour {

        [MenuItem("GameObject/Voxel Play/Create Terrain Far Chunks Manager", false, 100)]
        public static void CreateManager() {
            TerrainFarChunks f = FindObjectOfType<TerrainFarChunks>();
            if (f != null) {
                Selection.activeGameObject = f.gameObject;
                EditorGUIUtility.PingObject(f.gameObject);
                return;
            }
            GameObject go = Instantiate<GameObject>(Resources.Load<GameObject>("TerrainFarChunks/TerrainFarChunksManager"));
            go.name = "Terrain Far Chunks Manager";
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            go.transform.localRotation = Quaternion.Euler(0, 0, 0);
            go.transform.localScale = new Vector3(1f, 1f, 1f);
            Selection.activeObject = go;
        }
    }

}