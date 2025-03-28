using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using NSMB.Utils;
using UnityEngine.UI;

public class PezoliBoss : KillableEntity
{
    public AudioSource introVoc, fishMp3;
    public Vector2 centerStage;
    public int HP = 8;
    public PezoliBossAttackPhase attackPhase = PezoliBossAttackPhase.Intro;
    public float attackTimer, timeSinceAttackChange;
    public SpriteRenderer fish;

    public Transform Model;

    private bool Flipping; //Used For Flipping Sides, Also Used To Prevent Spaming That Move
    private bool RandomBool; //This is A Random Bool I Added, It's Totally not used For Randomizing Attacksssss

    //Geyser Stuffs
    public BoxCollider2D Geyser;

    public enum PezoliBossAttackPhase
    {
        Intro,
        Stationary,
        SwapSides,
        Fish,
        Flooded,
        WaterGeyser,
        //BouncyFish, scrapped
    }
    public override void FixedUpdate()
    {
        base.FixedUpdate();
        timeSinceAttackChange += Time.fixedDeltaTime;
        if(attackTimer > 0)
        {
            attackTimer -= Time.fixedDeltaTime;
        }
        if(attackTimer < 0)
        {
            attackTimer = 0;
        }
        switch(attackPhase)
        {
            case PezoliBossAttackPhase.Stationary:
                {
                    AttackPhaseStationary();
                    break;
                }
            case PezoliBossAttackPhase.SwapSides:
                {
                    AttackPhaseSwapSides();
                    break;
                }
            case PezoliBossAttackPhase.Fish:
                {
                    AttackPhaseFish();
                    break;
                }
            case PezoliBossAttackPhase.Flooded:
                {
                    AttackPhaseFlooded();
                    break;
                }
            case PezoliBossAttackPhase.WaterGeyser:
                {
                    AttackPhaseWaterGeyser();
                    break;
                }
        }
    }




    [PunRPC]
    public void SetAttack(int attack, float attackTimer)
    {
        photonView.RPC(nameof(SetLeft), RpcTarget.All, body.position.x > centerStage.x); //Failsafe

        this.attackTimer = attackTimer;
        attackPhase = (PezoliBossAttackPhase)attack;
        timeSinceAttackChange = 0;
        RandomBool = Random.Range(0, 2) == 1;
    }

    public void AttackPhaseStationary() //FIN. stay still for a bit, then pick a random attack action to transition to. 
    {
        if(attackTimer <= 0) {
            photonView.RPC(nameof(SetAttack), RpcTarget.All, Random.Range(Flipping ? 3 : 2, 6), 1f);
            Flipping = false;
        }
    }
    public void AttackPhaseSwapSides() { //FIN. moves down to a set Y position, and then goes to the opposite side of the stage. using centerStage as a reference point.
        if (timeSinceAttackChange > 0.75f) {
            if (!Flipping) { //Flip Side Depending On Facing Direction, But Can Fake Out When Under Half Health
                bool FlipTo = HP > 4 ? left : RandomBool;
                body.position = new Vector2((FlipTo ? -4.5f : 4.5f) + centerStage.x, centerStage.y + 8);
                photonView.RPC(nameof(SetLeft), RpcTarget.All, body.position.x > centerStage.x);
                Flipping = true;
            }
            body.velocity = new Vector2(0, -10); //Zoom Downwards
        } else {
            body.velocity = new Vector2(0, 10); //Zoom Upwards
        }
        if (timeSinceAttackChange > 1.5f && body.position.y <= centerStage.y) { //Reset
            //animator.SetBool("Swim", true);
            body.velocity = new Vector2(0, 0);
            photonView.RPC(nameof(SetAttack), RpcTarget.All, (int)PezoliBossAttackPhase.Stationary, 2f);
        }
    }
    public void AttackPhaseFish() //FIN. Throw a fish
    {
        if(timeSinceAttackChange > .775f)
        {
            if (fish.gameObject.activeSelf)
            {
                fishMp3.Play();
                SpawnAFish();
            }
            fish.gameObject.SetActive(false);
            animator.ResetTrigger("Fish");
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Float"))
            {
                photonView.RPC(nameof(SetAttack), RpcTarget.All, (int)PezoliBossAttackPhase.Stationary, 2f);
            }
        }
        else
        {
            fish.gameObject.SetActive(true);
            animator.SetTrigger("Fish");
            fish.flipX = !left;
        }
    }
    public void AttackPhaseFlooded() { //while staying still, raise the water level of the stage. After the water is risen, Pez can swim after the player, and do a melee phase. Lasts 15 seconds as to not risk the player drowning. 
        photonView.RPC(nameof(SetAttack), RpcTarget.All, (int)PezoliBossAttackPhase.Stationary, 0f);
    }
    public void AttackPhaseWaterGeyser() { //FIN. The water below one of the 2 platforms the player is standing on starts to bubble, then a water geyser spouts, damaging the player if hit. 
        if (timeSinceAttackChange > 1.4f) { //Enable Hitbox
            Geyser.enabled = true;
        } else { //Start Geyser
            Geyser.gameObject.SetActive(true); 
            Geyser.gameObject.transform.position = new Vector2((RandomBool ? -1.75f : 1.75f) + centerStage.x, centerStage.y + 0);
        }
        if (timeSinceAttackChange > 1.75f) { //Stop, Calm Down The Particles
            Geyser.enabled = false;
            if (timeSinceAttackChange > 2.25f) { //Reset
                Geyser.gameObject.SetActive(false);
                photonView.RPC(nameof(SetAttack), RpcTarget.All, (int)PezoliBossAttackPhase.Stationary, 2f);
            }
        }
    }


    [PunRPC]
    public override void SetLeft(bool left) { //New, To Turn Them Around Properly
        this.left = left;
        body.velocity = new Vector2(Mathf.Abs(body.velocity.x) * (left ? -1 : 1), body.velocity.y);
        Model.rotation = Quaternion.Euler(0, left ? -110 : 110, 0);
    }
    [PunRPC]
    public override void SpecialKill(bool right, bool groundpound, int combo)
    {

    }
    [PunRPC]
    public override void SpecialKillWithForce(bool right, bool groundpound, int combo)
    {

    }
    [PunRPC]
    public override void Kill()
    {

    }

    public void StartFight()
    {
        photonView.RPC(nameof(SetAttack), RpcTarget.All, (int)PezoliBossAttackPhase.Stationary, 2f);
        GameManager.Instance.paralizePlayer = false;
    }
    public void FunnyIntroAnim()
    {
        introVoc.Play();
        animator.Play("Pre-game Pez");
        GameManager.Instance.paralizePlayer = true;
    }
    public void SpawnAFish()
    {
        if (photonView.IsMine)
        {
            GameObject fishObj = PhotonNetwork.Instantiate("Prefabs/Enemy/CheepCheep", fish.transform.position, Quaternion.identity);
            CheepCheep fishEnt = fishObj.GetComponent<CheepCheep>();
            if (fishEnt != null)
            {
                fishEnt.body.velocity = new Vector2(left ? -8 : 8, 8);
            }
        }
    }
}
