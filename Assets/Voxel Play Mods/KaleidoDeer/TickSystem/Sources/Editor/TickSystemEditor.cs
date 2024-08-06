using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VoxelPlay;
using System;

namespace VoxelPlay
{
    [CustomEditor(typeof(VoxelPlayEnvironment))]
    public class TickEnvironmentEditor : UnityEditor.Editor
    {
        SerializedProperty TickRate;

        Color titleColor;
        static GUIStyle titleLabelStyle;
        private Editor DefaultEditor;
        void OnEnable()
        {
            DefaultEditor = CreateEditor(targets, typeof(VoxelPlay.VoxelPlayEnvironmentEditor));
            titleColor = EditorGUIUtility.isProSkin ? new Color(0.52f, 0.66f, 0.9f) : new Color(0.12f, 0.16f, 0.4f);
            TickRate = serializedObject.FindProperty("TickRate");
        }

        private void OnDisable()
        {
            if (DefaultEditor != null)
            {
                DestroyImmediate(DefaultEditor);
            }
        }

        public override void OnInspectorGUI()
        {
            DefaultEditor.OnInspectorGUI();
            serializedObject.UpdateIfRequiredOrScript();
            if (titleLabelStyle == null)
            {
                titleLabelStyle = new GUIStyle(EditorStyles.label);
            }

            titleLabelStyle.normal.textColor = titleColor;
            titleLabelStyle.fontStyle = FontStyle.Bold;
            EditorGUIUtility.labelWidth = 130;
            EditorGUILayout.Separator();
            GUILayout.Label("Tick Settings", titleLabelStyle);

            EditorGUILayout.PropertyField(TickRate);
            serializedObject.ApplyModifiedProperties();
        }


    }
}