using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

using Photon.Pun;
using ExitGames.Client.Photon;
using NSMB.Utils;
using UnityEngine.AI;
using UnityEngine.Playables;

public class PlayerController : MonoBehaviourPun, IFreezableEntity, ICustomSerializeView, IOnPhotonViewPreNetDestroy {

    [Header("Modded")]
    public float tbag_tracker;
    public bool tbagging;
    public List<WaterSplash> WaterSplashList = new List<WaterSplash>();
    public int buttBurnt; //set to 2 when burnt, landing while it's 2 makes Mario bounce and set it to 1, and after landing at 1, Mario is reset
    public Vector2 aim;
    public float dashTimer;
    public bool dashed;
    public float sandmanTimer;
    public Portal portalA, portalB;
    private bool alreadySpawned;
    public bool underWater;
    public float oxygen = 1, maxOxygen;
    public float stillDrownTimer;

    public static List<PlayerController> instances = new List<PlayerController>();
    [Header("Base Game")]

    #region Variables

    // == NETWORKING VARIABLES ==
    private static readonly float EPSILON = 0.2f, RESEND_RATE = 0.5f;

    public bool Active { get; set; } = true;
    private Vector2 previousJoystick, previousAim;
    private short previousFlags;
    private byte previousFlags2;
    private double lastSendTimestamp;

    // == MONOBEHAVIOURS ==

    public int playerId = -1;
    public bool dead = false, spawned = false;
    public Enums.PowerupState state = Enums.PowerupState.Small, previousState;
    public float slowriseGravity = 0.85f, normalGravity = 2.5f, flyingGravity = 0.8f, flyingTerminalVelocity = 1.25f, drillVelocity = 7f, groundpoundTime = 0.25f, groundpoundVelocity = 10, blinkingSpeed = 0.25f, terminalVelocity = -7f, jumpVelocity = 6.25f, megaJumpVelocity = 16f, launchVelocity = 12f, wallslideSpeed = -4.25f, giantStartTime = 1.5f, soundRange = 10f, slopeSlidingAngle = 12.5f, pickupTime = 0.5f;
    public float propellerLaunchVelocity = 6, propellerFallSpeed = 2, propellerSpinFallSpeed = 1.5f, propellerSpinTime = 0.75f, propellerDrillBuffer, heightSmallModel = 0.42f, heightLargeModel = 0.82f;

    BoxCollider2D[] hitboxes;
    GameObject models;

    public CameraController cameraController;
    public FadeOutManager fadeOut;

    public AudioSource sfx, sfxBrick;
    public Animator animator;
    public Rigidbody2D body;

    public PlayerAnimationController AnimationController { get; private set; }

    public bool onGround, previousOnGround, crushGround, doGroundSnap, jumping, properJump, hitRoof, skidding, turnaround, facingRight = true, singlejump, doublejump, triplejump, bounce, crouching, groundpound, groundpoundLastFrame, sliding, knockback, hitBlock, running, functionallyRunning, jumpHeld, flying, drill, inShell, hitLeft, hitRight, stuckInBlock, alreadyStuckInBlock, propeller, usedPropellerThisJump, stationaryGiantEnd, fireballKnockback, startedSliding, canShootProjectile;
    public float jumpLandingTimer, landing, koyoteTime, groundpoundCounter, groundpoundStartTimer, pickupTimer, groundpoundDelay, hitInvincibilityCounter, powerupFlash, throwInvincibility, jumpBuffer, giantStartTimer, giantEndTimer, propellerTimer, propellerSpinTimer, fireballTimer;
    public float invincible, giantTimer, floorAngle, knockbackTimer, pipeTimer, slowdownTimer;

    //MOVEMENT STAGES
    private static readonly int WALK_STAGE = 1, RUN_STAGE = 3, STAR_STAGE = 4;
    private static readonly float[] SPEED_STAGE_MAX = { 0.9375f, 2.8125f, 4.21875f, 5.625f, 8.4375f };
    private static readonly float SPEED_SLIDE_MAX = 7.5f;
    private static readonly float[] SPEED_STAGE_ACC = { 0.131835975f, 0.06591802875f, 0.05859375f, 0.0439453125f, 1.40625f };
    private static readonly float[] WALK_TURNAROUND_ACC = { 0.0659179686f, 0.146484375f, 0.234375f };
    private static readonly float BUTTON_RELEASE_DEC = 0.0659179686f;
    private static readonly float SKIDDING_THRESHOLD = 4.6875f;
    private static readonly float SKIDDING_DEC = 0.17578125f;
    private static readonly float SKIDDING_STAR_DEC = 1.40625f;

    private static readonly float WALLJUMP_HSPEED = 4.21874f;
    private static readonly float WALLJUMP_VSPEED = 6.4453125f;

    private static readonly float KNOCKBACK_DEC = 0.131835975f;

    private static readonly float[] SPEED_STAGE_SPINNER_MAX = { 1.12060546875f, 2.8125f };
    private static readonly float[] SPEED_STAGE_SPINNER_ACC = { 0.1318359375f, 0.06591796875f };

    private static readonly float[] SPEED_STAGE_MEGA_ACC = { 0.46875f, 0.0805664061f, 0.0805664061f, 0.0805664061f, 0.0805664061f };
    private static readonly float[] WALK_TURNAROUND_MEGA_ACC = { 0.0769042968f, 0.17578125f, 0.3515625f };

    private static readonly float TURNAROUND_THRESHOLD = 2.8125f;
    private static readonly float TURNAROUND_ACC = 0.46875f;
    private float turnaroundFrames;
    private int turnaroundBoostFrames;

    private static readonly float[] BUTTON_RELEASE_ICE_DEC = { 0.00732421875f, 0.02471923828125f, 0.02471923828125f, 0.02471923828125f, 0.02471923828125f };
    private static readonly float SKIDDING_ICE_DEC = 0.06591796875f;
    private static readonly float WALK_TURNAROUND_ICE_ACC = 0.0439453125f;

    private static readonly float SLIDING_45_ACC = 0.2197265625f;
    private static readonly float SLIDING_22_ACC = 0.087890625f;

    public float RunningMaxSpeed => SPEED_STAGE_MAX[RUN_STAGE];
    public float WalkingMaxSpeed => SPEED_STAGE_MAX[WALK_STAGE];

    private int MovementStage {
        get {
            float xVel = Mathf.Abs(body.velocity.x);
            float[] arr = flying ? SPEED_STAGE_SPINNER_MAX : SPEED_STAGE_MAX;
            for (int i = 0; i < arr.Length; i++) {
                if (xVel <= arr[i])
                    return i;
            }
            return arr.Length - 1;
        }
    }

    //Walljumping variables
    private float wallSlideTimer, wallJumpTimer;
    public bool wallSlideLeft, wallSlideRight;

    private int _starCombo;
    public int StarCombo {
        get => invincible > 0 ? _starCombo : 0;
        set => _starCombo = invincible > 0 ? value : 0;
    }

    public Vector2 pipeDirection;
    public int stars, coins, ammo, lives = -1;
    public Powerup storedPowerup = null;
    public HoldableEntity holding, holdingOld;
    public FrozenCube frozenObject;

    private bool powerupButtonHeld, altPowerupButtonHeld;
    private readonly float analogDeadzone = 0.35f;
    public Vector2 joystick, giantSavedVelocity, previousFrameVelocity, previousFramePosition;

    public GameObject onSpinner;
    public PipeManager pipeEntering;
    public bool step, alreadyGroundpounded;
    public PlayerData character;

    //Tile data
    private Enums.Sounds footstepSound = Enums.Sounds.Player_Walk_Grass;
    public bool onIce;
    private readonly List<Vector3Int> tilesStandingOn = new(), tilesJumpedInto = new(), tilesHitSide = new();

    private GameObject trackIcon;

    private Hashtable gameState = new() {
        [Enums.NetPlayerProperties.GameState] = new Hashtable()
    }; //only used to update joining spectators

    private bool initialKnockbackFacingRight = true;

    // == FREEZING VARIABLES ==
    public bool Frozen { get; set; }
    bool IFreezableEntity.IsCarryable => true;
    bool IFreezableEntity.IsFlying => flying || propeller; //doesn't work consistently?


    public BoxCollider2D MainHitbox => hitboxes[0];
    public Vector2 WorldHitboxSize => MainHitbox.size * transform.lossyScale;

    private readonly Dictionary<GameObject, double> lastCollectTime = new();

    #endregion

    #region Pun Serialization
    public void Serialize(List<byte> buffer) {
        bool updateJoystick = Vector2.Distance(joystick, previousJoystick) > EPSILON;
        bool updateAim = Vector2.Distance(aim, previousAim) > EPSILON;

        SerializationUtils.PackToShort(out short flags, running, jumpHeld, crouching, groundpound,
                facingRight, onGround, knockback, flying, drill, sliding, skidding, wallSlideLeft,
                wallSlideRight, invincible > 0, propellerSpinTimer > 0, wallJumpTimer > 0);
        SerializationUtils.PackToByte(out byte flags2, turnaround, propeller);
        bool updateFlags = flags != previousFlags || flags2 != previousFlags2;

        bool forceResend = PhotonNetwork.Time - lastSendTimestamp > RESEND_RATE;

        if (forceResend || updateJoystick || updateAim || updateFlags) {
            //send joystick for simulation reasons
            SerializationUtils.PackToShort(buffer, joystick, -1, 1);
            SerializationUtils.PackToShort(buffer, aim, -1, 1);
            previousJoystick = joystick;
            previousAim = aim;

            //serialize movement flags
            SerializationUtils.WriteShort(buffer, flags);
            previousFlags = flags;
            SerializationUtils.WriteByte(buffer, flags2);
            previousFlags2 = flags2;

            lastSendTimestamp = PhotonNetwork.Time;
        }
    }

    public void Deserialize(List<byte> buffer, ref int index, PhotonMessageInfo info) {
        //controller position
        SerializationUtils.UnpackFromShort(buffer, ref index, -1, 1, out joystick);
        SerializationUtils.UnpackFromShort(buffer, ref index, -1, 1, out aim);

        //controller flags
        SerializationUtils.UnpackFromShort(buffer, ref index, out bool[] flags);
        running = flags[0];
        jumpHeld = flags[1];
        crouching = flags[2];
        groundpound = flags[3];
        facingRight = flags[4];
        previousOnGround = doGroundSnap = onGround = flags[5];
        knockback = flags[6];
        flying = flags[7];
        drill = flags[8];
        sliding = flags[9];
        skidding = flags[10];
        wallSlideLeft = flags[11];
        wallSlideRight = flags[12];
        invincible = flags[13] ? 1 : 0;
        propellerSpinTimer = flags[14] ? 1 : 0;
        wallJumpTimer = flags[15] ? 1 : 0;

        SerializationUtils.UnpackFromByte(buffer, ref index, out bool[] flags2);
        turnaround = flags2[0];
        propeller = flags2[1];

        //resimulations
        float lag = (float) (PhotonNetwork.Time - info.SentServerTime);
        int fullResims = (int) (lag / Time.fixedDeltaTime);
        float partialResim = lag % Time.fixedDeltaTime;

        while (fullResims-- > 0)
            HandleMovement(Time.fixedDeltaTime);
        HandleMovement(partialResim);
    }

    #endregion

    #region Unity Methods
    public void Awake() {
        instances.Add(this);
        cameraController = GetComponent<CameraController>();
        cameraController.IsControllingCamera = photonView.IsMine;

        animator = GetComponentInChildren<Animator>();
        body = GetComponent<Rigidbody2D>();
        sfx = GetComponent<AudioSource>();
        sfxBrick = GetComponents<AudioSource>()[1];
        //hitboxManager = GetComponent<WrappingHitbox>();
        AnimationController = GetComponent<PlayerAnimationController>();
        fadeOut = GameObject.FindGameObjectWithTag("FadeUI").GetComponent<FadeOutManager>();

        body.position = transform.position = GameManager.Instance.GetSpawnpoint(playerId);

        models = transform.Find("Models").gameObject;

        int count = 0;

        foreach (var player in PhotonNetwork.PlayerList) {

            Utils.GetCustomProperty(Enums.NetPlayerProperties.Spectator, out bool spectating, photonView.Owner.CustomProperties);
            if (spectating)
                continue;

            if (player == photonView.Owner)
                break;
            count++;
        }

        playerId = count;
        Utils.GetCustomProperty(Enums.NetRoomProperties.Lives, out lives);

        if (photonView.IsMine) {
            InputSystem.controls.Player.Movement.performed += OnMovement;
            InputSystem.controls.Player.Movement.canceled += OnMovement;
            InputSystem.controls.Player.Aim.performed += OnLook;
            InputSystem.controls.Player.Aim.canceled += OnLook;
            InputSystem.controls.Player.Jump.performed += OnJump;
            InputSystem.controls.Player.Sprint.started += OnSprint;
            InputSystem.controls.Player.Sprint.canceled += OnSprint;
            InputSystem.controls.Player.PowerupAction.performed += OnPowerupAction;
            InputSystem.controls.Player.AltPowerupAction.performed += OnAltPowerupAction;
            InputSystem.controls.Player.ReserveItem.performed += OnReserveItem;
        }

        GameManager.Instance.players.Add(this);
    }

    public void OnPreNetDestroy(PhotonView rootView) {
        GameManager.Instance.players.Remove(this);
    }

    public void Start() {
        hitboxes = GetComponents<BoxCollider2D>();
        trackIcon = UIUpdater.Instance.CreatePlayerIcon(this);
        transform.position = body.position = GameManager.Instance.spawnpoint;

        LoadFromGameState();
        spawned = true;
        cameraController.Recenter();
    }

    public void OnDestroy() {
        instances.Remove(this);
        if (!photonView.IsMine)
            return;

        InputSystem.controls.Player.Movement.performed -= OnMovement;
        InputSystem.controls.Player.Movement.canceled -= OnMovement;
        InputSystem.controls.Player.Aim.performed -= OnLook;
        InputSystem.controls.Player.Aim.canceled -= OnLook;
        InputSystem.controls.Player.Jump.performed -= OnJump;
        InputSystem.controls.Player.Sprint.started -= OnSprint;
        InputSystem.controls.Player.Sprint.canceled -= OnSprint;
        InputSystem.controls.Player.PowerupAction.performed -= OnPowerupAction;
        InputSystem.controls.Player.ReserveItem.performed -= OnReserveItem;

        SpawnParticle("Prefabs/Particle/Puff", body.position);
        GameManager.Instance.SpectationManager.Spectating = true;
    }

    public void OnGameStart() {
        Debug.Log("OnGameStart()");
        photonView.RPC(nameof(PreRespawn), RpcTarget.All);

        gameState = new() {
            [Enums.NetPlayerProperties.GameState] = new Hashtable()
        };
    }

    public void LoadFromGameState() {

        //Don't load from our own state
        if (photonView.IsMine)
            return;

        //We don't have a state to load
        if (photonView.Owner.CustomProperties[Enums.NetPlayerProperties.GameState] is not Hashtable gs)
            return;

        lives = (int) gs[Enums.NetPlayerGameState.Lives];
        stars = (int) gs[Enums.NetPlayerGameState.Stars];
        coins = (int) gs[Enums.NetPlayerGameState.Coins];
        state = (Enums.PowerupState) gs[Enums.NetPlayerGameState.PowerupState];
        if (gs[Enums.NetPlayerGameState.ReserveItem] != null) {
            storedPowerup = (Powerup) Resources.Load("Scriptables/Powerups/" + (Enums.PowerupState) gs[Enums.NetPlayerGameState.ReserveItem]);
        } else {
            storedPowerup = null;
        }
    }

    public void UpdateGameState() {
        if (!photonView.IsMine)
            return;

        UpdateGameStateVariable(Enums.NetPlayerGameState.Lives, lives);
        UpdateGameStateVariable(Enums.NetPlayerGameState.Stars, stars);
        UpdateGameStateVariable(Enums.NetPlayerGameState.Coins, coins);
        UpdateGameStateVariable(Enums.NetPlayerGameState.PowerupState, (byte) state);
        UpdateGameStateVariable(Enums.NetPlayerGameState.ReserveItem, storedPowerup ? storedPowerup.state : null);

        photonView.Owner.SetCustomProperties(gameState);
    }

    private void UpdateGameStateVariable(string key, object value) {
        ((Hashtable) gameState[Enums.NetPlayerProperties.GameState])[key] = value;
    }

