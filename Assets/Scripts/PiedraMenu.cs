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

        [Tooltip("Actívalo en la piedra de Ajustes para poder abrir y cerrar el panel varias veces.")]
        [SerializeField] private bool reiniciarAlCompletar = false;

        [Header("Feedback visual y sonoro (opcional)")]
        [SerializeField] private AudioSource audioGolpe;
        [SerializeField] private ParticleSystem particulasGolpe;
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
            if (pico.VelocidadActual < velocidadMinimaGolpe) return;

            _ultimoGolpeTime = Time.time;
            _golpesActuales++;

            bool completadoAhora = _golpesActuales >= golpesNecesarios;
            pico.Vibrar(completadoAhora ? 1f : 0.7f, completadoAhora ? 0.25f : 0.12f);
            Debug.Log($"[PiedraMenu] {name}: golpe {_golpesActuales}/{golpesNecesarios}");

            ReproducirFeedback();
            alGolpear?.Invoke();

            if (completadoAhora)
            {
                if (reiniciarAlCompletar)
                {
                    _golpesActuales = 0;
                }
                else
                {
                    _completado = true;
                }

                alCompletar?.Invoke();
            }
        }

        private void ReproducirFeedback()
        {
            if (audioGolpe != null) audioGolpe.Play();
            if (particulasGolpe != null) particulasGolpe.Play();

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
    }
}
