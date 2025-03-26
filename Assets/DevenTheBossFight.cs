using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UIElements;
using NSMB.Utils;
using UnityEngine.Events;

public class DevenTheBossFight : KillableEntity, IPunObservable //congrats Deven! on being the first boss to be playable in multiplayer!!!
{
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext((int)attackPhase);
            stream.SendNext(left);
            stream.SendNext(body.velocity);
            stream.SendNext(attackTimer);
            stream.SendNext(health);
        }
        else
        {
            transform.position = (Vector3)stream.ReceiveNext();
            attackPhase = (DevenAttackCyclePhase)stream.ReceiveNext();
            left = (bool)stream.ReceiveNext();
            body.velocity = (Vector2)stream.ReceiveNext();
            attackTimer = (float)stream.ReceiveNext();
            health = (int)stream.ReceiveNext();
        }
    }

    public SpeechBoxTrigger speechTrigger;
    public bool hologramsSpawned = false;
    public DevenTheBossFight originalDeven;
    public string ourPrefab;
    public Vector2 secondPhasePosL, secondPhasePosR;
    public float centerStage, centerStagePhase1, centerStagePhase2;
    public int health = 16;
    public PlayerController targetPlayer;
    private bool onGroundCheck = false;
    public float jumpDelay = 0, attackTimer;
    private Vector2 targetPosition;
    public GameObject model;
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public Material[] hologramMats = new Material[2];
    public bool fake = false;
    public string fireball = "Prefabs/BeegFireball";
    private bool movingUp = false;
    public int phase = 0;
    public List<DevenTheBossFight> holograms = new();
    public enum DevenAttackCyclePhase
    {
        None,
        Static,
        Death,
        SlamStart, //normal Slam starting state
        CloneSlamStart, //create holograms and shuffle them around at the top of the stage, with this Deven included. 
        Slam, //actual slamming action
        SlamTransition, //transitioning into the second phase by homing in on the player, and transitioning to TransitionCharge. 
        TransitionCharge, //grab player, land on ground, slam into wall. Start 2nd phase. 
        FiveGuysBurgersAndFriesAtFreddys, //don't blame me for this name. Transition state after entering the second phase boss arena, where Deven slams you into the far wall. 
    }
    public DevenAttackCyclePhase attackPhase = DevenAttackCyclePhase.None;
    public override void Start()
    {
        GameManager.Instance.SetBossMusic(true);
        speechTrigger.box = FindObjectOfType<SpeechBoxCanvas>(true);
        base.Start();
        if (fake)
        {
            skinnedMeshRenderer.materials = hologramMats;
        }
    }
    public override void FixedUpdate()
    {
        onGroundCheck = physics.onGround;
        centerStage = phase == 1 ? centerStagePhase2 : centerStagePhase1;
        if(!fake)
            GameManager.Instance.bossCamOrigin.x = phase > 0 ? centerStagePhase2 : centerStagePhase1;
        if(jumpDelay > 0)
        {
            jumpDelay -= Time.fixedDeltaTime;
        }
        base.FixedUpdate();
        if(targetPlayer == null)
        {
            ChangeTarget(FindNearestPlayer());
        }
        bool up = false;

        if (phase == 0)
        {
            up = body.velocity.y > 0;
        }
        else
        {
            up = body.velocity.y > 0 ? body.velocity.y > 2 : body.velocity.y > -2;
        }
        if (attackPhase == DevenAttackCyclePhase.None)
        {
            GameManager.Instance.paralizePlayer = false; //failsafe
            body.gravityScale = 1.354167f;
            physics.UpdateCollisions();

            if (Random.value > .9)
            {
                Jump();
            }

            if (physics.onGround)
            {
                body.velocity = Vector2.Lerp(body.velocity, new Vector2(0, body.velocity.y), Time.fixedDeltaTime * 15);
                animator.SetBool("Moving", Mathf.Abs(body.velocity.x) > .05f);
                if (!onGroundCheck)
                {
                    jumpDelay = .1f;
                }
            }
            else
            {
                if(!up && movingUp)
                {
                    if(health == 0)
                    {
                        attackPhase = DevenAttackCyclePhase.SlamStart;
                    }
                    else
                    {
                        ShootFireball();
                    }
                }
                if (physics.hitLeft || physics.hitRight)
                {
                    body.velocity = new Vector2(physics.hitLeft ? 5 : -5, 8);
                    if(phase == 1)
                    {
                        if (Random.value > .75f)
                        {
                            attackPhase = DevenAttackCyclePhase.CloneSlamStart;
                        }
                        else if(Random.value > .75f)
                        {
                            attackPhase = DevenAttackCyclePhase.SlamStart;
                        }
                    }
                    else
                    {
                        if(Random.value > .5f || health == 8)
                        {
                            attackPhase = DevenAttackCyclePhase.SlamStart;
                        }
                    }
                    left = physics.hitRight;
                }
            }

            animator.SetBool("OnGround", physics.onGround);
        } 
        if (attackPhase == DevenAttackCyclePhase.Death)
        {
            GameManager.Instance.music.pitch = Mathf.Lerp(GameManager.Instance.music.pitch, 0, Time.fixedDeltaTime * 3);
            PlayerPrefs.SetInt("DevenUnlocked", 1);
            GameManager.Instance.allowRuning = false; //walk up to Deven
            body.gravityScale = 5f;
            body.velocity = new Vector2(0, body.velocity.y);
            Vector3 pos = transform.position;
            pos.x = Mathf.Lerp(pos.x, centerStage + (left ? 4 : -4), Time.fixedDeltaTime * 2);
            transform.position = pos;
            if(speechTrigger)
                speechTrigger.gameObject.SetActive(Mathf.Abs(transform.position.x - (centerStage + (left ? 4 : -4))) < .125f);
        } 
        if (attackPhase == DevenAttackCyclePhase.Static)
        {
            GameManager.Instance.music.pitch = Mathf.Lerp(GameManager.Instance.music.pitch, 1, Time.fixedDeltaTime * 3);
            GameManager.Instance.SetFinalBossMusic(true);
            GameManager.Instance.SetBossMusic(false);
            body.velocity = Vector2.zero;
            body.isKinematic = true;
        } 
        else if(attackPhase == DevenAttackCyclePhase.SlamStart)
        {
            GameManager.Instance.paralizePlayer = false; //failsafe
            physics.UpdateCollisions();
            if (physics.onGround)
            {
                attackPhase = DevenAttackCyclePhase.Slam;
                body.velocity = Vector2.zero;
                animator.SetTrigger("Slam Land");
                animator.SetBool("OnGround", true);
                animator.SetBool("Moving", false);
            }
            if (body.velocity.y < 0)
            {
                body.isKinematic = true;
                body.velocity = Vector2.zero;
                attackTimer = .8f;
            }
            if(attackTimer > 0)
            {
                targetPosition = targetPlayer.transform.position;
                left = targetPosition.x < transform.position.x;
                attackTimer -= Time.fixedDeltaTime;
                if(attackTimer <= 0)
                {
                    body.velocity = (targetPosition - (Vector2)transform.position).normalized * 20;
                    body.velocity = new Vector2(body.velocity.x, -Mathf.Abs(body.velocity.y)); //just in case the player is above Deven

                    if ((phase == 0 && health == 8) || health == 0)
                    {
                        attackPhase = DevenAttackCyclePhase.SlamTransition;
                    }
                    else
                    {
                        attackPhase = DevenAttackCyclePhase.Slam;
                    }
                    animator.ResetTrigger("Slam Land");
                    animator.SetTrigger("SlamStart");
                    body.gravityScale = 0;
                }
            }
        }
        else if(attackPhase == DevenAttackCyclePhase.CloneSlamStart) //create hologram Devens, syncronize with them to create a ceiling of Devens, and slam directly downward.
        {
            GameManager.Instance.paralizePlayer = false; //failsafe
            if (!fake)
            {
                CreateHoloDevens(8);
            }
            if (attackTimer > 0)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 5);
                left = targetPosition.x < transform.position.x;
                attackTimer -= Time.fixedDeltaTime;
                if(attackTimer <= 0)
                {
                    body.velocity = Vector2.down * 20;

                    attackPhase = DevenAttackCyclePhase.Slam;
                    animator.ResetTrigger("Slam Land");
                    animator.SetTrigger("SlamStart");
                    body.gravityScale = 0;
                }
            }
            else
            {
                body.isKinematic = true;
                body.velocity = Vector2.zero;
                attackTimer = 2f;
            }
        }
        else if(attackPhase == DevenAttackCyclePhase.Slam)
        {
            hologramsSpawned = false;
            body.isKinematic = false;
            physics.UpdateCollisions();
            if (physics.onGround)
            {
                if (!onGroundCheck)
                {
                    CameraController.ScreenShake = .25f;
                }
                if (fake)
                {
                    PhotonNetwork.Destroy(gameObject);
                    return;
                }
                body.velocity = Vector2.zero;
                animator.SetTrigger("Slam Land");
                animator.SetBool("OnGround", true);
                animator.SetBool("Moving", false);
            }
            else
            {
                if (physics.hitLeft || physics.hitRight || physics.hitRoof)
                {
                    if (fake)
                    {
                        PhotonNetwork.Destroy(gameObject);
                        return;
                    }
                    body.velocity = new Vector2(physics.hitLeft ? 5 : -5, 5);
                    attackPhase = DevenAttackCyclePhase.SlamStart;
                    animator.Play("Jump");
                    body.gravityScale = 2;
                }
            }
            if(animator.GetCurrentAnimatorStateInfo(0).IsName("Wait") || animator.GetCurrentAnimatorStateInfo(0).IsName("Desperation"))
            {
                ChangeTarget(FindNearestPlayer());
                attackPhase = DevenAttackCyclePhase.None;
            }
        }
        else if(attackPhase == DevenAttackCyclePhase.SlamTransition)
        {
            body.isKinematic = false;
            physics.UpdateCollisions();

            targetPosition = targetPlayer.transform.position;
            left = targetPosition.x < transform.position.x;
            body.velocity = (targetPosition - (Vector2)transform.position).normalized * 20;
        }
        else if(attackPhase == DevenAttackCyclePhase.TransitionCharge)
        {
            physics.UpdateCollisions();
            body.gravityScale = 1.5f;
            targetPlayer.facingRight = left;
            GameManager.Instance.paralizePlayer = true;
            GameManager.Instance.allowRuning = false;
            if (physics.onGround)
            {
                if(health > 0)
                {
                    if (!onGroundCheck)
                    {
                        CameraController.ScreenShake = .5f;
                    }
                    body.velocity += Vector2.right * (left ? -45 : 45) * Time.fixedDeltaTime;
                    if (physics.hitRight || physics.hitLeft)
                    {
                        GameManager.Instance.allowRuning = true;
                        CameraController.ScreenShake = .5f;
                        transform.position = left ? secondPhasePosL : secondPhasePosR;
                        body.velocity = Vector2.right * (left ? -20 : 20);
                        attackPhase = DevenAttackCyclePhase.FiveGuysBurgersAndFriesAtFreddys;
                        phase = 1;
                        photonView.RPC(nameof(HackilyTransitionEveryone), RpcTarget.All);
                    }
                }
                else
                {
                    if (!onGroundCheck)
                    {
                        body.velocity = Vector2.right * (left ? -5 : 5);
                        CameraController.ScreenShake = .5f;
                    }
                    body.velocity += Vector2.right * (left ? -5 : 5) * Time.fixedDeltaTime;
                    if(targetPlayer.justJumped)
                    {
                        body.velocity += Vector2.right * (left ? 1f : -1f);
                    }
                    if (physics.hitRight || physics.hitLeft)
                    {
                        GameManager.Instance.paralizePlayer = false;
                        CameraController.ScreenShake = .5f;
                        attackPhase = DevenAttackCyclePhase.None;
                        targetPlayer.photonView.RPC(nameof(PlayerController.Powerdown), RpcTarget.All, false);
                        targetPlayer.photonView.RPC(nameof(PlayerController.Knockback), RpcTarget.All, left, 0, false, photonView.ViewID);
                        animator.Play("Jump");
                        targetPlayer.animator.Play("knockback-getup");
                    }

                    if ((left && transform.position.x > centerStage) || (!left && transform.position.x < centerStage))
                    {
                        transform.position -= (Vector3)body.velocity * 2 * Time.fixedDeltaTime;
                        photonView.RPC(nameof(TakeDamage), RpcTarget.All, left, false);
                        targetPlayer.body.velocity = Vector2.zero;
                        GameManager.Instance.paralizePlayer = false;
                        CameraController.ScreenShake = .5f;
                        targetPlayer.animator.Play("carry-throw");
                    }
                }
            }
            else if(health == 0)
            {
                Vector3 currentPosition = transform.position;
                currentPosition.x = Mathf.Lerp(currentPosition.x, centerStage, Time.fixedDeltaTime * 5);
                transform.position = currentPosition;
            }

            if (attackPhase != DevenAttackCyclePhase.Death)
            {
                targetPlayer.animator.Play("DevenPush");
                targetPlayer.transform.position = (Vector2)transform.position + (((Vector2.up * (physics.onGround ? 0 : 1)) + (Vector2.right * (left ? -1 : 1))) / 4);
                targetPlayer.body.velocity = body.velocity;
            }
        }
        else if(attackPhase == DevenAttackCyclePhase.FiveGuysBurgersAndFriesAtFreddys)
        {
            physics.UpdateCollisions();
            body.gravityScale = 1.2f;
            targetPlayer.facingRight = left;
            GameManager.Instance.paralizePlayer = true;
            targetPlayer.body.velocity = body.velocity;
            targetPlayer.transform.position = (Vector2)transform.position + ((Vector2.up + (Vector2.right * (left ? -1 : 1))) / 4);

            if (physics.hitLeft || physics.hitRight)
            {
                CameraController.ScreenShake = .5f;
                targetPlayer.photonView.RPC(nameof(PlayerController.KnockbackWithForce), RpcTarget.All, !left, 0, false, photonView.ViewID);
                GameManager.Instance.paralizePlayer = false;
                body.velocity = new Vector2(physics.hitLeft ? 5 : -5, 8);
                attackPhase = DevenAttackCyclePhase.None;
                animator.Play("Jump");
                animator.SetBool("Desperate", true);
            }

        }

        model.transform.rotation = Quaternion.Lerp(model.transform.rotation, Quaternion.Euler(0, left ? -110 : 110, 0), Time.fixedDeltaTime * 25);
        if(phase == 0)
        {
            movingUp = body.velocity.y > 0;
        }
        else
        {
            movingUp = body.velocity.y > 0 ? body.velocity.y > 2 : body.velocity.y > -2;
        }
    }

    public void Jump()
    {
        if (!photonView.IsMine)
            return;
        if (physics.onGround && jumpDelay <= 0)
        {
            if (phase == 0 && health == 8)
            {
                body.velocity = new Vector2(transform.position.x > centerStage ? 5 : -5, 8);
            }
            else
            {
                body.velocity = new Vector2(Random.value > .5f ? 5 : -5, 8);
            }
            physics.onGround = false;
            left = targetPlayer.transform.position.x < transform.position.x;
        }
    }

    public void ShootFireball()
    {
        if (photonView.IsMine)
        {
            BossFireball fireball = PhotonNetwork.Instantiate(this.fireball, transform.position, Quaternion.identity).GetComponent<BossFireball>();
            fireball.photonView.RPC(nameof(BossFireball.OnFireballShot), RpcTarget.All, (Vector2)((targetPlayer.transform.position - transform.position).normalized * 10));
        }
    }

    [PunRPC]
    public override void Kill()
    {
        if (fake)
        {
            PhotonNetwork.Destroy(gameObject);
            return;
        }
    }

    [PunRPC]
    public override void SpecialKill(bool right, bool groundpound, int combo)
    {
        if (fake)
        {
            PhotonNetwork.Destroy(gameObject);
            return;
        }
    }

    [PunRPC]
    public override void SpecialKillWithForce(bool right, bool groundpound, int combo)
    {
        if (fake)
        {
            PhotonNetwork.Destroy(gameObject);
            return;
        }
    }

    [PunRPC]
    public void TakeDamage(bool right, bool groundpound)
    {
        if (fake)
        {
            PhotonNetwork.Destroy(gameObject);
            return;
        }
        targetPlayer.groundpound = false;
        targetPlayer.body.velocity = new Vector2(right ? 8 : -8, 5);
        if (physics.onGround)
        {
            body.velocity = new Vector2(right ? -20 : 20, 0); //compencate for the friction. 
        }
        else
        {
            body.velocity = new Vector2(right ? -5 : 5, body.velocity.y);
        }
        animator.Play("Hit");
        attackPhase = DevenAttackCyclePhase.None;
        jumpDelay = .5f;
        body.gravityScale = 1.354167f;
        body.isKinematic = false;

        if ((phase == 0 && health == 8))
        {
            return;
        }
        PlaySound(Enums.Sounds.Player_Sound_DamageHealth);
        health--;
        if (holograms.Count > 0)
        {
            foreach (DevenTheBossFight deven in holograms)
            {
                if (deven.fake)
                {
                    PhotonNetwork.Destroy(deven.gameObject);
                }
            }
        }
        if(health == -1)
        {
            left = transform.position.x < centerStage;
            attackPhase = DevenAttackCyclePhase.Death;
            animator.Play("Death");
        }
    }

    public int FindNearestPlayer()
    {
        PlayerController ret = null;
        float dist = Mathf.Infinity;
        foreach(PlayerController con in FindObjectsOfType<PlayerController>())
        {
            float checkDist = Vector2.Distance(transform.position, con.transform.position);
            if (checkDist < dist)
            {
                dist = checkDist;
                ret = con;
            }
        }
        return ret.photonView.ViewID;
    }

    public override void InteractWithPlayer(PlayerController player)
    {
        if (player.Frozen || attackPhase == DevenAttackCyclePhase.CloneSlamStart || attackPhase == DevenAttackCyclePhase.Death || attackPhase == DevenAttackCyclePhase.Static)
            return;

        ChangeTarget(player.photonView.ViewID);

        if (((phase == 0 && health == 8) || health == 0) && attackPhase == DevenAttackCyclePhase.SlamTransition)
        {
            body.velocity = Vector2.up * 4;
            attackPhase = DevenAttackCyclePhase.TransitionCharge;
            if(health == 0)
            {
                left = transform.position.x < centerStage;
            }
            return;
        }

        Vector2 damageDirection = (player.body.position - body.position).normalized;
        bool attackedFromAbove = Vector2.Dot(damageDirection, Vector2.up) > 0.5f && !player.onGround && (attackPhase != DevenAttackCyclePhase.Slam || physics.onGround);
        if (player.invincible > 0 || player.inShell || player.sliding
            || (player.groundpound && player.state != Enums.PowerupState.MiniMushroom && attackedFromAbove)
            || player.state == Enums.PowerupState.MegaMushroom)
        {

            photonView.RPC(nameof(TakeDamage), RpcTarget.All, (player.body.position - body.position).x > 0, player.groundpound);
        }
        else if (attackedFromAbove)
        {
            if (player.state == Enums.PowerupState.MiniMushroom)
            {
                if (player.groundpound)
                {
                    player.groundpound = false;
                    photonView.RPC(nameof(TakeDamage), RpcTarget.All, (player.body.position - body.position).x > 0, player.groundpound);
                }
                //player.bounce = true;
            }
            else
            {
                photonView.RPC(nameof(TakeDamage), RpcTarget.All, (player.body.position - body.position).x > 0, player.groundpound);
                //player.bounce = !player.groundpound;
            }
            player.photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Enemy_Generic_Stomp);
            player.drill = false;

        }
        else if (player.hitInvincibilityCounter <= 0)
        {
            player.photonView.RPC(nameof(PlayerController.Powerdown), RpcTarget.All, false);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(secondPhasePosL, .25f);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(secondPhasePosR, .25f);
    }
    public void CreateHoloDevens(int devensToCoordinate)
    {
        if (fake || originalDeven != null || hologramsSpawned || !photonView.IsMine)
        {
            return;
        }

        // Spawn holograms across all clients
        for (int i = 0; i < devensToCoordinate; i++)
        {
            // Instantiate the hologram Devens across all clients
            GameObject obj = PhotonNetwork.Instantiate(ourPrefab, transform.position, Quaternion.identity);
            DevenTheBossFight newDeven = obj.GetComponent<DevenTheBossFight>();
            holograms.Add(newDeven);

            // Optionally, set any other properties that must be synced across clients.
            newDeven.photonView.RPC(nameof(newDeven.InitializeHologram), RpcTarget.All, photonView.ViewID, left, (int)attackPhase, targetPosition); //sync variables
        }

        hologramsSpawned = true;

        // Prepare the holograms for shuffling
        List<DevenTheBossFight> devensToShuffle = new List<DevenTheBossFight>(holograms)
        {
            this
        };

        // Insert a "null" slot as a gap to shuffle
        devensToShuffle.Insert(Random.Range(0, devensToShuffle.Count + 1), null);

        // Shuffle the holograms (including the gap)
        int count = devensToShuffle.Count;
        for (int i = count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (devensToShuffle[i], devensToShuffle[randomIndex]) = (devensToShuffle[randomIndex], devensToShuffle[i]);
        }

        // Define stage layout for positioning
        float startX = centerStage - (10f / 2f);
        float spacing = 10f / (count - 1);

        // Position holograms with a gap
        for (int i = 0; i < count; i++)
        {
            float xPos = startX + (i * spacing);

            if (devensToShuffle[i] != null)
            {
                devensToShuffle[i].photonView.RPC(nameof(SetHologramTargetposition), RpcTarget.All, new Vector2(xPos, 5));
            }
        }
    }

    [PunRPC]
    public void InitializeHologram(int originalDevenID, bool left, int attackPhase, Vector2 targetPosition)
    {

        this.originalDeven = PhotonView.Find(originalDevenID).GetComponent<DevenTheBossFight>();
        this.left = left;
        this.attackPhase = (DevenAttackCyclePhase)attackPhase;
        this.targetPosition = targetPosition;
        this.fake = true;

        // Set up hologram-specific components like animator
        this.animator = this.GetComponent<Animator>();
        this.animator.SetBool("Desperate", true);
        this.animator.SetBool("OnGround", false);
    }
    [PunRPC]
    public void SetHologramTargetposition(Vector2 targetPosition)
    {
        this.targetPosition = targetPosition;
    }



    private void OnDestroy()
    {
        if (originalDeven)
        {
            originalDeven.holograms.Remove(this);
        }
    }
    public void FreePlayer()
    {
        GameManager.Instance.allowRuning = true;
        GameManager.Instance.paralizePlayer = false;
        attackPhase = DevenAttackCyclePhase.Static;
        GameManager.Instance.RUNFORYOURLIFE();
    }

    public void ChangeTarget(int photonID)
    {
        photonView.RPC(nameof(ChangeTargetRPC), RpcTarget.All, photonID);
    }
    [PunRPC]
    public void ChangeTargetRPC(int photonID)
    {
        targetPlayer = PhotonView.Find(photonID).GetComponent<PlayerController>();
    }

    [PunRPC]
    public void HackilyTransitionEveryone()
    {
        foreach(PlayerController con in FindObjectsOfType<PlayerController>())
        {
            if(con != targetPlayer)
            {
                con.transform.position = targetPlayer.transform.position;
            }
        }
        GameManager.Instance.spawnpoint.x = centerStagePhase2;
        phase = 1;
    }
}
