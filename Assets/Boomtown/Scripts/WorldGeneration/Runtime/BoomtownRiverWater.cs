using UnityEngine;

namespace Boomtown.WorldGeneration
{
    /// <summary>
    /// Optional runtime control for the generated Fraser River material.
    /// The shader animates on its own; this component exposes simple scene
    /// controls for future weather, seasons, floods, and paused gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BoomtownRiverWater : MonoBehaviour
    {
        [SerializeField, Range(0f, 2f)]
        private float flowMultiplier = 1f;

        [SerializeField, Range(0f, 2f)]
        private float foamMultiplier = 1f;

        private Material runtimeMaterial;
        private float baseFlowSpeed;
        private float baseRapidFoam;

        private void Awake()
        {
            MeshRenderer meshRenderer =
                GetComponent<MeshRenderer>();

            if (meshRenderer == null ||
                meshRenderer.sharedMaterial == null)
            {
                enabled = false;
                return;
            }

            runtimeMaterial =
                meshRenderer.material;

            if (runtimeMaterial.HasProperty(
                    "_FlowSpeed"))
            {
                baseFlowSpeed =
                    runtimeMaterial.GetFloat(
                        "_FlowSpeed");
            }

            if (runtimeMaterial.HasProperty(
                    "_RapidFoam"))
            {
                baseRapidFoam =
                    runtimeMaterial.GetFloat(
                        "_RapidFoam");
            }

            Apply();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                Apply();
            }
        }

        public void SetFlowMultiplier(
            float multiplier)
        {
            flowMultiplier =
                Mathf.Max(
                    0f,
                    multiplier);

            Apply();
        }

        public void SetFoamMultiplier(
            float multiplier)
        {
            foamMultiplier =
                Mathf.Max(
                    0f,
                    multiplier);

            Apply();
        }

        private void Apply()
        {
            if (runtimeMaterial == null)
            {
                return;
            }

            if (runtimeMaterial.HasProperty(
                    "_FlowSpeed"))
            {
                runtimeMaterial.SetFloat(
                    "_FlowSpeed",
                    baseFlowSpeed *
                    flowMultiplier);
            }

            if (runtimeMaterial.HasProperty(
                    "_RapidFoam"))
            {
                runtimeMaterial.SetFloat(
                    "_RapidFoam",
                    baseRapidFoam *
                    foamMultiplier);
            }
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null)
            {
                Destroy(
                    runtimeMaterial);
            }
        }
    }
}
