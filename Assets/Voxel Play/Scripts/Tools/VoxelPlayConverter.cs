﻿using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

    /// <summary>
    /// A neutral / engine independent model definition. Basically an array of colors which specify voxels inside a model with specified size and optional center offset
    /// </summary>
    public struct ColorBasedModelDefinition {
        public string name;
        public int sizeX, sizeY, sizeZ;
        public int offsetX, offsetY, offsetZ;

        /// <summary>
        /// Colors are arranged in Y/Z/X structure
        /// </summary>
        public Color32[] colors;

        public static ColorBasedModelDefinition Null = new ColorBasedModelDefinition();
    }

    public static class VoxelPlayConverter {

        struct Cuboid {
            public Vector3 min, max;
            public Color32 color;
            public int textureIndex;
            public bool deleted;
        }

        struct Face {
            public Vector3 center;
            public Vector3 size;
            public Vector3[] vertices;
            public Vector3[] normals;
            public Color32 color;
            public int textureIndex;

            public Face(Vector3 center, Vector3 size, Vector3[] vertices, Vector3[] normals, Color32 color, int textureIndex) {
                this.center = center;
                this.size = size;
                this.vertices = vertices;
                this.normals = normals;
                this.color = color;
                this.textureIndex = textureIndex;
            }


            public static bool operator ==(Face f1, Face f2) {
                return f1.size == f2.size && f1.center == f2.center;
            }

            public static bool operator !=(Face f1, Face f2) {
                return f1.size != f2.size || f1.center != f2.center;
            }

            public override bool Equals(object obj) {
                if (obj == null || !(obj is Face))
                    return false;
                Face other = (Face)obj;
                return size == other.size && center == other.center;
            }

            public override int GetHashCode() {
                unchecked {
                    int hash = 23;
                    hash = hash * 31 + center.GetHashCode();
                    hash = hash * 31 + size.GetHashCode();
                    return hash;
                }
            }
        }

        static readonly Vector3[] faceVerticesForward =  {
            new Vector3 (0.5f, -0.5f, 0.5f),
            new Vector3 (0.5f, 0.5f, 0.5f),
            new Vector3 (-0.5f, -0.5f, 0.5f),
            new Vector3 (-0.5f, 0.5f, 0.5f)
        };
        static readonly Vector3[] faceVerticesBack = {
            new Vector3 (-0.5f, -0.5f, -0.5f),
            new Vector3 (-0.5f, 0.5f, -0.5f),
            new Vector3 (0.5f, -0.5f, -0.5f),
            new Vector3 (0.5f, 0.5f, -0.5f)
        };
        static readonly Vector3[] faceVerticesLeft = {
            new Vector3 (-0.5f, -0.5f, 0.5f),
            new Vector3 (-0.5f, 0.5f, 0.5f),
            new Vector3 (-0.5f, -0.5f, -0.5f),
            new Vector3 (-0.5f, 0.5f, -0.5f)
        };
        static readonly Vector3[] faceVerticesRight ={
            new Vector3 (0.5f, -0.5f, -0.5f),
            new Vector3 (0.5f, 0.5f, -0.5f),
            new Vector3 (0.5f, -0.5f, 0.5f),
            new Vector3 (0.5f, 0.5f, 0.5f)
        };
        static readonly Vector3[] faceVerticesTop =  {
            new Vector3 (-0.5f, 0.5f, 0.5f),
            new Vector3 (0.5f, 0.5f, 0.5f),
            new Vector3 (-0.5f, 0.5f, -0.5f),
            new Vector3 (0.5f, 0.5f, -0.5f)
        };
        static readonly Vector3[] faceVerticesBottom = {
            new Vector3 (-0.5f, -0.5f, -0.5f),
            new Vector3 (0.5f, -0.5f, -0.5f),
            new Vector3 (-0.5f, -0.5f, 0.5f),
            new Vector3 (0.5f, -0.5f, 0.5f)
        };
        static readonly Vector3[] normalsBack =  {
            Misc.vector3back, Misc.vector3back, Misc.vector3back, Misc.vector3back
        };
        static readonly Vector3[] normalsForward = {
            Misc.vector3forward, Misc.vector3forward, Misc.vector3forward, Misc.vector3forward
        };
        static readonly Vector3[] normalsLeft = {
            Misc.vector3left, Misc.vector3left, Misc.vector3left, Misc.vector3left
        };
        static readonly Vector3[] normalsRight = {
            Misc.vector3right, Misc.vector3right, Misc.vector3right, Misc.vector3right
        };
        static readonly Vector3[] normalsUp = {
            Misc.vector3up, Misc.vector3up, Misc.vector3up, Misc.vector3up
        };
        static readonly Vector3[] normalsDown = {
            Misc.vector3down, Misc.vector3down, Misc.vector3down, Misc.vector3down
        };
        static readonly Vector3[] faceUVs =  {
            new Vector3 (0, 0, 0), new Vector3 (0, 1, 0), new Vector3 (1, 0, 0), new Vector3 (1, 1, 0)
        };


        public static ModelDefinition GetModelDefinition(VoxelDefinition voxelTemplate, ColorBasedModelDefinition model, bool ignoreOffset, ColorToVoxelMap colorMap = null) {
            ModelDefinition md = ScriptableObject.CreateInstance<ModelDefinition>();
            md.sizeX = model.sizeX;
            md.sizeY = model.sizeY;
            md.sizeZ = model.sizeZ;
            if (!ignoreOffset) {
                md.offsetX = model.offsetX;
                md.offsetY = model.offsetY;
                md.offsetZ = model.offsetZ;
            }
            if (colorMap != null && voxelTemplate == null) {
                voxelTemplate = colorMap.defaultVoxelDefinition;

            }
            List<ModelBit> bits = new List<ModelBit>();
            for (int y = 0; y < model.sizeY; y++) {
                int posy = y * model.sizeX * model.sizeZ;
                for (int z = 0; z < model.sizeZ; z++) {
                    int posz = z * model.sizeX;
                    for (int x = 0; x < model.sizeX; x++) {
                        int index = posy + posz + x;
                        if (model.colors[index].a > 0) {
                            ModelBit bit = new ModelBit();
                            bit.voxelIndex = index;
                            if (colorMap != null) {
                                bit.voxelDefinition = colorMap.GetVoxelDefinition(model.colors[index], voxelTemplate);
                                bit.color = Misc.color32White;
                            } else {
                                bit.voxelDefinition = voxelTemplate;
                                bit.color = model.colors[index];
                            }
                            bits.Add(bit);
                        }
                    }
                }
            }
            md.SetBits(bits.ToArray());
            return md;
        }

        public static ColorToVoxelMap GetColorToVoxelMapDefinition(ColorBasedModelDefinition model, bool ignoreTransparency = true) {
            ColorToVoxelMap mapping = ScriptableObject.CreateInstance<ColorToVoxelMap>();
            List<Color32> uniqueColors = new List<Color32>();
            Color32 prevColor = Misc.color32Transparent;
            for (int k = 0; k < model.colors.Length; k++) {
                Color32 color = model.colors[k];
                if (color.a == 0) continue;
                if (color.r == prevColor.r && color.g == prevColor.g && color.b == prevColor.b)
                    continue;
                if (ignoreTransparency) {
                    color.a = 255;
                }
                if (!uniqueColors.Contains(color)) {
                    uniqueColors.Add(color);
                }
            }
            int colorCount = uniqueColors.Count;
            mapping.colorMap = new ColorToVoxelMapEntry[colorCount];
            for (int k = 0; k < colorCount; k++) {
                mapping.colorMap[k].color = uniqueColors[k];
            }
            return mapping;
        }


        static readonly List<Vector3> vertices = new List<Vector3>();
        static readonly List<int> indices = new List<int>();
        static readonly List<Vector3> uvs = new List<Vector3>();
        static readonly List<Vector3> normals = new List<Vector3>();
        static readonly List<Color32> meshColors = new List<Color32>();
        static Cuboid[] cuboids = new Cuboid[128];
        static Material vertexLitMat;

        public static GameObject GenerateVoxelObject(Color32[] colors, int sizeX, int sizeY, int sizeZ, Vector3 offset, Vector3 scale, bool skipTransparentColors = true, int alphaCutoutThreshold = 128) {
            return GenerateVoxelObject(colors, null, null, sizeX, sizeY, sizeZ, offset, scale, skipTransparentColors: skipTransparentColors, alphaCutoutThreshold: alphaCutoutThreshold);
        }

        static void Encapsulate(int k, Vector3 pointMin, Vector3 pointMax) {
            if (pointMin.x < cuboids[k].min.x) cuboids[k].min.x = pointMin.x;
            else if (pointMax.x > cuboids[k].max.x) cuboids[k].max.x = pointMax.x;
            if (pointMin.y < cuboids[k].min.y) cuboids[k].min.y = pointMin.y;
            else if (pointMax.y > cuboids[k].max.y) cuboids[k].max.y = pointMax.y;
            if (pointMin.z < cuboids[k].min.z) cuboids[k].min.z = pointMin.z;
            else if (pointMax.z > cuboids[k].max.z) cuboids[k].max.z = pointMax.z;
        }

        public static GameObject GenerateVoxelObject(Color32[] colors, Texture2D[] textures, Texture2D[] normalMaps, int sizeX, int sizeY, int sizeZ, Vector3 offset, Vector3 scale, bool skipTransparentColors = true, int alphaCutoutThreshold = 128) {

            // Pack textures & bumpMaps
            int textureSize = 0;
            int[] textureIndices = new int[sizeX * sizeY * sizeZ];
            bool useTextures = textures != null;
            if (useTextures) {
                for (int k = 0; k < textures.Length; k++) {
                    if (textures[k] != null) {
                        textureSize = textures[k].width;
                        break;
                    }
                }
                useTextures = textureSize > 0;
            }
            bool useNormalMaps = useTextures && normalMaps != null && normalMaps.Length == textures.Length;
            Texture2DArray texArray = null;
            if (useTextures) {
                TextureProviderSettings settings = TextureProviderSettings.Create(textureSize, 1, useNormalMaps, enableReliefMap: false, null);
                TextureArrayPacker packer = new TextureArrayPacker(settings);
                for (int k = 0; k < textures.Length; k++) {
                    Texture2D tex = textures[k];
                    if (tex != null) {
                        Texture2D texNRM = useNormalMaps ? normalMaps[k] : null;
                        textureIndices[k] = packer.AddTexture(tex, null, texNRM, null);
                    }
                }
                packer.CreateTextureArray();
                texArray = packer.textureArray;
            }

            int index;
            int ONE_Y_ROW = sizeZ * sizeX;
            int ONE_Z_ROW = sizeX;

            Cuboid cuboid = new Cuboid();
            int cuboidsCount = 0;
            for (int y = 0; y < sizeY; y++) {
                int posy = y * ONE_Y_ROW;
                for (int z = 0; z < sizeZ; z++) {
                    int posz = z * ONE_Z_ROW;
                    for (int x = 0; x < sizeX; x++) {
                        index = posy + posz + x;
                        Color32 color = colors[index];
                        if (!skipTransparentColors || color.a >= alphaCutoutThreshold) {
                            cuboid.min.x = x - sizeX / 2f;
                            cuboid.min.y = y;
                            cuboid.min.z = z - sizeZ / 2f;
                            cuboid.max.x = cuboid.min.x + 1;
                            cuboid.max.y = cuboid.min.y + 1;
                            cuboid.max.z = cuboid.min.z + 1;
                            cuboid.color = color;
                            cuboid.textureIndex = textureIndices != null ? textureIndices[index] : 0;
                            if (cuboidsCount >= cuboids.Length) {
                                Cuboid[] newCuboids = new Cuboid[cuboidsCount * 2];
                                System.Array.Copy(cuboids, newCuboids, cuboids.Length);
                                cuboids = newCuboids;
                            }
                            cuboids[cuboidsCount++] = cuboid;
                        }
                    }
                }
            }

            // Optimization 1: Fusion same color cuboids
            bool repeat = true;
            while (repeat) {
                repeat = false;
                for (int k = 0; k < cuboidsCount; k++) {
                    if (cuboids[k].deleted)
                        continue;
                    Vector3 f1min = cuboids[k].min;
                    Vector3 f1max = cuboids[k].max;
                    for (int j = k + 1; j < cuboidsCount; j++) {
                        if (cuboids[j].deleted)
                            continue;
                        if (cuboids[k].textureIndex == cuboids[j].textureIndex && cuboids[k].color.r == cuboids[j].color.r && cuboids[k].color.g == cuboids[j].color.g && cuboids[k].color.b == cuboids[j].color.b) {
                            bool touching = false;
                            Vector3 f2min = cuboids[j].min;
                            Vector3 f2max = cuboids[j].max;
                            // Touching back or forward faces?
                            if (f1min.x == f2min.x && f1max.x == f2max.x && f1min.y == f2min.y && f1max.y == f2max.y) {
                                touching = f1min.z == f2max.z || f1max.z == f2min.z;
                                // ... left or right faces?
                            } else if (f1min.z == f2min.z && f1max.z == f2max.z && f1min.y == f2min.y && f1max.y == f2max.y) {
                                touching = f1min.x == f2max.x || f1max.x == f2min.x;
                                // ... top or bottom faces?
                            } else if (f1min.x == f2min.x && f1max.x == f2max.x && f1min.z == f2min.z && f1max.z == f2max.z) {
                                touching = f1min.y == f2max.y || f1max.y == f2min.y;
                            }
                            if (touching) {
                                Encapsulate(k, cuboids[j].min, cuboids[j].max);
                                //cuboids [k].bounds.Encapsulate (f2);
                                f1min = cuboids[k].min;
                                f1max = cuboids[k].max;

                                cuboids[j].deleted = true;
                                repeat = true;
                            }
                        }
                    }
                }
            }

            // Optimization 2: Remove hidden cuboids
            for (int k = 0; k < cuboidsCount; k++) {
                if (cuboids[k].deleted)
                    continue;
                Vector3 f1min = cuboids[k].min;
                Vector3 f1max = cuboids[k].max;
                int occlusion = 0;
                for (int j = k + 1; j < cuboidsCount; j++) {
                    if (cuboids[j].deleted)
                        continue;
                    Vector3 f2min = cuboids[j].min;
                    Vector3 f2max = cuboids[j].max;
                    // Touching back or forward faces?
                    if (f1min.x >= f2min.x && f1max.x <= f2max.x && f1min.y >= f2min.y && f1max.y <= f2max.y) {
                        if (f1min.z == f2max.z)
                            occlusion++;
                        if (f1max.z == f2min.z)
                            occlusion++;
                        // ... left or right faces?
                    } else if (f1min.z >= f2min.z && f1max.z <= f2max.z && f1min.y >= f2min.y && f1max.y <= f2max.y) {
                        if (f1min.x == f2max.x)
                            occlusion++;
                        if (f1max.x == f2min.x)
                            occlusion++;
                        // ... top or bottom faces?
                    } else if (f1min.x >= f2min.x && f1max.x <= f2max.x && f1min.z >= f2min.z && f1max.z <= f2max.z) {
                        if (f1min.y == f2max.y)
                            occlusion++;
                        if (f1max.y == f2min.y)
                            occlusion++;
                    }
                    if (occlusion == 6) {
                        cuboids[k].deleted = true;
                        break;
                    }
                }
            }

            // Optimization 3: Fragment cuboids into faces and remove duplicates
            HashSet<Face> faces = new HashSet<Face>();
            for (int k = 0; k < cuboidsCount; k++) {
                if (cuboids[k].deleted)
                    continue;
                Vector3 min = cuboids[k].min;
                Vector3 max = cuboids[k].max;
                Vector3 size = max - min;
                Face top = new Face(new Vector3((min.x + max.x) * 0.5f, max.y, (min.z + max.z) * 0.5f), new Vector3(size.x, 0, size.z), faceVerticesTop, normalsUp, cuboids[k].color, cuboids[k].textureIndex);
                RemoveDuplicateOrAddFace(faces, top);
                Face bottom = new Face(new Vector3((min.x + max.x) * 0.5f, min.y, (min.z + max.z) * 0.5f), new Vector3(size.x, 0, size.z), faceVerticesBottom, normalsDown, cuboids[k].color, cuboids[k].textureIndex);
                RemoveDuplicateOrAddFace(faces, bottom);
                Face left = new Face(new Vector3(min.x, (min.y + max.y) * 0.5f, (min.z + max.z) * 0.5f), new Vector3(0, size.y, size.z), faceVerticesLeft, normalsLeft, cuboids[k].color, cuboids[k].textureIndex);
                RemoveDuplicateOrAddFace(faces, left);
                Face right = new Face(new Vector3(max.x, (min.y + max.y) * 0.5f, (min.z + max.z) * 0.5f), new Vector3(0, size.y, size.z), faceVerticesRight, normalsRight, cuboids[k].color, cuboids[k].textureIndex);
                RemoveDuplicateOrAddFace(faces, right);
                Face back = new Face(new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, min.z), new Vector3(size.x, size.y, 0), faceVerticesBack, normalsBack, cuboids[k].color, cuboids[k].textureIndex);
                RemoveDuplicateOrAddFace(faces, back);
                Face forward = new Face(new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, max.z), new Vector3(size.x, size.y, 0), faceVerticesForward, normalsForward, cuboids[k].color, cuboids[k].textureIndex);
                RemoveDuplicateOrAddFace(faces, forward);
            }

            // Create geometry & uv mapping
            vertices.Clear();
            uvs.Clear();
            indices.Clear();
            normals.Clear();
            meshColors.Clear();
            index = 0;
            foreach (Face face in faces) {
                Vector3 faceVertex;
                for (int j = 0; j < 4; j++) {
                    faceVertex.x = (face.center.x + face.vertices[j].x * face.size.x) * scale.x + offset.x;
                    faceVertex.y = (face.center.y + face.vertices[j].y * face.size.y) * scale.y + offset.y;
                    faceVertex.z = (face.center.z + face.vertices[j].z * face.size.z) * scale.z + offset.z;
                    vertices.Add(faceVertex);
                    meshColors.Add(face.color);
                    faceUVs[j].z = face.textureIndex;
                    uvs.Add(faceUVs[j]);
                }
                normals.AddRange(face.normals);
                indices.Add(index);
                indices.Add(index + 1);
                indices.Add(index + 2);
                indices.Add(index + 3);
                indices.Add(index + 2);
                indices.Add(index + 1);

                index += 4;
            }

            Mesh mesh = new Mesh();
            if (!Application.isMobilePlatform) {
                // support for very big models on desktop only - mobile support is not guaranteed! To fully support mobile, including MALI-400 GPUs,
                // we should partition the mesh into several submeshes of up to 65535 vertices each
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }
            mesh.SetVertices(vertices);
            if (textureIndices != null) {
                mesh.SetUVs(0, uvs);
            }
            mesh.SetNormals(normals);
            mesh.SetTriangles(indices, 0);
            mesh.SetColors(meshColors);

            GameObject obj = new GameObject("Model");
            MeshFilter mf = obj.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            MeshRenderer mr = obj.AddComponent<MeshRenderer>();

            Material mat;
            if (useNormalMaps) {
                mat = new Material(Shader.Find("Voxel Play/Models/Texture Array/Opaque"));
                mat.SetTexture(ShaderParams.MainTex, texArray);
                mat.EnableKeyword(VoxelPlayEnvironment.SKW_VOXELPLAY_USE_NORMAL);
            } else if (useTextures) {
                mat = new Material(Shader.Find("Voxel Play/Models/Texture Array/Opaque"));
                mat.SetTexture(ShaderParams.MainTex, texArray);
            } else {
                if (vertexLitMat == null) {
                    vertexLitMat = Object.Instantiate(Resources.Load<Material>("VoxelPlay/Materials/VP Model VertexLit"));
                    vertexLitMat.DisableKeyword(VoxelPlayEnvironment.SKW_VOXELPLAY_GPU_INSTANCING); // keyword is set by Voxel Play at runtime
                }
                mat = vertexLitMat;
            }
            mr.sharedMaterial = mat;

            return obj;
        }

        static void RemoveDuplicateOrAddFace(HashSet<Face> faces, Face face) {
            if (faces.Contains(face)) {
                faces.Remove(face);
            } else {
                faces.Add(face);
            }
        }


        /// <summary>
        /// Generates a gameobject from a model definition. Currently it does not convert textures.
        /// </summary>
        /// <param name="usesTextures">Use textures from the voxel definitions</param>
        /// <param name="usesNormalMaps">Use normal maps from the voxel definitions</param>
        public static GameObject GenerateVoxelObject(ModelDefinition modelDefinition, Vector3 offset, Vector3 scale, bool useTextures = true, bool useNormalMaps = true) {

            int sizeY = modelDefinition.sizeY;
            int sizeZ = modelDefinition.sizeZ;
            int sizeX = modelDefinition.sizeX;
            Color32[] colors = new Color32[sizeY * sizeZ * sizeX];
            Texture2D[] textures = null;
            Texture2D[] normalMaps = null;
            for (int k = 0; k < modelDefinition.bits.Length; k++) {
                if (modelDefinition.bits[k].isEmpty) {
                    continue;
                }
                int voxelIndex = modelDefinition.bits[k].voxelIndex;
                if (voxelIndex >= 0 && voxelIndex < colors.Length) {
                    VoxelDefinition vd = modelDefinition.bits[k].voxelDefinition;
                    colors[voxelIndex] = modelDefinition.bits[k].finalColor;
                    if (vd != null) {
                        if (useTextures && vd.textureSide != null) {
                            if (textures == null) {
                                textures = new Texture2D[colors.Length];
                            }
                            textures[voxelIndex] = vd.textureSide;
                        }
                        if (useNormalMaps && vd.textureSideNRM != null) {
                            if (normalMaps == null) {
                                normalMaps = new Texture2D[colors.Length];
                            }
                            normalMaps[voxelIndex] = vd.textureSideNRM;
                        }
                    }
                }
            }
            return GenerateVoxelObject(colors, textures, normalMaps, sizeX, sizeY, sizeZ, offset, scale, true);
        }

    }

}
