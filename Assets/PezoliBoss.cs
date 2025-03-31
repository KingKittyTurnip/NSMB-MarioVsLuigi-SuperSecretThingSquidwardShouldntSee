using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using NSMB.Utils;
using UnityEngine.UI;
using System.Configuration;

public class PezoliBoss : KillableEntity
{
    public GameObject goalPrefab;
    public Vector2 goalSpawnPos;
    public AudioSource introVoc, fishMp3, angryVoc;
    public Vector2 centerStage;
    public int HP = 8;
    public PezoliBossAttackPhase attackPhase = PezoliBossAttackPhase.Intro;
    public float attackTimer, timeSinceAttackChange;
    public SpriteRenderer fish;
    bool turboPhase;

    //public Vector2 swimControl_Joystick; //unused

    public Transform Model;

    private bool Flipping; //Used For Flipping Sides, Also Used To Prevent Spaming That Move
    private bool RandomBool; //This is A Random Bool I Added, It's Totally not used For Randomizing Attacksssss
    private Vector2 playerPosition;

    //Geyser Stuffs
    public BoxCollider2D Geyser;

    public enum PezoliBossAttackPhase
    {
        Intro,
        Stationary,
        SwapSides,
        Fish,
        //Flooded,
        WaterGeyser,
        //BouncyFish, scrapped
        Owie,
        AnimationTimer,
    }
    public override void FixedUpdate()
    {
        base.FixedUpdate();
        switch (attackPhase)
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
            //case PezoliBossAttackPhase.Flooded:
            //    {
            //        AttackPhaseFlooded();
            //        break;
            //    }
            case PezoliBossAttackPhase.WaterGeyser:
                {
                    AttackPhaseWaterGeyser();
                    break;
                }
            case PezoliBossAttackPhase.Owie:
                {
                    AttackPhaseOwie();
                    break;
                }
            case PezoliBossAttackPhase.AnimationTimer:
                {
                    AttackPhaseAnimationTimer();
                    break;
                }
        }
        timeSinceAttackChange += Time.fixedDeltaTime;
        if (attackTimer > 0)
        {
            attackTimer -= Time.fixedDeltaTime;
        }
        if (attackTimer < 0)
        {
            attackTimer = 0;
        }
        if (turboPhase && attackPhase != PezoliBossAttackPhase.WaterGeyser)
        {
            animator.speed = 2;
            timeSinceAttackChange += Time.fixedDeltaTime;
            if (attackTimer > 0)
            {
                attackTimer -= Time.fixedDeltaTime;
            }
            if (attackTimer < 0)
            {
                attackTimer = 0;
            }
        }
        else
        {
            animator.speed = 1;
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
        playerPosition = GetPlayerAveragePosition();
    }

    private int lastAttackAction = -1; // To store the last attack action picked

