using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Positions Bill and Ted on the generated terrain and rebuilds
    /// the NavMesh after world generation.
    /// </summary>
    public static class BoomtownWorldSpawnGenerator
    {
        private const float BillOffsetFromRiver = 120f;
        private const float TedOffsetFromRiver = 128f;
        private const float TedForwardOffset = 8f;
        private const float SurfaceOffset = 1.05f;

        public static void Generate(
            Terrain terrain,
            BoomtownMapDefinition mapDefinition)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogError(
                    "[Boomtown World Spawn] No valid generated Terrain supplied.");

                return;
            }

            TerrainData terrainData = terrain.terrainData;
            Vector3 terrainOrigin = terrain.transform.position;

            float spawnZ =
                terrainOrigin.z +
                terrainData.size.z * 0.28f;

            float riverCentreX =
                terrainOrigin.x +
                terrainData.size.x * 0.5f;

            Vector3 billPosition =
                GetTerrainPosition(
                    terrain,
                    riverCentreX + BillOffsetFromRiver,
                    spawnZ);

            Vector3 tedPosition =
                GetTerrainPosition(
                    terrain,
                    riverCentreX + TedOffsetFromRiver,
                    spawnZ + TedForwardOffset);

            GameObject bill = GameObject.Find("Bill");
            GameObject ted = GameObject.Find("Ted");

            DeactivateCharacter(bill);
            DeactivateCharacter(ted);

            PositionCharacter(
                bill,
                billPosition,
                "Bill");

            PositionCharacter(
                ted,
                tedPosition,
                "Ted");

            NavMeshSurface surface =
                terrain.GetComponent<NavMeshSurface>();

            if (surface == null)
            {
                surface =
                    terrain.gameObject.AddComponent<NavMeshSurface>();
            }

            surface.useGeometry =
                NavMeshCollectGeometry.PhysicsColliders;

            surface.collectObjects =
                CollectObjects.All;

            surface.BuildNavMesh();

            ReactivateAndPlaceOnNavMesh(
                bill,
                billPosition,
                "Bill");

            ReactivateAndPlaceOnNavMesh(
                ted,
                tedPosition,
                "Ted");

            EditorUtility.SetDirty(surface);

            Debug.Log(
                $"[Boomtown World Spawn] Positioned Bill and Ted and rebuilt " +
                $"the NavMesh for {mapDefinition.mapName}.");
        }

        private static void DeactivateCharacter(
            GameObject character)
        {
            if (character == null)
            {
                return;
            }

            character.SetActive(false);
        }

        private static void PositionCharacter(
            GameObject character,
            Vector3 position,
            string characterName)
        {
            if (character == null)
            {
                Debug.LogWarning(
                    $"[Boomtown World Spawn] Could not find {characterName} " +
                    "in the active scene.");

                return;
            }

            character.transform.position = position;
        }

        private static void ReactivateAndPlaceOnNavMesh(
            GameObject character,
            Vector3 requestedPosition,
            string characterName)
        {
            if (character == null)
            {
                return;
            }

            character.SetActive(true);

            NavMeshAgent agent =
                character.GetComponent<NavMeshAgent>();

            if (NavMesh.SamplePosition(
                    requestedPosition,
                    out NavMeshHit hit,
                    20f,
                    NavMesh.AllAreas))
            {
                character.transform.position = hit.position;

                if (agent != null &&
                    agent.enabled)
                {
                    agent.Warp(hit.position);
                }
            }
            else
            {
                character.transform.position = requestedPosition;

                Debug.LogWarning(
                    $"[Boomtown World Spawn] Could not find a NavMesh point " +
                    $"near {characterName}. Used terrain position instead.");
            }

            EditorUtility.SetDirty(character);
        }

        private static Vector3 GetTerrainPosition(
            Terrain terrain,
            float worldX,
            float worldZ)
        {
            Vector3 samplePoint =
                new Vector3(
                    worldX,
                    0f,
                    worldZ);

            float worldY =
                terrain.SampleHeight(samplePoint) +
                terrain.transform.position.y +
                SurfaceOffset;

            return new Vector3(
                worldX,
                worldY,
                worldZ);
        }
    }
}