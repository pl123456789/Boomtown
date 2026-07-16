using UnityEditor;
using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Master world-generation coordinator.
    ///
    /// This compatibility version uses the currently working
    /// BoomtownTerrainGenerator.Generate(...) method, which already creates:
    /// terrain, paint, river, forest, spawn positions, camera, and NavMesh.
    ///
    /// It then generates the hidden geology asset.
    /// </summary>
    public static class BoomtownWorldGenerator
    {
        public static Terrain GenerateWorld(
            BoomtownMapDefinition mapDefinition)
        {
            if (mapDefinition == null)
            {
                Debug.LogError(
                    "[Boomtown World Generator] No map definition selected.");

                return null;
            }

            Debug.Log(
                $"[Boomtown World Generator] Beginning generation of " +
                $"{mapDefinition.mapName}, {mapDefinition.region}, " +
                $"{mapDefinition.year}.");

            // Uses the currently working terrain generator and all modules
            // already connected inside it.
            Terrain terrain =
                BoomtownTerrainGenerator.Generate(
                    mapDefinition);

            if (terrain == null)
            {
                Debug.LogError(
                    "[Boomtown World Generator] Terrain generation failed.");

                return null;
            }

            // Add the hidden geology asset after the playable world exists.
            BoomtownGeologyGenerator.Generate(
                terrain,
                mapDefinition);

            Selection.activeGameObject =
                terrain.gameObject;

            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[Boomtown World Generator] Finished generating " +
                $"{mapDefinition.mapName}, {mapDefinition.year}.");

            return terrain;
        }
    }
}