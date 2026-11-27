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
        [SerializeField] private TextMeshProUGUI textoBotonPausa;

        [Header("Navegación")]
        [SerializeField] private string escenaSalidaPorDefecto = "MenuPrincipal";

        private bool _enPausa = false;

        private void Start()
        {
            ConfigurarSkybox360();
            ConfigurarVideo();
            IniciarDesdeElPrincipio();
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
