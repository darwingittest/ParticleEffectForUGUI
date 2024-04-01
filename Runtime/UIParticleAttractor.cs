using System;
using UnityEngine;
using UnityEngine.Events;

namespace Coffee.UIExtensions
{
    [ExecuteAlways]
    public class UIParticleAttractor : MonoBehaviour
    {
        public enum Movement
        {
            Linear,
            Smooth,
            Sphere
        }

        public enum UpdateMode
        {
            Normal,
            UnscaledTime
        }

        [SerializeField]
        private ParticleSystem[] m_ParticleSystems;

        [Range(0.1f, 10f)]
        [SerializeField]
        private float m_DestinationRadius = 1;

        [Range(0f, 0.95f)]
        [SerializeField]
        private float m_DelayRate;

        [Range(0.001f, 100f)]
        [SerializeField]
        private float m_MaxSpeed = 1;

        [SerializeField]
        private Movement m_Movement;

        [SerializeField]
        private UpdateMode m_UpdateMode;

        [SerializeField]
        private UnityEvent m_OnAttracted;

        private void OnEnable()
        {
            ApplyParticleSystems();
            UIParticleUpdater.Register(this); // Make sure UIParticleUpdater exists in your project or adjust accordingly
        }

        private void OnDisable()
        {
            UIParticleUpdater.Unregister(this); // Adjust according to your project's structure
        }

        private void OnDestroy()
        {
            m_ParticleSystems = null;
        }

        internal void Attract()
        {
            foreach (var particleSystem in m_ParticleSystems)
            {
                if (particleSystem == null) continue;

                var count = particleSystem.particleCount;
                if (count == 0) continue;

                var particles = new ParticleSystem.Particle[count];
                particleSystem.GetParticles(particles);

                var dstPos = GetDestinationPosition(particleSystem);
                for (var i = 0; i < count; i++)
                {
                    var p = particles[i];
                    if (0f < p.remainingLifetime && Vector3.Distance(p.position, dstPos) < m_DestinationRadius)
                    {
                        p.remainingLifetime = 0f;
                        particles[i] = p;
                        m_OnAttracted?.Invoke();
                        continue;
                    }

                    var delayTime = p.startLifetime * m_DelayRate;
                    var duration = p.startLifetime - delayTime;
                    var time = Mathf.Max(0, p.startLifetime - p.remainingLifetime - delayTime);

                    if (time <= 0) continue;

                    p.position = GetAttractedPosition(p.position, dstPos, duration, time, particleSystem);
                    p.velocity *= 0.5f;
                    particles[i] = p;
                }

                particleSystem.SetParticles(particles, count);
            }
        }

        private Vector3 GetDestinationPosition(ParticleSystem particleSystem)
        {
            var psPos = particleSystem.transform.position;
            var attractorPos = transform.position;
            var dstPos = attractorPos;
            var isLocalSpace = particleSystem.main.simulationSpace == ParticleSystemSimulationSpace.Local;

            if (isLocalSpace)
            {
                dstPos = particleSystem.transform.InverseTransformPoint(dstPos);
            }

            return dstPos;
        }

        private Vector3 GetAttractedPosition(Vector3 current, Vector3 target, float duration, float time, ParticleSystem particleSystem)
        {
            var speed = m_MaxSpeed;
            switch (m_UpdateMode)
            {
                case UpdateMode.Normal:
                    speed *= 60 * Time.deltaTime;
                    break;
                case UpdateMode.UnscaledTime:
                    speed *= 60 * Time.unscaledDeltaTime;
                    break;
            }

            switch (m_Movement)
            {
                case Movement.Linear:
                    speed /= duration;
                    break;
                case Movement.Smooth:
                    target = Vector3.Lerp(current, target, time / duration);
                    break;
                case Movement.Sphere:
                    target = Vector3.Slerp(current, target, time / duration);
                    break;
            }

            return Vector3.MoveTowards(current, target, speed);
        }

        private void ApplyParticleSystems()
        {
            // This method could be expanded if there's any initialization needed for each ParticleSystem
            // For example, linking them with UI particles or other specific setup
        }
    }
}
