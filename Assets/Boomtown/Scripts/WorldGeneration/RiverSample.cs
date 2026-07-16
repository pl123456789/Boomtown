using System;
using UnityEngine;

namespace Boomtown.WorldGeneration
{
    /// <summary>
    /// Stores one physical sample along a generated river.
    ///
    /// River samples contain physical river facts plus the interpreted
    /// landscape classification for each bank.
    /// </summary>
    [Serializable]
    public struct RiverSample
    {
        [Tooltip("World-space position of the river centre at this sample.")]
        public Vector3 position;

        [Tooltip("Normalized direction the river travels at this sample.")]
        public Vector3 tangent;

        [Tooltip("Distance in metres from the centreline to the left water edge.")]
        [Min(0f)]
        public float leftWidth;

        [Tooltip("Distance in metres from the centreline to the right water edge.")]
        [Min(0f)]
        public float rightWidth;

        [Tooltip("Approximate water depth in metres at this sample.")]
        [Min(0f)]
        public float depth;

        [Tooltip("Approximate water velocity in metres per second.")]
        [Min(0f)]
        public float velocity;

        [Tooltip("Likelihood from 0 to 1 that gravel accumulates near this sample.")]
        [Range(0f, 1f)]
        public float gravelProbability;

        [Header("Interpreted River Landscape")]

        [Tooltip("Landscape environment on the left side of the river.")]
        public RiverLandscapeType leftLandscape;

        [Tooltip("Landscape environment on the right side of the river.")]
        public RiverLandscapeType rightLandscape;

        public float TotalWidth => leftWidth + rightWidth;

        public Vector3 RightDirection
        {
            get
            {
                Vector3 flatTangent =
                    new Vector3(tangent.x, 0f, tangent.z);

                if (flatTangent.sqrMagnitude <= 0.0001f)
                {
                    return Vector3.right;
                }

                flatTangent.Normalize();

                return new Vector3(
                    -flatTangent.z,
                    0f,
                    flatTangent.x);
            }
        }

        public Vector3 LeftBankPosition =>
            position - RightDirection * leftWidth;

        public Vector3 RightBankPosition =>
            position + RightDirection * rightWidth;
    }
}