using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using NSMB.Utils;
using UnityEngine.TextCore.Text;
using UnityEngine.Tilemaps;

public class FakeMario : KillableEntity, IPunObservable
{
    public bool debugMode;
    Vector2 origin;
    public GameObject goalObject;
    public float timeUntilCanMega = 1, timeUntilMushroom, deathUpTime = 0.6f, deathForce = 7f;
    public Animator powerup;
    public GameObject fireflower, iceflower, megamushroom, mushroom;
    public float powerupTimer, deathTimer;
    private MaterialPropertyBlock materialBlock;

    public float slowriseGravity = 0.85f, normalGravity = 2.5f, flyingGravity = 0.8f, flyingTerminalVelocity = 1.25f, drillVelocity = 7f, groundpoundTime = 0.25f, groundpoundVelocity = 10, blinkingSpeed = 0.25f, terminalVelocity = -7f, jumpVelocity = 6.25f, megaJumpVelocity = 16f, launchVelocity = 12f, wallslideSpeed = -4.25f, giantStartTime = 1.5f, soundRange = 10f, pickupTime = 0.5f, giantEndTimer, giantTimer = 0, fireballTimer;
    public bool bounce, canShootProjectile;
    private Enums.Sounds footstepSound = Enums.Sounds.Player_Walk_Grass;
    public bool jumpHeld, deathUp;
    private bool powerupSoundPlayed = false;
    #region copypaste
    [SerializeField] private ParticleSystem dust, giantParticle;
    [SerializeField] private float blinkDuration = 0.1f;
    private static readonly int WALK_STAGE = 1, RUN_STAGE = 3;
    private static readonly float[] SPEED_STAGE_MAX = { 0.9375f, 2.8125f, 4.21875f, 5.625f, 8.4375f };
    private static readonly float[] SPEED_STAGE_ACC = { 0.131835975f, 0.06591802875f, 0.05859375f, 0.0439453125f, 1.40625f };
    private static readonly float[] WALK_TURNAROUND_ACC = { 0.0659179686f, 0.146484375f, 0.234375f };
    private static readonly float BUTTON_RELEASE_DEC = 0.0659179686f;
    private static readonly float SKIDDING_THRESHOLD = 4.6875f;
    private static readonly float SKIDDING_DEC = 0.17578125f;

    private static readonly float WALLJUMP_HSPEED = 4.21874f;
    private static readonly float WALLJUMP_VSPEED = 6.4453125f;

    private static readonly float KNOCKBACK_DEC = 0.131835975f;

    private static readonly float[] SPEED_STAGE_MEGA_ACC = { 0.46875f, 0.0805664061f, 0.0805664061f, 0.0805664061f, 0.0805664061f };
    private static readonly float[] WALK_TURNAROUND_MEGA_ACC = { 0.0769042968f, 0.17578125f, 0.3515625f };

    private static readonly float TURNAROUND_THRESHOLD = 2.8125f;
    private static readonly float TURNAROUND_ACC = 0.46875f;

    public float RunningMaxSpeed => SPEED_STAGE_MAX[RUN_STAGE];
    public float WalkingMaxSpeed => SPEED_STAGE_MAX[WALK_STAGE];

    private float wallJumpTimer;

    private bool functionallyRunning = true;

    private bool skidding, crouching, knockback, turnaround, groundpound, wasTurnaround, step;
    private float turnaroundFrames, groundpoundCounter, giantStartTimer;
    private int turnaroundBoostFrames;


    private int MovementStage
    {
        get
        {
            float xVel = Mathf.Abs(body.velocity.x);
            float[] arr = SPEED_STAGE_MAX;
            for (int i = 0; i < arr.Length; i++)
            {
                if (xVel <= arr[i])
                    return i;
            }
            return arr.Length - 1;
        }
    }

    #endregion

    public Vector2 joystick;

