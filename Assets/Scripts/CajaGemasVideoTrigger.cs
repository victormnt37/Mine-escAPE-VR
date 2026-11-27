using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

namespace MineEscape
{
    /// <summary>
    /// Controla la caja de gemas: al ser golpeada 3 veces con el pico (PicoMenu),
    /// reproduce el video Trailer_360.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CajaGemasVideoTrigger : MonoBehaviour
    {
        public enum ModoVideo
        {
            PantallaVirtual,
            Skybox360
        }

        [Header("Configuración de Golpes")]
        [Tooltip("Número de golpes necesarios para activar el video.")]
        [SerializeField] private int golpesNecesarios = 3;
        [Tooltip("Velocidad mínima a la que debe impactar el pico para contar como golpe.")]
        [SerializeField] private float velocidadMinimaGolpe = 0.15f;
        [Tooltip("Tiempo mínimo entre golpes para evitar registros dobles.")]
        [SerializeField] private float cooldownGolpe = 0.3f;

        [Header("Configuración del Video")]
        [Tooltip("Modo de reproducción: pantalla flotante o proyección inmersiva 360 en el cielo.")]
        [SerializeField] private ModoVideo modo = ModoVideo.PantallaVirtual;
        [Tooltip("Clip de video a reproducir. Si está vacío se asigna Trailer_360 automáticamente.")]
        [SerializeField] private VideoClip videoClip;
        [Tooltip("VideoPlayer existente en la escena (opcional). Si no se asigna, se creará uno automáticamente.")]
        [SerializeField] private VideoPlayer videoPlayer;
        [Tooltip("Pantalla o GameObject a activar al terminar los golpes (opcional).")]
        [SerializeField] private GameObject pantallaObjeto;
        [Tooltip("Si no hay pantalla ni VideoPlayer asignados, crear una pantalla flotante automáticamente.")]
        [SerializeField] private bool autoCrearPantallaSiNoExiste = true;
        [Tooltip("Posición relativa de la pantalla virtual respecto a la caja.")]
        [SerializeField] private Vector3 offsetPantalla = new Vector3(0f, 1.2f, 1.8f);
        [Tooltip("Dimensiones de la pantalla virtual (ancho x alto).")]
        [SerializeField] private Vector2 tamanoPantalla = new Vector2(2.4f, 1.35f);
        [SerializeField] private bool bucle = false;

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
        private RenderTexture _skyboxRenderTexture;
        private Material _skyboxMaterial;

#if UNITY_EDITOR
        private void Reset()
        {
            // Auto-asignar el video Trailer_360 al añadir el componente
            if (videoClip == null)
            {
                videoClip = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/VideoTrailer/Trailer_360.mp4");
            }

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

            // Asegurar que si hay una pantalla asignada empiece apagada
            if (pantallaObjeto != null)
            {
                pantallaObjeto.SetActive(false);
            }
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

            // Comprobar si quien golpea es el pico del jugador
            PicoMenu pico = otro.GetComponentInParent<PicoMenu>();
            if (pico == null) return;

            // Verificar si el impacto fue con la cabeza del pico (si está configurada)
            if (pico.ColliderCabeza != null && otro != pico.ColliderCabeza) return;

            // Verificar si el golpe lleva suficiente velocidad
            if (velocidadMinimaGolpe > 0f && pico.VelocidadActual < velocidadMinimaGolpe) return;

            _ultimoGolpeTime = Time.time;
            _golpesActuales++;

            bool completadoAhora = _golpesActuales >= golpesNecesarios;
            Vector3 puntoImpacto = otro.ClosestPoint(transform.position);

            // Vibración háptica en los mandos VR
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

            ReproducirVideo();
            alCompletar?.Invoke();
        }

        /// <summary>
        /// Inicia la reproducción del video Trailer_360.
        /// </summary>
        [ContextMenu("Probar Reproducir Video")]
        public void ReproducirVideo()
        {
            _activado = true;
            Debug.Log("[CajaGemas] Activando video...");

            if (modo == ModoVideo.Skybox360)
            {
                ReproducirEnSkybox();
            }
            else
            {
                ReproducirEnPantalla();
            }
        }

        private void ReproducirEnPantalla()
        {
            // 1. Activar pantalla asignada si existe
            if (pantallaObjeto != null)
            {
                pantallaObjeto.SetActive(true);
            }

            // 2. Si no hay VideoPlayer ni pantalla, crear una automáticamente
            if (videoPlayer == null && autoCrearPantallaSiNoExiste)
            {
                ConstruirPantallaAutomatica();
            }

            // 3. Configurar y reproducir
            if (videoPlayer != null)
            {
                ConfigurarVideoPlayer(videoPlayer, VideoRenderMode.MaterialOverride);
                videoPlayer.Play();
            }
        }

        private void ReproducirEnSkybox()
        {
            if (videoPlayer == null)
            {
                GameObject vpGo = new GameObject("VideoPlayer_Skybox");
                vpGo.transform.SetParent(transform);
                videoPlayer = vpGo.AddComponent<VideoPlayer>();
                AudioSource audioSource = vpGo.AddComponent<AudioSource>();
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                videoPlayer.SetTargetAudioSource(0, audioSource);
            }

            if (_skyboxRenderTexture == null)
            {
                _skyboxRenderTexture = new RenderTexture(2048, 1024, 0, RenderTextureFormat.ARGB32);
                _skyboxRenderTexture.Create();
            }

            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = _skyboxRenderTexture;

            Shader skyShader = Shader.Find("Skybox/Panoramic");
            if (skyShader != null)
            {
                _skyboxMaterial = new Material(skyShader);
                _skyboxMaterial.mainTexture = _skyboxRenderTexture;
                RenderSettings.skybox = _skyboxMaterial;
                DynamicGI.UpdateEnvironment();
            }

            ConfigurarFuenteVideo(videoPlayer);
            videoPlayer.Play();
        }

        private void ConstruirPantallaAutomatica()
        {
            GameObject pantalla = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pantalla.name = "Pantalla_Trailer_360";

            // Posicionar frente a la caja mirando hacia la cámara del jugador
            Camera cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();

            Vector3 posCam = cam != null ? cam.transform.position : (transform.position + Vector3.back * 2f);
            Vector3 posPantalla = transform.position + offsetPantalla;

            pantalla.transform.position = posPantalla;
            pantalla.transform.localScale = new Vector3(tamanoPantalla.x, tamanoPantalla.y, 1f);

            Vector3 direccion = (posPantalla - posCam).normalized;
            if (direccion != Vector3.zero)
            {
                pantalla.transform.rotation = Quaternion.LookRotation(direccion);
            }

            // Quitar collider físico para no chocar
            Collider col = pantalla.GetComponent<Collider>();
            if (col != null) Destroy(col);

            MeshRenderer mr = pantalla.GetComponent<MeshRenderer>();
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            if (unlitShader != null && mr != null)
            {
                mr.material = new Material(unlitShader);
            }

            videoPlayer = pantalla.AddComponent<VideoPlayer>();
            AudioSource audioSource = pantalla.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, audioSource);

            videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
            videoPlayer.targetMaterialRenderer = mr;
            if (mr != null && mr.sharedMaterial != null && mr.sharedMaterial.HasProperty("_BaseMap"))
            {
                videoPlayer.targetMaterialProperty = "_BaseMap";
            }
            else
            {
                videoPlayer.targetMaterialProperty = "_MainTex";
            }

            pantallaObjeto = pantalla;
        }

