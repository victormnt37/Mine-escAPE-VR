using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace MineEscape
{
    public class PiedraMenu : MonoBehaviour
    {
        [Header("Configuración de golpes")]
        [SerializeField] private int golpesNecesarios = 3;
        [SerializeField] private float velocidadMinimaGolpe = 0.25f;
        [SerializeField] private float cooldownGolpe = 0.3f;

        [Tooltip("Espera por defecto si no hay Sonido Al Completar asignado.")]
        [SerializeField] private float retardoSinSonidoExtra = 0.25f;

        [Tooltip("Actívalo en la piedra de Ajustes para poder abrir y cerrar el panel varias veces.")]
        [SerializeField] private bool reiniciarAlCompletar = false;

        [Header("VFX y Sonidos (Assets directos)")]
        [SerializeField] private GameObject vfxImpactoPrefab;
        [SerializeField] private float duracionVfx = 2f;
        [SerializeField] private AudioClip sonidoGolpe;

        [Tooltip("Opcional (ej. avalancha/explosión en Jugar). Si se asigna, se espera a que termine.")]
        [SerializeField] private AudioClip sonidoAlCompletar;

        [Range(0f, 1f)]
        [SerializeField] private float volumenSonido = 1f;

        [Header("Rebote")]
        [SerializeField] private float intensidadRebote = 0.9f;
        [SerializeField] private float duracionRebote = 0.12f;

        [Header("Eventos")]
        [Tooltip("Se invoca en cada golpe válido.")]
        public UnityEvent alGolpear;

        [Tooltip("Se invoca al alcanzar los golpes necesarios (conéctalo a GestorEscenas).")]
        public UnityEvent alCompletar;

        private int _golpesActuales;
        private float _ultimoGolpeTime = -999f;
        private bool _completado;
        private Vector3 _escalaOriginal;
        private Coroutine _reboteCoroutine;

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
            if (_completado) return;
            if (Time.time - _ultimoGolpeTime < cooldownGolpe) return;

            PicoMenu pico = otro.GetComponentInParent<PicoMenu>();
            if (pico == null) return;
            if (pico.ColliderCabeza != null && otro != pico.ColliderCabeza) return;
            if (pico.VelocidadActual < velocidadMinimaGolpe) return;

            _ultimoGolpeTime = Time.time;
            _golpesActuales++;

            bool completadoAhora = _golpesActuales >= golpesNecesarios;
            Vector3 puntoImpacto = otro.ClosestPoint(transform.position);

            pico.Vibrar(completadoAhora ? 1f : 0.7f, completadoAhora ? 0.25f : 0.12f);
            Debug.Log($"[PiedraMenu] {name}: golpe {_golpesActuales}/{golpesNecesarios}");

            ReproducirFeedback(puntoImpacto, completadoAhora);
            alGolpear?.Invoke();

            if (completadoAhora)
            {
                _completado = true;
                float espera = sonidoAlCompletar != null ? sonidoAlCompletar.length : retardoSinSonidoExtra;
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

            if (reiniciarAlCompletar)
            {
                _golpesActuales = 0;
                _completado = false;
            }

            alCompletar?.Invoke();
        }
    }
}
