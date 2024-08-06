using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace VoxelPlay {

    public class Excavator : MonoBehaviour {

        VoxelPlayEnvironment env;

        int diggerMode;
        int brushRadius = 3;
        readonly List<VoxelIndex> indices = new();
        Vector3Int prevCamPos;
        Vector3d digCenter;
        VoxelDefinition voxelDefinition;

        void Start() {
            env = VoxelPlayEnvironment.instance;
            VoxelPlayUI.instance.OnConsoleNewCommand += Instance_OnConsoleNewCommand;
            VoxelPlayPlayer.instance.OnItemSelectedChanged += Instance_OnItemSelectedChanged;
            GetSelectedVoxelFromInventory();
        }

        private void Instance_OnItemSelectedChanged(int selectedItemIndex, int prevSelectedItemIndex) {
            GetSelectedVoxelFromInventory();
        }

        void GetSelectedVoxelFromInventory() {
            InventoryItem item = VoxelPlayPlayer.instance.GetSelectedItem();
            if (item != null) {
                voxelDefinition = item.item?.voxelType;
            }
        }


        private void Instance_OnConsoleNewCommand(string text) {

            text = text.Trim().ToUpper();
            if (text.StartsWith("/HELP") || text.StartsWith("/E HELP")) {
                env.ShowMessage("--- Excavator Mod Commands ---", colorName: "green");
                ShowCommandHelp("/e d 0", "disables mode");
                ShowCommandHelp("/e d 1", "digs on click");
                ShowCommandHelp("/e d 2", "digs around camera continuously");
                ShowCommandHelp("/e d 3", "paints on click");
                ShowCommandHelp("/e b n", "sets brush radius");
                ShowCommandHelp("/e v", "list a name of available voxel definitions");
                ShowCommandHelp("/e v filter", "list a name of available voxel definitions matching regular expression filter");
                ShowCommandHelp("/e v index", "selects a voxel definition by its index");
                return;
            }

            if (text.StartsWith("/E D")) {
                ParseDiggerEnableCommand(text);
                return;
            }

            if (text.StartsWith("/E B")) {
                string arg = text[4..].Trim();
                if (int.TryParse(arg, out int radius)) {
                    brushRadius = radius;
                    env.ShowMessage("Brush radius set to " + brushRadius);
                }
                return;
            }

            if (text.StartsWith("/E V")) {
                string arg = text[4..].Trim();
                if (int.TryParse(arg, out int vdIndex)) {
                    SelectVoxelDefinition(vdIndex);
                    return;
                }

                ListVoxelDefinitions(arg);
                return;
            }

        }

        void ShowCommandHelp(string cmd, string text) {
            env.ShowMessage("<color=yellow>" + cmd + "</color>: <color=green>" + text + "</color>");
        }

        void ParseDiggerEnableCommand(string text) {
            string state = text.Substring(4).Trim();
            if ("1".Equals(state)) {
                diggerMode = 1;
                env.ShowMessage("Excavator mode set to dig on click");
            } else if ("2".Equals(state)) {
                diggerMode = 2;
                VoxelPlayFirstPersonController.instance.isFlying = true;
                env.ShowMessage("Excavator mode set to dig around camera (continuous)");
            } else if ("3".Equals(state)) {
                diggerMode = 3;
                env.ShowMessage("Excavator mode set to build on click");
            } else {
                if ("0".Equals(state)) diggerMode = 0;
            }
        }

        private void Update() {
            if (diggerMode == 0) return;

            switch (diggerMode) {
                case 1:
                    VoxelPlayPlayer.instance.hitRange = 5000;
                    if (env.input.GetButtonClick(InputButtonNames.Button1)) {
                        DigAtTarget();
                    }
                    break;
                case 2:
                    DigAroundCamera();
                    break;
                case 3:
                    VoxelPlayPlayer.instance.hitRange = 5000;
                    if (env.input.GetButtonClick(InputButtonNames.Button1)) {
                        BuildAtTarget();
                    }
                    break;
            }
        }

        void DigAtTarget() {
            Vector3 center = env.lastHighlightInfo.voxelCenter;
            DigAtPosition(center);
        }

        void DigAroundCamera() {
            Vector3Int camPos = Vector3Int.FloorToInt(env.cameraMain.transform.position);
            if (camPos == prevCamPos) return;

            DigAtPosition(camPos);
        }

        void DigAtPosition(Vector3 center) {
            digCenter = center;
            env.GetVoxelIndices(center - Vector3.one * brushRadius, center + Vector3.one * brushRadius, indices, IsInsideBrush);
            foreach (VoxelIndex i in indices) {
                env.VoxelDestroy(i.position);
            }
        }

        public float IsInsideBrush(Vector3d position) {
            float dist = (float)Vector3d.Distance(position, digCenter) - brushRadius;
            return dist;
        }

        void BuildAtTarget() {
            Vector3 center = env.lastHighlightInfo.voxelCenter;
            BuildAtPosition(center);
        }

        void BuildAtPosition(Vector3 center) {
            if (voxelDefinition == null) {
                env.ShowError("First select a voxel using command /e sv name");
                return;
            }
            digCenter = center;
            env.GetVoxelIndices(center - Vector3.one * brushRadius, center + Vector3.one * brushRadius, indices, IsInsideBrush);
            env.VoxelPlace(indices, voxelDefinition, Misc.colorWhite);
        }

        void ListVoxelDefinitions(string filter) {
            StringBuilder sb = new StringBuilder();

            foreach (VoxelDefinition vd in env.voxelDefinitions) {
                if (vd == null) continue;
                if (!string.IsNullOrWhiteSpace(filter) && !vd.name.ToUpper().Contains(filter)) continue;

                if (sb.Length > 0) {
                    sb.Append("<color=green>, </color>");
                }
                sb.Append("<color=yellow>");
                sb.Append(vd.name);
                sb.Append("</color>(");
                sb.Append(vd.index);
                sb.Append(")");
            }
            env.ShowMessage("Available voxel definitions:", colorName: "green");
            env.ShowMessage(sb.ToString());
        }

        void SelectVoxelDefinition(int index) {
            VoxelDefinition vd = env.GetVoxelDefinition(index);
            if (vd != null) {
                voxelDefinition = vd;
                env.ShowMessage(vd.name + " selected.");
            }
        }

    }

}