using UnityEngine;
using UnityEditor;

namespace VoxelPlay {

    public class ExcavatorMenu : MonoBehaviour {

        [MenuItem("GameObject/Voxel Play/Create Excavator Manager", false, 100)]
        public static void CreateManager() {
            Excavator f = FindObjectOfType<Excavator>();
            if (f != null) {
                Selection.activeGameObject = f.gameObject;
                EditorGUIUtility.PingObject(f.gameObject);
                return;
            }
            GameObject go = new GameObject("Excavator Manager", typeof(Excavator));
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
    }

}