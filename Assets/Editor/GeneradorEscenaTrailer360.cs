using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace MineEscape.Editor
{
    [InitializeOnLoad]
    public static class GeneradorEscenaTrailer360
    {
        private const string RutaEscena = "Assets/Scenes/Trailer360.unity";
        private const string RutaRT = "Assets/VideoTrailer/Trailer360_RT.renderTexture";
        private const string RutaMaterial = "Assets/VideoTrailer/Trailer360_Skybox.mat";
        private const string RutaVideo = "Assets/VideoTrailer/Trailer_360.mp4";
        private const string GuidXROrigin = "f6336ac4ac8b4d34bc5072418cdc62a0";

        static GeneradorEscenaTrailer360()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && !File.Exists(RutaEscena))
                {
                    CrearOActualizarEscena();
                }
            };
        }

        [MenuItem("MineEscape/Crear o Regenerar Escena Trailer 360")]
        public static void CrearOActualizarEscena()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            // 1. Crear o cargar RenderTexture (sin mipmaps, resolución 3840x1920)
            RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RutaRT);
            if (rt == null)
            {
                rt = new RenderTexture(3840, 1920, 0, RenderTextureFormat.ARGB32);
                rt.useMipMap = false;
                rt.autoGenerateMips = false;
                AssetDatabase.CreateAsset(rt, RutaRT);
            }

            // 2. Crear o cargar Material Skybox/Panoramic
            Material skyMat = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
            if (skyMat == null)
            {
                Shader skyShader = Shader.Find("Skybox/Panoramic");
                skyMat = new Material(skyShader);
                AssetDatabase.CreateAsset(skyMat, RutaMaterial);
            }
            skyMat.mainTexture = rt;
            EditorUtility.SetDirty(skyMat);

            AssetDatabase.SaveAssets();

            // 3. Crear nueva escena de forma aditiva para no cerrar la escena actual del usuario
            Scene escenaAnterior = SceneManager.GetActiveScene();
            Scene escenaTrailer = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(escenaTrailer);

            try
            {
                RenderSettings.skybox = skyMat;

                // 4. Instanciar XR Origin (XR Rig) para VR 360 y rayos
                string rutaXROrigin = AssetDatabase.GUIDToAssetPath(GuidXROrigin);
                if (!string.IsNullOrEmpty(rutaXROrigin))
                {
                    GameObject prefabXR = AssetDatabase.LoadAssetAtPath<GameObject>(rutaXROrigin);
                    if (prefabXR != null)
                    {
                        GameObject xrInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabXR, escenaTrailer);
                        xrInstance.transform.position = Vector3.zero;
                        xrInstance.transform.rotation = Quaternion.identity;
                    }
                }
                else
                {
                    GameObject camGo = new GameObject("Main Camera");
                    camGo.tag = "MainCamera";
                    camGo.AddComponent<Camera>();
                    camGo.AddComponent<AudioListener>();
                    SceneManager.MoveGameObjectToScene(camGo, escenaTrailer);
                }

                // 5. EventSystem compatible con XR + Ratón
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                XRUIInputModule xrInput = esGo.AddComponent<XRUIInputModule>();
                xrInput.enableXRInput = true;
                xrInput.enableMouseInput = true;
                SceneManager.MoveGameObjectToScene(esGo, escenaTrailer);

                // 6. Controlador + VideoPlayer 360
                GameObject vpGo = new GameObject("VideoPlayer360");
                VideoPlayer vp = vpGo.AddComponent<VideoPlayer>();
                AudioSource audio = vpGo.AddComponent<AudioSource>();
                audio.playOnAwake = false;
                vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
                vp.SetTargetAudioSource(0, audio);
                vp.renderMode = VideoRenderMode.RenderTexture;
                vp.targetTexture = rt;
                vp.playOnAwake = false;

                VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(RutaVideo);
                if (clip != null) vp.clip = clip;

                Trailer360Controller controller = vpGo.AddComponent<Trailer360Controller>();
                SceneManager.MoveGameObjectToScene(vpGo, escenaTrailer);

                // 7. Canvas World Space con los 3 botones: Pausa, Volver a ver, Salir
                GameObject canvasGo = new GameObject("Canvas_Controles360");
                Canvas canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
                canvasGo.AddComponent<TrackedDeviceGraphicRaycaster>();

                RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
                canvasRect.position = new Vector3(0f, 0.85f, 2.3f);
                canvasRect.sizeDelta = new Vector2(900f, 160f);
                canvasRect.localScale = Vector3.one * 0.0022f;
                SceneManager.MoveGameObjectToScene(canvasGo, escenaTrailer);

                // Fondo del panel
                GameObject panelGo = new GameObject("Panel_Controles");
                panelGo.transform.SetParent(canvasGo.transform, false);
                Image panelImg = panelGo.AddComponent<Image>();
                panelImg.color = new Color(0.08f, 0.08f, 0.1f, 0.78f);
                RectTransform panelRect = panelGo.GetComponent<RectTransform>();
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;

                HorizontalLayoutGroup layout = panelGo.AddComponent<HorizontalLayoutGroup>();
                layout.padding = new RectOffset(30, 30, 25, 25);
                layout.spacing = 28f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;

                // Botón 1: Pausa
                (Button btnPausa, TextMeshProUGUI txtPausa) = CrearBoton(panelGo.transform, "Btn_Pausa", "Pausa", new Color(0.22f, 0.45f, 0.72f, 1f));
                UnityEventTools.AddPersistentListener(btnPausa.onClick, controller.AlternarPausa);

                // Botón 2: Volver a ver
                (Button btnReplay, _) = CrearBoton(panelGo.transform, "Btn_VolverAVer", "Volver a ver", new Color(0.25f, 0.58f, 0.36f, 1f));
                UnityEventTools.AddPersistentListener(btnReplay.onClick, controller.VolverAVer);

                // Botón 3: Salir
                (Button btnSalir, _) = CrearBoton(panelGo.transform, "Btn_Salir", "Salir", new Color(0.72f, 0.24f, 0.24f, 1f));
                UnityEventTools.AddPersistentListener(btnSalir.onClick, controller.Salir);

                // Asignar campos serializados en Trailer360Controller
                SerializedObject so = new SerializedObject(controller);
                so.FindProperty("videoPlayer").objectReferenceValue = vp;
                so.FindProperty("videoClip").objectReferenceValue = clip;
                so.FindProperty("renderTexture").objectReferenceValue = rt;
                so.FindProperty("skyboxMaterial").objectReferenceValue = skyMat;
                so.FindProperty("textoBotonPausa").objectReferenceValue = txtPausa;
                so.ApplyModifiedPropertiesWithoutUndo();

                // Guardar escena y registrar en Build Settings
                EditorSceneManager.SaveScene(escenaTrailer, RutaEscena);
                AgregarEscenaABuildSettings(RutaEscena);

                Debug.Log($"[Trailer360] Escena generada y añadida a Build Settings: {RutaEscena}");
            }
            finally
            {
                if (escenaAnterior.IsValid())
                {
                    SceneManager.SetActiveScene(escenaAnterior);
                }
                EditorSceneManager.CloseScene(escenaTrailer, true);
            }
        }

        private static (Button, TextMeshProUGUI) CrearBoton(Transform padre, string nombre, string texto, Color colorFondo)
        {
            GameObject btnGo = new GameObject(nombre);
            btnGo.transform.SetParent(padre, false);

            Image img = btnGo.AddComponent<Image>();
            img.color = colorFondo;

            Button btn = btnGo.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = colorFondo;
            colors.highlightedColor = colorFondo * 1.25f;
            colors.pressedColor = colorFondo * 0.75f;
            btn.colors = colors;

            GameObject txtGo = new GameObject("Text (TMP)");
            txtGo.transform.SetParent(btnGo.transform, false);
            TextMeshProUGUI tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = texto;
            tmp.fontSize = 36f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            RectTransform txtRect = txtGo.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(10f, 5f);
            txtRect.offsetMax = new Vector2(-10f, -5f);

            return (btn, tmp);
        }

        private static void AgregarEscenaABuildSettings(string ruta)
        {
            List<EditorBuildSettingsScene> escenas = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (EditorBuildSettingsScene escena in escenas)
            {
                if (escena.path == ruta)
                {
                    if (!escena.enabled)
                    {
                        escena.enabled = true;
                        EditorBuildSettings.scenes = escenas.ToArray();
                    }
                    return;
                }
            }

            escenas.Add(new EditorBuildSettingsScene(ruta, true));
            EditorBuildSettings.scenes = escenas.ToArray();
        }
    }
}
