using UnityEditor;
using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Paints generated terrain from slope, elevation, and RiverData.
    ///
    /// River classifications influence only the corridor near the water.
    /// Ground farther away keeps the original grass, dirt, and rock rules.
    /// </summary>
    public static class BoomtownTerrainPainter
    {
        private const string GeneratedFolder =
            "Assets/Boomtown/Scripts/WorldGeneration/Generated";

        private const string GrassTexturePath =
            GeneratedFolder + "/BT_Grass_Texture.asset";

        private const string DirtTexturePath =
            GeneratedFolder + "/BT_Dirt_Texture.asset";

        private const string RockTexturePath =
            GeneratedFolder + "/BT_Rock_Texture.asset";

        private const string GrassLayerPath =
            GeneratedFolder + "/BT_Grass_Layer.terrainlayer";

        private const string DirtLayerPath =
            GeneratedFolder + "/BT_Dirt_Layer.terrainlayer";

        private const string RockLayerPath =
            GeneratedFolder + "/BT_Rock_Layer.terrainlayer";

        // River classifications fade back into the normal terrain painting
        // outside this distance from the local water edge.
        private const float RiverInfluenceDistance = 80f;

        /// <summary>
        /// Compatibility overload. This preserves the old call until the
        /// terrain generator is wired to pass RiverData in the next step.
        /// </summary>
        public static void Paint(Terrain terrain)
        {
            Paint(terrain, null);
        }

        /// <summary>
        /// Paints terrain using the generated river landscape where available.
        /// </summary>
        public static void Paint(
            Terrain terrain,
            RiverData riverData)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogError(
                    "[Boomtown Terrain Painter] No valid Terrain was supplied.");

                return;
            }

            EnsureGeneratedFolderExists();

            // Alpha on these generated textures doubles as the terrain
            // shader's smoothness source, so it has to stay low or the
            // whole terrain reads as wet/plastic. Values are roughly
            // smoothness*255 (grass ~0.06, dirt ~0.10, rock ~0.16).
            TerrainLayer grassLayer = GetOrCreateLayer(
                GrassLayerPath,
                GrassTexturePath,
                new Color32(126, 143, 91, 15),
                new Vector2(8f, 8f),
                0.06f);

            TerrainLayer dirtLayer = GetOrCreateLayer(
                DirtLayerPath,
                DirtTexturePath,
                new Color32(118, 89, 58, 26),
                new Vector2(6f, 6f),
                0.10f);

            TerrainLayer rockLayer = GetOrCreateLayer(
                RockLayerPath,
                RockTexturePath,
                new Color32(104, 103, 96, 41),
                new Vector2(10f, 10f),
                0.16f);

            TerrainData terrainData = terrain.terrainData;

            terrainData.terrainLayers = new[]
            {
                grassLayer,
                dirtLayer,
                rockLayer
            };

            int resolution = terrainData.alphamapResolution;

            float[,,] splatMap =
                new float[resolution, resolution, 3];

            Vector3 terrainPosition =
                terrain.transform.position;

            Vector3 terrainSize =
                terrainData.size;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float normalizedX =
                        x / (float)(resolution - 1);

                    float normalizedY =
                        y / (float)(resolution - 1);

                    float normalizedHeight =
                        terrainData.GetInterpolatedHeight(
                            normalizedX,
                            normalizedY) /
                        terrainData.size.y;

                    float slope =
                        terrainData.GetSteepness(
                            normalizedX,
                            normalizedY);

                    float rockWeight =
                        Mathf.InverseLerp(
                            22f,
                            42f,
                            slope);

                    float lowAreaWeight =
                        1f -
                        Mathf.InverseLerp(
                            0.12f,
                            0.28f,
                            normalizedHeight);

                    float flatAreaWeight =
                        1f -
                        Mathf.InverseLerp(
                            12f,
                            28f,
                            slope);

                    float dirtWeight =
                        lowAreaWeight *
                        flatAreaWeight *
                        0.55f;

                    float grassWeight =
                        Mathf.Max(
                            0.01f,
                            1f -
                            rockWeight -
                            dirtWeight);

                    if (riverData != null &&
                        RiverLandscapeQuery.TryGetNearest(
                            riverData,
                            new Vector3(
                                terrainPosition.x +
                                normalizedX * terrainSize.x,
                                terrainPosition.y +
                                normalizedHeight * terrainSize.y,
                                terrainPosition.z +
                                normalizedY * terrainSize.z),
                            out RiverLandscapeQueryResult riverResult))
                    {
                        ApplyRiverLandscape(
                            riverResult,
                            ref grassWeight,
                            ref dirtWeight,
                            ref rockWeight);
                    }

                    NormalizeWeights(
                        ref grassWeight,
                        ref dirtWeight,
                        ref rockWeight);

                    splatMap[y, x, 0] = grassWeight;
                    splatMap[y, x, 1] = dirtWeight;
                    splatMap[y, x, 2] = rockWeight;
                }
            }

            terrainData.SetAlphamaps(
                0,
                0,
                splatMap);

            EditorUtility.SetDirty(terrainData);
            AssetDatabase.SaveAssets();

            Debug.Log(
                riverData == null
                    ? "[Boomtown Terrain Painter] Applied automatic grass, " +
                      "dirt, and rock painting without RiverData."
                    : "[Boomtown Terrain Painter] Applied RiverData-driven " +
                      "grass, gravel/dirt, and rock painting.");
        }

        private static void ApplyRiverLandscape(
            RiverLandscapeQueryResult result,
            ref float grassWeight,
            ref float dirtWeight,
            ref float rockWeight)
        {
            float influence =
                1f -
                Mathf.InverseLerp(
                    0f,
                    RiverInfluenceDistance,
                    result.distanceFromWaterEdge);

            if (result.isInsideActiveChannel)
            {
                influence = 1f;
            }

            if (influence <= 0f)
            {
                return;
            }

            float targetGrass;
            float targetDirt;
            float targetRock;

            switch (result.landscape)
            {
                case RiverLandscapeType.ActiveChannel:
                    targetGrass = 0.02f;
                    targetDirt = 0.68f;
                    targetRock = 0.30f;
                    break;

                case RiverLandscapeType.GravelBar:
                    targetGrass = 0.08f;
                    targetDirt = 0.82f;
                    targetRock = 0.10f;
                    break;

                case RiverLandscapeType.CutBank:
                    targetGrass = 0.05f;
                    targetDirt = 0.28f;
                    targetRock = 0.67f;
                    break;

                case RiverLandscapeType.Floodplain:
                    targetGrass = 0.72f;
                    targetDirt = 0.25f;
                    targetRock = 0.03f;
                    break;

                case RiverLandscapeType.BedrockMargin:
                    targetGrass = 0.03f;
                    targetDirt = 0.12f;
                    targetRock = 0.85f;
                    break;

                case RiverLandscapeType.Terrace:
                default:
                    // Terraces preserve most of the normal terrain paint.
                    targetGrass = grassWeight;
                    targetDirt = dirtWeight;
                    targetRock = rockWeight;
                    influence *= 0.20f;
                    break;
            }

            float smoothInfluence =
                influence *
                influence *
                (3f - 2f * influence);

            grassWeight =
                Mathf.Lerp(
                    grassWeight,
                    targetGrass,
                    smoothInfluence);

            dirtWeight =
                Mathf.Lerp(
                    dirtWeight,
                    targetDirt,
                    smoothInfluence);

            rockWeight =
                Mathf.Lerp(
                    rockWeight,
                    targetRock,
                    smoothInfluence);
        }

        private static void NormalizeWeights(
            ref float grassWeight,
            ref float dirtWeight,
            ref float rockWeight)
        {
            grassWeight = Mathf.Max(0f, grassWeight);
            dirtWeight = Mathf.Max(0f, dirtWeight);
            rockWeight = Mathf.Max(0f, rockWeight);

            float total =
                grassWeight +
                dirtWeight +
                rockWeight;

            if (total <= 0.0001f)
            {
                grassWeight = 1f;
                dirtWeight = 0f;
                rockWeight = 0f;
                return;
            }

            grassWeight /= total;
            dirtWeight /= total;
            rockWeight /= total;
        }

        private static TerrainLayer GetOrCreateLayer(
            string layerPath,
            string texturePath,
            Color32 colour,
            Vector2 tileSize,
            float smoothness)
        {
            TerrainLayer layer =
                AssetDatabase.LoadAssetAtPath<TerrainLayer>(
                    layerPath);

            if (layer == null)
            {
                layer = new TerrainLayer();

                AssetDatabase.CreateAsset(
                    layer,
                    layerPath);
            }

            Texture2D texture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    texturePath);

            bool isNewTexture =
                texture == null;

            if (isNewTexture)
            {
                texture =
                    new Texture2D(
                        4,
                        4,
                        TextureFormat.RGBA32,
                        false)
                    {
                        wrapMode = TextureWrapMode.Repeat,
                        filterMode = FilterMode.Bilinear,
                        name = "GeneratedTerrainColour"
                    };
            }

            // Always re-fill the pixels (not just on first creation) so
            // tuning the colour/alpha here takes effect on projects that
            // already generated these assets under the old values.
            FillSolidTexture(texture, colour);

            if (isNewTexture)
            {
                AssetDatabase.CreateAsset(
                    texture,
                    texturePath);
            }
            else
            {
                EditorUtility.SetDirty(texture);
            }

            layer.diffuseTexture = texture;
            layer.tileSize = tileSize;
            layer.tileOffset = Vector2.zero;
            layer.smoothness = smoothness;
            layer.metallic = 0f;

            EditorUtility.SetDirty(layer);

            return layer;
        }

        private static void FillSolidTexture(
            Texture2D texture,
            Color32 colour)
        {
            Color32[] pixels =
                new Color32[16];

            for (int i = 0;
                 i < pixels.Length;
                 i++)
            {
                pixels[i] = colour;
            }

            texture.SetPixels32(pixels);
            texture.Apply();
        }

        private static void EnsureGeneratedFolderExists()
        {
            const string root =
                "Assets/Boomtown/Scripts/WorldGeneration";

            if (!AssetDatabase.IsValidFolder(root))
            {
                if (!AssetDatabase.IsValidFolder(
                        "Assets/Boomtown/Scripts"))
                {
                    AssetDatabase.CreateFolder(
                        "Assets/Boomtown",
                        "Scripts");
                }

                AssetDatabase.CreateFolder(
                    "Assets/Boomtown/Scripts",
                    "WorldGeneration");
            }

            if (!AssetDatabase.IsValidFolder(
                    GeneratedFolder))
            {
                AssetDatabase.CreateFolder(
                    root,
                    "Generated");
            }
        }
    }
}