        private void ConfigurarVideoPlayer(VideoPlayer vp, VideoRenderMode renderMode)
        {
            vp.playOnAwake = false;
            vp.isLooping = bucle;

            if (pantallaObjeto != null && vp.targetMaterialRenderer == null)
            {
                MeshRenderer mr = pantallaObjeto.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    vp.renderMode = renderMode;
                    vp.targetMaterialRenderer = mr;
                    vp.targetMaterialProperty = (mr.sharedMaterial != null && mr.sharedMaterial.HasProperty("_BaseMap")) ? "_BaseMap" : "_MainTex";
                }
            }

            ConfigurarFuenteVideo(vp);
        }

        private void ConfigurarFuenteVideo(VideoPlayer vp)
        {
            if (videoClip != null)
            {
                vp.source = VideoSource.VideoClip;
                vp.clip = videoClip;
            }
            else
            {
                string rutaVideo = Path.Combine(Application.dataPath, "VideoTrailer", "Trailer_360.mp4");
                if (File.Exists(rutaVideo))
                {
                    vp.source = VideoSource.Url;
                    vp.url = rutaVideo;
                }
                else
                {
                    Debug.LogWarning($"[CajaGemas] No se encontró Trailer_360.mp4 en {rutaVideo}. Asigna el clip en el Inspector.");
                }
            }
        }

        /// <summary>
        /// Permite reiniciar la caja para poder picarla de nuevo.
        /// </summary>
        public void Reiniciar()
        {
            _golpesActuales = 0;
            _activado = false;
            transform.localScale = _escalaOriginal;

            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }

            if (pantallaObjeto != null)
            {
                pantallaObjeto.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_skyboxRenderTexture != null)
            {
                _skyboxRenderTexture.Release();
                Destroy(_skyboxRenderTexture);
            }

            if (_skyboxMaterial != null)
            {
                Destroy(_skyboxMaterial);
            }
        }
    }
}
