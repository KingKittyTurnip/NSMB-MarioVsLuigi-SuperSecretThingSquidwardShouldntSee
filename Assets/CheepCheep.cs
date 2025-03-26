using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using NSMB.Utils;

public class CheepCheep : KillableEntity
{
    public bool underwater = false;
    public float speed, terminalVelocity;
    public float boundsL, boundsR;
    public override void FixedUpdate()
    {
        if (GameManager.Instance && GameManager.Instance.gameover)
        {
            body.velocity = Vector2.zero;
            body.angularVelocity = 0;
            animator.enabled = false;
            body.isKinematic = true;
            return;
        }

        base.FixedUpdate();

        physics.UpdateCollisions();
        if (physics.hitLeft || physics.hitRight)
        {
            left = physics.hitRight;
        }
        if (!dead)
        {
            if (transform.position.x > boundsR && !left)
            {
                left = true;
            }
            if (transform.position.x < boundsL && left)
            {
                left = false;
            }
        }
        if (underwater)
        {
            body.velocity = new Vector2(speed * (left ? -1 : 1), Mathf.Max(terminalVelocity, body.velocity.y));
        }
        sRenderer.flipX = !left;
        if (underwater)
        {
            body.gravityScale = dead ? 1 : 0;
            body.velocity = new Vector2(body.velocity.x, Mathf.Lerp(body.velocity.y, 0, Time.fixedDeltaTime * 15));
        }
        else
        {
            body.gravityScale = 3;
        }
    }
    public override void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.GetComponent<WaterSplash>() != null && body.velocity.y < 0)
        {
            underwater = true;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(boundsL, transform.position.y, 0), .25f);
        Gizmos.DrawWireSphere(new Vector3(boundsR, transform.position.y, 0), .25f);
    }
}
