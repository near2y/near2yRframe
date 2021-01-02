using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectOfflineData : OfflineData
{
    public ParticleSystem[] m_Particles;
    public TrailRenderer[] m_TrailRenders;

    public override void ResetProp()
    {
        base.ResetProp();
        foreach (ParticleSystem particle in m_Particles)
        {
            particle.Clear(true);
            particle.Play();
        }
        foreach (TrailRenderer trail in m_TrailRenders)
        {
            trail.Clear();
        }
    }

    public override void BindData()
    {
        base.BindData();
        m_Particles = gameObject.GetComponentsInChildren<ParticleSystem>(true);
        m_TrailRenders = gameObject.GetComponentsInChildren<TrailRenderer>(true);
    }
}
