using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Photon.Pun;
using NSMB.Utils;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SingleplayerCharacterSelect : MonoBehaviour
{
    public Image playerIcon, rightArrow, leftArrow;
    public TMP_Text characterText;
    private int selectedCharacter;

    private void Awake()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.OfflineMode = true;
            PhotonNetwork.CreateRoom("Singleplayer", new()
            {
                CustomRoomProperties = NetworkUtils.DefaultRoomProperties
            });
        }
        SetChar(selectedCharacter);
        InputSystem.controls.Player.Movement.performed += OnMovement;
        InputSystem.controls.Player.Movement.canceled += OnMovement;
        InputSystem.controls.Player.Jump.performed += OnJump;
    }
    public void SetChar(int character)
    {
        ExitGames.Client.Photon.Hashtable prop = new() {
            { Enums.NetPlayerProperties.Character, character }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(prop);
        playerIcon.sprite = GlobalController.Instance.characters[character].readySprite;
        characterText.text = GlobalController.Instance.characters[character].name;
    }
    private void OnDisable()
    {
        InputSystem.controls.Player.Movement.performed -= OnMovement;
        InputSystem.controls.Player.Movement.canceled -= OnMovement;
        InputSystem.controls.Player.Jump.performed -= OnJump;
    }
    private void OnEnable()
    {
        InputSystem.controls.Player.Movement.performed += OnMovement;
        InputSystem.controls.Player.Movement.canceled += OnMovement;
        InputSystem.controls.Player.Jump.performed += OnJump;
    }
    private void OnDestroy()
    {
        InputSystem.controls.Player.Movement.performed -= OnMovement;
        InputSystem.controls.Player.Movement.canceled -= OnMovement;
        InputSystem.controls.Player.Jump.performed -= OnJump;
    }

    [SerializeField] private Vector2 joystick, previousJoystick;
    public void OnMovement(InputAction.CallbackContext context)
    {
        joystick = context.ReadValue<Vector2>();
        if (joystick.magnitude > .9f && previousJoystick.magnitude < .9f)
        {
            if(Mathf.Abs(joystick.x) > .75)
            {
                if(joystick.x > 0)
                {
                    selectedCharacter++;
                }
                else
                {
                    selectedCharacter--;
                }
                if (GlobalController.Instance.DevenUnlocked)
                {
                    if (selectedCharacter > GlobalController.Instance.characters.Length - 1)
                    {
                        selectedCharacter = 0;
                    }
                    if (selectedCharacter < 0)
                    {
                        selectedCharacter = GlobalController.Instance.characters.Length - 1;
                    }
                }
                else
                {
                    if (selectedCharacter > GlobalController.Instance.characters.Length - 2)
                    {
                        selectedCharacter = 0;
                    }
                    if (selectedCharacter < 0)
                    {
                        selectedCharacter = GlobalController.Instance.characters.Length - 2;
                    }
                }
                SetChar(selectedCharacter);
            }
        }
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        SceneManager.LoadScene("WorldMap");
    }
}