    public void AttackPhaseStationary() //FIN. stay still for a bit, then pick a random attack action to transition to. 
    {
        if (attackTimer <= 0)
        {
            // Generate a random attack action, but avoid the last one picked
            int attackAction;
            do
            {
                attackAction = Random.Range(Flipping ? 3 : 2, 5); // Generate a random action
            }
            while (attackAction == lastAttackAction); // Make sure it is not the same as the last one

            photonView.RPC(nameof(SetAttack), RpcTarget.All, attackAction, 1f);

            lastAttackAction = attackAction; // Store the last picked attack action
            Flipping = false;
        }
    }
    public void AttackPhaseSwapSides() //moves down to a set Y position, and then goes to the opposite side of the stage. using centerStage as a reference point.
    {
        //My code
        float moveSpeed = turboPhase ? 5 : 3;
        body.velocity = new Vector2(0, 0);
        if (left)
        {
            if (transform.position.x > centerStage.x)
            {
                if (body.position.y > centerStage.y - 3)
                {
                    transform.position += Vector3.down * Time.fixedDeltaTime * moveSpeed;
                }
                else
                {
                    animator.SetBool("Swimming", true);
                    transform.position += Vector3.left * Time.fixedDeltaTime * moveSpeed;
                }
            }
            else
            {
                if (body.position.x < centerStage.x - 4)
                {
                    if (body.position.y < centerStage.y)
                    {
                        transform.position += Vector3.up * Time.fixedDeltaTime * moveSpeed;
                        animator.SetBool("Swimming", false);
                    }
                    else
                    {
                        photonView.RPC(nameof(SetAttack), RpcTarget.All, (int)PezoliBossAttackPhase.Stationary, 2f);
                        transform.position = new Vector3(centerStage.x - 4, centerStage.y);
                    }
                }
                else
                {
                    transform.position += Vector3.left * Time.fixedDeltaTime * moveSpeed;
                }
            }
        }
        else
        {
            if (transform.position.x < centerStage.x)
            {
                if (body.position.y > centerStage.y - 3)
                {
                    transform.position += Vector3.down * Time.fixedDeltaTime * moveSpeed;
                }
                else
                {
                    animator.SetBool("Swimming", true);
                    transform.position += Vector3.right * Time.fixedDeltaTime * moveSpeed;
                }
            }
            else
            {
                if (body.position.x >= centerStage.x + 4)
                {
                    if (body.position.y < centerStage.y)
                    {
                        animator.SetBool("Swimming", false);
                        transform.position += Vector3.up * Time.fixedDeltaTime * moveSpeed;
                    }
                    else
                    {
                        photonView.RPC(nameof(SetAttack), RpcTarget.All, (int)PezoliBossAttackPhase.Stationary, 2f);
                        transform.position = new Vector3(centerStage.x + 4, centerStage.y);
                    }
                }
                else
                {
                    transform.position += Vector3.right * Time.fixedDeltaTime * moveSpeed;
                }
            }
        }


        ////Turnip's code
        //if (timeSinceAttackChange > 0.75f)
        //{
        //    if (!Flipping)
        //    { //Flip Side Depending On Facing Direction, But Can Fake Out When Under Half Health
        //        bool FlipTo = HP > 4 ? left : RandomBool;
        //        body.position = new Vector2((FlipTo ? -4.5f : 4.5f) + centerStage.x, centerStage.y + 8);
        //        photonView.RPC(nameof(SetLeft), RpcTarget.All, body.position.x > centerStage.x);
        //        Flipping = true;
        //    }
        //    body.velocity = new Vector2(0, -10); //Zoom Downwards
        //}
        //else
        //{
        //    body.velocity = new Vector2(0, 10); //Zoom Upwards
        //}
        //if (timeSinceAttackChange > 1.5f && body.position.y <= centerStage.y)
        //{ //Reset
        //    //animator.SetBool("Swim", true);
        //    body.velocity = new Vector2(0, 0);
        //    photonView.RPC(nameof(SetAttack), RpcTarget.All, (int)PezoliBossAttackPhase.Stationary, 2f);
        //}


        // My attempt to follow the notes a bit better. 
        // May or may not be AI generated :shrug: Good luck disecting this mess. 
        // Don't blame me, I was in a rush. 

        //It has 2 problems. it moves too fast, and always moves left. 
        //if (timeSinceAttackChange > 0.75f && !Flipping)
        //{
        //    bool FlipTo = left;
        //
        //    float targetX = FlipTo ? centerStage.x - 4.5f : centerStage.x + 4.5f;
        //
        //    float arcDuration = 5f;
        //
        //    // Calculate the angle based on the progress
        //    float angleProgress = Mathf.Clamp01((timeSinceAttackChange - 0.75f) / arcDuration); // Start progress after the 0.75f threshold
        //    Debug.Log($"Angle Progress: {angleProgress}");
        //
        //    float arcRadius = 4.5f;
        //
        //    float angle = Mathf.Lerp(0, Mathf.PI, angleProgress);
        //
        //    // Calculate the new position along the arc
        //    float offsetX = arcRadius * Mathf.Cos(angle);
        //    float offsetY = -arcRadius * Mathf.Sin(angle);
        //
        //    if (FlipTo)
        //    {
        //        offsetX = -offsetX;
        //    }
        //
        //    body.position = new Vector2(centerStage.x + offsetX, centerStage.y + offsetY);
        //
        //    photonView.RPC(nameof(SetLeft), RpcTarget.All, body.position.x > centerStage.x);
        //}
        //
        //if (timeSinceAttackChange > (0.75f + 2.5f))
        //{
        //    if (body.position.y < centerStage.y)
        //    {
        //        body.position = new Vector2(body.position.x, body.position.y + 0.05f);
        //    }
        //    else
        //    {
        //        body.position = new Vector2(body.position.x, centerStage.y);
        //        photonView.RPC(nameof(SetAttack), RpcTarget.All, (int)PezoliBossAttackPhase.Stationary, 2f);
        //        Flipping = false;
        //    }
        //}
    }
    public void AttackPhaseFish() //FIN. Throw a fish
    {
        if (timeSinceAttackChange > .775f)
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
    //public void AttackPhaseFlooded()
    //{ //while staying still, raise the water level of the stage. After the water is risen, Pez can swim after the player, and do a melee phase. Lasts 15 seconds as to not risk the player drowning. 
    //    photonView.RPC(nameof(SetAttack), RpcTarget.All, (int)PezoliBossAttackPhase.Stationary, 0f);
    //}
    public void AttackPhaseWaterGeyser()
    { //FIN. The water below one of the 2 platforms the player is standing on starts to bubble, then a water geyser spouts, damaging the player if hit. 
        if (timeSinceAttackChange < .5f)
        {
            animator.SetTrigger("RiseAction");
        }
        else
        {
            animator.ResetTrigger("RiseAction");
        }
        if (timeSinceAttackChange > 1.4f)
        { //Enable Hitbox
            Geyser.enabled = true;
        }
        else
        { //Start Geyser
            Geyser.gameObject.SetActive(true);
            Geyser.gameObject.transform.position = new Vector2((playerPosition.x < centerStage.x ? -1.75f : 1.75f) + centerStage.x, centerStage.y + 0);
        }
        if (timeSinceAttackChange > 1.75f)
        { //Stop, Calm Down The Particles
            Geyser.enabled = false;
            if (timeSinceAttackChange > 2.25f)
            { //Reset
                Geyser.gameObject.SetActive(false);
                photonView.RPC(nameof(SetAttack), RpcTarget.All, (int)PezoliBossAttackPhase.Stationary, 2f);
            }
        }
    }
    public void AttackPhaseOwie()
    {
        transform.position += new Vector3(attackTimer * (left ? 1 : -1) * 2 * Time.fixedDeltaTime, 0, 0);
        if(attackTimer <= 0)
        {
            photonView.RPC(nameof(SetAttack), RpcTarget.All, (int)PezoliBossAttackPhase.SwapSides, 1f);
            left = transform.position.x < centerStage.x;
            if(HP == 4)
            {
                angryVoc.Play();
                animator.SetBool("Swimming", false);
                animator.SetTrigger("Anger");
                animator.Play("Rage");
                photonView.RPC(nameof(SetAttack), RpcTarget.All, (int)PezoliBossAttackPhase.AnimationTimer, 1.667f); //synced with animation
            }
        }
    }
    public void AttackPhaseAnimationTimer()
    {
        if(attackTimer <= 0)
        {
            photonView.RPC(nameof(SetAttack), RpcTarget.All, (int)PezoliBossAttackPhase.SwapSides, 1f);
            left = transform.position.x < centerStage.x;
            turboPhase = true;
        }
    }


    [PunRPC]
    public override void SetLeft(bool left)
    { //New, To Turn Them Around Properly
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
    [PunRPC]
    public void Damage(int playerViewID)
    {
        PlayerController con = PhotonView.Find(playerViewID).GetComponent<PlayerController>();
        con.body.velocity = Vector2.up * 8;
        animator.SetTrigger("Hit");
        photonView.RPC(nameof(SetAttack), RpcTarget.All, (int)PezoliBossAttackPhase.Owie, 1.667f); //synced with animation
        HP--;
        if(HP <= 0)
        {
            PhotonNetwork.Destroy(gameObject);
        }
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
                Vector2 throwAngle = -(body.position - GetNearestPlayerPosition()).normalized;
                throwAngle.y = Mathf.Abs(throwAngle.y);
                if (throwAngle.y < 1)
                {
                    throwAngle.y = 1;
                }
                fishEnt.body.velocity = throwAngle * 12;
            }
        }
    }
    public Vector2 GetPlayerAveragePosition()
    {
        PlayerController[] cons = FindObjectsOfType<PlayerController>();

        if (cons.Length == 0)
            return Vector2.zero;

        Vector2 averagePos = Vector2.zero;

        foreach (PlayerController con in cons)
        {
            averagePos += (Vector2)con.transform.position;
        }

        averagePos /= cons.Length;

        return averagePos;
    }

