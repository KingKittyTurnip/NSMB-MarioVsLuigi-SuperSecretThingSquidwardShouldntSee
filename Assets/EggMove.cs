using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class EggMove : KillableEntity
{
    public GameObject goal;
    public Transform pivot;
    public float invuln;
    public Vector2 leftmostPosition, rightmostPosition;
    public Transform eggSprite;
    public Animator eggAnimator;
    public float bobTimer;
    public float startTimer = 1;
    private Vector2 origin;
    private float moveStartTimer, moveEndTimer, movementTimer;
    private bool leftToRight;
    public int health = 8;

    public BoxCollider2D hurtBox;
    public override void Start()
    {
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<BoxCollider2D>();
        audioSource = GetComponent<AudioSource>();
        sRenderer = GetComponent<SpriteRenderer>();
        physics = GetComponent<PhysicsEntity>();
    }
    private void Awake()
    {
        origin = transform.position;
    }

    public override void FixedUpdate()
    {
        hitbox.enabled = invuln <= 0;
        hurtBox.enabled = invuln <= 0;
        
        invuln -= Time.fixedDeltaTime;
        if (dead)
        {
            PhotonNetwork.Destroy(gameObject);
            Instantiate(goal, transform.position, Quaternion.identity);
            Instantiate(Resources.Load("Prefabs/Particle/Puff"), transform.position, Quaternion.identity);
            return;
        }
        eggSprite.transform.rotation = !leftToRight ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
        bobTimer += Time.fixedDeltaTime * ((4 - (health / 2)) + 1);
        base.FixedUpdate();
        if (startTimer > 0)
        {
            transform.position = Vector2.Lerp(rightmostPosition, origin, startTimer);
            startTimer -= Time.fixedDeltaTime * .25f;
        }
        else
        {
            if(movementTimer > 0)
            {
                moveEndTimer = .25f;
                moveStartTimer = .25f;
                if (leftToRight)
                {
                    transform.position = Vector2.Lerp(rightmostPosition, leftmostPosition, movementTimer);
                }
                else
                {
                    transform.position = Vector2.Lerp(leftmostPosition, rightmostPosition, movementTimer);
                }
                movementTimer -= Time.fixedDeltaTime * .125f;
            }
            else if(moveEndTimer > 0)
            {
                moveEndTimer -= Time.fixedDeltaTime;
                if (leftToRight)
                {
                    transform.position = rightmostPosition;
                }
                else
                {
                    transform.position = leftmostPosition;
                }
            }
            else if(moveStartTimer > 0)
            {
                if(moveStartTimer >= .25f)
                {
                    leftToRight = !leftToRight;
                }
                moveStartTimer -= Time.fixedDeltaTime;
                if (leftToRight)
                {
                    transform.position = leftmostPosition;
                }
                else
                {
                    transform.position = rightmostPosition;
                }

            }
            else
            {
                if (leftToRight)
                {
                    transform.position = leftmostPosition;
                }
                else
                {
                    transform.position = rightmostPosition;
                }
                movementTimer = 1;
            }
        }

        transform.position += Vector3.up * Mathf.Sin(bobTimer) * .1f;
        pivot.transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(bobTimer * 2) * 45);
    }
    [PunRPC]
    public override void SpecialKill(bool right, bool groundpound, int combo)
    {
        TakeDamage();
    }

    [PunRPC]
    public override void SpecialKillWithForce(bool right, bool groundpound, int combo)
    {
        health = 1;
        TakeDamage();
    }

    [PunRPC]
    public override void Kill()
    {
        TakeDamage();
    }
    public void TakeDamage()
    {
        if (invuln > 0)
            return;
        eggAnimator.SetTrigger("Hurt");
        health--;
        invuln = 2;
        if(health <= 0)
        {
            base.SpecialKill(true, true, 0);
        }
    }

    public void OnDealDamage()
    {
        eggAnimator.SetTrigger("SmugMF");
    }
    public override void InteractWithPlayer(PlayerController player)
    {
        if (player.Frozen)
            return;

        Vector2 damageDirection = (player.body.position - body.position).normalized;
        bool attackedFromAbove = Vector2.Dot(damageDirection, Vector2.up) > 0.5f && !player.onGround;

        if (!attackedFromAbove && player.CanShell() && player.crouching && !player.inShell)
        {
            photonView.RPC(nameof(SetLeft), RpcTarget.All, damageDirection.x > 0);
        }
        else if (player.invincible > 0 || player.inShell || player.sliding
            || (player.groundpound && player.state != Enums.PowerupState.MiniMushroom && attackedFromAbove)
            || player.state == Enums.PowerupState.MegaMushroom)
        {

            photonView.RPC(nameof(Kill), RpcTarget.All);
        }
        else if (attackedFromAbove)
        {
            if (player.state == Enums.PowerupState.MiniMushroom)
            {
                if (player.groundpound)
                {
                    player.groundpound = false;
                    photonView.RPC(nameof(Kill), RpcTarget.All);
                }
                player.bounce = true;
            }
            else
            {
                photonView.RPC(nameof(Kill), RpcTarget.All);
                player.bounce = !player.groundpound;
            }
            player.photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Enemy_Generic_Stomp);
            player.drill = false;

        }
        else if (player.hitInvincibilityCounter <= 0)
        {
            player.photonView.RPC(nameof(PlayerController.Powerdown), RpcTarget.All, false);
            OnDealDamage();
            photonView.RPC(nameof(SetLeft), RpcTarget.All, damageDirection.x < 0);
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(leftmostPosition, .25f);
        Gizmos.DrawWireSphere(rightmostPosition, .25f);
    }
}
