using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using NSMB.Utils;

public class PezoliBoss : KillableEntity
{
    public AudioSource introVoc, fishMp3;
    public Vector2 centerStage;
    public int HP = 8;
    public PezoliBossAttackPhase attackPhase = PezoliBossAttackPhase.Intro;
    public float attackTimer, timeSinceAttackChange;
    public SpriteRenderer fish;
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
        this.attackTimer = attackTimer;
        attackPhase = (PezoliBossAttackPhase)attack;
        timeSinceAttackChange = 0;
    }

    public void AttackPhaseStationary() //stay still for a bit, then pick a random attack action to transition to. 
    {
        if(attackTimer <= 0)
        {
            photonView.RPC(nameof(SetAttack), RpcTarget.All, (int)PezoliBossAttackPhase.Fish, 1f);
        }
    }
    public void AttackPhaseSwapSides() //moves down to a set Y position, and then goes to the opposite side of the stage. using centerStage as a reference point.
    {
        
    }
    public void AttackPhaseFish() //Throw a fish
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
    public void AttackPhaseFlooded() //while staying still, raise the water level of the stage. After the water is risen, Pez can swim after the player, and do a melee phase. Lasts 15 seconds as to not risk the player drowning. 
    {

    }
    public void AttackPhaseWaterGeyser() //The water below one of the 2 platforms the player is standing on starts to bubble, then a water geyser spouts, damaging the player if hit. 
    {

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
