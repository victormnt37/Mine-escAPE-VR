using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MineEscape
{
    public class PicoMenu : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private XRGrabInteractable grabInteractable;

        [Header("Retorno al pedestal")]
        [Tooltip("Segundos tras soltar el pico antes de que vuelva a su posición inicial.")]
        [SerializeField] private float tiempoParaVolver = 4f;

        [Tooltip("Si el pico cae por debajo de esta altura Y, vuelve inmediatamente.")]
        [SerializeField] private float alturaMinimaVolver = -2f;

        private Vector3 _posicionInicial;
        private Quaternion _rotacionInicial;
        private Vector3 _ultimaPosicion;
        private float _temporizadorSuelto;
        private bool _estaAgarrado;
        private XRBaseInputInteractor _interactorActual;

        public float VelocidadActual { get; private set; }

        private void Awake()
        {
            _posicionInicial = transform.position;
            _rotacionInicial = transform.rotation;
            _ultimaPosicion = transform.position;

            if (rb != null)
            {
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            if (grabInteractable != null)
            {
                grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
                grabInteractable.throwOnDetach = false;
            }

            foreach (Collider col in GetComponentsInChildren<Collider>())
            {
                col.isTrigger = false;
            }
        }

        private void OnEnable()
        {
            if (grabInteractable == null) return;
            grabInteractable.selectEntered.AddListener(AlAgarrar);
            grabInteractable.selectExited.AddListener(AlSoltar);
        }

        private void OnDisable()
        {
            if (grabInteractable == null) return;
            grabInteractable.selectEntered.RemoveListener(AlAgarrar);
            grabInteractable.selectExited.RemoveListener(AlSoltar);
        }

        private void Update()
        {
            if (Time.deltaTime > 0f)
            {
                VelocidadActual = (transform.position - _ultimaPosicion).magnitude / Time.deltaTime;
            }
            _ultimaPosicion = transform.position;
        }

        private void FixedUpdate()
        {
            if (_estaAgarrado)
            {
                return;
            }

            if (transform.position.y < alturaMinimaVolver)
            {
                VolverAlSitio();
                return;
            }

            if (Vector3.Distance(transform.position, _posicionInicial) > 0.05f)
            {
                _temporizadorSuelto += Time.fixedDeltaTime;
                if (_temporizadorSuelto >= tiempoParaVolver)
                {
                    VolverAlSitio();
                }
            }
        }

        private void AlAgarrar(SelectEnterEventArgs args)
        {
            _estaAgarrado = true;
            _temporizadorSuelto = 0f;
            _interactorActual = args.interactorObject as XRBaseInputInteractor;
        }

        private void AlSoltar(SelectExitEventArgs _)
        {
            _estaAgarrado = false;
            _temporizadorSuelto = 0f;
            _interactorActual = null;
        }

        public void Vibrar(float amplitud = 0.7f, float duracion = 0.12f)
        {
            if (_interactorActual != null)
            {
                _interactorActual.SendHapticImpulse(amplitud, duracion);
            }
        }

        public void VolverAlSitio()
        {
            _temporizadorSuelto = 0f;
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            transform.SetPositionAndRotation(_posicionInicial, _rotacionInicial);
            _ultimaPosicion = _posicionInicial;
            VelocidadActual = 0f;
        }
    }
}
