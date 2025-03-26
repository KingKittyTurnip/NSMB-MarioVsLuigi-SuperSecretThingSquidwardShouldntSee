using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;
using NSMB.Utils;

public class MapMario : MonoBehaviour
{
    [SerializeField] private StageScriptable selectedStage;
    [SerializeField] private WorldMapNode currentNode;
    [SerializeField] private Vector2 joystick, previousJoystick;
    [SerializeField] private Transform renderMask;
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer sprite;
    public Material[] charMats;
    private float moveTime = 0, courseInTimer = 0;
    private Vector2 lastPos, newPos;
    public AudioSource audioSource;
    public PlayerData character;
    public SpeechBoxCanvas speechBoxCanvas;
    private void Start()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.currentStage != null)
        {
            currentNode = WorldMapNode.FindNodeByStage(SaveManager.Instance.currentStage);
            transform.position = currentNode.transform.position;
            if (SaveManager.Instance.completedStages.Contains(SaveManager.Instance.currentStage))
            {
                if(Utils.GetCharacterIndex() < SaveManager.Instance.currentStage.endConvo.Length)
                {
                    speechBoxCanvas.InitiateConversation(SaveManager.Instance.currentStage.endConvo[Utils.GetCharacterIndex()]);
                }
            }
        }
        lastPos = currentNode.transform.position;
        newPos = currentNode.transform.position;
        moveTime = 1;
    }
    private void Awake()
    {
        character = Utils.GetCharacterData();
        sprite.material = charMats[Utils.GetCharacterIndex()];
        InputSystem.controls.Player.Movement.performed += OnMovement;
        InputSystem.controls.Player.Movement.canceled += OnMovement;
        InputSystem.controls.Player.Jump.performed += OnJump;
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

    public void OnMovement(InputAction.CallbackContext context)
    {
        if (selectedStage || moveTime < 1)
            return;
        joystick = context.ReadValue<Vector2>();
        if(joystick.magnitude > .9f && previousJoystick.magnitude < .9f)
        {
            Debug.Log("Move");
            if(currentNode.right != null && joystick.x > .75)
            {
                moveTime = 0;
                lastPos = transform.position;
                newPos = currentNode.right.transform.position;
                currentNode = currentNode.right;
            }
            if(currentNode.left != null && joystick.x < -.75)
            {
                moveTime = 0;
                lastPos = transform.position;
                newPos = currentNode.left.transform.position;
                currentNode = currentNode.left;
            }
            if(currentNode.up != null && joystick.y > .75)
            {
                moveTime = 0;
                lastPos = transform.position;
                newPos = currentNode.up.transform.position;
                currentNode = currentNode.up;
            }
            if(currentNode.down != null && joystick.y < -.75)
            {
                moveTime = 0;
                lastPos = transform.position;
                newPos = currentNode.down.transform.position;
                currentNode = currentNode.down;
            }
        }
        previousJoystick = joystick;
    }
    private void Update()
    {
#if UNITY_EDITOR
        if(Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            foreach(WorldMapNode node in FindObjectsOfType<WorldMapNode>())
            {
                node.WhenLevelCleared.Invoke();
            }
        }
#endif
        moveTime += 8 * Time.deltaTime;
        transform.position = Vector2.Lerp(lastPos, newPos, moveTime);
        if (selectedStage)
        {
            courseInTimer += Time.deltaTime;
            if (courseInTimer > 2)
            {
                renderMask.transform.localScale = Vector3.Lerp(renderMask.transform.localScale, Vector3.zero, Time.deltaTime * 5);
                LoopingMusic.instance.audioSource.volume = Mathf.Lerp(LoopingMusic.instance.audioSource.volume, 0, Time.deltaTime * 5);
                if (courseInTimer > 3)
                {
                    SingletonModeTag.mode = SingletonModeTag.Mode.Singleplayer;
                    SceneManager.LoadScene((int)selectedStage.sceneIndex);
                    gameObject.SetActive(false);
                }
            }
            else
            {
                renderMask.transform.localScale = Vector3.Lerp(renderMask.transform.localScale, Vector3.one * 2, Time.deltaTime * 5);
            }
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (selectedStage || moveTime < 1)
            return;
        if (context.action.WasPressedThisFrame())
        {
            if (currentNode.map != null)
            {
                selectedStage = currentNode.map;
                SaveManager.Instance.currentStage = selectedStage;
                anim.SetTrigger("Course In");
                audioSource.clip = Enums.Sounds.Player_Sound_Course_In.GetClip(character);
                audioSource.Play();
            }
        }
    }
}
