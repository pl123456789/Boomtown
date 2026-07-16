using System.Collections.Generic;
using UnityEngine;

namespace Boomtown.WorldGeneration
{
    /// <summary>
    /// Stores all generated river samples for a map.
    /// Generated automatically each time a world is built.
    /// </summary>
    [CreateAssetMenu(
        fileName = "RiverData",
        menuName = "Boomtown/World Generation/River Data")]
    public class RiverData : ScriptableObject
    {
        public List<RiverSample> samples = new();
    }
}