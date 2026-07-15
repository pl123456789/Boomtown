using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores and displays a queue of world-space movement waypoints.
/// </summary>
[DisallowMultipleComponent]
public sealed class WaypointPath : MonoBehaviour
{
    [Header("Route Display")]
    [SerializeField]
    private bool _showRoute = true;

    [SerializeField, Min(0f)]
    private float _lineWidth = 0.045f;

    [SerializeField, Min(0f)]
    private float _markerRadius = 0.18f;

    [SerializeField, Min(0f)]
    private float _markerHeight = 0.025f;

    [SerializeField]
    private Color _routeColor =
        new Color(1f, 0.8f, 0.1f, 1f);

    private readonly Queue<Vector3> _waypoints = new();
    private readonly List<GameObject> _markers = new();

    private LineRenderer _lineRenderer;
    private Material _routeMaterial;

    public bool HasWaypoints => _waypoints.Count > 0;

    private void Awake()
    {
        CreateLineRenderer();
        RefreshVisuals();
    }

    private void LateUpdate()
    {
        if (_showRoute && _waypoints.Count > 0)
        {
            RefreshLinePositions();
        }
    }

    public void SetWaypoint(Vector3 waypoint)
    {
        Clear();
        AddWaypoint(waypoint);
    }

    public void AddWaypoint(Vector3 waypoint)
    {
        _waypoints.Enqueue(waypoint);
        CreateMarker(waypoint);
        RefreshVisuals();
    }

    public bool TryGetCurrent(out Vector3 waypoint)
    {
        if (_waypoints.Count == 0)
        {
            waypoint = default;
            return false;
        }

        waypoint = _waypoints.Peek();
        return true;
    }

    public void CompleteCurrent()
    {
        if (_waypoints.Count == 0)
        {
            return;
        }

        _waypoints.Dequeue();

        if (_markers.Count > 0)
        {
            Destroy(_markers[0]);
            _markers.RemoveAt(0);
        }

        RefreshVisuals();
    }

    public void Clear()
    {
        _waypoints.Clear();

        foreach (GameObject marker in _markers)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }

        _markers.Clear();
        RefreshVisuals();
    }

    private void CreateLineRenderer()
    {
        GameObject lineObject =
            new GameObject("WaypointRoute");

        lineObject.transform.SetParent(
            transform,
            false);

        _lineRenderer =
            lineObject.AddComponent<LineRenderer>();

        _lineRenderer.useWorldSpace = true;
        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;
        _lineRenderer.startColor = _routeColor;
        _lineRenderer.endColor = _routeColor;
        _lineRenderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        _lineRenderer.receiveShadows = false;

        Shader shader =
            Shader.Find("Sprites/Default");

        if (shader != null)
        {
            _routeMaterial = new Material(shader);
            _routeMaterial.color = _routeColor;
            _lineRenderer.material = _routeMaterial;
        }
    }

    private void CreateMarker(Vector3 waypoint)
    {
        GameObject marker =
            GameObject.CreatePrimitive(
                PrimitiveType.Cylinder);

        marker.name =
            $"Waypoint {_markers.Count + 1}";

        marker.transform.position =
            waypoint +
            (Vector3.up * _markerHeight);

        marker.transform.localScale =
            new Vector3(
                _markerRadius * 2f,
                _markerHeight,
                _markerRadius * 2f);

        Collider markerCollider =
            marker.GetComponent<Collider>();

        if (markerCollider != null)
        {
            Destroy(markerCollider);
        }

        Renderer markerRenderer =
            marker.GetComponent<Renderer>();

        if (markerRenderer != null &&
            _routeMaterial != null)
        {
            markerRenderer.material =
                new Material(_routeMaterial);
        }

        _markers.Add(marker);
    }

    private void RefreshVisuals()
    {
        if (_lineRenderer == null)
        {
            return;
        }

        _lineRenderer.enabled =
            _showRoute &&
            _waypoints.Count > 0;

        RefreshLinePositions();
    }

    private void RefreshLinePositions()
    {
        if (_lineRenderer == null ||
            !_lineRenderer.enabled)
        {
            return;
        }

        Vector3[] routePoints =
            new Vector3[_waypoints.Count + 1];

        routePoints[0] =
            transform.position +
            (Vector3.up * _markerHeight);

        int index = 1;

        foreach (Vector3 waypoint in _waypoints)
        {
            routePoints[index] =
                waypoint +
                (Vector3.up * _markerHeight);

            index++;
        }

        _lineRenderer.positionCount =
            routePoints.Length;

        _lineRenderer.SetPositions(routePoints);
    }

    private void OnDestroy()
    {
        if (_routeMaterial != null)
        {
            Destroy(_routeMaterial);
        }
    }
}