    public Vector2 GetNearestPlayerPosition()
    {
        PlayerController[] cons = FindObjectsOfType<PlayerController>();

        if (cons.Length == 0)
            return Vector2.zero;

        PlayerController nearestPlayer = cons[0];
        float nearestDistance = Vector2.Distance(transform.position, nearestPlayer.transform.position);

        foreach (PlayerController con in cons)
        {
            float distance = Vector2.Distance(transform.position, con.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPlayer = con;
            }
        }

        return nearestPlayer.transform.position;
    }

    [PunRPC]
    public override void Freeze(int cube)
    {
        audioSource.Stop();
        PlaySound(Enums.Sounds.Enemy_Generic_Freeze);
        Frozen = true;
        animator.enabled = false;
        if (body)
        {
            body.velocity = Vector2.zero;
            body.angularVelocity = 0;
            body.isKinematic = true;
        }
    }
    public override void InteractWithPlayer(PlayerController player)
    {
        if (player.Frozen)
            return;

        Vector2 damageDirection = (player.body.position - body.position).normalized;
        bool attackedFromAbove = Vector2.Dot(damageDirection, Vector2.up) > 0.5f && !player.onGround && attackPhase == PezoliBossAttackPhase.SwapSides;

        if (player.invincible > 0 || player.inShell || player.sliding
            || (player.groundpound && player.state != Enums.PowerupState.MiniMushroom && attackedFromAbove)
            || player.state == Enums.PowerupState.MegaMushroom)
        {

            photonView.RPC(nameof(Damage), RpcTarget.All, player.photonView.ViewID);
        }
        else if (attackedFromAbove)
        {
            if (player.state == Enums.PowerupState.MiniMushroom)
            {
                if (player.groundpound)
                {
                    player.groundpound = false;
                    photonView.RPC(nameof(Damage), RpcTarget.All, player.photonView.ViewID);
                }
            }
            else
            {
                photonView.RPC(nameof(Damage), RpcTarget.All, player.photonView.ViewID);
            }
            player.photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Enemy_Generic_Stomp);
            player.drill = false;

        }
        else if (player.hitInvincibilityCounter <= 0)
        {
            player.photonView.RPC(nameof(PlayerController.Powerdown), RpcTarget.All, false);
        }
    }
    private void OnDestroy()
    {
        Instantiate(goalPrefab, goalSpawnPos, Quaternion.identity);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(centerStage, .5f);
        Gizmos.DrawWireSphere(goalSpawnPos, .5f);
    }
}
