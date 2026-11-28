using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace MineEscape
{
    /// <summary>
    /// Controla la reproducción del video 360 en Skybox y los botones de Pausa, Volver a ver y Salir.
    /// </summary>
    public class Trailer360Controller : MonoBehaviour
    {
        public static string EscenaRetorno { get; set; }

        [Header("Referencias de Video 360")]
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private VideoClip videoClip;
        [SerializeField] private RenderTexture renderTexture;
        [SerializeField] private Material skyboxMaterial;

        [Header("UI")]
        [SerializeField] private Canvas canvasControles;
        [SerializeField] private TextMeshProUGUI textoBotonPausa;
        [SerializeField] private float distanciaPanel = 2.0f;
        [SerializeField] private float alturaRelativaPanel = -0.2f;
        [SerializeField] private float anguloReubicacion = 65f;

        [Header("Navegación")]
        [SerializeField] private string escenaSalidaPorDefecto = "MenuPrincipal";

        private bool _enPausa = false;
        private Camera _camaraPrincipal;

        private void Start()
        {
            ConfigurarSkybox360();
            ConfigurarVideo();
            IniciarDesdeElPrincipio();
            StartCoroutine(InicializarUIFrenteACamara());
        }

        private IEnumerator InicializarUIFrenteACamara()
        {
            // Esperar un frame para que el tracking VR inicialice la posición/rotación de la cámara
            yield return null;

            if (canvasControles == null)
            {
                canvasControles = FindFirstObjectByType<Canvas>();
            }

            _camaraPrincipal = Camera.main;
            if (_camaraPrincipal == null)
            {
                _camaraPrincipal = FindFirstObjectByType<Camera>();
            }

            if (canvasControles != null)
            {
                canvasControles.additionalShaderChannels |=
                    AdditionalCanvasShaderChannels.TexCoord1 |
                    AdditionalCanvasShaderChannels.Normal |
                    AdditionalCanvasShaderChannels.Tangent;

                if (_camaraPrincipal != null)
                {
                    canvasControles.worldCamera = _camaraPrincipal;
                    PosicionarPanelFrenteACamara(_camaraPrincipal);
                }
            }
        }

        private void Update()
        {
            if (canvasControles == null || _camaraPrincipal == null) return;

            Vector3 camForward = Vector3.ProjectOnPlane(_camaraPrincipal.transform.forward, Vector3.up).normalized;
            if (camForward.sqrMagnitude < 0.001f) return;

            Vector3 dirToPanel = Vector3.ProjectOnPlane(
                canvasControles.transform.position - _camaraPrincipal.transform.position,
                Vector3.up).normalized;

            if (dirToPanel.sqrMagnitude < 0.001f || Vector3.Angle(camForward, dirToPanel) > anguloReubicacion)
            {
                PosicionarPanelFrenteACamara(_camaraPrincipal);
            }
        }

        private void PosicionarPanelFrenteACamara(Camera cam)
        {
            Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            Vector3 pos = cam.transform.position + forward * distanciaPanel + Vector3.up * alturaRelativaPanel;
            canvasControles.transform.position = pos;
            canvasControles.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        private void ConfigurarSkybox360()
        {
            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(3840, 1920, 0, RenderTextureFormat.ARGB32);
                renderTexture.useMipMap = false;
                renderTexture.Create();
            }

            if (skyboxMaterial == null)
            {
                Shader skyShader = Shader.Find("Skybox/Panoramic");
                if (skyShader != null)
                {
                    skyboxMaterial = new Material(skyShader);
                }
            }

            if (skyboxMaterial != null)
            {
                skyboxMaterial.mainTexture = renderTexture;
                RenderSettings.skybox = skyboxMaterial;
                DynamicGI.UpdateEnvironment();
            }
        }

        private void ConfigurarVideo()
        {
            if (videoPlayer == null)
            {
                videoPlayer = GetComponent<VideoPlayer>();
                if (videoPlayer == null)
                {
                    videoPlayer = gameObject.AddComponent<VideoPlayer>();
                }
            }

            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;

            if (videoClip != null)
            {
                videoPlayer.source = VideoSource.VideoClip;
                videoPlayer.clip = videoClip;
            }
            else
            {
                string rutaVideo = Path.Combine(Application.dataPath, "VideoTrailer", "Trailer_360.mp4");
                if (File.Exists(rutaVideo))
                {
                    videoPlayer.source = VideoSource.Url;
                    videoPlayer.url = rutaVideo;
                }
                else
                {
                    Debug.LogWarning($"[Trailer360] No se encontró el video en {rutaVideo}.");
                }
            }
        }

        private void IniciarDesdeElPrincipio()
        {
            if (videoPlayer == null) return;

            _enPausa = false;
            videoPlayer.time = 0;
            videoPlayer.Play();
            ActualizarTextoPausa();
        }

        /// <summary>
        /// Pausa o reanuda el video 360.
        /// </summary>
        public void AlternarPausa()
        {
            if (videoPlayer == null) return;

            if (_enPausa)
            {
                videoPlayer.Play();
                _enPausa = false;
            }
            else
            {
                videoPlayer.Pause();
                _enPausa = true;
            }

            ActualizarTextoPausa();
        }

        /// <summary>
        /// Reinicia el video 360 desde el principio.
        /// </summary>
        public void VolverAVer()
        {
            if (videoPlayer == null) return;

            videoPlayer.Stop();
            IniciarDesdeElPrincipio();
        }

        /// <summary>
        /// Sale de la escena 360 y vuelve a la escena anterior (o MenuPrincipal).
        /// </summary>
        public void Salir()
        {
            Time.timeScale = 1f;
            string destino = !string.IsNullOrEmpty(EscenaRetorno) ? EscenaRetorno : escenaSalidaPorDefecto;
            SceneManager.LoadScene(destino);
        }

        private void ActualizarTextoPausa()
        {
            if (textoBotonPausa != null)
            {
                textoBotonPausa.text = _enPausa ? "Reanudar" : "Pausa";
            }
        }
    }
}
