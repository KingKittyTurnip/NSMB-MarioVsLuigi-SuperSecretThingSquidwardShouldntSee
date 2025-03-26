using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.Serialization;
using NSMB.Utils;

public class SandmanBall : MonoBehaviourPun
{
    [SerializeField] private float lifeTime = 5;
    [SerializeField] private PhysicsEntity physics;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private int owner;
    private bool hit = false;
    // Update is called once per frame
    void FixedUpdate()
    {
        if (hit)
        {
            sprite.color = new Color(.5f, .5f, .5f, 1);
            gameObject.layer = Layers.LayerLooseCoin;
            body.includeLayers = 0;
        }
        physics.UpdateCollisions();
        if(physics.onGround)
        {
            body.velocity = new Vector2(body.velocity.x * .99f, body.velocity.y);
            body.angularVelocity = -body.velocity.x * 360;
        }
        lifeTime -= Time.deltaTime;
        if(lifeTime < 1)
        {
            sprite.enabled = Mathf.Sin(lifeTime * 25 * Mathf.Rad2Deg) > 0;
        }
        if(lifeTime < 0 && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hit)
        {
            return;
        }
        hit = true;
        if (physics.onGround)
            return;
        PlayerController con = collision.gameObject.GetComponent<PlayerController>();
        KillableEntity ent = collision.gameObject.GetComponent<KillableEntity>();
        if (con != null)
        {
            if (con.photonView.ViewID != owner)
            {
                con.photonView.RPC(nameof(PlayerController.KnockbackWithForce), RpcTarget.All, body.velocity.x < 0, 1, 0, owner);
            }
        }
        if (ent != null)
        {
            if (ent.photonView.ViewID != owner)
            {
                ent.photonView.RPC(nameof(KillableEntity.SpecialKill), RpcTarget.All, body.velocity.x > 0, false, 0);
            }
        }
    }

    private void OnDestroy()
    {
        SpawnParticle("Prefabs/Particle/Puff", body.position);
    }
    [PunRPC]
    protected void SpawnParticle(string particle, Vector2 worldPos)
    {
        Instantiate(Resources.Load(particle), worldPos, Quaternion.identity);
    }

    [PunRPC]
    public void Launch(Vector2 velocity, int originView)
    {
        body.velocity = velocity;
        owner = originView;
        body.angularVelocity = -body.velocity.x * 360;
    }
}
