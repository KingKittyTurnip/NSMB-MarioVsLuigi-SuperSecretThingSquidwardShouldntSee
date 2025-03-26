using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Photon.Pun;
using NSMB.Utils;


public class UIUpdater : MonoBehaviour {

    public static UIUpdater Instance;
    public GameObject playerTrackTemplate, starTrackTemplate;
    public PlayerController player;
    public Sprite storedItemNull;
    public TMP_Text uiStars, uiCoins, uiAmmo, uiDebug, uiLives, uiCountdown;
    public Image itemReserve, itemColor;
    public float pingSample = 0;

    private Material timerMaterial;
    private GameObject starsParent, coinsParent, ammoParent, livesParent, timerParent;
    private readonly List<Image> backgrounds = new();
    private bool uiHidden;
    public Image vinnette;

    private int coins = -1, ammo = -1, stars = -1, lives = -1, timer = -1;

    public void Start() {
        Instance = this;
        pingSample = PhotonNetwork.GetPing();

        starsParent = uiStars.transform.parent.gameObject;
        coinsParent = uiCoins.transform.parent.gameObject;
        ammoParent = uiAmmo.transform.parent.gameObject;
        livesParent = uiLives.transform.parent.gameObject;
        timerParent = uiCountdown.transform.parent.gameObject;

        backgrounds.Add(starsParent.GetComponentInChildren<Image>());
        backgrounds.Add(coinsParent.GetComponentInChildren<Image>());
        backgrounds.Add(ammoParent.GetComponentInChildren<Image>());
        backgrounds.Add(livesParent.GetComponentInChildren<Image>());
        backgrounds.Add(timerParent.GetComponentInChildren<Image>());

        foreach (Image bg in backgrounds)
            bg.color = GameManager.Instance.levelUIColor;
        itemColor.color = new(GameManager.Instance.levelUIColor.r - 0.2f, GameManager.Instance.levelUIColor.g - 0.2f, GameManager.Instance.levelUIColor.b - 0.2f, GameManager.Instance.levelUIColor.a);
    }

    public void Update() {
        pingSample = Mathf.Lerp(pingSample, PhotonNetwork.GetPing(), Mathf.Clamp01(Time.unscaledDeltaTime * 0.5f));
        if (pingSample == float.NaN)
            pingSample = 0;

        uiDebug.text = "<mark=#000000b0 padding=\"20, 20, 20, 20\"><font=\"defaultFont\">Ping: " + (int) pingSample + "ms</font>";

        //Player stuff update.
        if (!player && GameManager.Instance.localPlayer)
            player = GameManager.Instance.localPlayer.GetComponent<PlayerController>();

        if (!player) {
            if (!uiHidden)
                ToggleUI(true);

            return;
        }

        if (uiHidden)
            ToggleUI(false);

        UpdateStoredItemUI();
        UpdateTextUI();
    }

    private void ToggleUI(bool hidden) {
        uiHidden = hidden;

        starsParent.SetActive(!hidden);
        livesParent.SetActive(!hidden);
        coinsParent.SetActive(!hidden);
        timerParent.SetActive(!hidden);
    }

    private void UpdateStoredItemUI() {
        if (!player)
            return;

        itemReserve.sprite = player.storedPowerup != null ? player.storedPowerup.reserveSprite : storedItemNull;
    }

    private void UpdateTextUI() {
        if (!player || GameManager.Instance.gameover)
            return;

        vinnette.color = Color.Lerp(vinnette.color, (player.underWater && GameManager.Instance.freezingWater) ? new Color(.75f, 1f, 1f, .5f) : new Color(.75f, 1f, 1f, 0f), Time.deltaTime * (player.underWater ? .5f : 5));
        if(SingletonModeTag.mode == SingletonModeTag.Mode.Singleplayer || GameManager.Instance.musicState == Enums.MusicState.OVERTIME)
        {
            starsParent.SetActive(player.underWater);
        }
        else
        {
            starsParent.SetActive(true);
        }
        if (player.stars != stars || (SingletonModeTag.mode == SingletonModeTag.Mode.Singleplayer || player.underWater)) {
            stars = player.stars;

            if (SingletonModeTag.mode == SingletonModeTag.Mode.Singleplayer || player.underWater)
            {
                uiStars.text = "<sprite=49><sprite=25>" + Utils.GetSymbolString(Mathf.Ceil((float)(player.oxygen * player.maxOxygen)).ToString());
            }
            else
            {
                uiStars.text = Utils.GetSymbolString("Sx" + stars + "/" + GameManager.Instance.starRequirement);
            }
        }
        if (player.coins != coins) {
            coins = player.coins;
            if(SingletonModeTag.mode == SingletonModeTag.Mode.Singleplayer){

                uiCoins.text = Utils.GetSymbolString("Cx" + coins);
            }
            else
            {
                uiCoins.text = Utils.GetSymbolString("Cx" + coins + "/" + GameManager.Instance.coinRequirement);
            }
        }
        if(player.state == Enums.PowerupState.Glock)
        {
            ammoParent.SetActive(true);
            if (player.ammo != ammo)
            {
                ammo = player.ammo;
                uiAmmo.text = "<sprite=48><sprite=21>" + Utils.GetSymbolString(ammo.ToString());
            }
        }
        else
        {
            ammoParent.SetActive(false);
        }

        if (player.lives >= 0) {
            if (player.lives != lives) {
                lives = player.lives;
                uiLives.text = Utils.GetCharacterData(player.photonView.Owner).uistring + Utils.GetSymbolString("x" + lives);
            }
        } else {
            livesParent.SetActive(false);
        }

        if (GameManager.Instance.timedGameDuration > 0) {
            int seconds = Mathf.CeilToInt((GameManager.Instance.endServerTime - PhotonNetwork.ServerTimestamp) / 1000f);
            seconds = Mathf.Clamp(seconds, 0, GameManager.Instance.timedGameDuration);
            if (seconds != timer) {
                timer = seconds;
                uiCountdown.text = Utils.GetSymbolString("cx" + (timer / 60) + ":" + (seconds % 60).ToString("00"));
            }
            timerParent.SetActive(true);

            if (GameManager.Instance.endServerTime - PhotonNetwork.ServerTimestamp < 0) {
                if (timerMaterial == null) {
                    CanvasRenderer cr = uiCountdown.transform.GetChild(0).GetComponent<CanvasRenderer>();
                    cr.SetMaterial(timerMaterial = new(cr.GetMaterial()), 0);
                }

                float partialSeconds = (GameManager.Instance.endServerTime - PhotonNetwork.ServerTimestamp) / 1000f % 2f;
                byte gb = (byte) (Mathf.PingPong(partialSeconds, 1f) * 255);
                timerMaterial.SetColor("_Color", new Color32(255, gb, gb, 255));
            }
        } else {
            timerParent.SetActive(false);
        }
    }

    public GameObject CreatePlayerIcon(PlayerController player) {
        GameObject trackObject = Instantiate(playerTrackTemplate, playerTrackTemplate.transform.parent);
        TrackIcon icon = trackObject.GetComponent<TrackIcon>();
        icon.target = player.gameObject;

        trackObject.SetActive(true);

        return trackObject;
    }
}
