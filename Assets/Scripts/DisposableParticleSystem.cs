using UnityEngine;
using System.Collections;

public class DisposableParticleSystem : MonoBehaviour {

    private ParticleSystem ps;
 
 ////////////////////////////////////////////////////////////////
 
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        if (ps != null)
        {
            if (!ps.IsAlive())
            {
                Destroy(gameObject);
            }
        }
    }
}
