using UnityEngine;

namespace Boomtown.WorldGeneration
{
    /// <summary>
    /// Shared runtime queries for generated river landscape data.
    ///
    /// Terrain painting, prospecting, vegetation, roads, wildlife, and
    /// settlement systems can all use this class instead of independently
    /// guessing where the river or its banks are.
    /// </summary>
    public static class RiverLandscapeQuery
    {
        /// <summary>
        /// Finds the nearest river sample and classifies which side of the
        /// river the supplied world position lies on.
        /// </summary>
        public static bool TryGetNearest(
            RiverData riverData,
            Vector3 worldPosition,
            out RiverLandscapeQueryResult result)
        {
            result = default;

            if (riverData == null ||
                riverData.samples == null ||
                riverData.samples.Count == 0)
            {
                return false;
            }

            float closestDistanceSquared =
                float.PositiveInfinity;

            RiverSample closestSample =
                riverData.samples[0];

            for (int index = 0;
                 index < riverData.samples.Count;
                 index++)
            {
                RiverSample sample =
                    riverData.samples[index];

                Vector2 sampleXZ =
                    new Vector2(
                        sample.position.x,
                        sample.position.z);

                Vector2 worldXZ =
                    new Vector2(
                        worldPosition.x,
                        worldPosition.z);

                float distanceSquared =
                    (worldXZ - sampleXZ).sqrMagnitude;

                if (distanceSquared >= closestDistanceSquared)
                {
                    continue;
                }

                closestDistanceSquared =
                    distanceSquared;

                closestSample =
                    sample;
            }

            Vector3 offset =
                worldPosition -
                closestSample.position;

            offset.y = 0f;

            float signedSideDistance =
                Vector3.Dot(
                    offset,
                    closestSample.RightDirection);

            bool isRightSide =
                signedSideDistance >= 0f;

            float distanceFromCentreline =
                Mathf.Abs(signedSideDistance);

            float localWaterEdgeDistance =
                isRightSide
                    ? Mathf.Max(
                        0f,
                        distanceFromCentreline -
                        closestSample.rightWidth)
                    : Mathf.Max(
                        0f,
                        distanceFromCentreline -
                        closestSample.leftWidth);

            RiverLandscapeType landscape =
                isRightSide
                    ? closestSample.rightLandscape
                    : closestSample.leftLandscape;

            bool insideActiveChannel =
                isRightSide
                    ? distanceFromCentreline <=
                      closestSample.rightWidth
                    : distanceFromCentreline <=
                      closestSample.leftWidth;

            if (insideActiveChannel)
            {
                landscape =
                    RiverLandscapeType.ActiveChannel;
            }

            result =
                new RiverLandscapeQueryResult
                {
                    sample = closestSample,
                    landscape = landscape,
                    isRightSide = isRightSide,
                    isInsideActiveChannel =
                        insideActiveChannel,
                    distanceFromCentreline =
                        distanceFromCentreline,
                    distanceFromWaterEdge =
                        localWaterEdgeDistance
                };

            return true;
        }

        /// <summary>
        /// Returns the interpreted landscape type nearest to a world position.
        /// Falls back to Terrace when no RiverData is available.
        /// </summary>
        public static RiverLandscapeType GetLandscape(
            RiverData riverData,
            Vector3 worldPosition)
        {
            return TryGetNearest(
                    riverData,
                    worldPosition,
                    out RiverLandscapeQueryResult result)
                ? result.landscape
                : RiverLandscapeType.Terrace;
        }

        /// <summary>
        /// Returns the distance in metres from a world position to the nearest
        /// local water edge.
        /// </summary>
        public static float GetDistanceToWater(
            RiverData riverData,
            Vector3 worldPosition)
        {
            return TryGetNearest(
                    riverData,
                    worldPosition,
                    out RiverLandscapeQueryResult result)
                ? result.distanceFromWaterEdge
                : float.PositiveInfinity;
        }
    }

    /// <summary>
    /// Full result returned by RiverLandscapeQuery.
    /// </summary>
    public struct RiverLandscapeQueryResult
    {
        public RiverSample sample;
        public RiverLandscapeType landscape;
        public bool isRightSide;
        public bool isInsideActiveChannel;
        public float distanceFromCentreline;
        public float distanceFromWaterEdge;
    }
}