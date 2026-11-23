using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoPlayPauseToggleUI : MonoBehaviour
{
    [Header("Componentes")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Image botonImagen;

    [Header("Sprites de Estado")]
    [SerializeField] private Sprite spritePlay;   
    [SerializeField] private Sprite spritePausa;  

    private bool estaReproduciendo;

    private void Start()
    {
        if (botonImagen == null)
        {
            botonImagen = GetComponent<Image>();
        }

        if (videoPlayer != null)
        {
            estaReproduciendo = videoPlayer.playOnAwake;
        }
        else
        {
            estaReproduciendo = false;
        }

        ActualizarIcono();
    }

    public void TogglePlayPause()
    {
        if (videoPlayer == null) return;

        estaReproduciendo = !estaReproduciendo;

        if (estaReproduciendo)
        {
            videoPlayer.Play();
        }
        else
        {
            videoPlayer.Pause();
        }

        ActualizarIcono();
    }

    private void ActualizarIcono()
    {
        if (botonImagen == null) return;
        botonImagen.sprite = estaReproduciendo ? spritePausa : spritePlay;
    }
}