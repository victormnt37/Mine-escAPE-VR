using UnityEngine;

namespace MineEscape
{
    [RequireComponent(typeof(Light))]
    public class TorchFlicker : MonoBehaviour
    {
        [Header("Flicker Amount")]
        [Range(0f, 0.5f)]
        [SerializeField] private float flickerPercent = 0.15f;
        [SerializeField] private float flickerSpeed = 6f;

        private Light torchLight;
        private float baseIntensity;
        private float noiseOffset;

        private void Awake()
        {
            torchLight = GetComponent<Light>();
            if (torchLight != null)
            {
                baseIntensity = torchLight.intensity;
            }
            noiseOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (torchLight == null) return;
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, noiseOffset);
            // Flickers subtly (+- 15%) around whatever intensity you set in the Inspector
            float delta = (noise - 0.5f) * 2f * flickerPercent;
            torchLight.intensity = baseIntensity * (1f + delta);
        }
    }
}
