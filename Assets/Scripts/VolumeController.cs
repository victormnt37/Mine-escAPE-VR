using UnityEngine;
using UnityEngine.UI;

public class VolumeController : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;
#pragma warning disable CS0414
    [SerializeField] private bool cambiarVolumenHardwareMovil = true;
#pragma warning restore CS0414
    [SerializeField] private string prefsKey = "MasterVolume";

    private const int STREAM_MUSIC = 3;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject audioManager;
    private int maxDeviceVolume = 15;
#endif

    private void Awake()
    {
        if (volumeSlider == null)
            volumeSlider = GetComponent<Slider>();

        InicializarAudioManagerAndroid();
    }

    private void Start()
    {
        float volumenInicial = PlayerPrefs.GetFloat(prefsKey, 1.0f);

#if UNITY_ANDROID && !UNITY_EDITOR
        if (cambiarVolumenHardwareMovil && audioManager != null && maxDeviceVolume > 0)
        {
            int currentDevVol = audioManager.Call<int>("getStreamVolume", STREAM_MUSIC);
            volumenInicial = (float)currentDevVol / maxDeviceVolume;
        }
#endif

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = volumenInicial;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        SetVolume(volumenInicial);
    }

    public void SetVolume(float valor)
    {
        valor = Mathf.Clamp01(valor);
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat(prefsKey, valor);

#if UNITY_ANDROID && !UNITY_EDITOR
        if (cambiarVolumenHardwareMovil && audioManager != null && maxDeviceVolume > 0)
        {
            int targetVolume = Mathf.RoundToInt(valor * maxDeviceVolume);
            audioManager.Call("setStreamVolume", STREAM_MUSIC, targetVolume, 0);
        }
#endif
    }

    public void ToggleMute()
    {
        if (AudioListener.volume > 0.01f)
        {
            SetVolume(0f);
            if (volumeSlider != null) volumeSlider.value = 0f;
        }
        else
        {
            float volGuardado = PlayerPrefs.GetFloat(prefsKey, 1.0f);
            if (volGuardado <= 0.01f) volGuardado = 1.0f;
            SetVolume(volGuardado);
            if (volumeSlider != null) volumeSlider.value = volGuardado;
        }
    }

    private void InicializarAudioManagerAndroid()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext"))
            {
                audioManager = context.Call<AndroidJavaObject>("getSystemService", "audio");
                if (audioManager != null)
                {
                    maxDeviceVolume = audioManager.Call<int>("getStreamMaxVolume", STREAM_MUSIC);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[VolumeController] Error AudioManager Android: " + ex.Message);
        }
#endif
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
    }
}
