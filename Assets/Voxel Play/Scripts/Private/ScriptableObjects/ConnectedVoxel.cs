using System;
using UnityEngine;

namespace VoxelPlay {

    public delegate VoxelDefinition CustomVoxelDefinitionProviderDelegate(Vector3d position, VoxelDefinition vd, int rotation);
    public delegate VoxelDefinition CustomVoxelDefinitionForRenderingDelegate(Vector3d position, VoxelDefinition vd,
        int topCenterTypeIndex, int bottomCenterTypeIndex, int backLeftTypeIndex,
        int backTypeIndex, int backRightTypeIndex, int leftTypeIndex, int rightTypeIndex, int forwardLeftTypeIndex, int forwardTypeIndex, int forwardRightTypeIndex,
        int topBackLeftTypeIndex, int topBackTypeIndex, int topBackRightTypeIndex, int topLeftTypeIndex, int topRightTypeIndex, int topForwardLeftTypeIndex, int topForwardTypeIndex, int topForwardRightTypeIndex,
        int bottomBackLeftTypeIndex, int bottomBackTypeIndex, int bottomBackRightTypeIndex, int bottomLeftTypeIndex, int bottomRightTypeIndex, int bottomForwardLeftTypeIndex, int bottomForwardTypeIndex, int bottomForwardRightTypeIndex);

    public enum ConnectedVoxelConfigMatch {
        Anything,
        Equals,
        NotEquals,
        Empty,
        NotEmpty
    }

    public enum ConnectedVoxelConfigAction {
        Nothing,
        Replace,
        Random,
        Cycle
    }

    public enum ConnectedVoxelEvent {
        WhenPlacing,
        WhenRendering
    }

    [Serializable]
    public struct ConnectedVoxelConfig {
        public bool enabled;
        public ConnectedVoxelConfigMatch tl, t, tr, l, r, bl, b, br, tc, bc;
        public ConnectedVoxelConfigMatch tl2, t2, tr2, l2, r2, bl2, b2, br2;
        public ConnectedVoxelConfigMatch tl0, t0, tr0, l0, r0, bl0, b0, br0;
        public ConnectedVoxelConfigAction action;
        public VoxelDefinition replacementVoxelDefinition; // for Replace action
        public VoxelDefinition[] replacementVoxelDefinitionSet; // for Random/Cycle action
    }

    [CreateAssetMenu(menuName = "Voxel Play/Connected Voxel", fileName = "ConnectedVoxel", order = 132)]
    public class ConnectedVoxel : ScriptableObject {

        public string description;

        [Tooltip("The voxel being placed.")]
        public VoxelDefinition voxelDefinition;

        public ConnectedVoxelEvent ruleEvent = ConnectedVoxelEvent.WhenPlacing;

        [Tooltip("Rules that apply to this voxel.")]
        public ConnectedVoxelConfig[] config;

        VoxelPlayEnvironment env;
        int cycleIndex;
        VoxelIndex[] neighbours;
        int voxelDefinitionTypeIndex;

        public void Init(VoxelPlayEnvironment env) {
            this.env = env;
            if (voxelDefinition == null || config == null) return;

            neighbours = new VoxelIndex[27];
            if (ruleEvent == ConnectedVoxelEvent.WhenPlacing) {
                voxelDefinition.customVoxelDefinitionProvider = ResolveVoxelDefinition;
            } else {
                voxelDefinition.customVoxelDefinitionForRendering = ResolveVoxelDefinitionForRendering;
            }

            // register voxel definitions just in case
            env.AddVoxelDefinition(voxelDefinition);
            foreach (var entry in config) {
                env.AddVoxelDefinition(entry.replacementVoxelDefinition);
                env.AddVoxelDefinitions(entry.replacementVoxelDefinitionSet);
            }
        }


        public VoxelDefinition ResolveVoxelDefinition(Vector3d position, VoxelDefinition vd, int rotation) {
            if (env == null)
                return vd;

            env.GetVoxelNeighbourhood(position, ref neighbours, rotation);
            int forwardLeftTypeIndex = neighbours[15].typeIndex;
            int forwardTypeIndex = neighbours[16].typeIndex;
            int forwardRightTypeIndex = neighbours[17].typeIndex;
            int leftTypeIndex = neighbours[12].typeIndex;
            int rightTypeIndex = neighbours[14].typeIndex;
            int backLeftTypeIndex = neighbours[9].typeIndex;
            int backTypeIndex = neighbours[10].typeIndex;
            int backRightTypeIndex = neighbours[11].typeIndex;
            int topCenterTypeIndex = neighbours[22].typeIndex;
            int bottomCenterTypeIndex = neighbours[4].typeIndex;
            int topBackLeftTypeIndex = neighbours[18].typeIndex;
            int topBackTypeIndex = neighbours[19].typeIndex;
            int topBackRightTypeIndex = neighbours[20].typeIndex;
            int topLeftTypeIndex = neighbours[21].typeIndex;
            int topRightTypeIndex = neighbours[23].typeIndex;
            int topForwardLeftTypeIndex = neighbours[24].typeIndex;
            int topForwardTypeIndex = neighbours[25].typeIndex;
            int topForwardRightTypeIndex = neighbours[26].typeIndex;
            int bottomBackLeftTypeIndex = neighbours[0].typeIndex;
            int bottomBackTypeIndex = neighbours[1].typeIndex;
            int bottomBackRightTypeIndex = neighbours[2].typeIndex;
            int bottomLeftTypeIndex = neighbours[3].typeIndex;
            int bottomRightTypeIndex = neighbours[5].typeIndex;
            int bottomForwardLeftTypeIndex = neighbours[6].typeIndex;
            int bottomForwardTypeIndex = neighbours[7].typeIndex;
            int bottomForwardRightTypeIndex = neighbours[8].typeIndex;

            return ResolveVoxelDefinitionForRendering(position, vd,
                        topCenterTypeIndex, bottomCenterTypeIndex, backLeftTypeIndex,
        backTypeIndex, backRightTypeIndex, leftTypeIndex, rightTypeIndex, forwardLeftTypeIndex, forwardTypeIndex, forwardRightTypeIndex,
        topBackLeftTypeIndex, topBackTypeIndex, topBackRightTypeIndex, topLeftTypeIndex, topRightTypeIndex, topForwardLeftTypeIndex, topForwardTypeIndex, topForwardRightTypeIndex,
        bottomBackLeftTypeIndex, bottomBackTypeIndex, bottomBackRightTypeIndex, bottomLeftTypeIndex, bottomRightTypeIndex, bottomForwardLeftTypeIndex, bottomForwardTypeIndex, bottomForwardRightTypeIndex);
        }



