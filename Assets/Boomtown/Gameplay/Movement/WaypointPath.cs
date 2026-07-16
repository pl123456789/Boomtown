using System.Collections.Generic;
using UnityEngine;

public enum WaypointCommandType
{
    Move,
    PanForGold
}

public sealed class WaypointCommand
{
    public WaypointCommand(
        Vector3 position,
        WaypointCommandType commandType)
    {
        Position = position;
        CommandType = commandType;
    }

    public Vector3 Position { get; set; }
    public WaypointCommandType CommandType { get; set; }

    public bool IsActivity =>
        CommandType != WaypointCommandType.Move;
}

/// <summary>
/// Stores a terrain-grounded route. Move commands use circular markers.
/// A queued interaction uses an orange X marker.
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
    private float _moveMarkerRadius = 0.18f;

    [SerializeField, Min(0f)]
    private float _activityMarkerSize = 0.42f;

    [SerializeField, Min(0f)]
    private float _markerSurfaceOffset = 0.06f;

    [SerializeField, Min(0.5f)]
    private float _terrainLineSpacing = 3f;

    [SerializeField]
    private Color _routeColor =
        new Color(1f, 0.8f, 0.1f, 1f);

    [SerializeField]
    private Color _activityColor =
        new Color(1f, 0.35f, 0.1f, 1f);

    private readonly List<WaypointCommand> _commands = new();
    private readonly List<GameObject> _markers = new();

    private LineRenderer _lineRenderer;
    private Material _routeMaterial;
    private Material _activityMaterial;

    public bool HasWaypoints => _commands.Count > 0;
    public int CommandCount => _commands.Count;

    private void Awake()
    {
        CreateMaterials();
        CreateLineRenderer();
        RefreshVisuals();
    }

    private void LateUpdate()
    {
        if (_showRoute && _commands.Count > 0)
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
        AddCommand(
            waypoint,
            WaypointCommandType.Move);
    }

    public void SetCommand(
        Vector3 waypoint,
        WaypointCommandType commandType)
    {
        Clear();
        AddCommand(waypoint, commandType);
    }

    public void AddCommand(
        Vector3 waypoint,
        WaypointCommandType commandType)
    {
        Vector3 grounded =
            GroundPoint(waypoint);

        _commands.Add(
            new WaypointCommand(
                grounded,
                commandType));

        RebuildMarkers();
        RefreshVisuals();
    }

    public bool TryGetLastCommand(
        out WaypointCommand command)
    {
        if (_commands.Count == 0)
        {
            command = null;
            return false;
        }

        command = _commands[_commands.Count - 1];
        return true;
    }

    /// <summary>
    /// Changes the final queued move into an activity command.
    /// Returns false when no waypoint is queued.
    /// </summary>
    public bool MarkLastCommandAsActivity(
        WaypointCommandType activityType)
    {
        if (_commands.Count == 0 ||
            activityType == WaypointCommandType.Move)
        {
            return false;
        }

        _commands[_commands.Count - 1].CommandType =
            activityType;

        RebuildMarkers();
        RefreshVisuals();

        return true;
    }

    public bool TryGetCurrent(
        out Vector3 waypoint)
    {
        if (!TryGetCurrentCommand(
                out WaypointCommand command))
        {
            waypoint = default;
            return false;
        }

        waypoint = command.Position;
        return true;
    }

    public bool TryGetCurrentCommand(
        out WaypointCommand command)
    {
        if (_commands.Count == 0)
        {
            command = null;
            return false;
        }

        command = _commands[0];
        return true;
    }

    public void CompleteCurrent()
    {
        if (_commands.Count == 0)
        {
            return;
        }

        _commands.RemoveAt(0);

        if (_markers.Count > 0)
        {
            Destroy(_markers[0]);
            _markers.RemoveAt(0);
        }

        RefreshVisuals();
    }

    public void Clear()
    {
        _commands.Clear();
        DestroyMarkers();
        RefreshVisuals();
    }

    private void RebuildMarkers()
    {
        DestroyMarkers();

        for (int index = 0;
             index < _commands.Count;
             index++)
        {
            WaypointCommand command =
                _commands[index];

            GameObject marker =
                command.IsActivity
                    ? CreateActivityMarker(
                        command.Position)
                    : CreateMoveMarker(
                        command.Position);

            marker.name =
                command.IsActivity
                    ? $"Activity X {index + 1}"
                    : $"Move {index + 1}";

            _markers.Add(marker);
        }
    }

    private void DestroyMarkers()
    {
        foreach (GameObject marker in _markers)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }

        _markers.Clear();
    }

    private void CreateMaterials()
    {
        Shader shader =
            Shader.Find("Sprites/Default");

        if (shader == null)
        {
            return;
        }

        _routeMaterial =
            new Material(shader)
            {
                color = _routeColor
            };

        _activityMaterial =
            new Material(shader)
            {
                color = _activityColor
            };
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

        if (_routeMaterial != null)
        {
            _lineRenderer.material =
                _routeMaterial;
        }
    }

    private GameObject CreateMoveMarker(
        Vector3 position)
    {
        GameObject marker =
            GameObject.CreatePrimitive(
                PrimitiveType.Cylinder);

        marker.transform.position =
            GroundPoint(position) +
            Vector3.up *
            _markerSurfaceOffset;

        marker.transform.localScale =
            new Vector3(
                _moveMarkerRadius * 2f,
                0.025f,
                _moveMarkerRadius * 2f);

        RemoveCollider(marker);
        ApplyMaterial(
            marker,
            _routeMaterial);

        return marker;
    }

    private GameObject CreateActivityMarker(
        Vector3 position)
    {
        GameObject root =
            new GameObject("ActivityMarker");

        root.transform.position =
            GroundPoint(position) +
            Vector3.up *
            _markerSurfaceOffset;

        CreateXBar(
            root.transform,
            45f);

        CreateXBar(
            root.transform,
            -45f);

        return root;
    }

    private void CreateXBar(
        Transform parent,
        float rotationY)
    {
        GameObject bar =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube);

        bar.transform.SetParent(
            parent,
            false);

        bar.transform.localPosition =
            Vector3.zero;

        bar.transform.localRotation =
            Quaternion.Euler(
                0f,
                rotationY,
                0f);

        bar.transform.localScale =
            new Vector3(
                _activityMarkerSize,
                0.035f,
                _activityMarkerSize * 0.18f);

        RemoveCollider(bar);
        ApplyMaterial(
            bar,
            _activityMaterial);
    }

    private static void RemoveCollider(
        GameObject target)
    {
        Collider collider =
            target.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }
    }

    private static void ApplyMaterial(
        GameObject target,
        Material material)
    {
        Renderer renderer =
            target.GetComponent<Renderer>();

        if (renderer != null &&
            material != null)
        {
            renderer.material =
                material;
        }
    }

    private void RefreshVisuals()
    {
        if (_lineRenderer == null)
        {
            return;
        }

        _lineRenderer.enabled =
            _showRoute &&
            _commands.Count > 0;

        RefreshLinePositions();
    }

    private void RefreshLinePositions()
    {
        if (_lineRenderer == null ||
            !_lineRenderer.enabled)
        {
            return;
        }

        List<Vector3> routePoints =
            new List<Vector3>();

        Vector3 previous =
            GroundPoint(
                transform.position);

        routePoints.Add(
            previous +
            Vector3.up *
            _markerSurfaceOffset);

        foreach (WaypointCommand command
                 in _commands)
        {
            Vector3 destination =
                GroundPoint(
                    command.Position);

            AppendGroundedSegment(
                routePoints,
                previous,
                destination);

            previous =
                destination;
        }

        _lineRenderer.positionCount =
            routePoints.Count;

        _lineRenderer.SetPositions(
            routePoints.ToArray());
    }

    private void AppendGroundedSegment(
        List<Vector3> points,
        Vector3 start,
        Vector3 end)
    {
        float horizontalDistance =
            Vector2.Distance(
                new Vector2(
                    start.x,
                    start.z),
                new Vector2(
                    end.x,
                    end.z));

        int steps =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    horizontalDistance /
                    _terrainLineSpacing));

        for (int step = 1;
             step <= steps;
             step++)
        {
            float t =
                step / (float)steps;

            Vector3 point =
                Vector3.Lerp(
                    start,
                    end,
                    t);

            point =
                GroundPoint(point) +
                Vector3.up *
                _markerSurfaceOffset;

            points.Add(point);
        }
    }

    public static Vector3 GroundPoint(
        Vector3 worldPosition)
    {
        Terrain terrain =
            FindTerrain(worldPosition);

        if (terrain == null ||
            terrain.terrainData == null)
        {
            return worldPosition;
        }

        float y =
            terrain.SampleHeight(
                worldPosition) +
            terrain.transform.position.y;

        return new Vector3(
            worldPosition.x,
            y,
            worldPosition.z);
    }

    private static Terrain FindTerrain(
        Vector3 worldPosition)
    {
        Terrain[] terrains =
            Terrain.activeTerrains;

        foreach (Terrain terrain in terrains)
        {
            if (terrain == null ||
                terrain.terrainData == null)
            {
                continue;
            }

            Vector3 origin =
                terrain.transform.position;

            Vector3 size =
                terrain.terrainData.size;

            bool inside =
                worldPosition.x >= origin.x &&
                worldPosition.x <= origin.x + size.x &&
                worldPosition.z >= origin.z &&
                worldPosition.z <= origin.z + size.z;

            if (inside)
            {
                return terrain;
            }
        }

        return Terrain.activeTerrain;
    }

    private void OnDestroy()
    {
        if (_routeMaterial != null)
        {
            Destroy(_routeMaterial);
        }

        if (_activityMaterial != null)
        {
            Destroy(_activityMaterial);
        }
    }
}
