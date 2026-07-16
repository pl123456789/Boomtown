using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    public static class BoomtownWorldHierarchy
    {
        public const string WorldRootName = "World";
        public const string TerrainContainerName = "Terrain";
        public const string RiversContainerName = "Rivers";
        public const string ForestContainerName = "Forest";
        public const string SettlementsContainerName = "Settlements";

        public static GameObject Rebuild(BoomtownMapDefinition mapDefinition)
        {
            GameObject oldWorld = GameObject.Find(WorldRootName);

            if (oldWorld != null)
            {
                Object.DestroyImmediate(oldWorld);
            }

            RemoveLegacyGeneratedObjects(mapDefinition);
            return CreateHierarchy();
        }

        public static GameObject GetOrCreateWorld()
        {
            GameObject world = GameObject.Find(WorldRootName);
            return world != null ? world : CreateHierarchy();
        }

        public static Transform GetTerrainContainer()
        {
            return GetOrCreateContainer(TerrainContainerName);
        }

        public static Transform GetRiversContainer()
        {
            return GetOrCreateContainer(RiversContainerName);
        }

        public static Transform GetForestContainer()
        {
            return GetOrCreateContainer(ForestContainerName);
        }

        public static Transform GetSettlementsContainer()
        {
            return GetOrCreateContainer(SettlementsContainerName);
        }

        private static GameObject CreateHierarchy()
        {
            GameObject world = new GameObject(WorldRootName);
            world.transform.position = Vector3.zero;
            world.transform.rotation = Quaternion.identity;
            world.transform.localScale = Vector3.one;

            CreateContainer(world.transform, TerrainContainerName);
            CreateContainer(world.transform, RiversContainerName);
            CreateContainer(world.transform, ForestContainerName);
            CreateContainer(world.transform, SettlementsContainerName);

            return world;
        }

        private static Transform GetOrCreateContainer(string containerName)
        {
            GameObject world = GetOrCreateWorld();
            Transform existing = world.transform.Find(containerName);

            return existing != null
                ? existing
                : CreateContainer(world.transform, containerName);
        }

        private static Transform CreateContainer(
            Transform parent,
            string containerName)
        {
            GameObject container = new GameObject(containerName);
            container.transform.SetParent(parent, false);
            container.transform.localPosition = Vector3.zero;
            container.transform.localRotation = Quaternion.identity;
            container.transform.localScale = Vector3.one;
            return container.transform;
        }

        private static void RemoveLegacyGeneratedObjects(
            BoomtownMapDefinition mapDefinition)
        {
            if (mapDefinition == null)
            {
                return;
            }

            string safeMapName = MakeSafeName(mapDefinition.mapName);

            DestroyIfPresent($"GeneratedTerrain_{safeMapName}");
            DestroyIfPresent($"GeneratedRiver_{safeMapName}");
            DestroyIfPresent($"GeneratedForest_{safeMapName}");
        }

        private static void DestroyIfPresent(string objectName)
        {
            GameObject target = GameObject.Find(objectName);

            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }

        private static string MakeSafeName(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "Map"
                : value.Trim().Replace(" ", string.Empty);
        }
    }
}
