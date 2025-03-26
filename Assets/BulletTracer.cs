using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletTracer : MonoBehaviour
{
    public LineRenderer line;
    public float lifetime = 1;
    private float privateLifetime;
    private void Start()
    {
        privateLifetime = lifetime;
    }
    public void Update()
    {
        lifetime -= Time.deltaTime;
        line.textureScale = new Vector2(lifetime / privateLifetime, 1);
        if(lifetime <= 0)
        {
            Destroy(gameObject);
        }
    }
}
