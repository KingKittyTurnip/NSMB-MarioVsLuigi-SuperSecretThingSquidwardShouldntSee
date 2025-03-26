using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BouncyFunThingy : MonoBehaviour
{
    public float strength, poweredStrength;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        OnTriggerStay2D(collision);
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        PlayerController con = collision.gameObject.GetComponent<PlayerController>();
        if (con)
        {
            if (con.groundpound)
            {
                con.body.velocity = (con.body.position - (Vector2)transform.position).normalized * poweredStrength;
                con.groundpound = false;
            }
            else
            {
                con.body.velocity = (con.body.position - (Vector2)transform.position).normalized * strength;
            }
        }
    }
}
