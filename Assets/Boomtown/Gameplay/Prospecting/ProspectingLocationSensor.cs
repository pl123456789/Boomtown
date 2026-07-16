using Boomtown.WorldGeneration;
using UnityEngine;

namespace Boomtown.Gameplay.Prospecting
{
    public sealed class ProspectingLocationSensor : MonoBehaviour
    {
        [Header("Ground Detection")]

        [SerializeField, Min(0.1f)]
        private float rayStartHeight = 3f;

        [SerializeField, Min(1f)]
        private float rayDistance = 20f;

        [SerializeField]
        private LayerMask groundLayerMask = ~0;

        [Header("Generated River Data")]

        [SerializeField]
        private RiverData riverData;

        [Header("Panning Rules")]

        [SerializeField, Min(0.5f)]
        private float maximumWaterDistance = 10f;

        [SerializeField]
        private bool allowInsideActiveChannel;

        public bool IsOnTerrain { get; private set; }
        public bool IsNearWater { get; private set; }
        public bool IsInsideActiveChannel { get; private set; }

        public bool CanPanHere =>
            IsOnTerrain &&
            IsNearWater &&
            (allowInsideActiveChannel ||
             !IsInsideActiveChannel);

        public Vector3 SamplePoint { get; private set; }

        public float DistanceToWater { get; private set; } =
            float.PositiveInfinity;

        public RiverLandscapeType Landscape { get; private set; } =
            RiverLandscapeType.Terrace;

        public RiverData RiverData => riverData;

        private void Awake()
        {
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        public void SetRiverData(RiverData data)
        {
            riverData = data;
            Refresh();
        }

        public void Refresh()
        {
            RefreshGround();
            RefreshRiver();
        }

        public bool CanPanAt(Vector3 worldPosition)
        {
            Vector3 groundPoint =
                WaypointPath.GroundPoint(worldPosition);

            if (riverData == null ||
                !RiverLandscapeQuery.TryGetNearest(
                    riverData,
                    groundPoint,
                    out RiverLandscapeQueryResult result))
            {
                return false;
            }

            bool nearWater =
                result.distanceFromWaterEdge <=
                maximumWaterDistance;

            bool allowedChannel =
                allowInsideActiveChannel ||
                !result.isInsideActiveChannel;

            return nearWater && allowedChannel;
        }

        private void RefreshGround()
        {
            Vector3 origin =
                transform.position +
                Vector3.up * rayStartHeight;

            RaycastHit[] hits =
                Physics.RaycastAll(
                    origin,
                    Vector3.down,
                    rayDistance,
                    groundLayerMask,
                    QueryTriggerInteraction.Ignore);

            System.Array.Sort(
                hits,
                (first, second) =>
                    first.distance.CompareTo(second.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.transform == transform ||
                    hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                Terrain terrain =
                    hit.collider.GetComponent<Terrain>() ??
                    hit.collider.GetComponentInParent<Terrain>();

                if (terrain == null)
                {
                    continue;
                }

                IsOnTerrain = true;
                SamplePoint = hit.point;
                return;
            }

            IsOnTerrain = false;
            SamplePoint = transform.position;
        }

        private void RefreshRiver()
        {
            if (!IsOnTerrain ||
                riverData == null ||
                !RiverLandscapeQuery.TryGetNearest(
                    riverData,
                    SamplePoint,
                    out RiverLandscapeQueryResult result))
            {
                IsNearWater = false;
                IsInsideActiveChannel = false;
                DistanceToWater = float.PositiveInfinity;
                Landscape = RiverLandscapeType.Terrace;
                return;
            }

            DistanceToWater =
                result.distanceFromWaterEdge;

            IsNearWater =
                DistanceToWater <= maximumWaterDistance;

            IsInsideActiveChannel =
                result.isInsideActiveChannel;

            Landscape =
                result.landscape;
        }
    }
}
