using UnityEngine;

namespace Boomtown.WorldGeneration
{
    public enum GoldRevealMode
    {
        Original,
        Remaining,
        Recovered
    }

    /// <summary>
    /// Debug/end-game overlay that visualizes hidden finite placer gold.
    /// This component is not required during normal gameplay.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BoomtownGoldRevealOverlay : MonoBehaviour
    {
        [SerializeField]
        private BoomtownGeologyData geologyData;

        [SerializeField]
        private Terrain targetTerrain;

        [SerializeField]
        private GoldRevealMode revealMode =
            GoldRevealMode.Original;

        [SerializeField, Range(0.05f, 1f)]
        private float opacity = 0.72f;

        [SerializeField, Range(0.25f, 8f)]
        private float intensity = 2.5f;

        [SerializeField, Min(0.01f)]
        private float surfaceOffset = 0.85f;

        [SerializeField, Range(0, 10)]
        private int revealSpread = 5;

        [SerializeField, Range(0f, 1f)]
        private float minimumVisibleAlpha = 0.18f;

        [SerializeField]
        private Material overlayMaterial;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh generatedMesh;
        private Material runtimeMaterial;

        public BoomtownGeologyData GeologyData =>
            geologyData;

        public Terrain TargetTerrain =>
            targetTerrain;

        public GoldRevealMode RevealMode =>
            revealMode;

        public void Configure(
            BoomtownGeologyData geology,
            Terrain terrain,
            Material material,
            GoldRevealMode mode,
            float newOpacity,
            float newIntensity,
            int newRevealSpread,
            float newMinimumVisibleAlpha)
        {
            geologyData = geology;
            targetTerrain = terrain;
            overlayMaterial = material;
            revealMode = mode;
            opacity = newOpacity;
            intensity = newIntensity;
            revealSpread =
                Mathf.Clamp(
                    newRevealSpread,
                    0,
                    10);

            minimumVisibleAlpha =
                Mathf.Clamp01(
                    newMinimumVisibleAlpha);

            Rebuild();
        }

        public void SetMode(
            GoldRevealMode mode)
        {
            revealMode = mode;
            RebuildColours();
        }

        public void SetOpacity(
            float value)
        {
            opacity =
                Mathf.Clamp01(value);

            ApplyMaterialSettings();
        }

        public void SetIntensity(
            float value)
        {
            intensity =
                Mathf.Max(0.01f, value);

            RebuildColours();
        }

        [ContextMenu("Rebuild Gold Overlay")]
        public void Rebuild()
        {
            if (geologyData == null ||
                targetTerrain == null ||
                !geologyData.IsValid)
            {
                return;
            }

            EnsureComponents();
            RebuildMesh();
            ApplyMaterialSettings();
        }

        private void OnEnable()
        {
            if (geologyData != null &&
                targetTerrain != null)
            {
                Rebuild();
            }
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            Rebuild();
        }

        private void EnsureComponents()
        {
            meshFilter =
                GetComponent<MeshFilter>();

            if (meshFilter == null)
            {
                meshFilter =
                    gameObject.AddComponent<
                        MeshFilter>();
            }

            meshRenderer =
                GetComponent<MeshRenderer>();

            if (meshRenderer == null)
            {
                meshRenderer =
                    gameObject.AddComponent<
                        MeshRenderer>();
            }

            if (overlayMaterial != null)
            {
                if (runtimeMaterial == null ||
                    runtimeMaterial.shader !=
                    overlayMaterial.shader)
                {
                    DestroyRuntimeMaterial();

                    runtimeMaterial =
                        new Material(
                            overlayMaterial)
                        {
                            name =
                                "BT_GoldRevealOverlay_Runtime"
                        };
                }

                meshRenderer.sharedMaterial =
                    runtimeMaterial;
            }
        }

        private void RebuildMesh()
        {
            int resolutionX =
                geologyData.resolutionX;

            int resolutionZ =
                geologyData.resolutionZ;

            int vertexCount =
                resolutionX *
                resolutionZ;

            Vector3[] vertices =
                new Vector3[vertexCount];

            Vector2[] uvs =
                new Vector2[vertexCount];

            Color[] colours =
                new Color[vertexCount];

            int[] triangles =
                new int[
                    (resolutionX - 1) *
                    (resolutionZ - 1) *
                    6];

            Vector3 terrainOrigin =
                targetTerrain.transform.position;

            Vector3 terrainSize =
                targetTerrain.terrainData.size;

            for (int z = 0;
                 z < resolutionZ;
                 z++)
            {
                float nz =
                    z /
                    (float)(
                        resolutionZ - 1);

                for (int x = 0;
                     x < resolutionX;
                     x++)
                {
                    float nx =
                        x /
                        (float)(
                            resolutionX - 1);

                    int index =
                        z *
                        resolutionX +
                        x;

                    float worldX =
                        terrainOrigin.x +
                        nx * terrainSize.x;

                    float worldZ =
                        terrainOrigin.z +
                        nz * terrainSize.z;

                    float worldY =
                        targetTerrain.SampleHeight(
                            new Vector3(
                                worldX,
                                0f,
                                worldZ)) +
                        terrainOrigin.y +
                        surfaceOffset;

                    vertices[index] =
                        transform.InverseTransformPoint(
                            new Vector3(
                                worldX,
                                worldY,
                                worldZ));

                    uvs[index] =
                        new Vector2(nx, nz);

                    colours[index] =
                        EvaluateColour(index);
                }
            }

            int triangleIndex = 0;

            for (int z = 0;
                 z < resolutionZ - 1;
                 z++)
            {
                for (int x = 0;
                     x < resolutionX - 1;
                     x++)
                {
                    int lowerLeft =
                        z *
                        resolutionX +
                        x;

                    int lowerRight =
                        lowerLeft + 1;

                    int upperLeft =
                        lowerLeft +
                        resolutionX;

                    int upperRight =
                        upperLeft + 1;

                    triangles[triangleIndex++] =
                        lowerLeft;

                    triangles[triangleIndex++] =
                        upperLeft;

                    triangles[triangleIndex++] =
                        lowerRight;

                    triangles[triangleIndex++] =
                        lowerRight;

                    triangles[triangleIndex++] =
                        upperLeft;

                    triangles[triangleIndex++] =
                        upperRight;
                }
            }

            if (generatedMesh == null)
            {
                generatedMesh =
                    new Mesh
                    {
                        name =
                            "BT_GoldRevealOverlayMesh"
                    };

                generatedMesh.indexFormat =
                    UnityEngine.Rendering
                        .IndexFormat.UInt32;
            }
            else
            {
                generatedMesh.Clear();
            }

            generatedMesh.vertices =
                vertices;

            generatedMesh.uv =
                uvs;

            generatedMesh.colors =
                colours;

            generatedMesh.triangles =
                triangles;

            generatedMesh.RecalculateNormals();
            generatedMesh.RecalculateBounds();

            meshFilter.sharedMesh =
                generatedMesh;
        }

        private void RebuildColours()
        {
            if (generatedMesh == null ||
                geologyData == null ||
                !geologyData.IsValid)
            {
                Rebuild();
                return;
            }

            Color[] colours =
                new Color[
                    generatedMesh.vertexCount];

            for (int index = 0;
                 index < colours.Length;
                 index++)
            {
                colours[index] =
                    EvaluateColour(index);
            }

            generatedMesh.colors =
                colours;

            ApplyMaterialSettings();
        }

        private Color EvaluateColour(
            int index)
        {
            int centreX =
                index %
                geologyData.resolutionX;

            int centreZ =
                index /
                geologyData.resolutionX;

            float weightedOunces = 0f;
            float strongestOunces = 0f;
            float totalWeight = 0f;

            int radius =
                Mathf.Max(
                    0,
                    revealSpread);

            for (int offsetZ = -radius;
                 offsetZ <= radius;
                 offsetZ++)
            {
                int sampleZ =
                    centreZ +
                    offsetZ;

                if (sampleZ < 0 ||
                    sampleZ >=
                    geologyData.resolutionZ)
                {
                    continue;
                }

                for (int offsetX = -radius;
                     offsetX <= radius;
                     offsetX++)
                {
                    int sampleX =
                        centreX +
                        offsetX;

                    if (sampleX < 0 ||
                        sampleX >=
                        geologyData.resolutionX)
                    {
                        continue;
                    }

                    float distance =
                        Mathf.Sqrt(
                            offsetX * offsetX +
                            offsetZ * offsetZ);

                    if (distance >
                        radius +
                        0.001f)
                    {
                        continue;
                    }

                    int sampleIndex =
                        sampleZ *
                        geologyData.resolutionX +
                        sampleX;

                    float sampleOunces =
                        GetModeOunces(
                            sampleIndex);

                    if (sampleOunces <= 0f)
                    {
                        continue;
                    }

                    float weight =
                        radius <= 0
                            ? 1f
                            : Mathf.Pow(
                                1f -
                                Mathf.Clamp01(
                                    distance /
                                    (radius + 0.5f)),
                                1.65f);

                    weightedOunces +=
                        sampleOunces *
                        weight;

                    totalWeight +=
                        weight;

                    strongestOunces =
                        Mathf.Max(
                            strongestOunces,
                            sampleOunces *
                            weight);
                }
            }

            float displayOunces =
                Mathf.Max(
                    strongestOunces,
                    totalWeight > 0f
                        ? weightedOunces /
                          totalWeight
                        : 0f);

            if (displayOunces <= 0.000001f)
            {
                return Color.clear;
            }

            float scaled =
                Mathf.Log10(
                    1f +
                    displayOunces *
                    intensity);

            float normalized =
                Mathf.Clamp01(
                    scaled /
                    1.65f);

            Color colour;

            if (normalized < 0.28f)
            {
                colour =
                    Color.Lerp(
                        new Color(
                            0.65f,
                            0.10f,
                            0.00f,
                            0.30f),
                        new Color(
                            1f,
                            0.32f,
                            0.00f,
                            0.62f),
                        normalized /
                        0.28f);
            }
            else if (normalized < 0.68f)
            {
                colour =
                    Color.Lerp(
                        new Color(
                            1f,
                            0.32f,
                            0.00f,
                            0.62f),
                        new Color(
                            1f,
                            0.88f,
                            0.04f,
                            0.90f),
                        (normalized - 0.28f) /
                        0.40f);
            }
            else
            {
                colour =
                    Color.Lerp(
                        new Color(
                            1f,
                            0.88f,
                            0.04f,
                            0.90f),
                        new Color(
                            1f,
                            1f,
                            0.92f,
                            1f),
                        (normalized - 0.68f) /
                        0.32f);
            }

            colour.a =
                Mathf.Max(
                    minimumVisibleAlpha,
                    colour.a) *
                opacity;

            return colour;
        }

        private float GetModeOunces(
            int index)
        {
            float initial =
                geologyData.placerInitialOunces != null &&
                index <
                geologyData.placerInitialOunces.Length
                    ? Mathf.Max(
                        0f,
                        geologyData
                            .placerInitialOunces[index])
                    : 0f;

            float remaining =
                geologyData.placerRemainingOunces != null &&
                index <
                geologyData.placerRemainingOunces.Length
                    ? Mathf.Max(
                        0f,
                        geologyData
                            .placerRemainingOunces[index])
                    : 0f;

            switch (revealMode)
            {
                case GoldRevealMode.Remaining:
                    return remaining;

                case GoldRevealMode.Recovered:
                    return Mathf.Max(
                        0f,
                        initial - remaining);

                case GoldRevealMode.Original:
                default:
                    return initial;
            }
        }

        private void ApplyMaterialSettings()
        {
            if (runtimeMaterial == null)
            {
                return;
            }

            if (runtimeMaterial.HasProperty(
                    "_GlobalOpacity"))
            {
                runtimeMaterial.SetFloat(
                    "_GlobalOpacity",
                    opacity);
            }
        }

        private void OnDestroy()
        {
            if (generatedMesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(generatedMesh);
                }
                else
                {
                    DestroyImmediate(generatedMesh);
                }
            }

            DestroyRuntimeMaterial();
        }

        private void DestroyRuntimeMaterial()
        {
            if (runtimeMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(runtimeMaterial);
            }
            else
            {
                DestroyImmediate(runtimeMaterial);
            }

            runtimeMaterial = null;
        }
    }
}
