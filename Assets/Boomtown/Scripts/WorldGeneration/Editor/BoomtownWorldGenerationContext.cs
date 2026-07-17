using System;
using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Shared state passed through the generation pipeline.
    /// </summary>
    public sealed class BoomtownWorldGenerationContext
    {
        public BoomtownMapDefinition MapDefinition { get; }
        public DistrictData DistrictData { get; }

        public Terrain Terrain { get; set; }
        public RiverData RiverData { get; set; }
        public BoomtownGeologyData GeologyData { get; set; }

        public string GenerationId { get; }

        public BoomtownWorldGenerationContext(
            BoomtownMapDefinition mapDefinition,
            DistrictData districtData)
        {
            MapDefinition =
                mapDefinition ??
                throw new ArgumentNullException(
                    nameof(mapDefinition));

            DistrictData =
                districtData ??
                throw new ArgumentNullException(
                    nameof(districtData));

            GenerationId =
                Guid.NewGuid().ToString("N");
        }
    }
}
