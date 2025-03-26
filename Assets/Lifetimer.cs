using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lifetimer : MonoBehaviour
{
    public float lifetime;

    void FixedUpdate()
    {
        lifetime -= Time.fixedDeltaTime;
        if(lifetime <= 0)
        {
            Destroy(gameObject);
        }
    }
}
