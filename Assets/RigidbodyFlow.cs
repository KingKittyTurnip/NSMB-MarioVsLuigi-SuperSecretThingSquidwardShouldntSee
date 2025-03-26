using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigidbodyFlow : MonoBehaviour
{
    public Vector2 force;
    public List<Rigidbody2D> excluded = new List<Rigidbody2D>();
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (excluded.Contains(collision.attachedRigidbody) || collision.attachedRigidbody == null)
            return;
        collision.attachedRigidbody.AddForce(force * Time.fixedDeltaTime, ForceMode2D.Force);
    }
}
