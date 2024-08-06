using UnityEditor;
using UnityEngine;

namespace VoxelPlay {

    [CustomEditor(typeof(ConnectedVoxel))]
    public class VoxelPlayConnectedVoxelConfigEditor : UnityEditor.Editor {

        SerializedProperty voxelDefinition;
        SerializedProperty ruleEvent;
        SerializedProperty config;

        void OnEnable() {
            voxelDefinition = serializedObject.FindProperty("voxelDefinition");
            ruleEvent = serializedObject.FindProperty("ruleEvent");
            config = serializedObject.FindProperty("config");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.PropertyField(voxelDefinition, new GUIContent("Placing Voxel", "These rules will be applied when placing this voxel in the world."));
            EditorGUILayout.PropertyField(ruleEvent, new GUIContent("Event", "Choose if these rules are applied when placing a voxel or when rendering. If rules are applied when placing, the voxel will actually be changed in the chunk. However, if you select 'When Rendering', the contents of the chunk won't be modified, only the representation will change."));
            EditorGUILayout.HelpBox("Specify which adjacent prefabs are connected and which action and prefabs must be used in each case.", MessageType.Info);
            EditorGUILayout.PropertyField(config, new GUIContent("Configuration"), true);
            serializedObject.ApplyModifiedProperties();
        }



    }

}
