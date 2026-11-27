using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace MineEscape
{
    /// <summary>
    /// Controla la caja de gemas: al ser golpeada 3 veces con el pico (PicoMenu),
    /// abre la nueva escena del trailer en 360°.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CajaGemasVideoTrigger : MonoBehaviour
    {
        [Header("Configuración de Golpes")]
        [Tooltip("Número de golpes necesarios para abrir el trailer 360.")]
        [SerializeField] private int golpesNecesarios = 3;
        [Tooltip("Velocidad mínima a la que debe impactar el pico para contar como golpe.")]
        [SerializeField] private float velocidadMinimaGolpe = 0.15f;
        [Tooltip("Tiempo mínimo entre golpes para evitar registros dobles.")]
        [SerializeField] private float cooldownGolpe = 0.3f;

        [Header("Escena del Trailer 360")]
        [Tooltip("Nombre de la escena donde se reproduce el video en 360°.")]
        [SerializeField] private string nombreEscenaTrailer = "Trailer360";

        [Header("VFX y Sonidos de Impacto")]
        [SerializeField] private GameObject vfxImpactoPrefab;
        [SerializeField] private float escalaVfx = 0.45f;
        [SerializeField] private float duracionVfx = 2f;
        [SerializeField] private AudioClip sonidoGolpe;
        [SerializeField] private AudioClip sonidoAlCompletar;
        [Range(0f, 1f)]
        [SerializeField] private float volumenSonido = 1f;

        [Header("Efecto de Rebote")]
        [SerializeField] private float intensidadRebote = 0.92f;
        [SerializeField] private float duracionRebote = 0.12f;

        [Header("Eventos")]
        public UnityEvent alGolpear;
        public UnityEvent alCompletar;

        private int _golpesActuales = 0;
        private float _ultimoGolpeTime = -999f;
        private bool _activado = false;
        private Vector3 _escalaOriginal;
        private Coroutine _reboteCoroutine;

#if UNITY_EDITOR
        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }
#endif

        private void Awake()
        {
            _escalaOriginal = transform.localScale;
        }

        private void OnCollisionEnter(Collision collision)
        {
            ProcesarImpacto(collision.collider);
        }

        private void OnTriggerEnter(Collider other)
        {
            ProcesarImpacto(other);
        }

        private void ProcesarImpacto(Collider otro)
        {
            if (_activado) return;
            if (Time.time - _ultimoGolpeTime < cooldownGolpe) return;

            PicoMenu pico = otro.GetComponentInParent<PicoMenu>();
            if (pico == null) return;

            if (pico.ColliderCabeza != null && otro != pico.ColliderCabeza) return;
            if (velocidadMinimaGolpe > 0f && pico.VelocidadActual < velocidadMinimaGolpe) return;

            _ultimoGolpeTime = Time.time;
            _golpesActuales++;

            bool completadoAhora = _golpesActuales >= golpesNecesarios;
            Vector3 puntoImpacto = otro.ClosestPoint(transform.position);

            pico.Vibrar(completadoAhora ? 1f : 0.7f, completadoAhora ? 0.25f : 0.12f);
            Debug.Log($"[CajaGemas] {name}: golpe {_golpesActuales}/{golpesNecesarios}");

            ReproducirFeedback(puntoImpacto, completadoAhora);
            alGolpear?.Invoke();

            if (completadoAhora)
            {
                _activado = true;
                float espera = sonidoAlCompletar != null ? sonidoAlCompletar.length : 0.25f;
                StartCoroutine(EjecutarCompletadoConRetardo(espera));
            }
        }

        private void ReproducirFeedback(Vector3 puntoImpacto, bool esUltimoGolpe)
        {
            if (sonidoGolpe != null)
            {
                AudioSource.PlayClipAtPoint(sonidoGolpe, puntoImpacto, volumenSonido);
            }

            if (esUltimoGolpe && sonidoAlCompletar != null)
            {
                AudioSource.PlayClipAtPoint(sonidoAlCompletar, puntoImpacto, volumenSonido);
            }

            if (vfxImpactoPrefab != null)
            {
                GameObject vfx = Instantiate(vfxImpactoPrefab, puntoImpacto, Quaternion.identity);
                foreach (ParticleSystem ps in vfx.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = ps.main;
                    main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                }
                vfx.transform.localScale *= escalaVfx;
                Destroy(vfx, duracionVfx);
            }

            if (_reboteCoroutine != null) StopCoroutine(_reboteCoroutine);
            _reboteCoroutine = StartCoroutine(AnimarRebote());
        }

        private IEnumerator AnimarRebote()
        {
            transform.localScale = _escalaOriginal * intensidadRebote;
            yield return new WaitForSeconds(duracionRebote);
            transform.localScale = _escalaOriginal;
            _reboteCoroutine = null;
        }

        private IEnumerator EjecutarCompletadoConRetardo(float espera)
        {
            if (espera > 0f)
            {
                yield return new WaitForSeconds(espera);
            }

            alCompletar?.Invoke();
            AbrirEscenaTrailer360();
        }

        /// <summary>
        /// Guarda la escena actual y abre la escena del trailer 360.
        /// </summary>
        [ContextMenu("Probar Abrir Trailer 360")]
        public void AbrirEscenaTrailer360()
        {
            Trailer360Controller.EscenaRetorno = SceneManager.GetActiveScene().name;
            Time.timeScale = 1f;
            SceneManager.LoadScene(nombreEscenaTrailer);
        }
    }
}
