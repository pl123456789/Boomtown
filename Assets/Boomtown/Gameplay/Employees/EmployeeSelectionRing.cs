using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Creates a terrain-following yellow selection ring beneath an employee.
/// </summary>
[DisallowMultipleComponent]
public sealed class EmployeeSelectionRing : MonoBehaviour
{
    [SerializeField, Min(3)] private int _segments = 64;
    [SerializeField, Min(0f)] private float _radius = 0.75f;
    [SerializeField, Min(0f)] private float _lineWidth = 0.06f;
    [SerializeField, Min(0f)] private float _surfaceOffset = 0.035f;
    [SerializeField, Min(0.5f)] private float _rayStartHeight = 3f;
    [SerializeField, Min(1f)] private float _rayDistance = 12f;
    [SerializeField] private LayerMask _groundMask = ~0;
    [SerializeField] private Color _ringColor = new(1f, 0.78f, 0.05f, 1f);

    private LineRenderer _lineRenderer;
    private Material _ringMaterial;
    private Transform _ringTransform;
    private bool _visible;

    private void Awake()
    {
        CreateRing();
        UpdateRingPosition();
    }

    private void LateUpdate()
    {
        if (_visible)
            UpdateRingPosition();
    }

    public void SetVisible(bool isVisible)
    {
        _visible = isVisible;

        if (_lineRenderer != null)
            _lineRenderer.enabled = isVisible;

        if (isVisible)
            UpdateRingPosition();
    }

    private void CreateRing()
    {
        GameObject ring = new("SelectionRing");
        _ringTransform = ring.transform;
        _ringTransform.SetParent(null, true);

        _lineRenderer = ring.AddComponent<LineRenderer>();
        _lineRenderer.useWorldSpace = false;
        _lineRenderer.loop = true;
        _lineRenderer.positionCount = _segments;
        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;
        _lineRenderer.startColor = _ringColor;
        _lineRenderer.endColor = _ringColor;
        _lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _lineRenderer.receiveShadows = false;
        _lineRenderer.alignment = LineAlignment.TransformZ;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            _ringMaterial = new Material(shader)
            {
                color = _ringColor
            };
            _lineRenderer.material = _ringMaterial;
        }

        for (int i = 0; i < _segments; i++)
        {
            float angle = i / (float)_segments * Mathf.PI * 2f;
            _lineRenderer.SetPosition(i,
                new Vector3(Mathf.Cos(angle) * _radius, 0f, Mathf.Sin(angle) * _radius));
        }

        _lineRenderer.enabled = false;
    }

    private void UpdateRingPosition()
    {
        if (_ringTransform == null)
            return;

        Vector3 origin = transform.position + Vector3.up * _rayStartHeight;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                _rayDistance, _groundMask, QueryTriggerInteraction.Ignore))
        {
            _ringTransform.position = hit.point + hit.normal * _surfaceOffset;
            _ringTransform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        }
        else
        {
            _ringTransform.position = transform.position + Vector3.up * _surfaceOffset;
            _ringTransform.rotation = Quaternion.identity;
        }
    }

    private void OnDestroy()
    {
        if (_ringTransform != null)
            Destroy(_ringTransform.gameObject);

        if (_ringMaterial != null)
            Destroy(_ringMaterial);
    }
}