    public Enums.PowerupState state;
    public int phase, health, megaphaseHP = 4;
    public bool JustModelLol;
    public bool init;
    public Avatar smallAvatar, largeAvatar;
    public GameObject smallModel, largeModel, models;
    public Animator anim;
    private PlayerAnimationController playerAnim;
    public float smallHeight = 0.42f, largeHeight = 0.82f;
    public PlayerData playerData;
    // Start is called before the first frame update
    void Awake()
    {
        origin = transform.position + (Vector3.up / 2);
        SetParticleEmission(dust, false);
        SetParticleEmission(giantParticle, false);
    }
    void InitMDL()
    {
        playerAnim = GameManager.Instance.localPlayer.GetComponent<PlayerAnimationController>();
        gameObject.SetActive(false);
        anim.enabled = false;
        Enums.PowerupState playerState = playerAnim.controller.state;
        playerAnim.controller.state = Enums.PowerupState.Mushroom;
        playerAnim.UpdateAnimatorStates();
        models = Instantiate(playerAnim.models, transform);
        smallAvatar = playerAnim.smallAvatar;
        largeAvatar = playerAnim.largeAvatar;
        smallModel = transform.GetChild(4).GetChild(0).gameObject;
        largeModel = transform.GetChild(4).GetChild(1).gameObject;
        playerAnim.controller.state = playerState;
        playerAnim.UpdateAnimatorStates();
        models.transform.rotation = Quaternion.Euler(0, -110, 0);
        playerData = playerAnim.controller.character;
        smallHeight = playerAnim.controller.heightSmallModel;
        largeHeight = playerAnim.controller.heightLargeModel;
        anim.enabled = true;
        anim.runtimeAnimatorController = playerData.largeOverrides;
        anim.avatar = largeAvatar;
        init = true;
        gameObject.SetActive(true);
    }
    // Update is called once per frame
    public override void FixedUpdate()
    {
        HandleDeathAnimation();
        if (giantStartTimer <= 0)
            Utils.TickTimer(ref giantTimer, 0, Time.fixedDeltaTime);
        Utils.TickTimer(ref giantStartTimer, 0, Time.fixedDeltaTime);
        Utils.TickTimer(ref giantEndTimer, 0, Time.fixedDeltaTime);
        Utils.TickTimer(ref fireballTimer, 0, Time.fixedDeltaTime);
        if(state == Enums.PowerupState.Small)
            Utils.TickTimer(ref timeUntilMushroom, 0, Time.fixedDeltaTime);
        wallJumpTimer -= Time.fixedDeltaTime;


        if (!init && GameManager.Instance.localPlayer && GameManager.Instance.musicEnabled && GameManager.Instance.localPlayer.gameObject.GetComponent<PlayerController>().spawned)
        {
            InitMDL();
            return;
        }
        if (playerAnim == null)
        {
            playerAnim = GameManager.Instance.localPlayer.GetComponent<PlayerAnimationController>();
        }
        HandleModels();
        if (JustModelLol)
        {
            return;
        }
        jumpHeld = debugMode && playerAnim.controller.jumpHeld;
        bool moving = false;
        base.FixedUpdate();
        physics.UpdateCollisions();
        anim.SetBool("onGround", physics.onGround);
        UsePowerupAction();
        if (physics.onGround)
        {
            if (anim.GetCurrentAnimatorStateInfo(0).IsName("fakemario_Powerup"))
            {
                HandleWalkingRunning(false, false);
                if (anim.GetCurrentAnimatorStateInfo(0).normalizedTime > .8)
                {
                    if (fireflower.activeSelf)
                    {
                        if (state != Enums.PowerupState.IceFlower && !powerupSoundPlayed)
                            PlaySound(Enums.Sounds.Player_Sound_PowerupCollect);
                        state = Enums.PowerupState.FireFlower;
                    }
                    else if (iceflower.activeSelf)
                    {
                        if (state != Enums.PowerupState.FireFlower && !powerupSoundPlayed)
                            PlaySound(Enums.Sounds.Player_Sound_PowerupCollect);
                        state = Enums.PowerupState.IceFlower;
                    }
                    else if (mushroom.activeSelf)
                    {
                        if (state != Enums.PowerupState.Mushroom && !powerupSoundPlayed)
                            PlaySound(Enums.Sounds.Player_Sound_PowerupCollect);
                        state = Enums.PowerupState.Mushroom;
                        phase = 1;
                        health = megaphaseHP;
                    }
                    else if (megamushroom.activeSelf)
                    {
                        state = Enums.PowerupState.MegaMushroom;
                        giantStartTimer = giantStartTime;
                        knockback = false;
                        groundpound = false;
                        crouching = false;
                        flying = false;
                        giantTimer = 15f;
                        transform.localScale = Vector3.one;
                        Instantiate(Resources.Load("Prefabs/Particle/GiantPowerup"), transform.position, Quaternion.identity);
                        if (!powerupSoundPlayed)
                            PlaySoundEverywhere(Enums.Sounds.Player_Sound_MegaMushroom_Collect);
                    }
                    powerupSoundPlayed = true;
                }
                else
                {
                    powerupSoundPlayed = false;
                }
                moving = false;
            }
            else
            {
                if ((state == Enums.PowerupState.Mushroom && Random.Range(0, 100) > 95) || (state == Enums.PowerupState.Small && timeUntilMushroom <= 0))
                {
                    timeUntilCanMega -= 1;
                    anim.Play("fakemario_Powerup");
                    int item = 0;
                    if (phase == 0 || timeUntilCanMega > 0)
                    {
                        item = Random.Range(0, 2);
                    }
                    else
                    {
                        if (phase == 4)
                        {
                            item = 3;
                        }
                        else
                        {
                            item = Random.Range(0, 3);
                        }
                    }
                    fireflower.SetActive(item == 0);
                    iceflower.SetActive(item == 1);
                    megamushroom.SetActive(item == 2);
                    mushroom.SetActive(item == 3);
                    powerup.SetTrigger("powerup");
                }
                if (!debugMode)
                {
                    jumpHeld = Random.Range(0, 100) > 95 || physics.hitLeft || physics.hitRight;
                }
                if (state == Enums.PowerupState.MegaMushroom)
                {
                    moving = true;
                    HandleWalkingRunning(!movingRight, movingRight);
                    if (physics.hitLeft || physics.hitRight)
                    {
                        movingRight = !movingRight;
                    }
                }
                else
                {
                    moving = true;
                    //HandleWalkingRunning(playerAnim.transform.position.x > transform.position.x, playerAnim.transform.position.x < transform.position.x);
                    if (Random.Range(0, 99) > 95)
                    {
                        movingRight = !movingRight;
                    }
                    HandleWalkingRunning(!movingRight, movingRight);
                }
                HandleJumping(jumpHeld && physics.onGround);
            }
        }
        else
        {
            if (state == Enums.PowerupState.MegaMushroom)
            {
                jumpHeld = false;
                HandleWalkingRunning(!movingRight, movingRight);
                moving = true;
                if (physics.hitLeft || physics.hitRight)
                {
                    movingRight = !movingRight;
                }
            }
            else
            {
                jumpHeld = body.velocity.y > 0;
                if (wallJumpTimer > 0)
                {
                    HandleWalkingRunning(!movingRight, movingRight);
                }
                else
                {
                    HandleWalkingRunning(!movingRight, movingRight);
                    //HandleWalkingRunning(playerAnim.transform.position.x > transform.position.x, playerAnim.transform.position.x < transform.position.x);
                }
                if ((physics.hitLeft || physics.hitRight) && wallJumpTimer <= 0)
                {
                    wallJumpTimer = .5f;
                    body.velocity = new Vector2(WALLJUMP_HSPEED * (physics.hitRight ? -1 : 1), WALLJUMP_VSPEED);
                    left = physics.hitRight;
                    animator.SetTrigger("walljump");
                    Vector2 offset = new(hitbox.size.x / 2f * (!left ? -1 : 1), hitbox.size.y / 2f);
                    photonView.RPC(nameof(SpawnParticle), RpcTarget.All, "Prefabs/Particle/WalljumpParticle", body.position + offset, !left ? Vector3.zero : Vector3.up * 180);
                    photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Player_Sound_WallJump);
                    photonView.RPC(nameof(PlaySoundVarient), RpcTarget.All, Enums.Sounds.Player_Voice_WallJump, (byte)Random.Range(1, 3));
                    movingRight = !movingRight;
                }
            }

            float gravityModifier = state switch
            {
                Enums.PowerupState.MiniMushroom => 0.4f,
                _ => 1,
            };
            float slowriseModifier = state switch
            {
                Enums.PowerupState.MegaMushroom => 5f,
                _ => 1f,
            };
            if (groundpound)
                gravityModifier *= 1.5f;

            if (body.velocity.y > 2.5)
            {
                if (jumpHeld || state == Enums.PowerupState.MegaMushroom)
                {
                    body.gravityScale = slowriseGravity * slowriseModifier;
                }
                else
                {
                    body.gravityScale = normalGravity * 1.5f * gravityModifier;
                }
            }
            else if (physics.onGround || (groundpound && groundpoundCounter > 0))
            {
                body.gravityScale = 0f;
            }
            else
            {
                body.gravityScale = normalGravity * (gravityModifier / 1.2f);
            }
        }




