using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MineEscape
{
    public class PicoMenu : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private XRGrabInteractable grabInteractable;
        [Tooltip("Asigna aquí el BoxCollider de la cabeza del pico para que el mango no cuente golpes.")]
        [SerializeField] private Collider colliderCabeza;

        private Vector3 _ultimaPosicion;
        private XRBaseInputInteractor _interactorActual;

        public float VelocidadActual { get; private set; }
        public Collider ColliderCabeza => colliderCabeza;

        private void Awake()
        {
            _ultimaPosicion = transform.position;

            if (rb != null)
            {
                rb.useGravity = false;
                rb.isKinematic = true;
            }

            if (grabInteractable != null)
            {
                grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
                grabInteractable.useDynamicAttach = false;
                grabInteractable.throwOnDetach = false;
                grabInteractable.farAttachMode = InteractableFarAttachMode.Near;
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

        private void AlAgarrar(SelectEnterEventArgs args)
        {
            _interactorActual = args.interactorObject as XRBaseInputInteractor;
        }

        private void AlSoltar(SelectExitEventArgs _)
        {
            _interactorActual = null;
        }

        public void Vibrar(float amplitud = 0.7f, float duracion = 0.12f)
        {
            if (_interactorActual != null)
            {
                _interactorActual.SendHapticImpulse(amplitud, duracion);
            }
        }
    }
}
