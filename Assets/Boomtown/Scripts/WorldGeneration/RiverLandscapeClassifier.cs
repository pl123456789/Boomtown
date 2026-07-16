using UnityEngine;

namespace Boomtown.WorldGeneration
{
    /// <summary>
    /// Interprets physical RiverSample data as landscape environments.
    ///
    /// This class does not modify terrain, paint textures, place vegetation,
    /// or generate gold. It only translates physical river conditions into
    /// shared landscape categories for later systems.
    /// </summary>
    public static class RiverLandscapeClassifier
    {
        /// <summary>
        /// Classifies the left side of a river sample.
        /// </summary>
        public static RiverLandscapeType ClassifyLeftBank(
            RiverSample sample)
        {
            return ClassifyBank(
                sample,
                sample.leftWidth,
                sample.rightWidth);
        }

        /// <summary>
        /// Classifies the right side of a river sample.
        /// </summary>
        public static RiverLandscapeType ClassifyRightBank(
            RiverSample sample)
        {
            return ClassifyBank(
                sample,
                sample.rightWidth,
                sample.leftWidth);
        }

        /// <summary>
        /// Classifies the active water corridor itself.
        /// </summary>
        public static RiverLandscapeType ClassifyChannel(
            RiverSample sample)
        {
            return RiverLandscapeType.ActiveChannel;
        }

        private static RiverLandscapeType ClassifyBank(
            RiverSample sample,
            float thisSideWidth,
            float oppositeSideWidth)
        {
            float totalWidth =
                Mathf.Max(
                    0.01f,
                    sample.TotalWidth);

            float sideShare =
                thisSideWidth / totalWidth;

            bool stronglyWiderSide =
                sideShare >= 0.60f;

            bool stronglyNarrowerSide =
                sideShare <= 0.40f;

            bool slowDepositionalWater =
                sample.velocity <= 1.35f;

            bool gravelFriendly =
                sample.gravelProbability >= 0.55f;

            if (stronglyWiderSide &&
                (gravelFriendly ||
                 slowDepositionalWater))
            {
                return RiverLandscapeType.GravelBar;
            }

            if (stronglyNarrowerSide &&
                sample.velocity >= 1.45f)
            {
                return RiverLandscapeType.CutBank;
            }

            if (sample.depth >= 4.5f &&
                sample.velocity >= 1.7f)
            {
                return RiverLandscapeType.BedrockMargin;
            }

            if (sample.velocity <= 1.1f &&
                sample.gravelProbability < 0.45f)
            {
                return RiverLandscapeType.Floodplain;
            }

            return RiverLandscapeType.Terrace;
        }
    }
}