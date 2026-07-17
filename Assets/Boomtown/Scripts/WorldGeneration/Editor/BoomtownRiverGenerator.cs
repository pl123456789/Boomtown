using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Generates a deterministic carved river with variable and asymmetrical
    /// left/right widths, then classifies each bank environment.
    ///
    /// One shared RiverSample list drives:
    /// - terrain carving
    /// - visible water mesh
    /// - saved RiverData
    ///
    /// Inside bends widen toward the depositional side while outside bends
    /// stay narrower and steeper. This is the first foundation for gravel bars
    /// and cut banks.
    /// </summary>
    public static class BoomtownRiverGenerator
    {
        private const string GeneratedFolder =
            "Assets/Boomtown/Scripts/WorldGeneration/Generated";

        private const string RiverMaterialPath =
            GeneratedFolder + "/BT_River.mat";

        private const string RiverMeshPath =
            GeneratedFolder + "/BT_Hope_RiverMesh.asset";

        private const string RiverShaderName =
            "Boomtown/Fraser River Water";

        private const int SegmentCount = 120;

        private const float MinimumRiverWidth = 30f;
        private const float MaximumRiverWidth = 118f;

        private const float MaximumAsymmetryFraction = 0.42f;

        private const float BankBlendWidth = 36f;
        private const float WaterLowering = 2.5f;
        private const float BedDepth = 3f;
        private const float SurfaceOffset = 0.08f;

        private const int ProfileSmoothingPasses = 8;
        private const int WidthSmoothingPasses = 6;
        private const int AsymmetrySmoothingPasses = 5;

        public static GameObject Generate(
            Terrain terrain,
            BoomtownMapDefinition mapDefinition)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogError(
                    "[Boomtown River Generator] No valid Terrain supplied.");

                return null;
            }

            if (mapDefinition == null)
            {
                Debug.LogError(
                    "[Boomtown River Generator] No map definition supplied.");

                return null;
            }

            EnsureGeneratedFolderExists();

            string safeMapName =
                MakeSafeName(mapDefinition.mapName);

            string riverObjectName =
                $"GeneratedRiver_{safeMapName}";

            GameObject oldRiver =
                GameObject.Find(riverObjectName);

            if (oldRiver != null)
            {
                UnityEngine.Object.DestroyImmediate(oldRiver);
            }

            AssetDatabase.DeleteAsset(RiverMeshPath);

            List<RiverSample> samples =
                BuildRiverSamples(
                    terrain,
                    mapDefinition.worldSeed);

            CarveTerrain(
                terrain,
                samples);

            terrain.Flush();

            Mesh riverMesh =
                BuildRiverMesh(samples);

            riverMesh.name =
                $"BT_{safeMapName}_RiverMesh";

            AssetDatabase.CreateAsset(
                riverMesh,
                RiverMeshPath);

            Material riverMaterial =
                GetOrCreateRiverMaterial();

            GameObject riverObject =
                new GameObject(riverObjectName);

            MeshFilter meshFilter =
                riverObject.AddComponent<MeshFilter>();

            MeshRenderer meshRenderer =
                riverObject.AddComponent<MeshRenderer>();

            meshFilter.sharedMesh = riverMesh;
            meshRenderer.sharedMaterial = riverMaterial;

            riverObject.AddComponent<
                BoomtownRiverWater>();

            riverObject.transform.position = Vector3.zero;

            riverObject.transform.SetParent(
                BoomtownWorldHierarchy.GetRiversContainer(),
                true);

            RiverData riverData =
                RiverDataBuilder.Save(
                    mapDefinition,
                    samples);

            if (riverData == null)
            {
                Debug.LogWarning(
                    "[Boomtown River Generator] River geometry generated, " +
                    "but RiverData could not be saved.");
            }

            Selection.activeGameObject = riverObject;

            EditorUtility.SetDirty(terrain.terrainData);
            EditorUtility.SetDirty(riverMesh);
            EditorUtility.SetDirty(riverObject);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[Boomtown River Generator] Generated asymmetrical river " +
                $"with {samples.Count} samples for " +
                $"{mapDefinition.mapName}, {mapDefinition.year}.");

            return riverObject;
        }

        private static List<RiverSample> BuildRiverSamples(
            Terrain terrain,
            string seedText)
        {
            TerrainData terrainData =
                terrain.terrainData;

            Vector3 terrainPosition =
                terrain.transform.position;

            Vector3[] centres =
                new Vector3[SegmentCount + 1];

            float[] waterHeights =
                new float[SegmentCount + 1];

            float[] totalWidths =
                new float[SegmentCount + 1];

            float[] asymmetry =
                new float[SegmentCount + 1];

            int seed =
                StableHash(seedText + "_RIVER");

            float widthNoiseOffset =
                Mathf.Abs(seed % 10000) * 0.0137f;

            for (int index = 0;
                 index <= SegmentCount;
                 index++)
            {
                float t =
                    index / (float)SegmentCount;

                float worldZ =
                    terrainPosition.z +
                    t * terrainData.size.z;

                float centreX =
                    BoomtownRiverSpine.GetWorldCentreX(
                        terrain,
                        t,
                        seedText);

                Vector3 samplePoint =
                    new Vector3(
                        centreX,
                        0f,
                        worldZ);

                float originalTerrainY =
                    terrain.SampleHeight(samplePoint) +
                    terrainPosition.y;

                float waterY =
                    originalTerrainY -
                    WaterLowering;

                centres[index] =
                    new Vector3(
                        centreX,
                        waterY,
                        worldZ);

                waterHeights[index] =
                    waterY;

                float broadWidthNoise =
                    Mathf.PerlinNoise(
                        widthNoiseOffset + t * 2.2f,
                        widthNoiseOffset * 0.37f);

                float widthWave =
                    0.5f +
                    0.5f *
                    Mathf.Sin(
                        t * Mathf.PI * 4.2f +
                        seed * 0.0003f);

                float pinchNoise =
                    Mathf.PerlinNoise(
                        widthNoiseOffset +
                        700f +
                        t * 5.6f,
                        widthNoiseOffset * 0.21f);

                float canyonPinch =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        pinchNoise);

                float combinedWidth =
                    broadWidthNoise * 0.58f +
                    widthWave * 0.20f +
                    canyonPinch * 0.22f;

                combinedWidth =
                    Mathf.Pow(
                        Mathf.Clamp01(
                            combinedWidth),
                        1.12f);

                totalWidths[index] =
                    Mathf.Lerp(
                        MinimumRiverWidth,
                        MaximumRiverWidth,
                        combinedWidth);
            }

            SmoothValues(
                waterHeights,
                ProfileSmoothingPasses);

            SmoothValues(
                totalWidths,
                WidthSmoothingPasses);

            for (int index = 0;
                 index < centres.Length;
                 index++)
            {
                centres[index].y =
                    waterHeights[index];
            }

            for (int index = 0;
                 index < centres.Length;
                 index++)
            {
                asymmetry[index] =
                    CalculateSignedBend(
                        centres,
                        index);
            }

            SmoothValues(
                asymmetry,
                AsymmetrySmoothingPasses);

            List<RiverSample> samples =
                new List<RiverSample>(centres.Length);

            for (int index = 0;
                 index < centres.Length;
                 index++)
            {
                Vector3 previous =
                    centres[
                        Mathf.Max(0, index - 1)];

                Vector3 next =
                    centres[
                        Mathf.Min(
                            centres.Length - 1,
                            index + 1)];

                Vector3 tangent =
                    next - previous;

                tangent.y = 0f;

                if (tangent.sqrMagnitude <= 0.0001f)
                {
                    tangent = Vector3.forward;
                }
                else
                {
                    tangent.Normalize();
                }

                float totalWidth =
                    totalWidths[index];

                float signedBend =
                    Mathf.Clamp(
                        asymmetry[index],
                        -1f,
                        1f);

                float sideShift =
                    totalWidth *
                    MaximumAsymmetryFraction *
                    signedBend;

                float leftWidth =
                    totalWidth * 0.5f -
                    sideShift;

                float rightWidth =
                    totalWidth * 0.5f +
                    sideShift;

                float minimumSideWidth =
                    totalWidth * 0.18f;

                leftWidth =
                    Mathf.Max(
                        minimumSideWidth,
                        leftWidth);

                rightWidth =
                    Mathf.Max(
                        minimumSideWidth,
                        rightWidth);

                float correctedTotal =
                    leftWidth + rightWidth;

                if (correctedTotal > 0.001f)
                {
                    float correction =
                        totalWidth / correctedTotal;

                    leftWidth *= correction;
                    rightWidth *= correction;
                }

                float bendStrength =
                    Mathf.Abs(signedBend);

                float widthFactor =
                    Mathf.InverseLerp(
                        MinimumRiverWidth,
                        MaximumRiverWidth,
                        totalWidth);

                float velocity =
                    Mathf.Lerp(
                        2.4f,
                        0.9f,
                        widthFactor);

                velocity =
                    Mathf.Lerp(
                        velocity,
                        velocity * 0.82f,
                        bendStrength);

                float gravelProbability =
                    Mathf.Clamp01(
                        0.18f +
                        bendStrength * 0.50f +
                        widthFactor * 0.26f);

                RiverSample sample =
                    new RiverSample
                    {
                        position = centres[index],
                        tangent = tangent,
                        leftWidth = leftWidth,
                        rightWidth = rightWidth,
                        depth = BedDepth,
                        velocity = velocity,
                        gravelProbability =
                            gravelProbability
                    };

                sample.leftLandscape =
                    RiverLandscapeClassifier.ClassifyLeftBank(
                        sample);

                sample.rightLandscape =
                    RiverLandscapeClassifier.ClassifyRightBank(
                        sample);

                samples.Add(sample);
            }

            return samples;
        }

        private static float CalculateSignedBend(
            Vector3[] centres,
            int index)
        {
            if (index <= 0 ||
                index >= centres.Length - 1)
            {
                return 0f;
            }

            Vector3 incoming =
                centres[index] -
                centres[index - 1];

            Vector3 outgoing =
                centres[index + 1] -
                centres[index];

            incoming.y = 0f;
            outgoing.y = 0f;

            if (incoming.sqrMagnitude <= 0.0001f ||
                outgoing.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            incoming.Normalize();
            outgoing.Normalize();

            float angle =
                Vector3.Angle(
                    incoming,
                    outgoing);

            float bendStrength =
                Mathf.InverseLerp(
                    0f,
                    8f,
                    angle);

            float crossY =
                Vector3.Cross(
                    incoming,
                    outgoing).y;

            float directionSign =
                Mathf.Sign(crossY);

            return bendStrength *
                   directionSign;
        }

        private static void CarveTerrain(
            Terrain terrain,
            IReadOnlyList<RiverSample> samples)
        {
            TerrainData terrainData =
                terrain.terrainData;

            int resolution =
                terrainData.heightmapResolution;

            float[,] heights =
                terrainData.GetHeights(
                    0,
                    0,
                    resolution,
                    resolution);

            Vector3 terrainPosition =
                terrain.transform.position;

            Vector3 terrainSize =
                terrainData.size;

            for (int z = 0;
                 z < resolution;
                 z++)
            {
                float normalizedZ =
                    z / (float)(resolution - 1);

                float worldZ =
                    terrainPosition.z +
                    normalizedZ * terrainSize.z;

                for (int x = 0;
                     x < resolution;
                     x++)
                {
                    float normalizedX =
                        x / (float)(resolution - 1);

                    float worldX =
                        terrainPosition.x +
                        normalizedX * terrainSize.x;

                    Vector2 worldPoint =
                        new Vector2(
                            worldX,
                            worldZ);

                    ClosestRiverPoint closest =
                        FindClosestRiverPoint(
                            worldPoint,
                            samples);

                    float sideWaterWidth =
                        closest.isRightSide
                            ? closest.rightWidth
                            : closest.leftWidth;

                    float outerBankRadius =
                        sideWaterWidth +
                        BankBlendWidth;

                    if (closest.distance >
                        outerBankRadius)
                    {
                        continue;
                    }

                    float currentWorldY =
                        terrainPosition.y +
                        heights[z, x] *
                        terrainSize.y;

                    float bedWorldY =
                        closest.waterY -
                        closest.depth;

                    float targetWorldY;

                    if (closest.distance <=
                        sideWaterWidth)
                    {
                        float centreFactor =
                            Mathf.InverseLerp(
                                0f,
                                sideWaterWidth,
                                closest.distance);

                        float bedCrown =
                            Mathf.SmoothStep(
                                0f,
                                0.55f,
                                centreFactor);

                        targetWorldY =
                            bedWorldY +
                            bedCrown;
                    }
                    else
                    {
                        float bankT =
                            Mathf.InverseLerp(
                                sideWaterWidth,
                                outerBankRadius,
                                closest.distance);

                        float smoothBankT =
                            bankT *
                            bankT *
                            (3f - 2f * bankT);

                        targetWorldY =
                            Mathf.Lerp(
                                bedWorldY + 0.55f,
                                currentWorldY,
                                smoothBankT);
                    }

                    float carvedWorldY =
                        Mathf.Min(
                            currentWorldY,
                            targetWorldY);

                    heights[z, x] =
                        Mathf.Clamp01(
                            (carvedWorldY -
                             terrainPosition.y) /
                            terrainSize.y);
                }
            }

            terrainData.SetHeights(
                0,
                0,
                heights);
        }

        private static Mesh BuildRiverMesh(
            IReadOnlyList<RiverSample> samples)
        {
            const int verticesPerSample = 5;
            const int stripsPerSegment =
                verticesPerSample - 1;

            int vertexCount =
                samples.Count *
                verticesPerSample;

            Vector3[] vertices =
                new Vector3[vertexCount];

            Vector2[] uvs =
                new Vector2[vertexCount];

            Color[] colours =
                new Color[vertexCount];

            int[] triangles =
                new int[
                    (samples.Count - 1) *
                    stripsPerSegment *
                    6];

            float[] crossSection =
            {
                -1f,
                -0.55f,
                0f,
                0.55f,
                1f
            };

            for (int index = 0;
                 index < samples.Count;
                 index++)
            {
                RiverSample sample =
                    samples[index];

                Vector3 centre =
                    sample.position +
                    Vector3.up *
                    SurfaceOffset;

                Vector3 rightDirection =
                    sample.RightDirection;

                float downstreamT =
                    index /
                    (float)(
                        samples.Count - 1);

                float speed01 =
                    Mathf.InverseLerp(
                        0.8f,
                        2.8f,
                        sample.velocity);

                for (int crossIndex = 0;
                     crossIndex <
                     verticesPerSample;
                     crossIndex++)
                {
                    float cross =
                        crossSection[
                            crossIndex];

                    float sideWidth =
                        cross < 0f
                            ? sample.leftWidth
                            : sample.rightWidth;

                    Vector3 position =
                        centre +
                        rightDirection *
                        sideWidth *
                        cross;

                    int vertexIndex =
                        index *
                        verticesPerSample +
                        crossIndex;

                    vertices[vertexIndex] =
                        position;

                    uvs[vertexIndex] =
                        new Vector2(
                            crossIndex /
                            (float)(
                                verticesPerSample -
                                1),
                            downstreamT *
                            14f);

                    float edgeAmount =
                        Mathf.InverseLerp(
                            0.45f,
                            1f,
                            Mathf.Abs(
                                cross));

                    colours[vertexIndex] =
                        new Color(
                            speed01,
                            sample.gravelProbability,
                            edgeAmount,
                            1f);
                }
            }

            int triangleIndex = 0;

            for (int index = 0;
                 index < samples.Count - 1;
                 index++)
            {
                int currentRow =
                    index *
                    verticesPerSample;

                int nextRow =
                    (index + 1) *
                    verticesPerSample;

                for (int strip = 0;
                     strip < stripsPerSegment;
                     strip++)
                {
                    int currentLeft =
                        currentRow +
                        strip;

                    int currentRight =
                        currentLeft + 1;

                    int nextLeft =
                        nextRow +
                        strip;

                    int nextRight =
                        nextLeft + 1;

                    triangles[triangleIndex++] =
                        currentLeft;

                    triangles[triangleIndex++] =
                        nextLeft;

                    triangles[triangleIndex++] =
                        currentRight;

                    triangles[triangleIndex++] =
                        currentRight;

                    triangles[triangleIndex++] =
                        nextLeft;

                    triangles[triangleIndex++] =
                        nextRight;
                }
            }

            Mesh mesh =
                new Mesh
                {
                    vertices = vertices,
                    uv = uvs,
                    colors = colours,
                    triangles = triangles
                };

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static ClosestRiverPoint FindClosestRiverPoint(
            Vector2 point,
            IReadOnlyList<RiverSample> samples)
        {
            float closestDistance =
                float.PositiveInfinity;

            float closestWaterY =
                samples[0].position.y;

            float closestLeftWidth =
                samples[0].leftWidth;

            float closestRightWidth =
                samples[0].rightWidth;

            float closestDepth =
                samples[0].depth;

            bool closestIsRightSide = true;

            for (int index = 0;
                 index < samples.Count - 1;
                 index++)
            {
                RiverSample startSample =
                    samples[index];

                RiverSample endSample =
                    samples[index + 1];

                Vector2 start =
                    new Vector2(
                        startSample.position.x,
                        startSample.position.z);

                Vector2 end =
                    new Vector2(
                        endSample.position.x,
                        endSample.position.z);

                Vector2 segment =
                    end - start;

                float lengthSquared =
                    segment.sqrMagnitude;

                float t =
                    lengthSquared <= Mathf.Epsilon
                        ? 0f
                        : Mathf.Clamp01(
                            Vector2.Dot(
                                point - start,
                                segment) /
                            lengthSquared);

                Vector2 nearest =
                    start +
                    segment * t;

                Vector2 offset =
                    point - nearest;

                float distance =
                    offset.magnitude;

                if (distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = distance;

                closestWaterY =
                    Mathf.Lerp(
                        startSample.position.y,
                        endSample.position.y,
                        t);

                closestLeftWidth =
                    Mathf.Lerp(
                        startSample.leftWidth,
                        endSample.leftWidth,
                        t);

                closestRightWidth =
                    Mathf.Lerp(
                        startSample.rightWidth,
                        endSample.rightWidth,
                        t);

                closestDepth =
                    Mathf.Lerp(
                        startSample.depth,
                        endSample.depth,
                        t);

                Vector2 tangent =
                    segment.sqrMagnitude <= Mathf.Epsilon
                        ? Vector2.up
                        : segment.normalized;

                Vector2 rightDirection =
                    new Vector2(
                        -tangent.y,
                        tangent.x);

                closestIsRightSide =
                    Vector2.Dot(
                        offset,
                        rightDirection) >= 0f;
            }

            return new ClosestRiverPoint(
                closestDistance,
                closestWaterY,
                closestLeftWidth,
                closestRightWidth,
                closestDepth,
                closestIsRightSide);
        }

        private static void SmoothValues(
            float[] values,
            int passes)
        {
            float[] buffer =
                new float[values.Length];

            for (int pass = 0;
                 pass < passes;
                 pass++)
            {
                buffer[0] =
                    values[0];

                buffer[values.Length - 1] =
                    values[values.Length - 1];

                for (int index = 1;
                     index < values.Length - 1;
                     index++)
                {
                    buffer[index] =
                        values[index - 1] * 0.25f +
                        values[index] * 0.5f +
                        values[index + 1] * 0.25f;
                }

                Array.Copy(
                    buffer,
                    values,
                    values.Length);
            }
        }

        private static Material GetOrCreateRiverMaterial()
        {
            Shader shader =
                Shader.Find(
                    RiverShaderName);

            if (shader == null)
            {
                Debug.LogWarning(
                    "[Boomtown River Generator] Custom Fraser River shader " +
                    "was not found. Falling back to URP Lit.");

                shader =
                    Shader.Find(
                        "Universal Render Pipeline/Lit");
            }

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Standard");
            }

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    RiverMaterialPath);

            if (material == null)
            {
                material =
                    new Material(shader);

                AssetDatabase.CreateAsset(
                    material,
                    RiverMaterialPath);
            }
            else if (shader != null &&
                     material.shader != shader)
            {
                material.shader =
                    shader;
            }

            SetColourIfPresent(
                material,
                "_ShallowColor",
                new Color32(
                    88,
                    151,
                    158,
                    210));

            SetColourIfPresent(
                material,
                "_DeepColor",
                new Color32(
                    24,
                    78,
                    101,
                    235));

            SetColourIfPresent(
                material,
                "_FoamColor",
                new Color32(
                    220,
                    232,
                    220,
                    230));

            SetFloatIfPresent(
                material,
                "_Transparency",
                0.82f);

            SetFloatIfPresent(
                material,
                "_FlowSpeed",
                0.42f);

            SetFloatIfPresent(
                material,
                "_WaveScale",
                0.075f);

            SetFloatIfPresent(
                material,
                "_WaveStrength",
                0.12f);

            SetFloatIfPresent(
                material,
                "_ShoreFoam",
                0.72f);

            SetFloatIfPresent(
                material,
                "_RapidFoam",
                0.58f);

            material.renderQueue =
                (int)UnityEngine.Rendering.RenderQueue.Transparent;

            EditorUtility.SetDirty(
                material);

            return material;
        }

        private static void SetFloatIfPresent(
            Material material,
            string property,
            float value)
        {
            if (material != null &&
                material.HasProperty(
                    property))
            {
                material.SetFloat(
                    property,
                    value);
            }
        }

        private static void SetColourIfPresent(
            Material material,
            string property,
            Color value)
        {
            if (material != null &&
                material.HasProperty(
                    property))
            {
                material.SetColor(
                    property,
                    value);
            }
        }

        private static int StableHash(string text)
        {
            unchecked
            {
                int hash = 23;

                foreach (char character
                         in text ?? string.Empty)
                {
                    hash =
                        hash * 31 +
                        character;
                }

                return hash;
            }
        }

        private static string MakeSafeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Map";
            }

            return value
                .Trim()
                .Replace(" ", string.Empty);
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

        private readonly struct ClosestRiverPoint
        {
            public readonly float distance;
            public readonly float waterY;
            public readonly float leftWidth;
            public readonly float rightWidth;
            public readonly float depth;
            public readonly bool isRightSide;

            public ClosestRiverPoint(
                float distance,
                float waterY,
                float leftWidth,
                float rightWidth,
                float depth,
                bool isRightSide)
            {
                this.distance = distance;
                this.waterY = waterY;
                this.leftWidth = leftWidth;
                this.rightWidth = rightWidth;
                this.depth = depth;
                this.isRightSide = isRightSide;
            }
        }
    }
}