        public VoxelDefinition ResolveVoxelDefinitionForRendering(Vector3d position, VoxelDefinition vd,
             int topCenterTypeIndex, int bottomCenterTypeIndex, int backLeftTypeIndex,
        int backTypeIndex, int backRightTypeIndex, int leftTypeIndex, int rightTypeIndex, int forwardLeftTypeIndex, int forwardTypeIndex, int forwardRightTypeIndex,
        int topBackLeftTypeIndex, int topBackTypeIndex, int topBackRightTypeIndex, int topLeftTypeIndex, int topRightTypeIndex, int topForwardLeftTypeIndex, int topForwardTypeIndex, int topForwardRightTypeIndex,
        int bottomBackLeftTypeIndex, int bottomBackTypeIndex, int bottomBackRightTypeIndex, int bottomLeftTypeIndex, int bottomRightTypeIndex, int bottomForwardLeftTypeIndex, int bottomForwardTypeIndex, int bottomForwardRightTypeIndex
            ) {
            if (config == null)
                return vd;

            voxelDefinitionTypeIndex = voxelDefinition == null ? 0 : voxelDefinition.index;
            int configLength = config.Length;
            for (int k = 0; k < configLength; k++) {
                // middle slice
                if (!CheckConfigRuleMatch(config[k].tl, forwardLeftTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].t, forwardTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].tr, forwardRightTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].l, leftTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].r, rightTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].bl, backLeftTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].b, backTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].br, backRightTypeIndex)) continue;
                // central top & bottom
                if (!CheckConfigRuleMatch(config[k].tc, topCenterTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].bc, bottomCenterTypeIndex)) continue;
                // rest of bottom slice
                if (!CheckConfigRuleMatch(config[k].tl0, bottomForwardLeftTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].t0, bottomForwardTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].tr0, bottomForwardRightTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].l0, bottomLeftTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].r0, bottomRightTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].bl0, bottomBackLeftTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].b0, bottomBackTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].br0, bottomRightTypeIndex)) continue;
                // rest of top slice
                if (!CheckConfigRuleMatch(config[k].tl2, topForwardLeftTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].t2, topForwardTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].tr2, topForwardRightTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].l2, topLeftTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].r2, topRightTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].bl2, topBackLeftTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].b2, topBackTypeIndex)) continue;
                if (!CheckConfigRuleMatch(config[k].br2, topBackRightTypeIndex)) continue;

                switch (config[k].action) {
                    case ConnectedVoxelConfigAction.Nothing:
                        vd = null;
                        break;
                    case ConnectedVoxelConfigAction.Replace:
                        vd = config[k].replacementVoxelDefinition;
                        break;
                    case ConnectedVoxelConfigAction.Random: {
                            VoxelDefinition[] replacementSet = config[k].replacementVoxelDefinitionSet;
                            if (replacementSet != null && replacementSet.Length > 0) {
                                int index = WorldRand.Range(0, replacementSet.Length, position);
                                vd = replacementSet[index];
                            }
                        }
                        break;
                    case ConnectedVoxelConfigAction.Cycle: {
                            VoxelDefinition[] replacementSet = config[k].replacementVoxelDefinitionSet;
                            if (replacementSet != null && replacementSet.Length > 0) {
                                cycleIndex++;
                                if (cycleIndex >= replacementSet.Length) {
                                    cycleIndex = 0;
                                }
                                vd = replacementSet[cycleIndex];
                            }
                        }
                        break;
                }
                break;
            }
            return vd; // rule executed, exit
        }


        bool CheckConfigRuleMatch(ConnectedVoxelConfigMatch match, int typeIndex) {
            switch (match) {
                case ConnectedVoxelConfigMatch.Empty: return typeIndex == 0;
                case ConnectedVoxelConfigMatch.NotEmpty: return typeIndex > 0;
                case ConnectedVoxelConfigMatch.Equals: return voxelDefinitionTypeIndex == typeIndex;
                case ConnectedVoxelConfigMatch.NotEquals: return voxelDefinitionTypeIndex != typeIndex;
                default: return true;
            }
        }

    }

    public partial class VoxelDefinition : ScriptableObject {
        [NonSerialized]
        public CustomVoxelDefinitionProviderDelegate customVoxelDefinitionProvider;

        [NonSerialized]
        public CustomVoxelDefinitionForRenderingDelegate customVoxelDefinitionForRendering;
    }

}
