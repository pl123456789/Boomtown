using UnityEngine;

namespace Boomtown.WorldGeneration
{
    /// <summary>
    /// Smoothly levels a circular footprint of terrain to a flat plateau,
    /// for sitting a building down without it clipping into a slope or
    /// floating above one. Same technique as the world generator's rock
    /// shelves: average a neighbourhood for the target height (not one
    /// possibly-unrepresentative pixel) and blend outward with a smooth
    /// falloff instead of a hard edge, so there's no visible seam or
    /// "stupid plop" where the building meets the surrounding ground.
    ///
    /// Plain runtime-safe utility (no UnityEditor dependency) so it can be
    /// used both from Editor placement tools and, later, from in-game
    /// building placement.
    /// </summary>
    public static class BoomtownTerrainFootprintFlattener
    {
        /// <summary>
        /// Flattens the terrain under worldPosition. Everything within
        /// plateauRadius (metres) becomes flat at the sampled ground
        /// height; the flattening fades out smoothly over the next
        /// blendRadius metres so the transition back to natural terrain
        /// reads as a gentle grade rather than a cliff or step.
        /// </summary>
        /// <returns>
        /// The world-space height the footprint was flattened to -- use
        /// this to ground the building itself.
        /// </returns>
        public static float FlattenFootprint(
            Terrain terrain,
            Vector3 worldPosition,
            float plateauRadius,
            float blendRadius)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogError(
                    "[Terrain Footprint Flattener] No valid Terrain supplied.");

                return worldPosition.y;
            }

            TerrainData terrainData = terrain.terrainData;
            Vector3 terrainPosition = terrain.transform.position;
            int resolution = terrainData.heightmapResolution;

            float normalizedCentreX =
                (worldPosition.x - terrainPosition.x) /
                terrainData.size.x;

            float normalizedCentreZ =
                (worldPosition.z - terrainPosition.z) /
                terrainData.size.z;

            float totalNormalizedRadiusX =
                (plateauRadius + blendRadius) /
                terrainData.size.x;

            float totalNormalizedRadiusZ =
                (plateauRadius + blendRadius) /
                terrainData.size.z;

            int centrePixelX = Mathf.RoundToInt(
                normalizedCentreX * (resolution - 1));

            int centrePixelY = Mathf.RoundToInt(
                normalizedCentreZ * (resolution - 1));

            int pixelRadiusX = Mathf.CeilToInt(
                totalNormalizedRadiusX * (resolution - 1));

            int pixelRadiusY = Mathf.CeilToInt(
                totalNormalizedRadiusZ * (resolution - 1));

            int minX = Mathf.Clamp(centrePixelX - pixelRadiusX, 0, resolution - 1);
            int minY = Mathf.Clamp(centrePixelY - pixelRadiusY, 0, resolution - 1);
            int maxX = Mathf.Clamp(centrePixelX + pixelRadiusX, 0, resolution - 1);
            int maxY = Mathf.Clamp(centrePixelY + pixelRadiusY, 0, resolution - 1);

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;

            if (width <= 0 || height <= 0)
            {
                return worldPosition.y;
            }

            float[,] heights = terrainData.GetHeights(minX, minY, width, height);

            float targetHeight = SampleNeighbourhoodAverage(
                terrainData,
                centrePixelX,
                centrePixelY,
                Mathf.Max(2, Mathf.RoundToInt(
                    (plateauRadius / terrainData.size.x) * (resolution - 1) * 0.5f)));

            float plateauNormRadiusX = plateauRadius / terrainData.size.x;
            float plateauNormRadiusZ = plateauRadius / terrainData.size.z;
            float blendNormRadiusX = Mathf.Max(0.0001f, blendRadius / terrainData.size.x);
            float blendNormRadiusZ = Mathf.Max(0.0001f, blendRadius / terrainData.size.z);

            for (int y = 0; y < height; y++)
            {
                int pixelY = minY + y;
                float normalizedY = pixelY / (float)(resolution - 1);
                float dz = Mathf.Abs(normalizedY - normalizedCentreZ);

                for (int x = 0; x < width; x++)
                {
                    int pixelX = minX + x;
                    float normalizedX = pixelX / (float)(resolution - 1);
                    float dx = Mathf.Abs(normalizedX - normalizedCentreX);

                    // Ellipse-normalised distance: 0 at centre, 1 at the
                    // edge of the flat plateau, 2 at the outer edge of the
                    // blend ring.
                    float plateauDistance = Mathf.Sqrt(
                        Sqr(dx / Mathf.Max(0.0001f, plateauNormRadiusX)) +
                        Sqr(dz / Mathf.Max(0.0001f, plateauNormRadiusZ)));

                    if (plateauDistance <= 1f)
                    {
                        heights[y, x] = targetHeight;
                        continue;
                    }

                    float blendDistance = Mathf.Sqrt(
                        Sqr(dx / (plateauNormRadiusX + blendNormRadiusX)) +
                        Sqr(dz / (plateauNormRadiusZ + blendNormRadiusZ)));

                    if (blendDistance >= 1f)
                    {
                        continue;
                    }

                    // Smoothstep the blend ring so the join reads as a
                    // gentle grade, not a visible ring or step.
                    float blendT = Mathf.InverseLerp(1f, 0f, blendDistance);
                    blendT = blendT * blendT * (3f - 2f * blendT);

                    heights[y, x] = Mathf.Lerp(heights[y, x], targetHeight, blendT);
                }
            }

            terrainData.SetHeights(minX, minY, heights);

            return targetHeight * terrainData.size.y + terrainPosition.y;
        }

        private static float SampleNeighbourhoodAverage(
            TerrainData terrainData,
            int centrePixelX,
            int centrePixelY,
            int radius)
        {
            int resolution = terrainData.heightmapResolution;

            int minX = Mathf.Clamp(centrePixelX - radius, 0, resolution - 1);
            int minY = Mathf.Clamp(centrePixelY - radius, 0, resolution - 1);
            int maxX = Mathf.Clamp(centrePixelX + radius, 0, resolution - 1);
            int maxY = Mathf.Clamp(centrePixelY + radius, 0, resolution - 1);

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;

            float[,] sample = terrainData.GetHeights(minX, minY, width, height);

            float total = 0f;
            int count = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    total += sample[y, x];
                    count++;
                }
            }

            return count > 0
                ? total / count
                : terrainData.GetHeights(centrePixelX, centrePixelY, 1, 1)[0, 0];
        }

        private static float Sqr(float value)
        {
            return value * value;
        }
    }
}
