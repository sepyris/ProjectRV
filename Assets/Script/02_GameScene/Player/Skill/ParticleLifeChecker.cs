using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleLifeChecker : MonoBehaviour
{
    ParticleSystem particle = null;
    bool hasstarted = false;

    private void Start()
    {
        particle = GetComponent<ParticleSystem>();
    }
    // Update is called once per frame
    void Update()
    {
        if (particle != null)
        {
            if (particle.isPlaying && !hasstarted)
            {
                hasstarted = true;

            }
            if (hasstarted && particle.isStopped)
            {
                Destroy(particle);
                Destroy(gameObject);
            }
        }
    }
}
