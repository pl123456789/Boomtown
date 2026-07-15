using UnityEngine;

/// <summary>
/// Creates a circular selection ring beneath an employee.
/// </summary>
[DisallowMultipleComponent]
public sealed class EmployeeSelectionRing : MonoBehaviour
{
    [SerializeField, Min(3)]
    private int _segments = 64;

    [SerializeField, Min(0f)]
    private float _radius = 0.75f;

    [SerializeField, Min(0f)]
    private float _lineWidth = 0.06f;

    [SerializeField]
    private float _heightOffset = -0.97f;

    [SerializeField]
    private Color _ringColor = Color.green;

    private LineRenderer _lineRenderer;
    private Material _ringMaterial;

    private void Awake()
    {
        CreateRing();
    }

    public void SetVisible(bool isVisible)
    {
        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = isVisible;
        }
    }

    private void CreateRing()
    {
        GameObject ring =
            new GameObject("SelectionRing");

        ring.transform.SetParent(
            transform,
            false);

        ring.transform.localPosition =
            new Vector3(
                0f,
                _heightOffset,
                0f);

        _lineRenderer =
            ring.AddComponent<LineRenderer>();

        _lineRenderer.useWorldSpace = false;
        _lineRenderer.loop = true;
        _lineRenderer.positionCount = _segments;
        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;
        _lineRenderer.startColor = _ringColor;
        _lineRenderer.endColor = _ringColor;
        _lineRenderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        _lineRenderer.receiveShadows = false;

        Shader shader =
            Shader.Find("Sprites/Default");

        if (shader != null)
        {
            _ringMaterial = new Material(shader);
            _ringMaterial.color = _ringColor;
            _lineRenderer.material = _ringMaterial;
        }

        for (int index = 0;
             index < _segments;
             index++)
        {
            float angle =
                index /
                (float)_segments *
                Mathf.PI *
                2f;

            _lineRenderer.SetPosition(
                index,
                new Vector3(
                    Mathf.Cos(angle) * _radius,
                    0f,
                    Mathf.Sin(angle) * _radius));
        }

        _lineRenderer.enabled = false;
    }

    private void OnDestroy()
    {
        if (_ringMaterial != null)
        {
            Destroy(_ringMaterial);
        }
    }
}