    public void FixedUpdate() {
        if(tbag_tracker > 0)
        {
            if(tbag_tracker > 3)
            {
                tbag_tracker -= Time.fixedDeltaTime * 2;
                if(tbag_tracker <= 3)
                {
                    tbag_tracker = -3;
                }
            }
            else
            {
                tbag_tracker -= Time.fixedDeltaTime * 2;
            }
            if (tbag_tracker < 0)
            {
                tbag_tracker = 0;
            }
            tbagging = tbag_tracker > 3;
        }
        maxOxygen = state == Enums.PowerupState.Goggles ? 60 : 30;
        if (GameManager.Instance.freezingWater)
        {
            maxOxygen /= 2;
        }
        //game ended, freeze.
        if (!GameManager.Instance.musicEnabled) {
            models.SetActive(false);
            return;
        }
        if (GameManager.Instance.gameover) {
            body.velocity = Vector2.zero;
            animator.enabled = false;
            body.isKinematic = true;
            return;
        }
        if (!pipeEntering && !dead)
        {
            stillDrownTimer -= Time.fixedDeltaTime;
            underWater = WaterSplashList.Count > 0 || stillDrownTimer > 0;
            if (underWater)
            {
                oxygen -= Time.fixedDeltaTime / maxOxygen;
                if (oxygen <= 0)
                {
                    photonView.RPC(nameof(Death), RpcTarget.All, false, false);
                }
            }
            else
            {
                oxygen = 1;
            }
        }
        else
        {
            if (underWater)
            {
                stillDrownTimer = .1f;
            }
        }

        groundpoundLastFrame = groundpound;
        previousOnGround = onGround;
        if (!dead) {
            HandleBlockSnapping();
            bool snapped = GroundSnapCheck();
            HandleGroundCollision();
            onGround |= snapped;
            doGroundSnap = onGround;
            HandleTileProperties();
            TickCounters();
            HandleMovement(Time.fixedDeltaTime);
            HandleGiantTiles(true);
            UpdateHitbox();

            if (swordActive && photonView.IsMine && (state == Enums.PowerupState.Bat || state == Enums.PowerupState.Sword))
            {
                foreach (PlayerController con in FindObjectsOfType<PlayerController>())
                {
                    if (con != this && !con.dead && AreAABBsIntersecting(AnimationController.sword.transform.position, Vector2.one, (Vector2)con.transform.position + con.MainHitbox.offset, con.MainHitbox.size * con.transform.lossyScale))
                    {
                        con.photonView.RPC(state == Enums.PowerupState.Bat ? nameof(KnockbackWithForce) : nameof(Knockback), RpcTarget.All, body.position.x > con.body.position.x, 1, false, photonView.ViewID);
                        if (state == Enums.PowerupState.Bat)
                        {
                            photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Powerup_Bat_Hit);
                        }
                    }
                }
                foreach (KillableEntity entity in FindObjectsOfType<KillableEntity>())
                {
                    if (!entity.dead && AreAABBsIntersecting(AnimationController.sword.transform.position, Vector2.one, (Vector2)entity.transform.position + entity.hitbox.offset, entity.hitbox.size * entity.transform.lossyScale))
                    {
                        entity.photonView.RPC(state == Enums.PowerupState.Bat ? nameof(KillableEntity.SpecialKillWithForce) : nameof(KillableEntity.SpecialKill), RpcTarget.All, body.position.x < entity.body.position.x, true, 0);
                        if(state == Enums.PowerupState.Bat)
                        {
                            photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Powerup_Bat_Hit);
                        }
                    }
                }
                if(state == Enums.PowerupState.Bat)
                {
                    foreach (MovingPowerup powerup in FindObjectsOfType<MovingPowerup>())
                    {
                        if (IsPointInAABB(powerup.transform.GetChild(0).position, AnimationController.sword.transform.position, Vector2.one * 1.5f))
                        {
                            powerup.photonView.RPC(nameof(MovingPowerup.SmackWithForce), RpcTarget.All, facingRight);
                            if (state == Enums.PowerupState.Bat)
                            {
                                photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Powerup_Bat_Hit);
                            }
                        }
                    }
                }
            }
        }
        if (holding && holding.dead)
            holding = null;

        AnimationController.UpdateAnimatorStates();
        HandleLayerState();
        previousFrameVelocity = body.velocity;
        previousFramePosition = body.position;
        if((state == Enums.PowerupState.Glock || state == Enums.PowerupState.PortalGun || state == Enums.PowerupState.Glockos) && wallJumpTimer <= 0f && Mathf.Abs(aim.x) > .125f)
        {
            facingRight = aim.x > 0;
        }

        if (inShell && Mathf.Abs(joystick.x) < analogDeadzone)
        {
            inShell = false;
        }

        if(holding != null)
        {
            holding.gameObject.SetActive(!inShell);
        }
        if (sandmanTimer > 0)
        {
            sandmanTimer -= Time.fixedDeltaTime;
        }
        justJumped = false;
    }
    #endregion

    #region -- COLLISIONS --
    void HandleGroundCollision() {
        tilesJumpedInto.Clear();
        tilesStandingOn.Clear();
        tilesHitSide.Clear();

        bool ignoreRoof = false;
        int down = 0, left = 0, right = 0, up = 0;

        crushGround = false;
        foreach (BoxCollider2D hitbox in hitboxes) {
            ContactPoint2D[] contacts = new ContactPoint2D[20];
            int collisionCount = hitbox.GetContacts(contacts);

            for (int i = 0; i < collisionCount; i++) {
                ContactPoint2D contact = contacts[i];
                GameObject go = contact.collider.gameObject;
                Vector2 n = contact.normal;
                Vector2 p = contact.point + (contact.normal * -0.15f);
                if (n == Vector2.up && contact.point.y > body.position.y)
                    continue;

                Vector3Int vec = Utils.WorldToTilemapPosition(p);
                if (!contact.collider || contact.collider.CompareTag("Player"))
                    continue;

                if (Vector2.Dot(n, Vector2.up) > .05f) {
                    if (Vector2.Dot(body.velocity.normalized, n) > 0.1f && !onGround) {
                        if (!contact.rigidbody || contact.rigidbody.velocity.y < body.velocity.y)
                            //invalid flooring
                            continue;
                    }

                    crushGround |= !go.CompareTag("platform") && !go.CompareTag("frozencube");
                    down++;
                    tilesStandingOn.Add(vec);
                } else if (contact.collider.gameObject.layer == Layers.LayerGround) {
                    if (Vector2.Dot(n, Vector2.down) > .9f) {
                        up++;
                        tilesJumpedInto.Add(vec);
                    } else {
                        if (n.x < 0) {
                            right++;
                        } else {
                            left++;
                        }
                        tilesHitSide.Add(vec);
                    }
                }
            }
        }

        onGround = down >= 1;
        hitLeft = left >= 1;
        hitRight = right >= 1;
        hitRoof = !ignoreRoof && up > 1;
        if (onGround)
        {
            dashTimer = 0;
            dashed = false;
            if(buttBurnt > 1)
            {
                buttBurnt--;
                body.velocity = new Vector2(body.velocity.x / 1.25f, 3);
                onGround = false;
                doGroundSnap = false;
            }
            else if(buttBurnt > 0)
            {
                buttBurnt--;
            }
        }
        if ((hitLeft || hitRight || hitRoof) && dashTimer <= .2f)
        {
            dashTimer = 0;
        }
    }
    void HandleTileProperties() {
        onIce = false;
        footstepSound = Enums.Sounds.Player_Walk_Grass;
        foreach (Vector3Int pos in tilesStandingOn) {
            TileBase tile = Utils.GetTileAtTileLocation(pos);
            if (tile == null)
                continue;
            if (tile is TileWithProperties propTile) {
                footstepSound = propTile.footstepSound;
                onIce = propTile.iceSkidding;
            }
            else if (tile is BreakableBrickTile brickTile) {
                footstepSound = brickTile.footstepSound;
                onIce = brickTile.iceSkidding;
            }
        }
    }

    private ContactPoint2D[] contacts = new ContactPoint2D[0];
    public void OnCollisionStay2D(Collision2D collision) {
        if (!photonView.IsMine || (knockback && !fireballKnockback) || Frozen)
            return;

        GameObject obj = collision.gameObject;

        double time = PhotonNetwork.Time;
        if (time - lastCollectTime.GetValueOrDefault(obj) < 0.5d)
            return;

        lastCollectTime[obj] = time;

        switch (collision.gameObject.tag) {
        case "Player": {
            //hit players

            if (contacts.Length < collision.contactCount)
                contacts = new ContactPoint2D[collision.contactCount];
            collision.GetContacts(contacts);

            foreach (ContactPoint2D contact in contacts) {
                GameObject otherObj = collision.gameObject;
                PlayerController other = otherObj.GetComponent<PlayerController>();
                PhotonView otherView = other.photonView;

                if (other.invincible > 0) {
                    //They are invincible. let them decide if they've hit us.
                    if (invincible > 0) {
                        //oh, we both are. bonk.
                        photonView.RPC(nameof(Knockback), RpcTarget.All, otherObj.transform.position.x > body.position.x, 1, true, otherView.ViewID);
                        other.photonView.RPC(nameof(Knockback), RpcTarget.All, otherObj.transform.position.x < body.position.x, 1, true, photonView.ViewID);
                    }
                    return;
                }

                if (invincible > 0) {
                    //we are invincible. murder time :)
                    if (other.state == Enums.PowerupState.MegaMushroom) {
                        //wait fuck-
                        photonView.RPC(nameof(Knockback), RpcTarget.All, otherObj.transform.position.x > body.position.x, 1, true, otherView.ViewID);
                        return;
                    }

                    otherView.RPC(nameof(Powerdown), RpcTarget.All, false);
                    body.velocity = previousFrameVelocity;
                    return;
                }

                float dot = Vector2.Dot((body.position - other.body.position).normalized, Vector2.up);
                bool above = dot > 0.7f;
                bool otherAbove = dot < -0.7f;

                //mega mushroom cases
                if (state == Enums.PowerupState.MegaMushroom || other.state == Enums.PowerupState.MegaMushroom) {
                    if (state == Enums.PowerupState.MegaMushroom && other.state == Enums.PowerupState.MegaMushroom) {
                        //both giant
                        if (above) {
                            bounce = true;
                            groundpound = false;
                            drill = false;
                            photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Enemy_Generic_Stomp);
                        } else if (!otherAbove) {
                            otherView.RPC(nameof(Knockback), RpcTarget.All, otherObj.transform.position.x < body.position.x, 0, true, photonView.ViewID);
                            photonView.RPC(nameof(Knockback), RpcTarget.All, otherObj.transform.position.x > body.position.x, 0, true, otherView.ViewID);
                        }
                    } else if (state == Enums.PowerupState.MegaMushroom) {
                        //only we are giant
                        otherView.RPC(nameof(Powerdown), RpcTarget.All, false);
                        body.velocity = previousFrameVelocity;
                    }
                    return;
                }

                //blue shell cases
                if (inShell) {
                    //we are blue shell
                    if (!otherAbove) {
                        //hit them. powerdown them
                        if (other.inShell) {
                            //collide with both
                            otherView.RPC(nameof(Knockback), RpcTarget.All, otherObj.transform.position.x < body.position.x, 1, true, photonView.ViewID);
                            photonView.RPC(nameof(Knockback), RpcTarget.All, otherObj.transform.position.x > body.position.x, 1, true, otherView.ViewID);
                        } else {
                            otherView.RPC(nameof(Powerdown), RpcTarget.All, false);
                        }
                        float dotRight = Vector2.Dot((body.position - other.body.position).normalized, Vector2.right);
                        facingRight = dotRight > 0;
                        return;
                    }
                }
                if (CanShell() && otherAbove && (!other.groundpound && !other.drill) && (crouching || groundpound)) {
                    body.velocity = new(SPEED_STAGE_MAX[RUN_STAGE] * (otherObj.transform.position.x < body.position.x ? 1 : -1), body.velocity.y);
                }
                if (other.inShell && !above)
                    return;

                if (!above && other.CanShell() && !other.inShell && other.crouching && !groundpound && !drill) {
                    //they are blue shell
                    bounce = true;
                    photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Enemy_Generic_Stomp);
                    return;
                }

                if (above) {
                    //hit them from above
                    bounce = !groundpound && !drill;
                    bool groundpounded = groundpound || drill;

                    if (state == Enums.PowerupState.MiniMushroom && other.state != Enums.PowerupState.MiniMushroom) {
                        //we are mini, they arent. special rules.
                        if (groundpounded) {
                            otherView.RPC(nameof(Knockback), RpcTarget.All, otherObj.transform.position.x < body.position.x, 1, false, photonView.ViewID);
                            groundpound = false;
                            bounce = true;
                        } else {
                            photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Enemy_Generic_Stomp);
                        }
                    } else if (other.state == Enums.PowerupState.MiniMushroom && groundpounded) {
                        //we are big, groundpounding a mini opponent. squish.
                        otherView.RPC(nameof(Knockback), RpcTarget.All, otherObj.transform.position.x > body.position.x, 3, false, photonView.ViewID);
                        bounce = false;
                    } else {
                        if (other.state == Enums.PowerupState.MiniMushroom && groundpounded) {
                            otherView.RPC(nameof(Powerdown), RpcTarget.All, false);
                        } else {
                            otherView.RPC(nameof(Knockback), RpcTarget.All, otherObj.transform.position.x < body.position.x, groundpounded ? 3 : 1, false, photonView.ViewID);
                        }
                    }
                    body.velocity = new Vector2(previousFrameVelocity.x, body.velocity.y);

                    return;
                } else if (!knockback && !other.knockback && !otherAbove && onGround && other.onGround && (Mathf.Abs(previousFrameVelocity.x) > WalkingMaxSpeed || Mathf.Abs(other.previousFrameVelocity.x) > WalkingMaxSpeed)) {
                    //bump

                    otherView.RPC(nameof(Knockback), RpcTarget.All, otherObj.transform.position.x < body.position.x, 1, true, photonView.ViewID);
                    photonView.RPC(nameof(Knockback), RpcTarget.All, otherObj.transform.position.x > body.position.x, 1, true, otherView.ViewID);
                    if ((state == Enums.PowerupState.PortalGun || state == Enums.PowerupState.Glockos) && (int)Random.Range(0, 100) <= 50)
                    {
                        photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.Glados_Bump);
                    }
                }
            }
            break;
        }
        case "MarioBrosPlatform": {
            List<Vector2> points = new();
            foreach (ContactPoint2D c in collision.contacts) {
                if (c.normal != Vector2.down)
                    continue;

                points.Add(c.point);
            }
            if (points.Count == 0)
                return;

            Vector2 avg = new();
            foreach (Vector2 point in points)
                avg += point;
            avg /= points.Count;

            obj.GetPhotonView().RPC(nameof(MarioBrosPlatform.Bump), RpcTarget.All, photonView.ViewID, avg);
            break;
        }
        case "frozencube": {
            if (holding == obj || (holdingOld == obj && throwInvincibility > 0))
                return;

            obj.GetComponent<FrozenCube>().InteractWithPlayer(this);
            break;
        }
        }
    }

    public void OnTriggerEnter2D(Collider2D collider) {
        if (!photonView.IsMine || dead || Frozen || pipeEntering || !MainHitbox.IsTouching(collider))
            return;

        HoldableEntity holdable = collider.GetComponentInParent<HoldableEntity>();
        if (holdable && (holding == holdable || (holdingOld == holdable && throwInvincibility > 0)))
            return;

        KillableEntity killable = collider.GetComponentInParent<KillableEntity>();
        if (killable && !killable.dead && !killable.Frozen) {
            killable.InteractWithPlayer(this);
            return;
        }

        GameObject obj = collider.gameObject;
        switch (obj.tag) {
        case "Fireball": {
            FireballMover fireball = obj.GetComponentInParent<FireballMover>();
            if ((fireball.photonView.IsMine && !fireball.hitsOwner) || hitInvincibilityCounter > 0)
                return;

            fireball.photonView.RPC(nameof(KillableEntity.Kill), RpcTarget.All);

            if (knockback || invincible > 0 || state == Enums.PowerupState.MegaMushroom)
                return;

            if (CanShell() && (inShell || crouching || groundpound)) {
                if (fireball.isIceball) {
                    //slowdown
                    slowdownTimer = 0.65f;
                }
                return;
            }

            if (state == Enums.PowerupState.MiniMushroom) {
                photonView.RPC(nameof(Powerdown), RpcTarget.All, false);
                return;
            }

            if (!fireball.isIceball) {
                photonView.RPC(nameof(Knockback), RpcTarget.All, fireball.left, 1, true, fireball.photonView.ViewID);
            } else {
                if (!Frozen && !frozenObject && !pipeEntering) {
                    GameObject cube = PhotonNetwork.Instantiate("Prefabs/FrozenCube", transform.position, Quaternion.identity, 0, new object[] { photonView.ViewID });
                    frozenObject = cube.GetComponent<FrozenCube>();
                    return;
                }
            }
            break;
        }
        case "lava":
        {
            photonView.RPC(nameof(LavaBounce), RpcTarget.All);
            return;
        }
        case "poison": {
            if (!photonView.IsMine)
                return;
            photonView.RPC(nameof(Death), RpcTarget.All, false, obj.CompareTag("lava"));
            return;
        }
        }

        OnTriggerStay2D(collider);
    }

    protected void OnTriggerStay2D(Collider2D collider) {
        GameObject obj = collider.gameObject;
        if (obj.CompareTag("spinner")) {
            onSpinner = obj;
            return;
        }

        if (!photonView.IsMine || dead || Frozen)
            return;

        double time = PhotonNetwork.Time;
        if (time - lastCollectTime.GetValueOrDefault(obj) < 0.5d)
            return;

        switch (obj.tag) {
        case "Powerup": {
            if (!photonView.IsMine)
                return;
            MovingPowerup powerup = obj.GetComponentInParent<MovingPowerup>();
            if (powerup.followMeCounter > 0 || powerup.ignoreCounter > 0)
                break;

            photonView.RPC(nameof(AttemptCollectPowerup), RpcTarget.AllViaServer, powerup.photonView.ViewID);
            Destroy(collider);
            break;
        }
        case "bigstar": {
            Transform parent = obj.transform.parent;
            photonView.RPC(nameof(AttemptCollectBigStar), RpcTarget.AllViaServer, parent.gameObject.GetPhotonView().ViewID);
            break;
        }
        case "loosecoin": {
            Transform parent = obj.transform.parent;
            photonView.RPC(nameof(AttemptCollectCoin), RpcTarget.AllViaServer, parent.gameObject.GetPhotonView().ViewID, (Vector2) parent.position);
            break;
        }
        case "coin": {
            photonView.RPC(nameof(AttemptCollectCoin), RpcTarget.AllViaServer, obj.GetPhotonView().ViewID, new Vector2(obj.transform.position.x, collider.transform.position.y));
            break;
        }
        }
    }

    protected void OnTriggerExit2D(Collider2D collider) {
        if (collider.CompareTag("spinner"))
            onSpinner = null;
    }
    #endregion

    #region -- CONTROLLER FUNCTIONS --
    public void OnMovement(InputAction.CallbackContext context) {
        if (!photonView.IsMine)
            return;

        joystick = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (!photonView.IsMine)
            return;

        // Check the input device type
        if (context.control.device is UnityEngine.InputSystem.Mouse)
        {
            // Mouse input: handle relative aiming
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 screenCenter = transform.position + (Vector3.up * .5f);
            aim = (mousePosition - screenCenter).normalized;
        }
        else if (context.control.device is UnityEngine.InputSystem.Gamepad)
        {
            // Gamepad input: handle stick aiming
            Vector2 look = context.ReadValue<Vector2>();
            if (look.magnitude > 0.75f)
            {
                aim = look;
            }
        }
    }

    public bool justJumped = false;
    public void OnJump(InputAction.CallbackContext context) {
        if (!photonView.IsMine)
            return;
        justJumped = context.ReadValue<float>() >= 0.5f;
        if (CantMove())
        {
            return;
        }

        jumpHeld = context.ReadValue<float>() >= 0.5f;
        if (jumpHeld)
            jumpBuffer = 0.15f;
    }

    public void OnSprint(InputAction.CallbackContext context) {
        if (!photonView.IsMine)
            return;

        running = context.started;

        if (Frozen)
            return;

        if (running && (state == Enums.PowerupState.FireFlower || state == Enums.PowerupState.IceFlower) && GlobalController.Instance.settings.fireballFromSprint)
            ActivatePowerupAction();
    }

    public void OnPowerupAction(InputAction.CallbackContext context) {
        if (!photonView.IsMine || dead || GameManager.Instance.paused)
            return;

        powerupButtonHeld = context.ReadValue<float>() >= 0.5f;
        if (!powerupButtonHeld)
            return;

        ActivatePowerupAction();
    }
    public void OnAltPowerupAction(InputAction.CallbackContext context) {
        if (!photonView.IsMine || dead || GameManager.Instance.paused)
            return;

        altPowerupButtonHeld = context.ReadValue<float>() >= 0.5f;
        if (!altPowerupButtonHeld)
            return;

        ActivateAltPowerupAction();
    }

    private void ActivatePowerupAction() {
        if (knockback || pipeEntering || GameManager.Instance.gameover || dead || Frozen || holding || !GameManager.Instance.allowRuning || (FindObjectOfType<PezoliBoss>() && state == Enums.PowerupState.IceFlower))
            return;

        switch (state) {
        case Enums.PowerupState.IceFlower:
        case Enums.PowerupState.FireFlower: {
            if (wallSlideLeft || wallSlideRight || groundpound || triplejump || flying || drill || crouching || sliding)
                return;

            int count = 0;
            foreach (FireballMover existingFire in FindObjectsOfType<FireballMover>()) {
                if (existingFire.photonView.IsMine && !existingFire.hitsOwner && ++count >= 6)
                    return;
            }

            if (count <= 1) {
                fireballTimer = 1.25f;
                canShootProjectile = count == 0;
            } else if (fireballTimer <= 0) {
                fireballTimer = 1.25f;
                canShootProjectile = true;
            } else if (canShootProjectile) {
                canShootProjectile = false;
            } else {
                return;
            }

            bool ice = state == Enums.PowerupState.IceFlower;
            string projectile = ice ? "Iceball" : "Fireball";
            Enums.Sounds sound = ice ? Enums.Sounds.Powerup_Iceball_Shoot : Enums.Sounds.Powerup_Fireball_Shoot;

            Vector2 pos = body.position + new Vector2(facingRight ^ animator.GetCurrentAnimatorStateInfo(0).IsName("turnaround") ? 0.5f : -0.5f, 0.3f);
            if (Utils.IsTileSolidAtWorldLocation(pos)) {
                photonView.RPC(nameof(SpawnParticle), RpcTarget.All, $"Prefabs/Particle/{projectile}Wall", pos);
            } else {
                PhotonNetwork.Instantiate($"Prefabs/{projectile}", pos, Quaternion.identity, 0, new object[] { !facingRight ^ animator.GetCurrentAnimatorStateInfo(0).IsName("turnaround"), body.velocity.x });
            }
            photonView.RPC(nameof(PlaySound), RpcTarget.All, sound);

            animator.SetTrigger("fireball");
            wallJumpTimer = 0;
            break;
        }
        case Enums.PowerupState.PropellerMushroom: {
            if (wallSlideLeft || wallSlideRight || groundpound || triplejump || flying || drill || crouching || sliding)
                return;

            photonView.RPC(nameof(StartPropeller), RpcTarget.All);
            break;
        }
        case Enums.PowerupState.Glockos:
        case Enums.PowerupState.Glock:
        {
            if (wallSlideLeft || wallSlideRight || groundpound || triplejump || flying || drill || crouching || sliding)
               return;

            if(ammo > 0)
            {
                OnGlockShot();
                photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Powerup_Glock_Shoot);
                photonView.RPC(nameof(SetAnimatorTriggerRPC), RpcTarget.All, "Fire");
                        if (underWater)
                        {
                            stillDrownTimer = .1f;
                        }
                    }
            else
            {
                photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Powerup_Glock_Fail);
            }
            break;
        }
        case Enums.PowerupState.PortalGun:
        {
            if (wallSlideLeft || wallSlideRight || groundpound || triplejump || flying || drill || crouching || sliding)
               return;

            OnPortalShot(false);
            photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Powerup_Portal_ShootA);
            photonView.RPC(nameof(SetAnimatorTriggerRPC), RpcTarget.All, "Fire");
                    if (underWater)
                    {
                        stillDrownTimer = .1f;
                    }
                    break;
        }
        case Enums.PowerupState.Sword:
        {
            if (wallSlideLeft || wallSlideRight || groundpound || triplejump || flying || drill || crouching || sliding)
               return;

                    if (!onGround && !(animator.GetCurrentAnimatorStateInfo(0).IsName("SteveNair") || animator.GetCurrentAnimatorStateInfo(0).IsName("SteveBair") || animator.GetCurrentAnimatorStateInfo(0).IsName("SteveFair")))
                    {
                        if(joystick.x * (facingRight ? 1 : -1) < -analogDeadzone)
                        {
                            photonView.RPC(nameof(SetAnimatorTriggerRPC), RpcTarget.All, "SteveBair");
                        }
                        else if(joystick.x * (facingRight ? 1 : -1) > analogDeadzone)
                        {
                            photonView.RPC(nameof(SetAnimatorTriggerRPC), RpcTarget.All, "SteveFair");
                        }
                        else
                        {
                            photonView.RPC(nameof(SetAnimatorTriggerRPC), RpcTarget.All, "SteveNair");
                        }
                    }
                    else if(onGround && !animator.GetCurrentAnimatorStateInfo(0).IsName("SlashC"))
                    {
                        if (!(animator.GetCurrentAnimatorStateInfo(0).IsName("SlashA") || animator.GetCurrentAnimatorStateInfo(0).IsName("SlashB") || animator.GetCurrentAnimatorStateInfo(0).IsName("SlashC")))
                        {
                            photonView.RPC(nameof(SetAnimatorTriggerRPC), RpcTarget.All, "SlashA");
                        }
                        else if (animator.GetCurrentAnimatorStateInfo(0).IsName("SlashA"))
                        {
                            photonView.RPC(nameof(SetAnimatorTriggerRPC), RpcTarget.All, "SlashB");
                        }
                        else if (animator.GetCurrentAnimatorStateInfo(0).IsName("SlashB"))
                        {
                            photonView.RPC(nameof(SetAnimatorTriggerRPC), RpcTarget.All, "SlashC");
                        }
                    }
            break;
        }
        case Enums.PowerupState.Bat:
        {
            if (wallSlideLeft || wallSlideRight || groundpound || triplejump || flying || drill || crouching || sliding)
               return;
                    if (animator.GetCurrentAnimatorStateInfo(0).IsName("BatSwing"))
                        return;

                    if (onGround)
                    {
                        photonView.RPC(nameof(SetAnimatorTriggerRPC), RpcTarget.All, "BatSwing");
                    }
            break;
        }
        case Enums.PowerupState.Strawberry:
        {
            if (wallSlideLeft || wallSlideRight || groundpound || triplejump || flying || drill || crouching || sliding)
               return;
                    if (!onGround)
                    {
                        Dash();
                    }
            break;
        }
        }
    }
    private void ActivateAltPowerupAction() {
        if (knockback || pipeEntering || GameManager.Instance.gameover || dead || Frozen || holding)
            return;

        switch (state) {
        case Enums.PowerupState.Glockos:
        case Enums.PowerupState.Glock:
        {
            if (wallSlideLeft || wallSlideRight || groundpound || triplejump || flying || drill || crouching || sliding)
               return;

            if(ammo < 12 && coins > 1)
            {
                int ammoNeeded = 12 - ammo;
                int coinsToUse = Mathf.Min(coins / 2, ammoNeeded);

                ammo += coinsToUse;
                coins -= coinsToUse * 2;
                photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Powerup_Glock_Reload);
                photonView.RPC(nameof(SetAnimatorTriggerRPC), RpcTarget.All, "Reload");
            }
            break;
        }
        case Enums.PowerupState.PortalGun:
        {
            if (wallSlideLeft || wallSlideRight || groundpound || triplejump || flying || drill || crouching || sliding)
               return;

            OnPortalShot(true);
            photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Powerup_Portal_ShootB);
            photonView.RPC(nameof(SetAnimatorTriggerRPC), RpcTarget.All, "Fire");
                    if (underWater)
                    {
                        stillDrownTimer = .1f;
                    }
                    break;
        }
        case Enums.PowerupState.Bat:
        {
            if (wallSlideLeft || wallSlideRight || groundpound || triplejump || flying || drill || crouching || sliding || sandmanTimer > 0)
               return;

            if (animator.GetCurrentAnimatorStateInfo(0).IsName("BatSwing") || animator.GetCurrentAnimatorStateInfo(0).IsName("BatSwingAlt"))
               return;

            photonView.RPC(nameof(SetAnimatorTriggerRPC), RpcTarget.All, "BatSwingAlt");
            photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Powerup_Bat_Ball, (byte)Random.Range(1, 2));
            sandmanTimer = 5;
            GameObject sandman = PhotonNetwork.Instantiate("Prefabs/SandmanBall", transform.position + (Vector3.up * .5f) + (Vector3.right * (facingRight ? .5f : -.5f)), Quaternion.identity);
            sandman.GetPhotonView().RPC(nameof(SandmanBall.Launch), RpcTarget.All, new Vector2(facingRight ? 8f : -8f, 8), photonView.ViewID);
            break;
        }
        }
    }

    public void OnGlockShot()
    {
        ammo--;
        MainHitbox.enabled = false;
        LayerMask mask = LayerMask.GetMask("Entity", "Ground", "Default");
        Vector3 centerPoint = transform.position + (Vector3.up * .5f);
        RaycastHit2D hitCenter = Physics2D.Raycast(centerPoint, aim, GameManager.Instance.levelWidthTile / 5, mask);
        if (hitCenter.collider != null && hitCenter.collider.gameObject != gameObject) //returning early here for some reason
        {
            Debug.Log("HitCenter");
            SpawnTracer(centerPoint, hitCenter.point);
            OnGlockHit(hitCenter);
            return;
        }
        if (GameManager.Instance.loopingLevel)
        {
            Vector3 rightPoint = transform.position + (Vector3.up * .5f) + (Vector3.right * (float)GameManager.Instance.levelWidthTile / 2.01f);
            Vector3 leftPoint = transform.position + (Vector3.up * .5f) + (Vector3.left * (float)GameManager.Instance.levelWidthTile / 2.01f);
            RaycastHit2D hitRight = Physics2D.Raycast(rightPoint, aim, GameManager.Instance.levelWidthTile / 5, mask); //glock shot looped from the left
            RaycastHit2D hitLeft = Physics2D.Raycast(leftPoint, aim, GameManager.Instance.levelWidthTile / 5, mask); //glock shot looped from the right

            if (hitRight.collider != null && hitRight.collider.gameObject != gameObject)
            {
                Debug.Log("HitRight");
                SpawnTracer(rightPoint, hitRight.point);
                OnGlockHit(hitRight);
                return;
            }

            if (hitLeft.collider != null && hitLeft.collider.gameObject != gameObject)
            {
                Debug.Log("HitLeft");
                SpawnTracer(leftPoint, hitLeft.point);
                OnGlockHit(hitLeft);
                return;
            }
        }
        SpawnTracer(centerPoint, centerPoint + ((Vector3)aim * 8));
        MainHitbox.enabled = true;
    }    
    public void OnPortalShot(bool altPortal)
    {
        MainHitbox.enabled = false;
        LayerMask mask = LayerMask.GetMask("Ground", "Default");
        Vector3 centerPoint = transform.position + (Vector3.up * .5f);
        RaycastHit2D hitCenter = Physics2D.Raycast(centerPoint, aim, GameManager.Instance.levelWidthTile / 5, mask);
        if (hitCenter.collider != null && hitCenter.collider.gameObject != gameObject)
        {
            Debug.Log("HitCenter");
            SpawnPortalTracer(centerPoint, hitCenter.point, altPortal);
            OnPortalHit(hitCenter, altPortal);
            return;
        }
        SpawnPortalTracer(centerPoint, centerPoint + ((Vector3)aim * 8), altPortal);
        MainHitbox.enabled = true;
    }
    public void OnPortalHit(RaycastHit2D hit, bool altPortal)
    {
        MainHitbox.enabled = true;
        GameObject portalTemplate = (GameObject)Resources.Load("Prefabs/Portal");
        GameObject portalOBJ = Instantiate(portalTemplate, new Vector2((Mathf.Round(hit.point.x * 2) / 2), (Mathf.Round(hit.point.y * 2) / 2)) + (hit.normal * .25f), Quaternion.identity);
        Color color = Color.white;
        if (AnimationController.primaryColor.a == 0)
        {
            color = altPortal ? Color.blue : Color.red;
        }
        else
        {
            color = altPortal ? AnimationController.secondaryColor : AnimationController.primaryColor;
        }
        portalOBJ.transform.rotation = Quaternion.Euler(new Vector3(0, 0, (Mathf.Atan2(hit.normal.y, hit.normal.x) * Mathf.Rad2Deg) - 90));
        if (altPortal)
        {
            if (portalB)
            {
                Destroy(portalB.gameObject);
            }
            portalB = portalOBJ.GetComponent<Portal>();
            if (portalB)
            {
                portalA.other = portalB;
                portalB.other = portalA;
            }
            portalB.sprite.color = color;
        }
        else
        {
            if (portalA)
            {
                Destroy(portalA.gameObject);
            }
            portalA = portalOBJ.GetComponent<Portal>();
            if (portalB)
            {
                portalB.other = portalA;
                portalA.other = portalB;
            }
            portalA.sprite.color = color;
        }
    }
    public void SpawnTracer(Vector3 origin, Vector3 hitPoint)
    {
        BulletTracer tracer = ((GameObject)Instantiate(Resources.Load("Prefabs/Particle/BulletTracer"), Vector3.zero, Quaternion.identity)).GetComponent<BulletTracer>();
        tracer.line.SetPosition(0, origin);
        tracer.line.SetPosition(1, hitPoint);
    }
    public void SpawnPortalTracer(Vector3 origin, Vector3 hitPoint, bool altPortal)
    {
        BulletTracer tracer = ((GameObject)Instantiate(Resources.Load("Prefabs/Particle/PortalTracers"), Vector3.zero, Quaternion.identity)).GetComponent<BulletTracer>();
        tracer.line.SetPosition(0, origin);
        tracer.line.SetPosition(1, hitPoint);
        if(AnimationController.primaryColor.a == 0)
        {
            tracer.line.startColor = altPortal ? Color.blue : Color.red;
            tracer.line.endColor = altPortal ? Color.blue : Color.red;
        }
        else
        {
            tracer.line.startColor = altPortal ? AnimationController.secondaryColor : AnimationController.primaryColor;
            tracer.line.endColor = altPortal ? AnimationController.secondaryColor : AnimationController.primaryColor;
        }
    }
    public void OnGlockHit(RaycastHit2D hit)
    {
        MainHitbox.enabled = true;
        PlayerController hitCon = hit.collider.gameObject.GetComponent<PlayerController>();
        KillableEntity hitEnt = hit.collider.GetComponent<KillableEntity>();
        photonView.RPC(nameof(SpawnParticle), RpcTarget.All, "Prefabs/Particle/BulletParticle", hit.point, new Vector3(0, 0, (Mathf.Atan2(hit.normal.y, hit.normal.x) * Mathf.Rad2Deg) - 90));
        if (hitEnt)
        {
            hitEnt.photonView.RPC("SpecialKill", RpcTarget.All, facingRight, false, 0);
        }
        if (hitCon && hitCon != this)
        {
            hitCon.photonView.RPC("Knockback", RpcTarget.All, facingRight, 1, true, photonView.ViewID);
        }
    }

    [PunRPC]
    protected void StartPropeller() {
        if (usedPropellerThisJump)
            return;

        body.velocity = new Vector2(body.velocity.x, propellerLaunchVelocity * (underWater ? .15f : 1)); //propellers aren't great at their job in water
        propellerTimer = 1f;
        PlaySound(Enums.Sounds.Powerup_PropellerMushroom_Start);

        animator.SetTrigger("propeller_start");
        propeller = true;
        flying = false;
        crouching = false;

        singlejump = false;
        doublejump = false;
        triplejump = false;

        wallSlideLeft = false;
        wallSlideRight = false;

        if (onGround) {
            onGround = false;
            doGroundSnap = false;
            body.position += Vector2.up * 0.15f;
        }
        usedPropellerThisJump = true;
    }

    public void OnReserveItem(InputAction.CallbackContext context) {
        if (!photonView.IsMine || GameManager.Instance.paused || GameManager.Instance.gameover)
            return;

        if (storedPowerup == null || dead || !spawned) {
            PlaySound(Enums.Sounds.UI_Error);
            return;
        }

        photonView.RPC(nameof(SpawnReserveItem), RpcTarget.All);
        storedPowerup = null;

        UpdateGameState();
    }
    #endregion

    #region -- POWERUP / POWERDOWN --
    [PunRPC]
    public void AttemptCollectPowerup(int powerupID, PhotonMessageInfo info) {
        //only the owner can request a powerup, and only the master client can decide for us
        if (info.Sender != photonView.Owner || !PhotonNetwork.IsMasterClient)
            return;

        if (dead || !spawned)
            return;

        //powerup doesn't eixst?
        PhotonView view = PhotonView.Find(powerupID);
        if (!view || view.gameObject.GetComponent<MovingPowerup>() is not MovingPowerup powerup)
            return;

        if (Utils.WrappedDistance(body.position, view.transform.position) > 5f && powerup.powerupScriptable.state != Enums.PowerupState.PropellerMushroom)
            return;

        if (powerup.Collected || powerup.followMeCounter > 0)
            return;

        powerup.Collected = true;

        //we can collect
        photonView.RPC(nameof(Powerup), RpcTarget.All, powerupID);
    }

    [PunRPC]
    protected void Powerup(int actor, PhotonMessageInfo info) {
        //only trust the master client
        if (!info.Sender.IsMasterClient)
            return;
        
        PhotonView view;
        if (dead || !(view = PhotonView.Find(actor)))
            return;
        aim = new Vector2(facingRight ? 1 : -1, 0);
        MovingPowerup powerupObj = view.GetComponent<MovingPowerup>();

        Powerup powerup = powerupObj.powerupScriptable;
        Enums.PowerupState newState = powerup.state;
        Enums.PriorityPair pp = Enums.PowerupStatePriority[powerup.state];
        Enums.PriorityPair cp = Enums.PowerupStatePriority[state];
        bool reserve = cp.statePriority > pp.itemPriority || state == newState;
        bool soundPlayed = false;
        dashed = false;
        if (powerup.state == Enums.PowerupState.MegaMushroom && state != Enums.PowerupState.MegaMushroom) {

            giantStartTimer = giantStartTime;
            knockback = false;
            groundpound = false;
            crouching = false;
            propeller = false;
            usedPropellerThisJump = false;
            flying = false;
            drill = false;
            inShell = false;
            giantTimer = 15f;
            transform.localScale = Vector3.one;
            Instantiate(Resources.Load("Prefabs/Particle/GiantPowerup"), transform.position, Quaternion.identity);

            PlaySoundEverywhere(powerup.soundEffect);
            soundPlayed = true;

        } else if (powerup.prefab == "Star") {
            //starman
            if (invincible <= 0)
                StarCombo = 0;

            invincible = 10f;
            PlaySound(powerup.soundEffect);

            if (holding && photonView.IsMine) {
                holding.photonView.RPC(nameof(KillableEntity.SpecialKill), RpcTarget.All, facingRight, false, 0);
                holding = null;
            }

            if (view.IsMine)
                PhotonNetwork.Destroy(view);
            Destroy(view.gameObject);

            return;
        } else if (powerup.prefab == "PoisonMushroom") {

            Instantiate(Resources.Load("Prefabs/Particle/PoisonPuff"), view.gameObject.transform.position + (Vector3.up * .25f), Quaternion.identity);
            photonView.RPC(nameof(Powerdown), RpcTarget.All, false);
            Destroy(view.gameObject);

            return;
        } else if (powerup.prefab == "1-Up") {
            lives++;
            UpdateGameState();
            PlaySound(powerup.soundEffect);
            Instantiate(Resources.Load("Prefabs/Particle/1Up"), transform.position, Quaternion.identity);

            if (view.IsMine)
                PhotonNetwork.Destroy(view);
            Destroy(view.gameObject);

            return;
        } else if (state == Enums.PowerupState.MiniMushroom) {
            //check if we're in a mini area to avoid crushing ourselves
            if (onGround && Physics2D.Raycast(body.position, Vector2.up, 0.3f, Layers.MaskOnlyGround)) {
                reserve = true;
            }
        }

        if (reserve) {
            if (storedPowerup == null || (storedPowerup != null && Enums.PowerupStatePriority[storedPowerup.state].statePriority <= pp.statePriority && !(state == Enums.PowerupState.Mushroom && newState != Enums.PowerupState.Mushroom))) {
                //dont reserve mushrooms
                storedPowerup = powerup;
            }
            PlaySound(Enums.Sounds.Player_Sound_PowerupReserveStore);
        } else {
            if (!(state == Enums.PowerupState.Mushroom && newState != Enums.PowerupState.Mushroom) && (storedPowerup == null || Enums.PowerupStatePriority[storedPowerup.state].statePriority <= cp.statePriority)) {
                storedPowerup = (Powerup) Resources.Load("Scriptables/Powerups/" + state);
            }

            previousState = state;
            state = newState;
            powerupFlash = 2;
            crouching |= ForceCrouchCheck();
            propeller = false;
            usedPropellerThisJump = false;
            drill &= flying;
            propellerTimer = 0;

            if (!soundPlayed)
                PlaySound(powerup.soundEffect);
        }

        UpdateGameState();

        if (view.IsMine)
            PhotonNetwork.Destroy(view);
        Destroy(view.gameObject);

        //hitboxManager.Update();
    }

    [PunRPC]
    public void Powerdown(bool ignoreInvincible) {
        if (!ignoreInvincible && (hitInvincibilityCounter > 0 || invincible > 0))
            return;

        previousState = state;
        bool nowDead = false;

        switch (state) {
        case Enums.PowerupState.MiniMushroom:
        case Enums.PowerupState.Small: {
            if (photonView.IsMine)
                photonView.RPC(nameof(Death), RpcTarget.All, false, false);
            nowDead = true;
            break;
        }
        case Enums.PowerupState.Strawberry:
        case Enums.PowerupState.Mushroom: {
            state = Enums.PowerupState.Small;
            powerupFlash = 2f;
            SpawnStars(1, false);
            break;
        }
        default: {
            state = Enums.PowerupState.Mushroom;
            powerupFlash = 2f;
            SpawnStars(1, false);
            break;
        }
        }
        propeller = false;
        propellerTimer = 0;
        propellerSpinTimer = 0;
        usedPropellerThisJump = false;

        if (!nowDead) {
            hitInvincibilityCounter = 3f;
            PlaySound(Enums.Sounds.Player_Sound_Powerdown);
        }
    }
    [PunRPC]
    public void LavaBounce()
    {
        bool wasHitInvuln = hitInvincibilityCounter > 0;
        Powerdown(false);
        if (!dead)
        {
            if (!wasHitInvuln)
            {
                PlaySound(Enums.Sounds.Player_Sound_LavaBurnt);
            }
            body.velocity = new Vector2(body.velocity.x, 10);
            buttBurnt = 2;
            groundpound = false;
            crouching = false;
        }
        else
        {
            buttBurnt = 0;
        }
    }
    #endregion

    #region -- FREEZING --
    [PunRPC]
    public void Freeze(int cube) {
        if (knockback || hitInvincibilityCounter > 0 || invincible > 0 || Frozen || state == Enums.PowerupState.MegaMushroom)
            return;

        PlaySound(Enums.Sounds.Enemy_Generic_Freeze);
        frozenObject = PhotonView.Find(cube).GetComponentInChildren<FrozenCube>();
        Frozen = true;
        frozenObject.autoBreakTimer = 1.75f;
        animator.enabled = false;
        body.isKinematic = true;
        body.simulated = false;
        knockback = false;
        skidding = false;
        drill = false;
        wallSlideLeft = false;
        wallSlideRight = false;
        propeller = false;

        propellerTimer = 0;
        skidding = false;
    }

    [PunRPC]
    public void Unfreeze(byte reasonByte) {
        if (!Frozen)
            return;

        Frozen = false;
        animator.enabled = true;
        body.simulated = true;
        body.isKinematic = false;

        int knockbackStars = reasonByte switch {
            (byte) IFreezableEntity.UnfreezeReason.Timer => 0,
            (byte) IFreezableEntity.UnfreezeReason.Groundpounded => 2,
            _ => 1
        };

        if (frozenObject && frozenObject.photonView.IsMine) {
            frozenObject.holder?.photonView.RPC(nameof(Knockback), RpcTarget.All, frozenObject.holder.facingRight, 1, true, photonView.ViewID);
            frozenObject.Kill();
        }

        if (knockbackStars > 0)
            Knockback(facingRight, knockbackStars, true, -1);
        else
            hitInvincibilityCounter = 1.5f;
    }
    #endregion

    #region -- COIN / STAR COLLECTION --
    [PunRPC]
    protected void AttemptCollectBigStar(int starID, PhotonMessageInfo info) {
        //only the owner can request a big star, and only the master client can decide for us
        if (info.Sender != photonView.Owner || !PhotonNetwork.IsMasterClient)
            return;

        if (dead || !spawned)
            return;

        //star doesn't eixst?
        PhotonView star = PhotonView.Find(starID);
        if (!star)
            return;

        if (Utils.WrappedDistance(body.position, star.transform.position) > 5f)
            return;

        StarBouncer starScript = star.gameObject.GetComponent<StarBouncer>();
        if (!starScript.Collectable || starScript.Collected)
            return;

        starScript.Collected = true;

        //we can collect
        photonView.RPC(nameof(CollectBigStar), RpcTarget.All, (Vector2) starScript.transform.position, starID, stars + 1);
        if (starScript.stationary)
            GameManager.Instance.SendAndExecuteEvent(Enums.NetEventIds.ResetTiles, null, SendOptions.SendReliable);
    }

    [PunRPC]
    public void CollectBigStar(Vector2 particle, int starView, int newCount, PhotonMessageInfo info) {
        //only trust the master client
        if (!info.Sender.IsMasterClient)
            return;

        //state
        stars = Mathf.Min(newCount, GameManager.Instance.starRequirement);
        UpdateGameState();

        //game mechanics
        GameManager.Instance.CheckForWinner();

        //fx
        PlaySoundEverywhere(photonView.IsMine ? Enums.Sounds.World_Star_Collect_Self : Enums.Sounds.World_Star_Collect_Enemy);
        Instantiate(Resources.Load("Prefabs/Particle/StarCollect"), particle, Quaternion.identity);

        //destroy
        PhotonView star = PhotonView.Find(starView);
        if (star && star.IsMine) {
            PhotonNetwork.Destroy(star);
        } else {
            Destroy(star.gameObject);
        }
    }

    [PunRPC]
    public void AttemptCollectCoin(int coinID, Vector2 particle, PhotonMessageInfo info) {
        //only the owner can request a coin, and only the master client can decide for us
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (coinID != -1) {
            PhotonView coin = PhotonView.Find(coinID);
            if (!coin || !coin.IsMine || !coin.GetComponent<SpriteRenderer>().enabled)
                return;

            if (coin.GetComponent<LooseCoin>() is LooseCoin lc) {
                if (lc.Collected)
                    return;

                lc.Collected = true;
            }
        }

        photonView.RPC(nameof(CollectCoin), RpcTarget.All, coinID, coins + 1, particle);
    }

    [PunRPC]
    protected void CollectCoin(int coinID, int newCount, Vector2 position, PhotonMessageInfo info) {
        //only trust the master client
        if (!info.Sender.IsLocal && !info.Sender.IsMasterClient)
            return;

        PhotonView coin = PhotonView.Find(coinID);
        if (coin) {
            coin.GetComponent<SpriteRenderer>().enabled = false;
            coin.GetComponent<BoxCollider2D>().enabled = false;
            if (coin.CompareTag("loosecoin") && coin.IsMine) {
                //loose coin, just destroy
                PhotonNetwork.Destroy(coin);
            }
        }

        Instantiate(Resources.Load("Prefabs/Particle/CoinCollect"), position, Quaternion.identity);

        PlaySound(Enums.Sounds.World_Coin_Collect);
        NumberParticle num = ((GameObject) Instantiate(Resources.Load("Prefabs/Particle/Number"), position, Quaternion.identity)).GetComponentInChildren<NumberParticle>();
        num.text.text = Utils.GetSymbolString((coins + 1).ToString(), Utils.numberSymbols);
        num.ApplyColor(AnimationController.GlowColor);

        coins = newCount;
        oxygen += .125f;
        oxygen = Mathf.Clamp(oxygen, 0, 1);
        if (coins >= GameManager.Instance.coinRequirement && SingletonModeTag.mode != SingletonModeTag.Mode.Singleplayer) {
            SpawnCoinItem();
            coins = 0;
        }

        UpdateGameState();
    }

    [PunRPC]
    public void SpawnReserveItem() {
        if (storedPowerup == null)
            return;

        if (!PhotonNetwork.IsMasterClient)
            return;

        string prefab = storedPowerup.prefab;
        PhotonNetwork.InstantiateRoomObject("Prefabs/Powerup/" + prefab, body.position + Vector2.up * 5f, Quaternion.identity, 0, new object[] { photonView.ViewID });
        photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Player_Sound_PowerupReserveUse);
        storedPowerup = null;
        UpdateGameState();
    }

    public void SpawnCoinItem() {
        if (coins < GameManager.Instance.coinRequirement)
            return;

        if (!PhotonNetwork.IsMasterClient)
            return;

        string prefab = Utils.GetRandomItem(this).prefab;
        PhotonNetwork.InstantiateRoomObject("Prefabs/Powerup/" + prefab, body.position + Vector2.up * 5f, Quaternion.identity, 0, new object[] { photonView.ViewID });
        photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Player_Sound_PowerupReserveUse);

        coins = 0;
    }

    private void SpawnStars(int amount, bool deathplane) {
        if (!PhotonNetwork.IsMasterClient) {
            stars = Mathf.Max(0, stars - amount);
            return;
        }

        bool fastStars = amount > 2 && stars > 2;
        int starDirection = facingRight ? 1 : 2;

        while (amount > 0) {
            if (stars <= 0)
                break;

            if (!fastStars) {
                if (starDirection == 0)
                    starDirection = 2;
                if (starDirection == 3)
                    starDirection = 1;
            }
            PhotonNetwork.InstantiateRoomObject("Prefabs/BigStar", body.position + Vector2.up * transform.localScale * MainHitbox.size, Quaternion.identity, 0, new object[] { starDirection, photonView.ViewID, PhotonNetwork.ServerTimestamp + 1000, deathplane });

            starDirection = (starDirection + 1) % 4;
            stars--;
            amount--;
        }
        GameManager.Instance.CheckForWinner();
        UpdateGameState();
    }
    #endregion

    #region -- DEATH / RESPAWNING --
    [PunRPC]
    public void Death(bool deathplane, bool fire) {
        if (dead)
            return;

        //if (info.Sender != photonView.Owner)
        //    return;
        if(SingletonModeTag.mode == SingletonModeTag.Mode.Singleplayer)
        {
            GameManager.Instance.music.volume = 0f;
        }
        animator.Play("deadstart");
        if(state == Enums.PowerupState.PortalGun || state == Enums.PowerupState.Glockos)
        {
            photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.Glados_Taunt_Die, (byte)Random.Range(0, 5));
        }
        lives--;
        if (deathplane)
            spawned = false;
        dead = true;
        onSpinner = null;
        pipeEntering = null;
        inShell = false;
        propeller = false;
        propellerSpinTimer = 0;
        flying = false;
        drill = false;
        sliding = false;
        crouching = false;
        skidding = false;
        turnaround = false;
        groundpound = false;
        knockback = false;
        wallSlideLeft = false;
        wallSlideRight = false;
        animator.SetBool("knockback", false);
        animator.SetBool("flying", false);
        animator.SetBool("firedeath", fire);

        PlaySound(cameraController.IsControllingCamera ? Enums.Sounds.Player_Sound_Death : Enums.Sounds.Player_Sound_DeathOthers);

        SpawnStars(1, deathplane);
        body.isKinematic = false;
        if (holding && !inShell) {
            holding.photonView.RPC(nameof(HoldableEntity.Throw), RpcTarget.All, !facingRight, true, body.position);
            holding = null;
        }
        holdingOld = null;

        if (photonView.IsMine)
            ScoreboardUpdater.instance.OnDeathToggle();
    }

    [PunRPC]
    public void PreRespawn()
    {
        oxygen = 1;
        if(SingletonModeTag.mode == SingletonModeTag.Mode.Singleplayer && dead)
        {
            SaveManager.Instance.playerState = Enums.PowerupState.Small;
            SceneManager.LoadScene("WorldMap");
        }
        if (lives == 0)
        {
            GameManager.Instance.CheckForWinner();
            return;
        }
        Debug.Log("PreRespawn()");
        sfx.enabled = true;
        if (lives == 0)
        {
            GameManager.Instance.CheckForWinner();
            Destroy(trackIcon);
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(photonView);
                GameManager.Instance.SpectationManager.Spectating = true;
            }
            Destroy(gameObject);
            return;
        }
        transform.localScale = Vector2.one;
        transform.position = body.position = GameManager.Instance.GetSpawnpoint(playerId);
        dead = false;
        previousState = state = Enums.PowerupState.Small;
        AnimationController.DisableAllModels();
        spawned = false;
        animator.SetTrigger("respawn");
        invincible = 0;
        giantTimer = 0;
        giantEndTimer = 0;
        giantStartTimer = 0;
        groundpound = false;
        body.isKinematic = false;

        GameObject particle = (GameObject)Instantiate(Resources.Load(SingletonModeTag.mode == SingletonModeTag.Mode.Singleplayer ? "Prefabs/Particle/RespawnInst" : "Prefabs/Particle/Respawn"), body.position, Quaternion.identity);
        particle.GetComponent<RespawnParticle>().player = this;

        gameObject.SetActive(false);
        cameraController.Recenter();
    }


    [PunRPC]
    public void Respawn()
    {
        Debug.Log("Respawn()");

        gameObject.SetActive(true);
        dead = false;
        spawned = true;
        state = Enums.PowerupState.Small;
        previousState = Enums.PowerupState.Small;
        body.velocity = Vector2.zero;
        wallSlideLeft = false;
        wallSlideRight = false;
        wallSlideTimer = 0;
        wallJumpTimer = 0;
        flying = false;

        propeller = false;
        propellerSpinTimer = 0;
        usedPropellerThisJump = false;
        propellerTimer = 0;

        crouching = false;
        onGround = false;
        sliding = false;
        koyoteTime = 1f;
        jumpBuffer = 0;
        invincible = 0;
        giantStartTimer = 0;
        giantTimer = 0;
        singlejump = false;
        doublejump = false;
        turnaround = false;
        triplejump = false;
        knockback = false;
        bounce = false;
        skidding = false;
        groundpound = false;
        inShell = false;
        landing = 0f;
        if (SaveManager.Instance && !alreadySpawned)
        {
            state = SaveManager.Instance.playerState;
            storedPowerup = SaveManager.Instance.playerReserve;
        }
        ResetKnockback();
        Instantiate(Resources.Load("Prefabs/Particle/Puff"), transform.position, Quaternion.identity);
        models.transform.rotation = Quaternion.Euler(0, 180, 0);

        if (photonView.IsMine)
            ScoreboardUpdater.instance.OnRespawnToggle();

        alreadySpawned = true;
        UpdateGameState();
    }
    #endregion

    #region -- SOUNDS / PARTICLES --
    [PunRPC]
    public void PlaySoundEverywhere(Enums.Sounds sound) {
        GameManager.Instance.sfx.PlayOneShot(sound.GetClip(character));
    }
    [PunRPC]
    public void PlaySound(Enums.Sounds sound, byte variant, float volume) {
        if((sound.IsVoc() && character.soundFolder != "deven") && underWater)
        {
            return;
        }
        if (sound == Enums.Sounds.Powerup_MegaMushroom_Break_Block) {
            sfxBrick.Stop();
            sfxBrick.clip = sound.GetClip(character, variant);
            sfxBrick.Play();
        } else {
            sfx.PlayOneShot(sound.GetClip(character, variant), volume);
        }
    }
    [PunRPC]
    public void PlaySound(Enums.Sounds sound, byte variant) {
        PlaySound(sound, variant, 1);
    }
    [PunRPC]
    public void PlaySound(Enums.Sounds sound, PhotonMessageInfo info) {
        //Debug.Log(info.Sender?.NickName + " " + info.Sender?.UserId + " - " + sound);

        PlaySound(sound, 0, 1);
    }
    public void PlaySound(Enums.Sounds sound) {
        PlaySound(sound, new PhotonMessageInfo());
    }

    [PunRPC]
    protected void SpawnParticle(string particle, Vector2 worldPos) {
        Instantiate(Resources.Load(particle), worldPos, Quaternion.identity);
    }

    [PunRPC]
    protected void SpawnParticle(string particle, Vector2 worldPos, Vector3 rot) {
        Instantiate(Resources.Load(particle), worldPos, Quaternion.Euler(rot));
    }

    protected void GiantFootstep() {
        CameraController.ScreenShake = 0.15f;
        SpawnParticle("Prefabs/Particle/GroundpoundDust", body.position + new Vector2(facingRight ? 0.5f : -0.5f, 0));
        PlaySound(Enums.Sounds.Powerup_MegaMushroom_Walk, (byte) (step ? 1 : 2));
        step = !step;
    }

    protected void Footstep() {
        if (state == Enums.PowerupState.MegaMushroom)
            return;

        bool right = joystick.x > analogDeadzone;
        bool left = joystick.x < -analogDeadzone;
        bool reverse = body.velocity.x != 0 && ((left ? 1 : -1) == Mathf.Sign(body.velocity.x));
        if (onIce && (left ^ right) && reverse) {
            PlaySound(Enums.Sounds.World_Ice_Skidding);
            return;
        }
        if (propeller) {
            PlaySound(Enums.Sounds.Powerup_PropellerMushroom_Kick);
            return;
        }
        if (Mathf.Abs(body.velocity.x) < WalkingMaxSpeed)
            return;

        PlaySound(footstepSound, (byte) (step ? 1 : 2), Mathf.Abs(body.velocity.x) / (RunningMaxSpeed + 4));
        step = !step;
    }
    #endregion

    #region -- TILE COLLISIONS --
    void HandleGiantTiles(bool pipes) {
        if (state != Enums.PowerupState.MegaMushroom || !photonView.IsMine || giantStartTimer > 0)
            return;

        Vector2 checkSize = WorldHitboxSize * 1.1f;

        bool grounded = previousFrameVelocity.y < -8f && onGround;
        Vector2 offset = Vector2.zero;
        if (grounded)
            offset = Vector2.down / 2f;

        Vector2 checkPosition = body.position + (Vector2.up * checkSize * 0.5f) + (2 * Time.fixedDeltaTime * body.velocity) + offset;

        Vector3Int minPos = Utils.WorldToTilemapPosition(checkPosition - (checkSize * 0.5f), wrap: false);
        Vector3Int size = Utils.WorldToTilemapPosition(checkPosition + (checkSize * 0.5f), wrap: false) - minPos;

        for (int x = 0; x <= size.x; x++) {
            for (int y = 0; y <= size.y; y++) {
                Vector3Int tileLocation = new(minPos.x + x, minPos.y + y, 0);
                Vector2 worldPosCenter = Utils.TilemapToWorldPosition(tileLocation) + Vector3.one * 0.25f;
                Utils.WrapTileLocation(ref tileLocation);

                InteractableTile.InteractionDirection dir = InteractableTile.InteractionDirection.Up;
                if (worldPosCenter.y - 0.25f + Physics2D.defaultContactOffset * 2f <= body.position.y) {
                    if (!grounded && !groundpound)
                        continue;

                    dir = InteractableTile.InteractionDirection.Down;
                } else if (worldPosCenter.y + Physics2D.defaultContactOffset * 2f >= body.position.y + size.y) {
                    dir = InteractableTile.InteractionDirection.Up;
                } else if (worldPosCenter.x <= body.position.x) {
                    dir = InteractableTile.InteractionDirection.Left;
                } else if (worldPosCenter.x >= body.position.x) {
                    dir = InteractableTile.InteractionDirection.Right;
                }

                BreakablePipeTile pipe = GameManager.Instance.tilemap.GetTile<BreakablePipeTile>(tileLocation);
                if (pipe && (pipe.upsideDownPipe || !pipes || groundpound))
                    continue;

                InteractWithTile(tileLocation, dir);
            }
        }
        if (pipes) {
            for (int x = 0; x <= size.x; x++) {
                for (int y = size.y; y >= 0; y--) {
                    Vector3Int tileLocation = new(minPos.x + x, minPos.y + y, 0);
                    Vector2 worldPosCenter = Utils.TilemapToWorldPosition(tileLocation) + Vector3.one * 0.25f;
                    Utils.WrapTileLocation(ref tileLocation);

                    InteractableTile.InteractionDirection dir = InteractableTile.InteractionDirection.Up;
                    if (worldPosCenter.y - 0.25f + Physics2D.defaultContactOffset * 2f <= body.position.y) {
                        if (!grounded && !groundpound)
                            continue;

                        dir = InteractableTile.InteractionDirection.Down;
                    } else if (worldPosCenter.x - 0.25f < checkPosition.x - checkSize.x * 0.5f) {
                        dir = InteractableTile.InteractionDirection.Left;
                    } else if (worldPosCenter.x + 0.25f > checkPosition.x + checkSize.x * 0.5f) {
                        dir = InteractableTile.InteractionDirection.Right;
                    }

                    BreakablePipeTile pipe = GameManager.Instance.tilemap.GetTile<BreakablePipeTile>(tileLocation);
                    if (!pipe || !pipe.upsideDownPipe || dir == InteractableTile.InteractionDirection.Up)
                        continue;

                    InteractWithTile(tileLocation, dir);
                }
            }
        }
    }

    int InteractWithTile(Vector3Int tilePos, InteractableTile.InteractionDirection direction) {
        if (!photonView.IsMine)
            return 0;

        TileBase tile = GameManager.Instance.tilemap.GetTile(tilePos);
        if (!tile)
            return 0;
        if (tile is InteractableTile it)
            return it.Interact(this, direction, Utils.TilemapToWorldPosition(tilePos)) ? 1 : 0;

        return 0;
    }
    #endregion

    #region -- KNOCKBACK --

    [PunRPC]
    public void Knockback(bool fromRight, int starsToDrop, bool fireball, int attackerView) {
        if (fireball && fireballKnockback && knockback)
            return;
        if (knockback && !fireballKnockback)
            return;

        if (!GameManager.Instance.started || hitInvincibilityCounter > 0 || pipeEntering || Frozen || dead || giantStartTimer > 0 || giantEndTimer > 0)
            return;

        if (state == Enums.PowerupState.MiniMushroom && starsToDrop > 1) {
            SpawnStars(2, false);
            Powerdown(false);
            return;
        }

        if (knockback || fireballKnockback)
            starsToDrop = Mathf.Min(1, starsToDrop);

        knockback = true;
        knockbackTimer = 0.5f;
        fireballKnockback = fireball;
        initialKnockbackFacingRight = facingRight;

        PhotonView attacker = PhotonNetwork.GetPhotonView(attackerView);
        if (attackerView >= 0) {
            if (attacker)
                SpawnParticle("Prefabs/Particle/PlayerBounce", attacker.transform.position);

            if (fireballKnockback)
                PlaySound(Enums.Sounds.Player_Sound_Collision_Fireball, 0, 3);
            else
                PlaySound(Enums.Sounds.Player_Sound_Collision, 0, 3);
        }
        animator.SetBool("fireballKnockback", fireball);
        animator.SetBool("knockforwards", facingRight != fromRight);

        float megaVelo = (state == Enums.PowerupState.MegaMushroom ? 3 : 1);
        body.velocity = new Vector2(
            (fromRight ? -1 : 1) *
            ((starsToDrop + 1) / 2f) *
            4f *
            megaVelo *
            (fireball ? 0.5f : 1f),

            fireball ? 0 : 4.5f
        );

        if (onGround && !fireball)
            body.position += Vector2.up * 0.15f;

        onGround = false;
        doGroundSnap = false;
        inShell = false;
        groundpound = false;
        flying = false;
        propeller = false;
        propellerTimer = 0;
        propellerSpinTimer = 0;
        sliding = false;
        drill = false;
        body.gravityScale = normalGravity;
        wallSlideLeft = wallSlideRight = false;

        SpawnStars(starsToDrop, false);
        HandleLayerState();
    }

    [PunRPC]
    public void KnockbackWithForce(bool fromRight, int starsToDrop, bool fireball, int attackerView) {
        if (fireball && fireballKnockback && knockback)
            return;
        if (knockback && !fireballKnockback)
            return;

        if (!GameManager.Instance.started || hitInvincibilityCounter > 0 || pipeEntering || Frozen || dead || giantStartTimer > 0 || giantEndTimer > 0)
            return;

        if (state == Enums.PowerupState.MiniMushroom && starsToDrop > 1) {
            SpawnStars(2, false);
            Powerdown(false);
            return;
        }

        if (knockback || fireballKnockback)
            starsToDrop = Mathf.Min(1, starsToDrop);

        knockback = true;
        knockbackTimer = 0.5f;
        fireballKnockback = fireball;
        initialKnockbackFacingRight = facingRight;

        PhotonView attacker = PhotonNetwork.GetPhotonView(attackerView);
        if (attackerView >= 0) {
            if (attacker)
                SpawnParticle("Prefabs/Particle/PlayerBounce", attacker.transform.position);

            if (fireballKnockback)
                PlaySound(Enums.Sounds.Player_Sound_Collision_Fireball, 0, 3);
            else
                PlaySound(Enums.Sounds.Player_Sound_Collision, 0, 3);
        }
        animator.SetBool("fireballKnockback", fireball);
        animator.SetBool("knockforwards", facingRight != fromRight);

        float megaVelo = (state == Enums.PowerupState.MegaMushroom ? 3 : 1);
        body.velocity = new Vector2(15 * (fromRight ? 1 : -1), 15);

        if (onGround && !fireball)
            body.position += Vector2.up * 0.15f;

        onGround = false;
        doGroundSnap = false;
        inShell = false;
        groundpound = false;
        flying = false;
        propeller = false;
        propellerTimer = 0;
        propellerSpinTimer = 0;
        sliding = false;
        drill = false;
        body.gravityScale = normalGravity;
        wallSlideLeft = wallSlideRight = false;

        SpawnStars(starsToDrop, false);
        HandleLayerState();
    }

    public void ResetKnockbackFromAnim() {
        if (photonView.IsMine)
            photonView.RPC(nameof(ResetKnockback), RpcTarget.All);
    }

    [PunRPC]
    protected void ResetKnockback() {
        if(SingletonModeTag.mode != SingletonModeTag.Mode.Singleplayer)
            hitInvincibilityCounter = state != Enums.PowerupState.MegaMushroom ? 2f : 0f;
        bounce = false;
        knockback = false;
        body.velocity = new(0, body.velocity.y);
        facingRight = initialKnockbackFacingRight;
    }
    #endregion

    #region -- ENTITY HOLDING --
    [PunRPC]
    protected void HoldingWakeup(PhotonMessageInfo info) {
        holding = null;
        holdingOld = null;
        throwInvincibility = 0;
        Powerdown(false);
    }
    [PunRPC]
    public void SetHolding(int view) {
        if (view == -1) {
            if (holding)
                holding.holder = null;
            holding = null;
            return;
        }
        holding = PhotonView.Find(view).GetComponent<HoldableEntity>();
        if (holding is FrozenCube) {
            animator.Play("head-pickup");
            animator.ResetTrigger("fireball");
            PlaySound(Enums.Sounds.Player_Voice_DoubleJump, 2);
            pickupTimer = 0;
        } else {
            pickupTimer = pickupTime;
        }
        animator.ResetTrigger("throw");
        animator.SetBool("holding", true);

        SetHoldingOffset();
    }
    [PunRPC]
    public void SetHoldingOld(int view) {
        if (view == -1) {
            holding = null;
            return;
        }
        PhotonView v = PhotonView.Find(view);
        if (v == null)
            return;
        holdingOld = v.GetComponent<HoldableEntity>();
        throwInvincibility = 0.15f;
    }
    #endregion

    private void HandleSliding(bool up, bool down, bool left, bool right) {
        startedSliding = false;
        if (groundpound) {
            if (onGround) {
                if (state == Enums.PowerupState.MegaMushroom) {
                    groundpound = false;
                    groundpoundCounter = 0.5f;
                    return;
                }
                if (!inShell && Mathf.Abs(floorAngle) >= slopeSlidingAngle) {
                    groundpound = false;
                    sliding = true;
                    alreadyGroundpounded = true;
                    body.velocity = new Vector2(-Mathf.Sign(floorAngle) * SPEED_SLIDE_MAX, 0);
                    startedSliding = true;
                } else {
                    body.velocity = Vector2.zero;
                    if (!down || state == Enums.PowerupState.MegaMushroom) {
                        groundpound = false;
                        groundpoundCounter = state == Enums.PowerupState.MegaMushroom ? 0.4f : 0.25f;
                    }
                }
            }
            if (up && groundpoundCounter <= 0.05f) {
                groundpound = false;
                body.velocity = Vector2.down * groundpoundVelocity;
            }
        }
        if (!((facingRight && hitRight) || (!facingRight && hitLeft)) && crouching && Mathf.Abs(floorAngle) >= slopeSlidingAngle && !inShell && state != Enums.PowerupState.MegaMushroom) {
            sliding = true;
            crouching = false;
            alreadyGroundpounded = true;
        }
        if (sliding && onGround && Mathf.Abs(floorAngle) > slopeSlidingAngle) {
            float angleDeg = floorAngle * Mathf.Deg2Rad;

            bool uphill = Mathf.Sign(floorAngle) == Mathf.Sign(body.velocity.x);
            float speed = Time.fixedDeltaTime * 5f * (uphill ? Mathf.Clamp01(1f - (Mathf.Abs(body.velocity.x) / RunningMaxSpeed)) : 4f);

            float newX = Mathf.Clamp(body.velocity.x - (Mathf.Sin(angleDeg) * speed), -(RunningMaxSpeed * 1.3f), RunningMaxSpeed * 1.3f);
            float newY = Mathf.Sin(angleDeg) * newX + 0.4f;
            body.velocity = new Vector2(newX, newY);

        }

        if (sliding && (up || ((left ^ right) && !down) || (Mathf.Abs(floorAngle) < slopeSlidingAngle && onGround && body.velocity.x == 0 && !down) || (facingRight && hitRight) || (!facingRight && hitLeft))) {
            sliding = false;
            if (body.velocity.x == 0 && onGround)
                PlaySound(Enums.Sounds.Player_Sound_SlideEnd);

            //alreadyGroundpounded = false;
        }
    }

    private void HandleSlopes() {
        if (!onGround) {
            floorAngle = 0;
            return;
        }

        RaycastHit2D hit = Physics2D.BoxCast(body.position + (Vector2.up * 0.05f), new Vector2((MainHitbox.size.x - Physics2D.defaultContactOffset * 2f) * transform.lossyScale.x, 0.1f), 0, body.velocity.normalized, (body.velocity * Time.fixedDeltaTime).magnitude, Layers.MaskAnyGround);
        if (hit) {
            //hit ground
            float angle = Vector2.SignedAngle(Vector2.up, hit.normal);
            if (Mathf.Abs(angle) > 89)
                return;

            float x = floorAngle != angle ? previousFrameVelocity.x : body.velocity.x;

            floorAngle = angle;

            float change = Mathf.Sin(angle * Mathf.Deg2Rad) * x * 1.25f;
            body.velocity = new Vector2(x, change);
            onGround = true;
            doGroundSnap = true;
        } else if (onGround) {
            hit = Physics2D.BoxCast(body.position + (Vector2.up * 0.05f), new Vector2((MainHitbox.size.x + Physics2D.defaultContactOffset * 3f) * transform.lossyScale.x, 0.1f), 0, Vector2.down, 0.3f, Layers.MaskAnyGround);
            if (hit) {
                float angle = Vector2.SignedAngle(Vector2.up, hit.normal);
                if (Mathf.Abs(angle) > 89)
                    return;

                float x = floorAngle != angle ? previousFrameVelocity.x : body.velocity.x;
                floorAngle = angle;

                float change = Mathf.Sin(angle * Mathf.Deg2Rad) * x * 1.25f;
                body.velocity = new Vector2(x, change);
                onGround = true;
                doGroundSnap = true;
            } else {
                floorAngle = 0;
            }
        }
    }

    void HandleLayerState() {
        bool hitsNothing = animator.GetBool("pipe") || dead || stuckInBlock || giantStartTimer > 0 || (giantEndTimer > 0 && stationaryGiantEnd);
        bool shouldntCollide = (hitInvincibilityCounter > 0 && invincible <= 0) || (knockback && !fireballKnockback);

        int layer = Layers.LayerDefault;
        if (hitsNothing) {
            layer = Layers.LayerHitsNothing;
        } else if (shouldntCollide) {
            layer = Layers.LayerPassthrough;
        }

        gameObject.layer = layer;
    }

    bool GroundSnapCheck() {
        if (dead || (body.velocity.y > 0 && !onGround) || !doGroundSnap || pipeEntering || gameObject.layer == Layers.LayerHitsNothing)
            return false;

        bool prev = Physics2D.queriesStartInColliders;
        Physics2D.queriesStartInColliders = false;
        RaycastHit2D hit = Physics2D.BoxCast(body.position + Vector2.up * 0.1f, new Vector2(WorldHitboxSize.x, 0.05f), 0, Vector2.down, 0.4f, Layers.MaskAnyGround);
        Physics2D.queriesStartInColliders = prev;
        if (hit) {
            body.position = new(body.position.x, hit.point.y + Physics2D.defaultContactOffset);
            return true;
        }
        return false;
    }

    #region -- PIPES --

    void DownwardsPipeCheck() {
        if (!photonView.IsMine || joystick.y > -analogDeadzone || state == Enums.PowerupState.MegaMushroom || !onGround || knockback || inShell)
            return;

        foreach (RaycastHit2D hit in Physics2D.RaycastAll(body.position, Vector2.down, 0.1f)) {
            GameObject obj = hit.transform.gameObject;
            if (!obj.CompareTag("pipe"))
                continue;
            PipeManager pipe = obj.GetComponent<PipeManager>();
            if (pipe.miniOnly && state != Enums.PowerupState.MiniMushroom)
                continue;
            if (!pipe.entryAllowed)
                continue;

            //Enter pipe
            pipeEntering = pipe;
            pipeDirection = Vector2.down;

            body.velocity = Vector2.down;
            transform.position = body.position = new Vector2(obj.transform.position.x, transform.position.y);

            photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Player_Sound_Powerdown);
            crouching = false;
            sliding = false;
            propeller = false;
            drill = false;
            usedPropellerThisJump = false;
            groundpound = false;
            inShell = false;
            break;
        }
    }

    void UpwardsPipeCheck() {
        if (!photonView.IsMine || groundpound || !hitRoof || joystick.y < analogDeadzone || state == Enums.PowerupState.MegaMushroom)
            return;

        //todo: change to nonalloc?
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(body.position, Vector2.up, 1f)) {
            GameObject obj = hit.transform.gameObject;
            if (!obj.CompareTag("pipe"))
                continue;
            PipeManager pipe = obj.GetComponent<PipeManager>();
            if (pipe.miniOnly && state != Enums.PowerupState.MiniMushroom)
                continue;
            if (!pipe.entryAllowed)
                continue;

            //pipe found
            pipeEntering = pipe;
            pipeDirection = Vector2.up;

            body.velocity = Vector2.up;
            transform.position = body.position = new Vector2(obj.transform.position.x, transform.position.y);

            photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Player_Sound_Powerdown);
            crouching = false;
            sliding = false;
            propeller = false;
            usedPropellerThisJump = false;
            flying = false;
            inShell = false;
            break;
        }
    }
    #endregion

    void HandleCrouching(bool crouchInput) {
        if (!photonView.IsMine || sliding || propeller || knockback)
            return;

        if (state == Enums.PowerupState.MegaMushroom) {
            crouching = false;
            return;
        }
        bool prevCrouchState = crouching || groundpound;
        crouching = ((onGround && crouchInput && !groundpound) || (!onGround && crouchInput && crouching) || (crouching && ForceCrouchCheck())) && !holding;
        if (crouching && !prevCrouchState) {
            //crouch start sound
            photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Player_Sound_Crouch);
            if(state == Enums.PowerupState.PortalGun || state == Enums.PowerupState.Glockos)
            tbag_tracker++;
            if(tbag_tracker > 3)
            {
                tbag_tracker = 10;
                if(!tbagging)
                    photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.Glados_Taunt_TBag);
            }
        }
    }

    bool ForceCrouchCheck() {
        //janky fortress ceilingn check, m8
        if (CanShell() && onGround && SceneManager.GetActiveScene().buildIndex != 4)
            return false;
        if (state <= Enums.PowerupState.MiniMushroom)
            return false;

        float width = MainHitbox.bounds.extents.x;

        bool triggerState = Physics2D.queriesHitTriggers;
        Physics2D.queriesHitTriggers = false;

        float uncrouchHeight = GetHitboxSize(false).y * transform.lossyScale.y;

        bool ret = Physics2D.BoxCast(body.position + Vector2.up * 0.1f, new(width - 0.05f, 0.05f), 0, Vector2.up, uncrouchHeight - 0.1f, Layers.MaskOnlyGround);

        Physics2D.queriesHitTriggers = triggerState;
        return ret;
    }

    void HandleWallslide(bool holdingLeft, bool holdingRight, bool jump) {

        Vector2 currentWallDirection;
        if (holdingLeft) {
            currentWallDirection = Vector2.left;
        } else if (holdingRight) {
            currentWallDirection = Vector2.right;
        } else if (wallSlideLeft) {
            currentWallDirection = Vector2.left;
        } else if (wallSlideRight) {
            currentWallDirection = Vector2.right;
        } else {
            return;
        }

        HandleWallSlideChecks(currentWallDirection, holdingRight, holdingLeft);

        wallSlideRight &= wallSlideTimer > 0 && hitRight;
        wallSlideLeft &= wallSlideTimer > 0 && hitLeft;

        if (wallSlideLeft || wallSlideRight) {
            //walljump check
            facingRight = wallSlideLeft;
            if (jump && wallJumpTimer <= 0) {
                //perform walljump

                hitRight = false;
                hitLeft = false;
                if (underWater)
                {
                    body.velocity = new Vector2(WALLJUMP_HSPEED * (wallSlideLeft ? 1 : -1), WALLJUMP_VSPEED / 1.5f);
                }
                else
                {
                    body.velocity = new Vector2(WALLJUMP_HSPEED * (wallSlideLeft ? 1 : -1), WALLJUMP_VSPEED);
                }
                singlejump = false;
                doublejump = false;
                triplejump = false;
                onGround = false;
                bounce = false;
                photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Player_Sound_WallJump);
                photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Player_Voice_WallJump, (byte) Random.Range(1, 3));

                Vector2 offset = new(MainHitbox.size.x / 2f * (wallSlideLeft ? -1 : 1), MainHitbox.size.y / 2f);
                photonView.RPC(nameof(SpawnParticle), RpcTarget.All, "Prefabs/Particle/WalljumpParticle", body.position + offset, wallSlideLeft ? Vector3.zero : Vector3.up * 180);

                wallJumpTimer = 16 / 60f;
                animator.SetTrigger("walljump");
                wallSlideTimer = 0;
            }
        } else {
            //walljump starting check
            bool canWallslide = !inShell && body.velocity.y < -0.1 && !groundpound && !onGround && !holding && state != Enums.PowerupState.MegaMushroom && !flying && !drill && !crouching && !sliding && !knockback;
            if (!canWallslide)
                return;

            //Check 1
            if (wallJumpTimer > 0)
                return;

            //Check 2
            if (wallSlideTimer - Time.fixedDeltaTime <= 0)
                return;

            //Check 4: already handled
            //Check 5.2: already handled

            //Check 6
            if (crouching)
                return;

            //Check 8
            if (!((currentWallDirection == Vector2.right && joystick.x > 0) || (currentWallDirection == Vector2.left && joystick.x < 0)))
                return;

            //Start wallslide
            wallSlideRight = currentWallDirection == Vector2.right;
            wallSlideLeft = currentWallDirection == Vector2.left;
            propeller = false;
        }

        wallSlideRight &= wallSlideTimer > 0 && hitRight;
        wallSlideLeft &= wallSlideTimer > 0 && hitLeft;
    }

    void HandleWallSlideChecks(Vector2 wallDirection, bool right, bool left) {
        bool floorCheck = !Physics2D.Raycast(body.position, Vector2.down, 0.3f, Layers.MaskAnyGround);
        if (!floorCheck) {
            wallSlideTimer = 0;
            return;
        }

        bool moveDownCheck = body.velocity.y < 0;
        if (!moveDownCheck)
            return;

        bool wallCollisionCheck = wallDirection == Vector2.left ? hitLeft : hitRight;
        if (!wallCollisionCheck)
            return;

        bool heightLowerCheck = Physics2D.Raycast(body.position + new Vector2(0, .2f), wallDirection, MainHitbox.size.x * 2, Layers.MaskOnlyGround);
        if (!heightLowerCheck)
            return;

        if ((wallDirection == Vector2.left && !left) || (wallDirection == Vector2.right && !right))
            return;

        wallSlideTimer = 16 / 60f;
    }

    void HandleJumping(bool jump) {
        if (knockback || drill || (state == Enums.PowerupState.MegaMushroom && singlejump))
            return;

        bool topSpeed = Mathf.Abs(body.velocity.x) >= RunningMaxSpeed;
        if (bounce || (jump && (onGround || (koyoteTime < 0.07f && !propeller)) && !startedSliding)) {

            bool canSpecialJump = (jump || (bounce && jumpHeld)) && properJump && !flying && !propeller && topSpeed && landing < 0.45f && !holding && !triplejump && !crouching && !inShell && ((body.velocity.x < 0 && !facingRight) || (body.velocity.x > 0 && facingRight)) && !Physics2D.Raycast(body.position + new Vector2(0, 0.1f), Vector2.up, 1f, Layers.MaskOnlyGround);
            float jumpBoost = 0;

            koyoteTime = 1;
            jumpBuffer = 0;
            skidding = false;
            turnaround = false;
            sliding = false;
            wallSlideLeft = false;
            wallSlideRight = false;
            //alreadyGroundpounded = false;
            groundpound = false;
            groundpoundCounter = 0;
            drill = false;
            flying &= bounce;
            propeller &= bounce;

            if (!bounce && onSpinner && !holding) {
                photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Player_Voice_SpinnerLaunch);
                photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.World_Spinner_Launch);
                body.velocity = new Vector2(body.velocity.x, launchVelocity * (underWater ? .75f : 1));
                flying = true;
                onGround = false;
                body.position += Vector2.up * 0.075f;
                doGroundSnap = false;
                previousOnGround = false;
                crouching = false;
                inShell = false;
                return;
            }

            float vel = state switch {
                Enums.PowerupState.MegaMushroom => megaJumpVelocity,
                _ => jumpVelocity + Mathf.Abs(body.velocity.x) / RunningMaxSpeed * 1.05f,
            };
            if (underWater)
            {
                vel /= 1.5f;
            }


            if (canSpecialJump && singlejump) {
                //Double jump
                singlejump = false;
                doublejump = true;
                triplejump = false;
                photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Player_Voice_DoubleJump, (byte) Random.Range(1, 3));
            } else if (canSpecialJump && doublejump) {
                //Triple Jump
                singlejump = false;
                doublejump = false;
                triplejump = true;
                jumpBoost = 0.5f;
                photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Player_Voice_TripleJump);
                if ((state == Enums.PowerupState.PortalGun || state == Enums.PowerupState.Glockos) && (int)Random.Range(0, 100) <= 10)
                {
                    photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.Glados_Taunt_Rolljump);
                }
            } else {

                if ((state == Enums.PowerupState.PortalGun || state == Enums.PowerupState.Glockos) && (int)Random.Range(0, 100) <= 2)
                {
                    photonView.RPC("PlaySound", RpcTarget.All, Enums.Sounds.Glados_Taunt_Jump);
                }
                //Normal jump
                singlejump = true;
                doublejump = false;
                triplejump = false;
            }
            body.velocity = new Vector2(body.velocity.x, vel + jumpBoost);
            onGround = false;
            doGroundSnap = false;
            body.position += Vector2.up * 0.075f;
            groundpoundCounter = 0;
            properJump = true;
            jumping = true;

            if (!bounce) {
                //play jump sound
                Enums.Sounds sound = state switch {
                    Enums.PowerupState.MiniMushroom => Enums.Sounds.Powerup_MiniMushroom_Jump,
                    Enums.PowerupState.MegaMushroom => Enums.Sounds.Powerup_MegaMushroom_Jump,
                    _ => Enums.Sounds.Player_Sound_Jump,
                };
                photonView.RPC(nameof(PlaySound), RpcTarget.All, sound);
            }
            bounce = false;
        }
    }


    public void UpdateHitbox() {
        bool crouchHitbox = state != Enums.PowerupState.MiniMushroom && pipeEntering == null && ((crouching && !groundpound) || inShell || sliding);
        Vector2 hitbox = GetHitboxSize(crouchHitbox);

        MainHitbox.size = hitbox;
        MainHitbox.offset = Vector2.up * 0.5f * hitbox;
    }

    public Vector2 GetHitboxSize(bool crouching) {
        float height;

        if (state <= Enums.PowerupState.Small || (invincible > 0 && !onGround && !crouching && !sliding && !flying && !propeller) || groundpound) {
            height = heightSmallModel;
        } else {
            height = heightLargeModel;
        }

        if (crouching)
            height *= state <= Enums.PowerupState.Small ? 0.7f : 0.5f;

        return new(MainHitbox.size.x, height);
    }

    void HandleWalkingRunning(bool left, bool right) {

        if (wallJumpTimer > 0) {
            if (wallJumpTimer < (14 / 60f) && (hitLeft || hitRight)) {
                wallJumpTimer = 0;
            } else {
                body.velocity = new(WALLJUMP_HSPEED * (facingRight ? 1 : -1), body.velocity.y);
                return;
            }
        }

        if (groundpound || groundpoundCounter > 0 || knockback || pipeEntering || jumpLandingTimer > 0 || !(wallJumpTimer <= 0 || onGround || body.velocity.y < 0))
            return;

        if (!onGround)
            skidding = false;

        if (inShell) {
            body.velocity = new(SPEED_STAGE_MAX[RUN_STAGE] * (facingRight ? 1 : -1) * (1f - slowdownTimer), body.velocity.y);
            return;
        }

        bool run;
        if(state == Enums.PowerupState.Glock || state == Enums.PowerupState.PortalGun || state == Enums.PowerupState.Glockos)
        {
            run = (facingRight ? 1 : -1) * joystick.x > 0 && !flying;
        }
        else
        {
            run = functionallyRunning && (!flying || state == Enums.PowerupState.MegaMushroom);
        }
        if (!GameManager.Instance.allowRuning)
        {
            run = false;
        }
        int maxStage;
        if (invincible > 0 && run && onGround)
            maxStage = STAR_STAGE;
        else if (run)
            maxStage = RUN_STAGE;
        else
            maxStage = WALK_STAGE;

        int stage = MovementStage;
        if(stage > maxStage)
        {
            stage = maxStage;
        }
        float acc = state == Enums.PowerupState.MegaMushroom ? SPEED_STAGE_MEGA_ACC[stage] : SPEED_STAGE_ACC[stage];
        float sign = Mathf.Sign(body.velocity.x);
        bool cantMove = CantMove();
        if (!cantMove && (left ^ right) && (!crouching || (crouching && !onGround && !CanShell())) && !knockback && !sliding) {
            //we can walk here

            float speed = Mathf.Abs(body.velocity.x);
            bool reverse = body.velocity.x != 0 && ((left ? 1 : -1) == sign);

            //check that we're not going above our limit
            float max = SPEED_STAGE_MAX[maxStage];
            if (speed > max) {
                acc = -acc;
            }
            if (underWater)
            {
                acc /= 2f;
                max /= 1.25f;
            }

            if (reverse) {
                turnaround = false;
                if (onGround) {
                    if (speed >= SKIDDING_THRESHOLD && !holding && state != Enums.PowerupState.MegaMushroom) {
                        skidding = true;
                        facingRight = sign == 1;
                    }

                    if (skidding) {
                        if (onIce) {
                            acc = SKIDDING_ICE_DEC;
                        } else if (speed > SPEED_STAGE_MAX[RUN_STAGE] && invincible > 0) { //a small change to the physics from HCM that makes it smoother. 
                            acc = SKIDDING_STAR_DEC;
                        }  else {
                            acc = SKIDDING_DEC;
                        }
                        turnaroundFrames = 0;
                    } else {
                        if (onIce) {
                            acc = WALK_TURNAROUND_ICE_ACC;
                        } else {
                            turnaroundFrames = Mathf.Min(turnaroundFrames + 0.2f, WALK_TURNAROUND_ACC.Length - 1);
                            acc = state == Enums.PowerupState.MegaMushroom ? WALK_TURNAROUND_MEGA_ACC[(int) turnaroundFrames] : WALK_TURNAROUND_ACC[(int) turnaroundFrames];
                        }
                    }
                } else {
                    acc = SPEED_STAGE_ACC[0];
                }
            } else {

                if (skidding && !turnaround) {
                    skidding = false;
                }

                if (turnaround && turnaroundBoostFrames > 0 && speed != 0) {
                    turnaround = false;
                    skidding = false;
                }

                if (turnaround && speed < TURNAROUND_THRESHOLD) {
                    if (--turnaroundBoostFrames <= 0) {
                        acc = TURNAROUND_ACC;
                        skidding = false;
                    } else {
                        acc = 0;
                    }
                } else {
                    turnaround = false;
                }
            }

            int direction = left ? -1 : 1;
            float newX = body.velocity.x + acc * direction;

            if (Mathf.Abs(newX) - speed > 0) {
                //clamp only if accelerating
                newX = Mathf.Clamp(newX, -max, max);
            }

            if (skidding && !turnaround && Mathf.Sign(newX) != sign) {
                //turnaround
                turnaround = true;
                turnaroundBoostFrames = 5;
                newX = 0;
            }

            body.velocity = new(newX, body.velocity.y);

        } else if (onGround) {
            //not holding anything, sliding, or holding both directions. decelerate

            skidding = false;
            turnaround = false;

            if (body.velocity.x == 0)
                return;

            if (sliding) {
                float angle = Mathf.Abs(floorAngle);
                if (angle > slopeSlidingAngle) {
                    //uphill / downhill
                    acc = (angle > 30 ? SLIDING_45_ACC : SLIDING_22_ACC) * ((Mathf.Sign(floorAngle) == sign) ? -1 : 1);
                } else {
                    //flat ground
                    acc = -SPEED_STAGE_ACC[0];
                }
            } else if (onIce)
                acc = -BUTTON_RELEASE_ICE_DEC[stage];
            else if (knockback)
                acc = -KNOCKBACK_DEC;
            else
                acc = -BUTTON_RELEASE_DEC;

            int direction = (int) Mathf.Sign(body.velocity.x);
            float newX = body.velocity.x + acc * direction;

            if ((direction == -1) ^ (newX <= 0))
                newX = 0;

            if (sliding) {
                newX = Mathf.Clamp(newX, -SPEED_SLIDE_MAX, SPEED_SLIDE_MAX);
            }

            body.velocity = new(newX, body.velocity.y);

            //if (newX != 0)
                //facingRight = newX > 0;
        }

        inShell |= CanShell() && Mathf.Abs(joystick.x) > analogDeadzone && !sliding && onGround && functionallyRunning && Mathf.Abs(body.velocity.x) >= SPEED_STAGE_MAX[RUN_STAGE] * 0.9f;
        if (onGround || previousOnGround)
            body.velocity = new(body.velocity.x, 0);
    }

    bool HandleStuckInBlock() {
        if (!body || state == Enums.PowerupState.MegaMushroom || body.excludeLayers != 0)
            return false;

        Vector2 checkSize = WorldHitboxSize * new Vector2(1, 0.75f);
        Vector2 checkPos = transform.position + (Vector3) (Vector2.up * checkSize / 2f);

        if (!Utils.IsAnyTileSolidBetweenWorldBox(checkPos, checkSize * 0.9f, false)) {
            alreadyStuckInBlock = stuckInBlock = false;
            return false;
        }
        stuckInBlock = true;
        body.gravityScale = 0;
        body.velocity = Vector2.zero;
        groundpound = false;
        propeller = false;
        drill = false;
        flying = false;
        onGround = true;

        if (!alreadyStuckInBlock) {
            // Code for mario to instantly teleport to the closest free position when he gets stuck

            //prevent mario from clipping to the floor if we got pushed in via our hitbox changing (shell on ice, for example)
            transform.position = body.position = previousFramePosition;
            checkPos = transform.position + (Vector3) (Vector2.up * checkSize / 2f);

            float distanceInterval = 0.025f;
            float minimDistance = 0.95f; // if the minimum actual distance is anything above this value this code will have no effect
            float travelDistance = 0;
            float targetInd = -1; // Basically represents the index of the interval that'll be chosen for mario to be popped out
            int angleInterval = 45;

            for (float i = 0; i < 360 / angleInterval; i ++) { // Test for every angle in the given interval
                float ang = i * angleInterval;
                float testDistance = 0;

                float radAngle = Mathf.PI * ang / 180;
                Vector2 testPos;

                // Calculate the distance mario would have to be moved on a certain angle to stop collisioning
                do {
                    testPos = checkPos + new Vector2(Mathf.Cos(radAngle) * testDistance, Mathf.Sin(radAngle) * testDistance);
                    testDistance += distanceInterval;
                }
                while (Utils.IsAnyTileSolidBetweenWorldBox(testPos, checkSize * 0.975f));

                // This is to give right angles more priority over others when deciding
                float adjustedDistance = testDistance * (1 + Mathf.Abs(Mathf.Sin(radAngle * 2) / 2));

                // Set the new minimum only if the new position is inside of the visible level
                if (testPos.y > GameManager.Instance.cameraMinY && testPos.x > GameManager.Instance.cameraMinX && testPos.x < GameManager.Instance.cameraMaxX){
                    if (adjustedDistance < minimDistance) {
                        minimDistance = adjustedDistance;
                        travelDistance = testDistance;
                        targetInd = i;
                    }
                }
            }

            // Move him
            if (targetInd != -1) {
                float radAngle = Mathf.PI * (targetInd * angleInterval) / 180;
                Vector2 lastPos = checkPos;
                checkPos += new Vector2(Mathf.Cos(radAngle) * travelDistance, Mathf.Sin(radAngle) * travelDistance);
                transform.position = body.position = new(checkPos.x, body.position.y + (checkPos.y - lastPos.y));
                stuckInBlock = false;
                return false; // Freed
            }
        }

        alreadyStuckInBlock = true;
        body.velocity = Vector2.right * 2f;
        return true;
    }

    void TickCounters() {
        float delta = Time.fixedDeltaTime;
        if (!pipeEntering)
            Utils.TickTimer(ref invincible, 0, delta);

        Utils.TickTimer(ref throwInvincibility, 0, delta);
        Utils.TickTimer(ref jumpBuffer, 0, delta);
        if (giantStartTimer <= 0)
            Utils.TickTimer(ref giantTimer, 0, delta);
        Utils.TickTimer(ref giantStartTimer, 0, delta);
        Utils.TickTimer(ref groundpoundCounter, 0, delta);
        Utils.TickTimer(ref giantEndTimer, 0, delta);
        Utils.TickTimer(ref groundpoundDelay, 0, delta);
        Utils.TickTimer(ref hitInvincibilityCounter, 0, delta);
        Utils.TickTimer(ref propellerSpinTimer, 0, delta);
        Utils.TickTimer(ref propellerTimer, 0, delta);
        Utils.TickTimer(ref knockbackTimer, 0, delta);
        Utils.TickTimer(ref pipeTimer, 0, delta);
        Utils.TickTimer(ref wallSlideTimer, 0, delta);
        Utils.TickTimer(ref wallJumpTimer, 0, delta);
        Utils.TickTimer(ref jumpLandingTimer, 0, delta);
        Utils.TickTimer(ref pickupTimer, 0, -delta, pickupTime);
        Utils.TickTimer(ref fireballTimer, 0, delta);
        Utils.TickTimer(ref slowdownTimer, 0, delta * 0.5f);

        if (onGround)
            Utils.TickTimer(ref landing, 0, -delta);
    }

    [PunRPC]
    public void FinishMegaMario(bool success) {
        if (success) {
            PlaySoundEverywhere(Enums.Sounds.Player_Voice_MegaMushroom);
        } else {
            //hit a ceiling, cancel
            giantSavedVelocity = Vector2.zero;
            state = Enums.PowerupState.Mushroom;
            giantEndTimer = giantStartTime - giantStartTimer;
            animator.enabled = true;
            animator.Play("mega-cancel", 0, 1f - (giantEndTimer / giantStartTime));
            giantStartTimer = 0;
            stationaryGiantEnd = true;
            storedPowerup = (Powerup) Resources.Load("Scriptables/Powerups/MegaMushroom");
            giantTimer = 0;
            PlaySound(Enums.Sounds.Player_Sound_PowerupReserveStore);
        }
        body.isKinematic = false;
        UpdateGameState();
    }

    void HandleFacingDirection() {
        if ((groundpound && !onGround) || state == Enums.PowerupState.Glock || state == Enums.PowerupState.PortalGun || state == Enums.PowerupState.Glockos)
            return;
        if(state == Enums.PowerupState.Sword && !onGround)
        {
            return;
        }
        if(CantMove())
        {
            return;
        }

        //Facing direction
        bool right = joystick.x > analogDeadzone;
        bool left = joystick.x < -analogDeadzone;

        if (wallJumpTimer > 0) {
            facingRight = body.velocity.x > 0;
        } else if (!inShell && !sliding && !skidding && !knockback && !(animator.GetCurrentAnimatorStateInfo(0).IsName("turnaround") || turnaround)) {
            if (right ^ left)
                facingRight = right;
        } else if (giantStartTimer <= 0 && giantEndTimer <= 0 && !skidding && !(animator.GetCurrentAnimatorStateInfo(0).IsName("turnaround") || turnaround)) {
            if (knockback || (onGround && state != Enums.PowerupState.MegaMushroom && Mathf.Abs(body.velocity.x) > 0.05f)) {
                facingRight = body.velocity.x > 0;
            } else if (((wallJumpTimer <= 0 && !inShell) || giantStartTimer > 0) && (right || left)) {
                facingRight = right;
            }
            if (!inShell && ((Mathf.Abs(body.velocity.x) < 0.5f && crouching) || onIce) && (right || left))
                facingRight = right;
        }
    }

    [PunRPC]
    public void EndMega() {
        giantEndTimer = giantStartTime / 2f;
        state = Enums.PowerupState.Mushroom;
        stationaryGiantEnd = false;
        hitInvincibilityCounter = 3f;
        PlaySoundEverywhere(Enums.Sounds.Powerup_MegaMushroom_End);
        body.velocity = new(body.velocity.x, body.velocity.y > 0 ? (body.velocity.y / 3f) : body.velocity.y);
    }

    public void HandleBlockSnapping() {
        if (pipeEntering || drill)
            return;

        //if we're about to be in the top 2 pixels of a block, snap up to it, (if we can fit)

        if (body.velocity.y > 0)
            return;

        Vector2 nextPos = body.position + Time.fixedDeltaTime * 2f * body.velocity;

        if (!Utils.IsAnyTileSolidBetweenWorldBox(nextPos + WorldHitboxSize.y * 0.5f * Vector2.up, WorldHitboxSize))
            //we are not going to be inside a block next fixed update
            return;

        //we ARE inside a block. figure out the height of the contact
        // 32 pixels per unit
        bool orig = Physics2D.queriesStartInColliders;
        Physics2D.queriesStartInColliders = true;
        RaycastHit2D contact = Physics2D.BoxCast(nextPos + 3f / 32f * Vector2.up, new(WorldHitboxSize.y, 1f / 32f), 0, Vector2.down, 3f / 32f, Layers.MaskAnyGround);
        Physics2D.queriesStartInColliders = orig;

        if (!contact || contact.normal.y < 0.1f) {
            //we didn't hit the ground, we must've hit a ceiling or something.
            return;
        }

        float point = contact.point.y + Physics2D.defaultContactOffset;
        if (body.position.y > point + Physics2D.defaultContactOffset) {
            //dont snap when we're above the block
            return;
        }

        Vector2 newPosition = new(body.position.x, point);

        if (Utils.IsAnyTileSolidBetweenWorldBox(newPosition + WorldHitboxSize.y * 0.5f * Vector2.up, WorldHitboxSize)) {
            //it's an invalid position anyway, we'd be inside something.
            return;
        }

        //valid position, snap upwards
        body.position = newPosition;
    }

    private void HandleMovement(float delta) {
        functionallyRunning = running || state == Enums.PowerupState.MegaMushroom || propeller;

        if (dead || !spawned)
            return;

        if (photonView.IsMine && body.position.y + transform.lossyScale.y < GameManager.Instance.GetLevelMinY()) {
            //death via pit
            photonView.RPC(nameof(Death), RpcTarget.All, true, false);
            return;
        }

        if (Frozen) {
            if (!frozenObject) {
                Unfreeze((byte) IFreezableEntity.UnfreezeReason.Other);
            } else {
                body.velocity = Vector2.zero;
                return;
            }
        }

        if (photonView.IsMine && holding && (holding.dead || Frozen || holding.Frozen))
            photonView.RPC(nameof(SetHolding), RpcTarget.All, -1);

        FrozenCube holdingCube;
        if (((holdingCube = holding as FrozenCube) && holdingCube) || ((holdingCube = holdingOld as FrozenCube) && holdingCube)) {
            foreach (BoxCollider2D hitbox in hitboxes) {
                Physics2D.IgnoreCollision(hitbox, holdingCube.hitbox, throwInvincibility > 0);
            }
        }

        bool paused = GameManager.Instance.paused && photonView.IsMine;

        if (giantStartTimer > 0) {
            body.velocity = Vector2.zero;
            transform.position = body.position = previousFramePosition;
            if (giantStartTimer - delta <= 0 && photonView.IsMine) {
                photonView.RPC(nameof(FinishMegaMario), RpcTarget.All, true);
                giantStartTimer = 0;
            } else {
                body.isKinematic = true;
                if (animator.GetCurrentAnimatorClipInfo(0).Length <= 0 || animator.GetCurrentAnimatorClipInfo(0)[0].clip.name != "mega-scale")
                    animator.Play("mega-scale");


                Vector2 checkSize = WorldHitboxSize * new Vector2(0.75f, 1.1f);
                Vector2 normalizedVelocity = body.velocity;
                if (!groundpound)
                    normalizedVelocity.y = Mathf.Max(0, body.velocity.y);

                Vector2 offset = Vector2.zero;
                if (singlejump && onGround)
                    offset = Vector2.down / 2f;

                Vector2 checkPosition = body.position + Vector2.up * checkSize / 2f + offset;

                Vector3Int minPos = Utils.WorldToTilemapPosition(checkPosition - (checkSize / 2), wrap: false);
                Vector3Int size = Utils.WorldToTilemapPosition(checkPosition + (checkSize / 2), wrap: false) - minPos;

                for (int x = 0; x <= size.x; x++) {
                    Vector3Int tileLocation = new(minPos.x + x, minPos.y + size.y, 0);
                    Utils.WrapTileLocation(ref tileLocation);
                    TileBase tile = Utils.GetTileAtTileLocation(tileLocation);

                    bool cancelMega;
                    if (tile is BreakableBrickTile bbt)
                        cancelMega = !bbt.breakableByGiantMario;
                    else
                        cancelMega = Utils.IsTileSolidAtTileLocation(tileLocation);

                    if (cancelMega) {
                        photonView.RPC(nameof(FinishMegaMario), RpcTarget.All, false);
                        return;
                    }
                }
            }
            return;
        }
        if (giantEndTimer > 0 && stationaryGiantEnd) {
            body.velocity = Vector2.zero;
            body.isKinematic = true;
            transform.position = body.position = previousFramePosition;

            if (giantEndTimer - delta <= 0) {
                hitInvincibilityCounter = 2f;
                body.velocity = giantSavedVelocity;
                animator.enabled = true;
                body.isKinematic = false;
                state = previousState;
                UpdateGameState();
            }
            return;
        }

        if (state == Enums.PowerupState.MegaMushroom) {
            HandleGiantTiles(true);
            if (onGround && singlejump) {
                photonView.RPC(nameof(SpawnParticle), RpcTarget.All, "Prefabs/Particle/GroundpoundDust", body.position);
                CameraController.ScreenShake = 0.15f;
                singlejump = false;
            }
            invincible = 0;
        }

        //pipes > stuck in block, else the animation gets janked.
        if (pipeEntering || giantStartTimer > 0 || (giantEndTimer > 0 && stationaryGiantEnd) || animator.GetBool("pipe"))
            return;
        if (HandleStuckInBlock())
            return;

        //Pipes
        if (!paused && pipeTimer <= 0) {
            DownwardsPipeCheck();
            UpwardsPipeCheck();
        }

        if (knockback) {
            if (bounce && photonView.IsMine)
                photonView.RPC(nameof(ResetKnockback), RpcTarget.All);

            wallSlideLeft = false;
            wallSlideRight = false;
            crouching = false;
            inShell = false;
            body.velocity -= body.velocity * (delta * 2f);
            if (photonView.IsMine && onGround && (Mathf.Abs(body.velocity.x) < 0.35f && knockbackTimer <= 0))
                photonView.RPC(nameof(ResetKnockback), RpcTarget.All);
            if (holding && !inShell) {
                holding.photonView.RPC(nameof(HoldableEntity.Throw), RpcTarget.All, !facingRight, true, body.position);
                holding = null;
            }
        }

        //activate blocks jumped into
        if (hitRoof) {
            body.velocity = new Vector2(body.velocity.x, Mathf.Min(body.velocity.y, -0.1f));
            bool tempHitBlock = false;
            foreach (Vector3Int tile in tilesJumpedInto) {
                int temp = InteractWithTile(tile, InteractableTile.InteractionDirection.Up);
                if (temp != -1)
                    tempHitBlock |= temp == 1;
            }
            if (tempHitBlock && state == Enums.PowerupState.MegaMushroom) {
                CameraController.ScreenShake = 0.15f;
                photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.World_Block_Bump);
            }
        }

        bool right = joystick.x > analogDeadzone && !paused;
        bool left = joystick.x < -analogDeadzone && !paused;
        bool crouch = joystick.y < -analogDeadzone && !paused;
        alreadyGroundpounded &= crouch;
        bool up = joystick.y > analogDeadzone && !paused;
        bool jump = jumpBuffer > 0 && (onGround || koyoteTime < 0.07f || wallSlideLeft || wallSlideRight) && !paused;

        if (drill) {
            propellerSpinTimer = 0;
            if (propeller) {
                if (!crouch) {
                    Utils.TickTimer(ref propellerDrillBuffer, 0, Time.deltaTime);
                    if (propellerDrillBuffer <= 0)
                        drill = false;
                } else {
                    propellerDrillBuffer = 0.15f;
                }
            }
        }

        if (propellerTimer > 0)
            body.velocity = new Vector2(body.velocity.x, (propellerLaunchVelocity * (underWater ? .25f : 1)) - (propellerTimer < .4f ? (1 - (propellerTimer / .4f)) * propellerLaunchVelocity : 0) * (underWater ? .25f : 1));

        if (powerupButtonHeld && wallJumpTimer <= 0 && (propeller || !usedPropellerThisJump)) {
            if (body.velocity.y < -0.1f && propeller && !drill && !wallSlideLeft && !wallSlideRight && propellerSpinTimer < propellerSpinTime / 4f) {
                propellerSpinTimer = propellerSpinTime;
                propeller = true;
                photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Powerup_PropellerMushroom_Spin);
            }
        }

        if (holding) {
            wallSlideLeft = false;
            wallSlideRight = false;
            SetHoldingOffset();
        }

        //throwing held item
        ThrowHeldItem(left, right, crouch);

        //blue shell enter/exit
        if (!CanShell() || !functionallyRunning)
            inShell = false;

        if (inShell) {
            crouch = true;
            if (photonView.IsMine && (hitLeft || hitRight)) {
                foreach (var tile in tilesHitSide)
                    InteractWithTile(tile, InteractableTile.InteractionDirection.Up);
                facingRight = hitLeft;
                photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.World_Block_Bump);
            }
        }

        //Ground
        if (onGround) {
            if (photonView.IsMine && hitRoof && crushGround && body.velocity.y <= 0.1 && state != Enums.PowerupState.MegaMushroom) {
                //Crushed.
                photonView.RPC(nameof(Powerdown), RpcTarget.All, true);
            }

            koyoteTime = 0;
            usedPropellerThisJump = false;
            wallSlideLeft = false;
            wallSlideRight = false;
            jumping = false;
            if (drill)
                SpawnParticle("Prefabs/Particle/GroundpoundDust", body.position);

            if (onSpinner && Mathf.Abs(body.velocity.x) < 0.3f && !holding) {
                Transform spnr = onSpinner.transform;
                float diff = body.position.x - spnr.transform.position.x;
                if (Mathf.Abs(diff) >= 0.02f)
                    body.position += -0.6f * Mathf.Sign(diff) * Time.fixedDeltaTime * Vector2.right;
            }
        } else {
            koyoteTime += delta;
            landing = 0;
            skidding = false;
            turnaround = false;
            if (!jumping)
                properJump = false;
        }

        //Crouching
        HandleCrouching(crouch && !CantMove());

        HandleWallslide(left, right, jump);

        HandleSlopes();

        if (crouch && !alreadyGroundpounded)
        {
            if (state != Enums.PowerupState.Strawberry && !CantMove() && buttBurnt <= 0)
            {
                HandleGroundpoundStart(left, right);
            }
        } else {
            groundpoundStartTimer = 0;
        }
        HandleGroundpound();

        HandleSliding(up, crouch, left, right);

        if (onGround) {
            if (propellerTimer < 0.5f) {
                propeller = false;
                propellerTimer = 0;
            }
            flying = false;
            drill = false;
            if (landing <= Time.fixedDeltaTime + 0.01f && !groundpound && !crouching && !inShell && !holding && state != Enums.PowerupState.MegaMushroom) {
                bool edge = !Physics2D.BoxCast(body.position, MainHitbox.size * 0.75f, 0, Vector2.down, 0, Layers.MaskAnyGround);
                bool edgeLanding = false;

                if ((triplejump && !(left ^ right))
                    || edgeLanding
                    || (Mathf.Abs(body.velocity.x) < 0.1f)) {

                    if (!onIce)
                        body.velocity = Vector2.zero;

                    animator.Play("jumplanding" + (edgeLanding ? "-edge" : ""));
                    if (edgeLanding)
                        jumpLandingTimer = 0.15f;
                }
            }
            if (landing > 0.2f) {
                singlejump = false;
                doublejump = false;
                triplejump = false;
            }
        }


        if (!(groundpound && !onGround)) {
            //Normal walking/running
            HandleWalkingRunning(left, right);

            //Jumping
            HandleJumping(jump);
        }


        if (state == Enums.PowerupState.MegaMushroom && giantTimer <= 0 && photonView.IsMine) {
            photonView.RPC(nameof(EndMega), RpcTarget.All);
        }

        HandleSlopes();
        HandleFacingDirection();

        //slow-rise check
        if(dashTimer > 0)
        {
            body.gravityScale = 0;
            dashTimer -= Time.fixedDeltaTime;
            body.velocity = Vector2.Lerp(body.velocity, Vector2.zero, .05f);
        }
        else if (flying || propeller) {
            body.gravityScale = flyingGravity;
        } else {
            float gravityModifier = state switch {
                Enums.PowerupState.MiniMushroom => 0.4f,
                _ => 1,
            };
            float slowriseModifier = state switch {
                Enums.PowerupState.MegaMushroom => 3f,
                _ => 1f,
            };
            if (groundpound)
                gravityModifier *= 1.5f;

            if (body.velocity.y > 2.5) {
                if (jump || jumpHeld || state == Enums.PowerupState.MegaMushroom || buttBurnt > 0) {
                    body.gravityScale = slowriseGravity * slowriseModifier;
                } else {
                    body.gravityScale = normalGravity * 1.5f * gravityModifier;
                }
            } else if (onGround || (groundpound && groundpoundCounter > 0)) {
                body.gravityScale = 0f;
            } else {
                body.gravityScale = normalGravity * (gravityModifier / 1.2f);
            }
        }

        if (underWater)
        {
            body.gravityScale /= 2.5f;
        }

        //Terminal velocity
        float terminalVelocityModifier = state switch {
            Enums.PowerupState.MiniMushroom => 0.625f,
            Enums.PowerupState.MegaMushroom => 2f,
            _ => 1f,
        };
        if (underWater)
        {
            terminalVelocityModifier /= 1.5f;
        }
        if(dashTimer <= 0)
        {
            if (flying)
            {
                if (drill)
                {
                    body.velocity = new(body.velocity.x, -drillVelocity);
                }
                else
                {
                    body.velocity = new(body.velocity.x, Mathf.Max(body.velocity.y, -flyingTerminalVelocity));
                }
            }
            else if (propeller)
            {
                if (drill)
                {
                    body.velocity = new(Mathf.Clamp(body.velocity.x, -WalkingMaxSpeed, WalkingMaxSpeed), -drillVelocity);
                }
                else
                {
                    float htv = WalkingMaxSpeed * 1.18f + (propellerTimer * 2f);
                    body.velocity = new(Mathf.Clamp(body.velocity.x, -htv, htv), Mathf.Max(body.velocity.y, propellerSpinTimer > 0 ? -propellerSpinFallSpeed : -propellerFallSpeed));
                }
            }
            else if (wallSlideLeft || wallSlideRight)
            {
                body.velocity = new(body.velocity.x, Mathf.Max(body.velocity.y, wallslideSpeed));
            }
            else if (groundpound)
            {

                if (underWater)
                {
                    body.velocity = new(body.velocity.x, Mathf.Max(body.velocity.y, -groundpoundVelocity / 1.5f));
                }
                else
                {
                    body.velocity = new(body.velocity.x, Mathf.Max(body.velocity.y, -groundpoundVelocity));
                }
            }
            else
            {
                body.velocity = new(body.velocity.x, Mathf.Max(body.velocity.y, terminalVelocity * terminalVelocityModifier));
            }
        }

        if (crouching || sliding || skidding) {
            wallSlideLeft = false;
            wallSlideRight = false;
        }

        if (previousOnGround && !onGround && !properJump && crouching && !inShell && !groundpound)
            body.velocity = new(body.velocity.x, -3.75f);
    }

    public void SetHoldingOffset() {
        if (holding is FrozenCube) {
            holding.holderOffset = new(0, MainHitbox.size.y * (1f - Utils.QuadraticEaseOut(1f - (pickupTimer / pickupTime))), -2);
        } else {
            holding.holderOffset = new((facingRight ? 1 : -1) * 0.25f, state >= Enums.PowerupState.Mushroom ? 0.5f : 0.25f, !facingRight ? -0.09f : 0f);
        }
    }

    void ThrowHeldItem(bool left, bool right, bool crouch) {
        if (!((!functionallyRunning || state == Enums.PowerupState.MiniMushroom || state == Enums.PowerupState.MegaMushroom || invincible > 0 || flying || propeller) && holding) || inShell)
            return;

        bool throwLeft = !facingRight;
        if (left ^ right)
            throwLeft = left;

        crouch &= holding.canPlace;

        holdingOld = holding;
        throwInvincibility = 0.15f;

        if (photonView.IsMine)
            holding.photonView.RPC(nameof(HoldableEntity.Throw), RpcTarget.All, throwLeft, crouch, body.position);

        if (!crouch && !knockback) {
            PlaySound(Enums.Sounds.Player_Voice_WallJump, 2);
            throwInvincibility = 0.5f;
            animator.SetTrigger("throw");
        }

        holding = null;
    }

    void HandleGroundpoundStart(bool left, bool right) {
        if (!photonView.IsMine)
            return;

        if (groundpoundStartTimer == 0)
            groundpoundStartTimer = 0.065f;

        Utils.TickTimer(ref groundpoundStartTimer, 0, Time.fixedDeltaTime);

        if (groundpoundStartTimer != 0)
            return;

        if (onGround || knockback || groundpound || drill
            || holding || crouching || sliding || inShell
            || wallSlideLeft || wallSlideRight || groundpoundDelay > 0)
            return;

        if (!propeller && !flying && (left || right))
            return;

        if (flying) {
            //start drill
            if (body.velocity.y < 0) {
                drill = true;
                hitBlock = true;
                body.velocity = new(0, body.velocity.y);
            }
        } else if (propeller) {
            //start propeller drill
            if (propellerTimer < 0.6f && body.velocity.y < 7) {
                drill = true;
                propellerTimer = 0;
                hitBlock = true;
            }
        } else {
            //start groundpound
            //check if high enough above ground
            if (Physics2D.BoxCast(body.position, MainHitbox.size * Vector2.right * transform.localScale, 0, Vector2.down, 0.15f * (state == Enums.PowerupState.MegaMushroom ? 2.5f : 1), Layers.MaskAnyGround))
                return;

            wallSlideLeft = false;
            wallSlideRight = false;
            groundpound = true;
            singlejump = false;
            doublejump = false;
            triplejump = false;
            hitBlock = true;
            sliding = false;
            body.velocity = Vector2.up * 1.5f;
            groundpoundCounter = groundpoundTime * (state == Enums.PowerupState.MegaMushroom ? 1.5f : 1);
            photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Player_Sound_GroundpoundStart);
            alreadyGroundpounded = true;
            //groundpoundDelay = 0.75f;
        }
    }

    void HandleGroundpound() {
        if (groundpound && groundpoundCounter > 0 && groundpoundCounter <= .1f)
            body.velocity = Vector2.zero;

        if (groundpound && groundpoundCounter > 0 && groundpoundCounter - Time.fixedDeltaTime <= 0)
            body.velocity = Vector2.down * groundpoundVelocity;

        if (!(photonView.IsMine && onGround && (groundpound || drill) && hitBlock))
            return;

        bool tempHitBlock = false, hitAnyBlock = false;
        foreach (Vector3Int tile in tilesStandingOn) {
            int temp = InteractWithTile(tile, InteractableTile.InteractionDirection.Down);
            if (temp != -1) {
                hitAnyBlock = true;
                tempHitBlock |= temp == 1;
            }
        }
        hitBlock = tempHitBlock;
        if (drill) {
            flying &= hitBlock;
            propeller &= hitBlock;
            drill = hitBlock;
            if (hitBlock)
                onGround = false;
        } else {
            //groundpound
            if (hitAnyBlock) {
                if (state != Enums.PowerupState.MegaMushroom) {
                    Enums.Sounds sound = state switch {
                        Enums.PowerupState.MiniMushroom => Enums.Sounds.Powerup_MiniMushroom_Groundpound,
                        _ => Enums.Sounds.Player_Sound_GroundpoundLanding,
                    };
                    photonView.RPC(nameof(PlaySound), RpcTarget.All, sound);
                    photonView.RPC(nameof(SpawnParticle), RpcTarget.All, "Prefabs/Particle/GroundpoundDust", body.position);
                    groundpoundDelay = 0;
                } else {
                    CameraController.ScreenShake = 0.15f;
                }
            }
            if (hitBlock) {
                koyoteTime = 1.5f;
            } else if (state == Enums.PowerupState.MegaMushroom) {
                photonView.RPC(nameof(PlaySound), RpcTarget.All, Enums.Sounds.Powerup_MegaMushroom_Groundpound);
                photonView.RPC(nameof(SpawnParticle), RpcTarget.All, "Prefabs/Particle/GroundpoundDust", body.position);
                CameraController.ScreenShake = 0.35f;
            }
        }
    }

    public bool CanPickup() {
        return state != Enums.PowerupState.MiniMushroom && !skidding && !turnaround && !holding && running && !propeller && !flying && !crouching && !dead && !wallSlideLeft && !wallSlideRight && !doublejump && !triplejump && !groundpound;
    }

    public bool CanShell()
    {
        return holding != null && holding is KoopaWalk koopa && koopa.blue;
    }
    void OnDrawGizmos() {
        if (swordActive)
        {
            Gizmos.DrawWireCube(AnimationController.sword.transform.position, Vector2.one);
        }
        if (!body)
            return;

        Gizmos.DrawRay(body.position, body.velocity);
        Gizmos.DrawCube(body.position + new Vector2(0, WorldHitboxSize.y * 0.5f) + (body.velocity * Time.fixedDeltaTime), WorldHitboxSize);

        Gizmos.color = Color.white;
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) {
            if (r is ParticleSystemRenderer)
                continue;

            Gizmos.DrawWireCube(r.bounds.center, r.bounds.size);
        }
    }

    public void SetSwordHitboxActive(int active)
    {
        if (photonView.IsMine)
        {
            swordActive = active > 0;
            if (swordActive)
            {
                photonView.RPC(nameof(PlaySound), RpcTarget.All, state == Enums.PowerupState.Sword ? Enums.Sounds.Powerup_Sword_Sweep : Enums.Sounds.Powerup_Bat_Swing);
            }
        }
    }

    public bool swordActive;

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
    public void ForceLivesTo(int lives)
    {
        this.lives = lives;
    }

    [PunRPC]
    public void SetAnimatorTriggerRPC(string trigger)
    {
        animator.SetTrigger(trigger);
    }
    public bool CantMove()
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsName("SlashA") || animator.GetCurrentAnimatorStateInfo(0).IsName("SlashB") || animator.GetCurrentAnimatorStateInfo(0).IsName("SlashC") || animator.GetCurrentAnimatorStateInfo(0).IsName("BatSwing") || GameManager.Instance.paralizePlayer;
    }
    public void Dash()
    {
        if (dashed)
            return;
        Vector2 dashDirection = (Mathf.Abs(joystick.x) > analogDeadzone || Mathf.Abs(joystick.y) > analogDeadzone) ? DeadzonedJoystick() : Vector2.right * (facingRight ? 1 : -1);
        body.velocity = dashDirection * 12.5f;
        dashTimer = .25f;
        dashed = true;
        singlejump = false;
        doublejump = false;
        triplejump = false;
        wallJumpTimer = 0;
    }
    public Vector2 DeadzonedJoystick()
    {
        Vector2 ret = Vector2.zero;
        if(joystick.x > analogDeadzone)
        {
            ret.x = 1;
        }
        else if(joystick.x < -analogDeadzone)
        {
            ret.x = -1;
        }
        if(joystick.y > analogDeadzone)
        {
            ret.y = 1;
        }
        else if(joystick.y < -analogDeadzone)
        {
            ret.y = -1;
        }
        if(Mathf.Abs(joystick.x) > analogDeadzone && Mathf.Abs(joystick.y) > analogDeadzone)
        {
            ret *= .9f;
        }
        return ret;
    }

    public static PlayerController FindNearbyPlayer(Vector2 fromPoint)
    {
        float distance = Mathf.Infinity;
        PlayerController ret = null;
        foreach(PlayerController con in instances)
        {
            float dist = Vector2.Distance(fromPoint, con.body.position);
            if (dist < distance)
            {
                distance = dist;
                ret = con;
            }
        }
        return ret;
    }
    [PunRPC]
    public void TeleportToPoint(Vector2 position)
    {
        transform.position = position;
    }

    public static PlayerController GetHostPlayer()
    {
        foreach(PlayerController con in FindObjectsOfType<PlayerController>())
        {
            if (con.photonView.Owner.IsMasterClient)
            {
                return con;
            }
        }
        return null;
    }
}
