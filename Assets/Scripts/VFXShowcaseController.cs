using UnityEngine;

namespace MineEscape
{
    public class VFXShowcaseController : MonoBehaviour
    {
        [Header("Referencias de VFX")]
        public ParticleSystem avalanchaFrente;
        public ParticleSystem derrumbeTecho;
        public ParticleSystem nieblaResidual;
        public ParticleSystem impactoRoca;
        public ParticleSystem motasAmbiente;

        private void OnGUI()
        {
            GUI.Box(new Rect(20, 20, 260, 310), "Visor de VFX - Avalancha y Cueva");

            GUILayout.BeginArea(new Rect(30, 50, 240, 270));

            if (GUILayout.Button("1. Frente Avalancha (Loop)"))
            {
                TogglePS(avalanchaFrente);
            }

            if (GUILayout.Button("2. Derrumbe Techo (Loop)"))
            {
                TogglePS(derrumbeTecho);
            }

            if (GUILayout.Button("3. Niebla Residual (Loop)"))
            {
                TogglePS(nieblaResidual);
            }

            if (GUILayout.Button("4. Impacto Roca (One-Shot)"))
            {
                if (impactoRoca != null)
                {
                    impactoRoca.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    impactoRoca.Play(true);
                }
            }

            if (GUILayout.Button("5. Motas Ambiente Cueva"))
            {
                TogglePS(motasAmbiente);
            }

            GUILayout.Space(12);

            GUI.color = Color.green;
            if (GUILayout.Button("¡ACTIVAR TODO (Simulacion)!"))
            {
                ActivarTodo();
            }

            GUI.color = Color.red;
            if (GUILayout.Button("Detener Todo"))
            {
                DetenerTodo();
            }
            GUI.color = Color.white;

            GUILayout.EndArea();
        }

        private void TogglePS(ParticleSystem ps)
        {
            if (ps == null) return;
            if (ps.isPlaying)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            else
            {
                ps.Play(true);
            }
        }

        public void ActivarTodo()
        {
            if (avalanchaFrente != null) avalanchaFrente.Play(true);
            if (derrumbeTecho != null) derrumbeTecho.Play(true);
            if (nieblaResidual != null) nieblaResidual.Play(true);
            if (motasAmbiente != null) motasAmbiente.Play(true);
            if (impactoRoca != null)
            {
                impactoRoca.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                impactoRoca.Play(true);
            }
        }

        public void DetenerTodo()
        {
            if (avalanchaFrente != null) avalanchaFrente.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (derrumbeTecho != null) derrumbeTecho.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (nieblaResidual != null) nieblaResidual.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (motasAmbiente != null) motasAmbiente.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (impactoRoca != null) impactoRoca.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}

