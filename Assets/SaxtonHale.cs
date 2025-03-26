using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using NSMB.Utils;
using UnityEditor;
using ExitGames.Client.Photon;

public class SaxtonHale : KillableEntity //wip boss Saxton
{
    public PlayerController target;
    public float maxSpeed, accell, airAcc, airDeacc, jumpDist, spaceDist, crouchDist;
    public Transform saxtonModel;
    private bool BraveJumped;
    public int ticksUntilVoc = 5;
    public int health = 100;
    public float gravScaleNormal = 3, gravScaleDrop = 5;
    public float canPunchTimer = 5;
    public GameObject corpse;
    public GameObject goalOrb;
    [PunRPC]
    public override void SpecialKill(bool right, bool groundpound, int combo)
    {
        if (groundpound)
        {
            health -= 5;
        }
        else
        {
            health--;
        }
        if(health > 0)
        {
            GameManager.Instance.SendAndExecuteEvent(Enums.NetEventIds.ResetTiles, null, SendOptions.SendReliable);
            photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Boss_Saxton_Hit);
            if (groundpound)
            {
                body.velocity = new Vector2(right ? 5 : -5, 5);
            }
            else
            {
                body.velocity += Vector2.right * (right ? .5f : -.5f);
            }
        }
        else if(health <= 0) //todo: ragdoll and spawn goal
        {
            dead = true;
            photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Boss_Saxton_Kill);
        }
    }
    [PunRPC]
    public override void SpecialKillWithForce(bool right, bool groundpound, int combo)
    {
        SpecialKill(right, groundpound, combo);
    }

    public override void FixedUpdate()
    {
        if (dead)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("Die", RpcTarget.All);
            }
            return;
        }
        if(canPunchTimer > 0)
        {
            canPunchTimer -= Time.fixedDeltaTime;
        }
        if (ticksUntilVoc > 0)
        {
            ticksUntilVoc--;
            if (ticksUntilVoc <= 0)
            {
                ticksUntilVoc = Random.Range(200, 1000);
                photonView.RPC(nameof(PlaySoundVarient), RpcTarget.All, Enums.Sounds.Boss_Saxton_Idle, (byte)Random.Range(1, 6));
            }
        }
        physics.UpdateCollisions();
        base.FixedUpdate();

        if(target != null)
        {
            if(canPunchTimer > .7f && canPunchTimer < .8f)
            {
                if (AreAABBsIntersecting(target.body.position, target.MainHitbox.size, body.position + (Vector2.right * (left ? -1 : 1)), hitbox.size / 2))
                {
                    target.photonView.RPC(nameof(PlayerController.Powerdown), RpcTarget.All, false);
                }
            }
            bool lookAtTarget = false;
            bool moveRight = target.body.position.x > body.position.x;
            if(body.position.y > target.body.position.y + 1 && physics.onGround)
            {
                moveRight = body.velocity.x > 0;
            }
            else if(Mathf.Abs(body.position.x - target.body.position.x) < spaceDist && physics.onGround && Mathf.Abs(target.body.velocity.x) < 2)
            {
                moveRight = !moveRight;
            }
            if(Mathf.Abs(body.position.x - target.body.position.x) < crouchDist && physics.onGround)
            {
                if(Mathf.Abs(body.velocity.x) < 2)
                {
                    lookAtTarget = true;
                }
                SaxtonPunch(false);
            }
            if (physics.onGround)
            {
                BraveJumped = false;
                if (moveRight)
                {
                    left = false;
                    body.velocity += Vector2.right * (body.velocity.x > 0 ? accell : airDeacc) * Time.fixedDeltaTime;
                    if(body.velocity.x > maxSpeed)
                    {
                        body.velocity = new Vector2(maxSpeed, body.velocity.y);
                    }
                }
                else
                {
                    left = true;
                    body.velocity += Vector2.left * (body.velocity.x < 0 ? accell : airDeacc) * Time.fixedDeltaTime;

                    if (body.velocity.x < -maxSpeed)
                    {
                        body.velocity = new Vector2(-maxSpeed, body.velocity.y);
                    }
                }
            }
            else
            {
                if (moveRight)
                {
                    left = false;
                    body.velocity += Vector2.right * (body.velocity.x > 0 ? airAcc : airDeacc) * Time.fixedDeltaTime;
                    if (body.velocity.x > maxSpeed)
                    {
                        body.velocity = new Vector2(maxSpeed, body.velocity.y);
                    }
                }
                else
                {
                    left = true;
                    body.velocity += Vector2.left * (body.velocity.x < 0 ? airAcc : airDeacc) * Time.fixedDeltaTime;

                    if (body.velocity.x < -maxSpeed)
                    {
                        body.velocity = new Vector2(-maxSpeed, body.velocity.y);
                    }
                }
            }
            if (lookAtTarget)
            {
                left = target.body.position.x < body.position.x;
            }
            //animator.SetFloat("LookingUp", (target.body.position.y - body.position.y) / 90);
            if(Mathf.Abs(body.position.x - target.body.position.x) < jumpDist && body.position.y < target.body.position.y + 1 && body.position.y + (hitbox.size.y) < target.body.position.y || physics.hitLeft || physics.hitRight)
            {
                Jump();
            }
            if(!physics.onGround && body.velocity.y < 0 && Mathf.Abs(body.position.x - target.body.position.x) < jumpDist && body.position.y < target.body.position.y + 1 && body.position.y + (hitbox.size.y) < target.body.position.y)
            {
                BraveJump();
            }
            if(!physics.onGround && body.velocity.y < 0 && Mathf.Abs(body.position.x - target.body.position.x) < jumpDist && body.position.y > target.body.position.y - 1 && body.position.y - (hitbox.size.y) > target.body.position.y)
            {
                body.gravityScale = gravScaleDrop;
            }
            else
            {
                body.gravityScale = gravScaleNormal;
            }
            animator.SetBool("Crouch", lookAtTarget);
        }
        else
        {
            target = PlayerController.FindNearbyPlayer(body.position);
        }
        animator.SetFloat("XVelocity", body.velocity.x * (left ? -1 : 1));
        animator.SetBool("OnGround", physics.onGround && body.velocity.y < .1f);
        saxtonModel.rotation = Quaternion.Lerp(saxtonModel.rotation, Quaternion.Euler(0, 112 * (left ? -1 : 1), 0), Time.fixedDeltaTime * 15);


    }

    public void Jump()
    {
        if (!physics.onGround)
            return;
        body.velocity = new Vector2(body.velocity.x, 15);
        physics.onGround = false;
        animator.SetTrigger("Jump");
    }

    public void BraveJump() //screw gravity
    {
        if(BraveJumped) return;

        photonView.RPC(nameof(PlaySoundVarient), RpcTarget.All, Enums.Sounds.Boss_Saxton_BraveJump, (byte)Random.Range(1, 9));
        BraveJumped = true;
        body.velocity = new Vector2(body.velocity.x, 15);
        animator.SetTrigger("BraveJump");
    }

    public void SweepingCharge()
    {

    }

    public void SaxtonPunch(bool strong)
    {
        if (canPunchTimer > 0)
            return;
        animator.SetTrigger("PunchR");
        canPunchTimer = 1;
    }

    public override void InteractWithPlayer(PlayerController player)
    {
        if (player.Frozen)
            return;

        Vector2 damageDirection = (player.body.position - body.position).normalized;
        bool attackedFromAbove = Vector2.Dot(damageDirection, Vector2.up) > 0.75f && !player.onGround && player.groundpound;
        bool steppingFromBelow = Vector2.Dot(damageDirection, Vector2.up) < -0.75f;

        if (!attackedFromAbove && player.CanShell() && player.crouching && !player.inShell)
        {
            photonView.RPC(nameof(SetLeft), RpcTarget.All, damageDirection.x > 0);
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
            player.groundpound = false;

        }
        else if (steppingFromBelow)
        {
            player.photonView.RPC(nameof(PlayerController.Powerdown), RpcTarget.All, false);
            photonView.RPC(nameof(SetLeft), RpcTarget.All, damageDirection.x < 0);
            body.velocity = new Vector2(body.velocity.x, 15);
            physics.onGround = false;
            animator.SetTrigger("Jump");
        }
        else if (player.hitInvincibilityCounter <= 0)
        {
            player.photonView.RPC(nameof(PlayerController.Knockback), RpcTarget.All, !left, 1, true, photonView.ViewID);
        }
    }
    [PunRPC]
    public override void Unfreeze(byte reasonByte)
    {
        Frozen = false;
        animator.enabled = true;
        if (body)
            body.isKinematic = false;
        foreach (BoxCollider2D hitboxes in GetComponentsInChildren<BoxCollider2D>(true))
        {
            hitboxes.enabled = true;
        }
        hitbox.enabled = true;
        audioSource.enabled = true;

        SpecialKill(false, false, 0);
    }

    [PunRPC]
    public void PlaySoundVarient(Enums.Sounds sound, byte variant)
    {
        PlaySoundVarient(sound, variant, 1);
    }

    [PunRPC]
    public void PlaySoundVarient(Enums.Sounds sound, byte variant, float volume)
    {
        audioSource.Stop();
        audioSource.clip = sound.GetClip(null, variant);
        audioSource.Play();
    }

    public bool IsPointInAABB(Vector2 point, Vector2 aabbPosition, Vector2 aabbScale)
    {
        return point.x >= aabbPosition.x - aabbScale.x / 2 && point.x <= aabbPosition.x + aabbScale.x / 2 &&
               point.y >= aabbPosition.y - aabbScale.y / 2 && point.y <= aabbPosition.y + aabbScale.y / 2;
    }
    public bool AreAABBsIntersecting(Vector2 aabb1Position, Vector2 aabb1Scale, Vector2 aabb2Position, Vector2 aabb2Scale)
    {
        bool xOverlap = aabb1Position.x - aabb1Scale.x / 2 < aabb2Position.x + aabb2Scale.x / 2 &&
                        aabb1Position.x + aabb1Scale.x / 2 > aabb2Position.x - aabb2Scale.x / 2;

        bool yOverlap = aabb1Position.y - aabb1Scale.y / 2 < aabb2Position.y + aabb2Scale.y / 2 &&
                        aabb1Position.y + aabb1Scale.y / 2 > aabb2Position.y - aabb2Scale.y / 2;

        return xOverlap && yOverlap;
    }

    [PunRPC]
    public void Die()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        GameObject currentCorpse = Instantiate(corpse, saxtonModel.transform.position, saxtonModel.transform.rotation);
        currentCorpse.GetComponentInChildren<Rigidbody>().velocity = body.velocity;
        Instantiate(goalOrb, body.position, Quaternion.identity);
    }
}