        if (models)
        {
            anim.enabled = true;
            anim.SetBool("onGround", physics.onGround);

            float animatedVelocity = Mathf.Abs(body.velocity.x);
            if (debugMode)
            {
                moving = Mathf.Abs(joystick.x) > .7f;
            }
            if (moving)
            {
                if (animatedVelocity < 2.813f)
                {
                    animatedVelocity = Mathf.Max(2.5f, animatedVelocity * 1.2442232492001421969427657305379f); //incredibly precise number to get 2.813 to 3.499
                }
                else
                {
                    animatedVelocity = Mathf.Max(3.5f, animatedVelocity);
                }
            }
            anim.SetFloat("velocityX", animatedVelocity);
            anim.SetFloat("velocityY", body.velocity.y);
            //models.transform.rotation = Quaternion.Euler(0, models.transform.rotation.y, 0);
            HandleAnimations();
        }


        if (giantStartTimer > 0)
        {
            body.velocity = Vector2.zero;
            if (giantStartTimer - Time.fixedDeltaTime <= 0 && photonView.IsMine)
            {
                photonView.RPC(nameof(FinishMegaMario), RpcTarget.All, true);
                giantStartTimer = 0;
            }
            else
            {
                body.isKinematic = true;
                if (animator.GetCurrentAnimatorClipInfo(0).Length <= 0 || animator.GetCurrentAnimatorClipInfo(0)[0].clip.name != "mega-scale")
                    animator.Play("mega-scale");
            }
            return;
        }
        if (state == Enums.PowerupState.MegaMushroom && giantTimer <= 0 && photonView.IsMine)
        {
            photonView.RPC(nameof(EndMega), RpcTarget.All);
        }
    }
    #region no-man's land!
    bool movingRight;
    void HandleWalkingRunning(bool left, bool right)
    {
        if (debugMode)
        {
            joystick = playerAnim.controller.joystick;
            left = joystick.x < -.7f;
            right = joystick.x > .7f;
        }
        if (!skidding && !turnaround)
        {
            if(left || right)
            {
                this.left = left;
            }
        }
        movingRight = right;
        if (wallJumpTimer > 0)
        {
            if (wallJumpTimer < (14 / 60f) && (physics.hitLeft || physics.hitRight))
            {
                wallJumpTimer = 0;
            }
            else
            {
                body.velocity = new(WALLJUMP_HSPEED * (left ? -1 : 1), body.velocity.y);
                return;
            }
        }

        if (groundpound || groundpoundCounter > 0 || knockback || !(wallJumpTimer <= 0 || physics.onGround || body.velocity.y < 0))
            return;

        if (!physics.onGround)
            skidding = false;
        bool run;
        run = functionallyRunning || (state == Enums.PowerupState.MegaMushroom);

        int maxStage;
        if (run)
            maxStage = RUN_STAGE;
        else
            maxStage = WALK_STAGE;

        int stage = MovementStage;
        if (stage > maxStage)
        {
            stage = maxStage;
        }
        float acc = state == Enums.PowerupState.MegaMushroom ? SPEED_STAGE_MEGA_ACC[stage] : SPEED_STAGE_ACC[stage];
        float sign = Mathf.Sign(body.velocity.x);
        if ((left ^ right) && (!crouching || (crouching && !physics.onGround)) && !knockback)
        {
            //we can walk here

            float speed = Mathf.Abs(body.velocity.x);
            bool reverse = body.velocity.x != 0 && ((left ? 1 : -1) == sign);

            //check that we're not going above our limit
            float max = SPEED_STAGE_MAX[maxStage];
            if (speed > max)
            {
                acc = -acc;
            }

            if (reverse)
            {
                turnaround = false;
                if (physics.onGround)
                {
                    if (speed >= SKIDDING_THRESHOLD && state != Enums.PowerupState.MegaMushroom)
                    {
                        skidding = true;
                        this.left = sign != 1;
                    }

                    if (skidding)
                    {
                        acc = SKIDDING_DEC;
                        turnaroundFrames = 0;
                    }
                    else
                    {
                        turnaroundFrames = Mathf.Min(turnaroundFrames + 0.2f, WALK_TURNAROUND_ACC.Length - 1);
                        acc = state == Enums.PowerupState.MegaMushroom ? WALK_TURNAROUND_MEGA_ACC[(int)turnaroundFrames] : WALK_TURNAROUND_ACC[(int)turnaroundFrames];
                    }
                }
                else
                {
                    acc = SPEED_STAGE_ACC[0];
                }
            }
            else
            {

                if (skidding && !turnaround)
                {
                    skidding = false;
                }

                if (turnaround && turnaroundBoostFrames > 0 && speed != 0)
                {
                    turnaround = false;
                    skidding = false;
                }

                if (turnaround && speed < TURNAROUND_THRESHOLD)
                {
                    if (--turnaroundBoostFrames <= 0)
                    {
                        acc = TURNAROUND_ACC;
                        skidding = false;
                    }
                    else
                    {
                        acc = 0;
                    }
                }
                else
                {
                    turnaround = false;
                }
            }

            int direction = left ? -1 : 1;
            float newX = body.velocity.x + acc * direction;

            if (Mathf.Abs(newX) - speed > 0)
            {
                //clamp only if accelerating
                newX = Mathf.Clamp(newX, -max, max);
            }

            if (skidding && !turnaround && Mathf.Sign(newX) != sign)
            {
                //turnaround
                turnaround = true;
                turnaroundBoostFrames = 5;
                newX = 0;
            }

            body.velocity = new(newX, body.velocity.y);

        }
        else if (physics.onGround)
        {
            //not holding anything, sliding, or holding both directions. decelerate

            skidding = false;
            turnaround = false;

            if (body.velocity.x == 0)
                return;

            if (knockback)
                acc = -KNOCKBACK_DEC;
            else
                acc = -BUTTON_RELEASE_DEC;

            int direction = (int)Mathf.Sign(body.velocity.x);
            float newX = body.velocity.x + acc * direction;

            if ((direction == -1) ^ (newX <= 0))
                newX = 0;

            body.velocity = new(newX, body.velocity.y);

            //if (newX != 0)
            //facingRight = newX > 0;
        }

        if (physics.onGround)
            body.velocity = new(body.velocity.x, 0);
    }


    Enums.PlayerEyeState eyeState;
    float blinkTimer;
    public void HandleAnimations()
    {
        animator.SetBool("mega", state == Enums.PowerupState.MegaMushroom);
        animator.SetBool("turnaround", turnaround);
        animator.SetBool("skidding", skidding);
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("fakemario_Powerup") || dead)
        {
            models.transform.rotation = Quaternion.Lerp(models.transform.rotation, Quaternion.Euler(new Vector3(0, 180, 0)), Time.fixedDeltaTime * 15);
        }
        else
        {
            models.transform.rotation = Quaternion.Lerp(models.transform.rotation, Quaternion.Euler(new Vector3(0, left ? 250 : 110, 0)), Time.fixedDeltaTime * 15);
        }
        SetParticleEmission(dust, (physics.onGround && (skidding || (crouching && Mathf.Abs(body.velocity.x) > 1))));
        SetParticleEmission(giantParticle, state == Enums.PowerupState.MegaMushroom && giantStartTimer <= 0);
        return;
    }


    void HandleJumping(bool jump)
    {
        if (knockback)
            return;

        bool topSpeed = Mathf.Abs(body.velocity.x) >= RunningMaxSpeed;
        if (bounce || (jump && physics.onGround))
        {

            skidding = false;
            turnaround = false;
            //alreadyGroundpounded = false;
            groundpound = false;
            groundpoundCounter = 0;
            flying &= bounce;


            float vel = state switch
            {
                Enums.PowerupState.MegaMushroom => megaJumpVelocity,
                _ => jumpVelocity + Mathf.Abs(body.velocity.x) / RunningMaxSpeed * 1.05f,
            };


            body.velocity = new Vector2(body.velocity.x, vel);
            physics.onGround = false;
            body.position += Vector2.up * 0.075f;
            groundpoundCounter = 0;

            if (!bounce)
            {
                //play jump sound
                Enums.Sounds sound = state switch
                {
                    Enums.PowerupState.MiniMushroom => Enums.Sounds.Powerup_MiniMushroom_Jump,
                    Enums.PowerupState.MegaMushroom => Enums.Sounds.Powerup_MegaMushroom_Jump,
                    _ => Enums.Sounds.Player_Sound_Jump,
                };
                photonView.RPC(nameof(PlaySound), RpcTarget.All, sound);
            }
            bounce = false;
        }
    }


    private void SetParticleEmission(ParticleSystem particle, bool value)
    {
        if (value)
        {
            if (particle.isStopped)
                particle.Play();
        }
        else
        {
            if (particle.isPlaying)
                particle.Stop();
        }
    }


    protected void GiantFootstep()
    {
        CameraController.ScreenShake = 0.15f;
        SpawnParticle("Prefabs/Particle/GroundpoundDust", body.position + new Vector2(left ? -0.5f : 0.5f, 0));
        PlaySoundVarient(Enums.Sounds.Powerup_MegaMushroom_Walk, (byte)(step ? 1 : 2));
        step = !step;
    }

    protected void Footstep()
    {
        if (state == Enums.PowerupState.MegaMushroom)
            return;

        bool right = joystick.x > .5f;
        bool left = joystick.x < -.5f;
        bool reverse = body.velocity.x != 0 && ((left ? 1 : -1) == Mathf.Sign(body.velocity.x));
        if ((left ^ right) && reverse)
        {
            PlaySound(Enums.Sounds.World_Ice_Skidding);
            return;
        }
        if (Mathf.Abs(body.velocity.x) < WalkingMaxSpeed)
            return;

        PlaySoundVarient(footstepSound, (byte)(step ? 1 : 2), Mathf.Abs(body.velocity.x) / (RunningMaxSpeed + 4));
        step = !step;
    }
    #endregion
    [PunRPC]
    public void PlaySoundVarient(Enums.Sounds sound, byte variant)
    {
        PlaySoundVarient(sound, variant, 1);
    }

    [PunRPC]
    public void PlaySoundVarient(Enums.Sounds sound, byte variant, float volume)
    {
        audioSource.PlayOneShot(sound.GetClip(playerData, variant), volume);
        //audioSource.Stop();
        //audioSource.volume = volume;
        //audioSource.clip = sound.GetClip(null, variant);
        //audioSource.Play();
    }


    [PunRPC]
    public void PlaySoundEverywhere(Enums.Sounds sound)
    {
        GameManager.Instance.sfx.PlayOneShot(sound.GetClip(playerData));
    }

    [PunRPC]
    protected void SpawnParticle(string particle, Vector2 worldPos)
    {
        Instantiate(Resources.Load(particle), worldPos, Quaternion.identity);
    }

    [PunRPC]
    protected void SpawnParticle(string particle, Vector2 worldPos, Vector3 rot)
    {
        Instantiate(Resources.Load(particle), worldPos, Quaternion.Euler(rot));
    }

    public void HandleModels()
    {
        bool large = state >= Enums.PowerupState.Mushroom;

        largeModel.SetActive(large);
        smallModel.SetActive(!large);

        anim.avatar = large ? largeAvatar : smallAvatar;
        anim.runtimeAnimatorController = large ? playerData.largeOverrides : playerData.smallOverrides;
        if (giantEndTimer > 0)
        {
            transform.localScale = Vector3.one + (Vector3.one * (Mathf.Min(1, giantEndTimer / (giantStartTime / 2f)) * 2.6f));
        }
        else
        {
            transform.localScale = state switch
            {
                Enums.PowerupState.MiniMushroom => Vector3.one / 2,
                Enums.PowerupState.MegaMushroom => Vector3.one + (Vector3.one * (Mathf.Min(1, 1 - (giantStartTimer / giantStartTime)) * 2.6f)),
                _ => Vector3.one,
            };
        }
        //Shader effects
        if (materialBlock == null)
            materialBlock = new();
        int ps = state switch
        {
            Enums.PowerupState.FireFlower => 1,
            Enums.PowerupState.PropellerMushroom => 2,
            Enums.PowerupState.IceFlower => 3,
            Enums.PowerupState.Bat => 4,
            _ => 0
        };
        if (dead)
        {
            eyeState = Enums.PlayerEyeState.Death;
        }
        else
        {
            if (state == Enums.PowerupState.Glock || state == Enums.PowerupState.Sword || state == Enums.PowerupState.Bat)
            {
                if ((blinkTimer -= Time.fixedDeltaTime) < 0)
                    blinkTimer = 3f + (Random.value * 6f);
                if (blinkTimer < blinkDuration)
                {
                    eyeState = Enums.PlayerEyeState.AngryHalfBlink;
                }
                else if (blinkTimer < blinkDuration * 2f)
                {
                    eyeState = Enums.PlayerEyeState.FullBlink;
                }
                else if (blinkTimer < blinkDuration * 3f)
                {
                    eyeState = Enums.PlayerEyeState.AngryHalfBlink;
                }
                else
                {
                    eyeState = Enums.PlayerEyeState.Angry;
                }
            }
            else
            {
                if ((blinkTimer -= Time.fixedDeltaTime) < 0)
                    blinkTimer = 3f + (Random.value * 6f);
                if (blinkTimer < blinkDuration)
                {
                    eyeState = Enums.PlayerEyeState.HalfBlink;
                }
                else if (blinkTimer < blinkDuration * 2f)
                {
                    eyeState = Enums.PlayerEyeState.FullBlink;
                }
                else if (blinkTimer < blinkDuration * 3f)
                {
                    eyeState = Enums.PlayerEyeState.HalfBlink;
                }
                else
                {
                    eyeState = Enums.PlayerEyeState.Normal;
                }
            }
        }
        materialBlock.SetFloat("PowerupState", ps);
        materialBlock.SetFloat("EyeState", (int)eyeState);
        materialBlock.SetFloat("ModelScale", transform.lossyScale.x);

        if (playerAnim)
        {
            materialBlock.SetVector("OverallsColor", playerAnim.primaryColor);
            materialBlock.SetVector("ShirtColor", playerAnim.secondaryColor);
        }

        List<Renderer> renderers = new();
        if (renderers.Count == 0)
        {
            renderers.AddRange(GetComponentsInChildren<MeshRenderer>(true));
            renderers.AddRange(GetComponentsInChildren<SkinnedMeshRenderer>(true));
        }
        foreach (Renderer r in renderers)
            r.SetPropertyBlock(materialBlock);
    }
    [PunRPC]
    public override void Kill()
    {
        TakeDamage(false, groundpound);
    }

    [PunRPC]
    public override void SpecialKill(bool right, bool groundpound, int combo)
    {
        TakeDamage(groundpound, groundpound);
    }

    [PunRPC]
    public override void SpecialKillWithForce(bool right, bool groundpound, int combo)
    {
        TakeDamage(groundpound, groundpound);
    }


    [PunRPC]
    public override void Unfreeze(byte reasonByte)
    {
        Frozen = false;
        animator.enabled = true;
        if (body)
            body.isKinematic = false;
        hitbox.enabled = true;
        audioSource.enabled = true;

        foreach (BoxCollider2D hitboxes in GetComponentsInChildren<BoxCollider2D>(true))
        {
            hitboxes.enabled = true;
        }
    }


    [PunRPC]
    public override void Freeze(int cube)
    {
        audioSource.Stop();
        PlaySound(Enums.Sounds.Enemy_Generic_Freeze);
        Frozen = true;
        animator.enabled = false;
        foreach (BoxCollider2D hitboxes in GetComponentsInChildren<BoxCollider2D>())
        {
            hitboxes.enabled = false;
        }
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
        bool attackedFromAbove = Vector2.Dot(damageDirection, Vector2.up) > 0.5f && !player.onGround && state != Enums.PowerupState.MegaMushroom;

        if (!attackedFromAbove && player.CanShell() && player.crouching && !player.inShell)
        {
            photonView.RPC(nameof(SetLeft), RpcTarget.All, damageDirection.x > 0);
        }
        else if (player.invincible > 0 || player.inShell || player.sliding
            || (player.groundpound && player.state != Enums.PowerupState.MiniMushroom && attackedFromAbove)
            || player.state == Enums.PowerupState.MegaMushroom)
        {

            photonView.RPC(nameof(SpecialKill), RpcTarget.All, player.body.velocity.x > 0, player.groundpound, player.StarCombo++);
        }
        else if (attackedFromAbove)
        {
            if (player.state == Enums.PowerupState.MiniMushroom)
            {
                if (player.groundpound)
                {
                    player.groundpound = false;
                    photonView.RPC(nameof(SpecialKill), RpcTarget.All, true, false, 0);
                }
                player.bounce = true;
            }
            else
            {
                photonView.RPC(nameof(TakeDamage), RpcTarget.All, false, true);
                player.bounce = !player.groundpound;
            }
            player.photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Enemy_Generic_Stomp);
            player.drill = false;

        }
        else if (player.hitInvincibilityCounter <= 0)
        {
            player.photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Enemy_Generic_Stomp);
            bounce = true;
            player.photonView.RPC(nameof(PlayerController.Powerdown), RpcTarget.All, false);
        }
    }

    [PunRPC]
    public void TakeDamage(bool groundpound, bool melee)
    {
        bool right = playerAnim.transform.position.x > transform.position.x;
        if (melee)
        {
            playerAnim.controller.body.velocity = new Vector2(5 * (right ? 1 : -1), 5);
        }
        body.velocity = new Vector2(5 * (right ? -1 : 1), 0);
        if (state == Enums.PowerupState.MegaMushroom)
        {
            return;
        }
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("fakemario_Powerup") && anim.GetCurrentAnimatorStateInfo(0).normalizedTime > .2 && melee)
        {
            Powerup toDrop = ((Powerup)Resources.Load("Scriptables/Powerups/" + GetUsingPowerupShittily()));
            PhotonNetwork.Instantiate("Prefabs/Powerup/" + toDrop.prefab, fireflower.transform.position + (Vector3.up / 2), Quaternion.identity);
            powerup.Play("Nothing");
        }
        anim.Play("knockback-getup");
        if (state > Enums.PowerupState.Mushroom)
        {
            state = Enums.PowerupState.Mushroom;
            PlaySound(Enums.Sounds.Player_Sound_Powerdown);
            return;
        }
        if (groundpound)
        {
            PlaySoundEverywhere(Enums.Sounds.Player_Sound_DamageHealth);
            health--;
            if (state == Enums.PowerupState.Small)
            {
                PlaySound(Enums.Sounds.Player_Sound_Death);
                GameManager.Instance.SetBossMusic(false);
                GameManager.Instance.music.volume = 0;
                dead = true;
                return;
            }
            phase = state == Enums.PowerupState.MegaMushroom ? 3 : health > 0 ? (health > megaphaseHP ? 0 : 1) : 4;
            if (phase == 4)
            {
                state = Enums.PowerupState.Small;
                timeUntilMushroom = 5;
                PlaySound(Enums.Sounds.Player_Sound_Powerdown);
            }
        }
    }
    public string GetUsingPowerupShittily()
    {
        if (fireflower.activeSelf)
        {
            return "FireFlower";
        }
        else if (iceflower.activeSelf)
        {
            return "IceFlower";
        }
        else if (mushroom.activeSelf)
        {
            return "Mushroom";
        }
        else if (megamushroom.activeSelf)
        {
            return "MegaMushroom";
        }
        return "null";
    }


    [PunRPC]
    public void FinishMegaMario(bool success)
    {
        PlaySoundEverywhere(Enums.Sounds.Player_Voice_MegaMushroom);
        body.isKinematic = false;
    }

    [PunRPC]
    public void EndMega()
    {
        timeUntilCanMega = 5;
        giantEndTimer = giantStartTime / 2f;
        state = Enums.PowerupState.Mushroom;
        PlaySoundEverywhere(Enums.Sounds.Powerup_MegaMushroom_End);
        body.velocity = new(body.velocity.x, body.velocity.y > 0 ? (body.velocity.y / 3f) : body.velocity.y);
    }

    public void UsePowerupAction()
    {
        if (state != Enums.PowerupState.IceFlower && state != Enums.PowerupState.FireFlower || !photonView.IsMine)
            return;

        if (groundpound || flying || crouching)
            return;

        canShootProjectile = Random.Range(0, 100) > 98;
        fireballTimer = .5f;
        if (fireballTimer <= 0)
        {
            canShootProjectile = true;
            fireballTimer = .5f;
        }
        else if (canShootProjectile)
        {
            canShootProjectile = false;
        }
        else
        {
            return;
        }

        bool ice = state == Enums.PowerupState.IceFlower;
        string projectile = ice ? "Iceball" : "Fireball";
        Enums.Sounds sound = ice ? Enums.Sounds.Powerup_Iceball_Shoot : Enums.Sounds.Powerup_Fireball_Shoot;

        Vector2 pos = body.position + new Vector2(!left ^ animator.GetCurrentAnimatorStateInfo(0).IsName("turnaround") ? 0.5f : -0.5f, 0.3f);
        if (Utils.IsTileSolidAtWorldLocation(pos))
        {
            photonView.RPC(nameof(SpawnParticle), RpcTarget.All, $"Prefabs/Particle/{projectile}Wall", pos);
        }
        else
        {
            GameObject ball = PhotonNetwork.Instantiate($"Prefabs/{projectile}", pos, Quaternion.identity, 0, new object[] { left ^ animator.GetCurrentAnimatorStateInfo(0).IsName("turnaround"), body.velocity.x });
            if (ball)
            {
                FireballMover fireball = ball.GetComponent<FireballMover>();
                if (fireball != null)
                {
                    fireball.hitsOwner = true;
                }
            }
        }
        photonView.RPC(nameof(PlaySound), RpcTarget.All, sound);

        animator.SetTrigger("fireball");
        wallJumpTimer = 0;
    }


    void HandleDeathAnimation()
    {
        if (!dead)
        {
            deathTimer = 0;
            return;
        }
        hitbox.enabled = false;
        deathTimer += Time.fixedDeltaTime;
        if (deathTimer < deathUpTime)
        {
            deathUp = false;
            body.gravityScale = 0;
            body.velocity = Vector2.zero;
            animator.Play("deadstart");
        }
        else
        {
            if (!deathUp && body.position.y > GameManager.Instance.GetLevelMinY())
            {
                body.velocity = new Vector2(0, deathForce);
                deathUp = true;
                if (animator.GetBool("firedeath"))
                {
                    PlaySound(Enums.Sounds.Player_Voice_LavaDeath);
                    PlaySound(Enums.Sounds.Player_Sound_LavaHiss);
                }
                animator.SetTrigger("deathup");
            }
            body.gravityScale = 1.2f;
            body.velocity = new Vector2(0, Mathf.Max(-deathForce, body.velocity.y));

            if (body.position.y < GameManager.Instance.GetLevelMinY() - transform.lossyScale.y)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        if (!JustModelLol)
        {
            Instantiate(goalObject, origin, Quaternion.identity);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(body.velocity);
            stream.SendNext(joystick);
            stream.SendNext(left);
            stream.SendNext((int)state);
            stream.SendNext(phase);
            stream.SendNext(health);
            stream.SendNext(timeUntilCanMega);
            stream.SendNext(timeUntilMushroom);
        }
        else
        {
            transform.position = (Vector3)stream.ReceiveNext();
            body.velocity = (Vector2)stream.ReceiveNext();
            joystick = (Vector2)stream.ReceiveNext();
            left = (bool)stream.ReceiveNext();
            state = (Enums.PowerupState)stream.ReceiveNext();
            phase = (int)stream.ReceiveNext();
            health = (int)stream.ReceiveNext();
            timeUntilCanMega = (float)stream.ReceiveNext();
            timeUntilMushroom = (float)stream.ReceiveNext();
        }
    }
}
