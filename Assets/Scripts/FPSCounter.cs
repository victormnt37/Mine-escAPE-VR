using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Cada cuántos segundos se actualiza el cálculo de FPS")]
    [SerializeField] private float intervaloActualizacion = 0.25f;

    [Header("Visualización UI (Opcional)")]
    [Tooltip("Si tienes un TextMeshPro en tu Canvas, arrástralo aquí. Si lo dejas vacío, se dibujará directamente en pantalla con OnGUI.")]
    [SerializeField] private TextMeshProUGUI textoTMP;

    [Header("Ajustes OnGUI (Si no usas TextMeshPro)")]
    [SerializeField] private bool mostrarOnGUI = true;
    [SerializeField] private TextAnchor posicion = TextAnchor.UpperRight;
    [SerializeField] private int tamanoFuente = 24;
    [SerializeField] private bool usarTamanoAdaptativo = true;

    private float tiempoAcumulado = 0f;
    private int fotogramas = 0;
    private float tiempoRestante;
    private float fpsActual = 0f;
    private float msActual = 0f;

    private GUIStyle estiloGUI;
    private Color colorActual = Color.green;

    private void Awake()
    {
        // Fuerza 60 FPS en móvil para evitar que el sistema limite a 30 o caiga a 15 por VSync
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    private void Start()
    {
        tiempoRestante = intervaloActualizacion;
    }

    private void Update()
    {
        float delta = Time.unscaledDeltaTime;
        tiempoRestante -= delta;
        tiempoAcumulado += delta;
        fotogramas++;

        if (tiempoRestante <= 0f)
        {
            fpsActual = fotogramas / Mathf.Max(tiempoAcumulado, 0.0001f);
            msActual = (tiempoAcumulado / Mathf.Max(fotogramas, 1)) * 1000f;

            // Color según rendimiento
            if (fpsActual >= 50f)
                colorActual = new Color(0.2f, 1f, 0.2f, 1f); // Verde
            else if (fpsActual >= 30f)
                colorActual = new Color(1f, 0.85f, 0.2f, 1f); // Amarillo
            else
                colorActual = new Color(1f, 0.25f, 0.25f, 1f); // Rojo

            if (textoTMP != null)
            {
                textoTMP.text = $"{fpsActual:0.0} FPS ({msActual:0.0} ms)";
                textoTMP.color = colorActual;
            }

            tiempoRestante = intervaloActualizacion;
            tiempoAcumulado = 0f;
            fotogramas = 0;
        }
    }

    private void OnGUI()
    {
        // Solo dibujar con OnGUI si no se asignó un componente TextMeshPro o si mostrarOnGUI está activo
        if (!mostrarOnGUI || (textoTMP != null && textoTMP.gameObject.activeInHierarchy))
            return;

        if (estiloGUI == null)
        {
            estiloGUI = new GUIStyle();
            estiloGUI.fontStyle = FontStyle.Bold;
        }

        int tamano = usarTamanoAdaptativo
            ? Mathf.Max(tamanoFuente, Mathf.RoundToInt(Screen.height * 0.028f))
            : tamanoFuente;

        estiloGUI.fontSize = tamano;
        estiloGUI.normal.textColor = colorActual;

        string texto = $"{fpsActual:0.0} FPS\n{msActual:0.0} ms";

        float margen = 20f;
        float ancho = 220f;
        float alto = tamano * 2.4f;
        Rect rect;

        switch (posicion)
        {
            case TextAnchor.UpperLeft:
                estiloGUI.alignment = TextAnchor.UpperLeft;
                rect = new Rect(margen, margen, ancho, alto);
                break;
            case TextAnchor.UpperRight:
                estiloGUI.alignment = TextAnchor.UpperRight;
                rect = new Rect(Screen.width - ancho - margen, margen, ancho, alto);
                break;
            case TextAnchor.LowerLeft:
                estiloGUI.alignment = TextAnchor.LowerLeft;
                rect = new Rect(margen, Screen.height - alto - margen, ancho, alto);
                break;
            case TextAnchor.LowerRight:
                estiloGUI.alignment = TextAnchor.LowerRight;
                rect = new Rect(Screen.width - ancho - margen, Screen.height - alto - margen, ancho, alto);
                break;
            default:
                estiloGUI.alignment = TextAnchor.UpperRight;
                rect = new Rect(Screen.width - ancho - margen, margen, ancho, alto);
                break;
        }

        // Sombra oscura para máxima legibilidad sobre fondos claros
        GUIStyle sombra = new GUIStyle(estiloGUI);
        sombra.normal.textColor = new Color(0f, 0f, 0f, 0.75f);
        GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), texto, sombra);

        // Texto en primer plano
        GUI.Label(rect, texto, estiloGUI);
    }
}

