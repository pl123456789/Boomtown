using System.Collections.Generic;
using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    public static class BoomtownGoldSourceGenerator
    {
        public static void Generate(
            Terrain terrain,
            BoomtownMapDefinition mapDefinition,
            BoomtownGeologyData geology)
        {
            geology.goldMountains = new List<GoldMountainData>();
            geology.quartzVeins = new List<QuartzVeinData>();

            int seed = StableHash(mapDefinition.worldSeed + "_HARD_ROCK");
            System.Random random = new(seed);
            List<Candidate> candidates = BuildCandidates(terrain, random);
            candidates.Sort((a, b) => b.score.CompareTo(a.score));

            int mountainCount = random.Next(1, 3);
            int nextVeinId = 0;

            for (int m = 0; m < mountainCount && m < candidates.Count; m++)
            {
                Candidate c = candidates[m];
                GoldMountainData mountain = new()
                {
                    id = m,
                    displayName = $"Mineralized Mountain {m + 1}",
                    centre = c.position,
                    radius = Mathf.Lerp(180f, 420f, (float)random.NextDouble()),
                    elevation = c.position.y,
                    mineralization = Mathf.Lerp(0.42f, 0.95f, (float)random.NextDouble())
                };

                geology.goldMountains.Add(mountain);

                int veinCount = random.Next(3, 7);
                for (int v = 0; v < veinCount; v++)
                {
                    QuartzVeinData vein = CreateVein(
                        terrain, mountain, nextVeinId++, random);

                    geology.quartzVeins.Add(vein);
                    mountain.quartzVeinIds.Add(vein.id);
                    mountain.totalHardRockGoldOunces += vein.initialGoldOunces;
                    mountain.remainingHardRockGoldOunces += vein.remainingGoldOunces;
                }
            }

            Debug.Log(
                $"[Gold Source Generator] Generated {geology.goldMountains.Count} " +
                $"mineralized mountain region(s), {geology.quartzVeins.Count} quartz veins, " +
                $"{geology.GetTotalInitialHardRockOunces():0.00} oz finite hard-rock gold.");
        }

        private static List<Candidate> BuildCandidates(
            Terrain terrain,
            System.Random random)
        {
            List<Candidate> result = new();
            TerrainData data = terrain.terrainData;
            Vector3 origin = terrain.transform.position;

            for (int i = 0; i < 24; i++)
            {
                float nx = Mathf.Lerp(0.06f, 0.94f, (float)random.NextDouble());
                float nz = Mathf.Lerp(0.06f, 0.94f, (float)random.NextDouble());
                if (Mathf.Abs(nx - 0.5f) < 0.18f) continue;

                float height = data.GetInterpolatedHeight(nx, nz);
                float slope = data.GetSteepness(nx, nz);
                float score =
                    (height / data.size.y) * 0.70f +
                    Mathf.InverseLerp(10f, 48f, slope) * 0.30f;

                result.Add(new Candidate(
                    new Vector3(
                        origin.x + nx * data.size.x,
                        origin.y + height,
                        origin.z + nz * data.size.z),
                    score));
            }

            return result;
        }

        private static QuartzVeinData CreateVein(
            Terrain terrain,
            GoldMountainData mountain,
            int id,
            System.Random random)
        {
            float angle = Mathf.Lerp(0f, Mathf.PI * 2f, (float)random.NextDouble());
            float length = Mathf.Lerp(120f, 620f, (float)random.NextDouble());
            Vector3 direction = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 lateral = new(-direction.z, 0f, direction.x);

            float offset = Mathf.Lerp(
                -mountain.radius * 0.42f,
                mountain.radius * 0.42f,
                (float)random.NextDouble());

            Vector3 centre = mountain.centre + lateral * offset;
            Vector3 start = centre - direction * length * 0.5f;
            Vector3 end = centre + direction * length * 0.5f;

            start.y = terrain.SampleHeight(start) + terrain.transform.position.y;
            end.y = terrain.SampleHeight(end) + terrain.transform.position.y;

            float width = Mathf.Lerp(0.35f, 3.8f, (float)random.NextDouble());
            float depth = Mathf.Lerp(25f, 260f, (float)random.NextDouble());
            float grade =
                Mathf.Lerp(0.04f, 0.95f, Mathf.Pow((float)random.NextDouble(), 2.2f)) *
                Mathf.Lerp(0.70f, 1.25f, mountain.mineralization);

            float oreTons = length * width *
                Mathf.Lerp(35f, 120f, (float)random.NextDouble());

            float initialGold = Mathf.Max(25f, oreTons * grade);
            float exposed = Mathf.Lerp(0.02f, 0.24f, (float)random.NextDouble());

            return new QuartzVeinData
            {
                id = id,
                mountainId = mountain.id,
                start = start,
                end = end,
                widthMetres = width,
                depthMetres = depth,
                gradeOuncesPerTon = grade,
                initialGoldOunces = initialGold,
                remainingGoldOunces = initialGold,
                exposedFraction = exposed,
                historicalWeatheredFraction = Mathf.Lerp(0.005f, 0.075f, exposed)
            };
        }

        private static int StableHash(string text)
        {
            unchecked
            {
                int hash = 23;
                foreach (char c in text ?? string.Empty)
                    hash = hash * 31 + c;
                return hash;
            }
        }

        private readonly struct Candidate
        {
            public readonly Vector3 position;
            public readonly float score;

            public Candidate(Vector3 position, float score)
            {
                this.position = position;
                this.score = score;
            }
        }
    }
}
