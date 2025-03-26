using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Photon.Pun;

public class SpeechBoxCanvas : MonoBehaviour //why is my text system racist? It hates my friends :(
{
    public Conversation conversation;
    public int conversationLineIndex;
    private uint textCounter;

    public UnityEvent onConvoEnd;
    public TMP_Text dialogue, speakerName;
    public Image leftIcon, rightIcon;
    private void Start()
    {
        InputSystem.controls.Player.Jump.performed += OnJump;
        InputSystem.controls.UI.Pause.performed += OnSkip;
    }
    private void OnDestroy() //prevent memory leaks
    {
        InputSystem.controls.Player.Jump.performed -= OnJump;
        InputSystem.controls.UI.Pause.performed -= OnSkip;
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        if (!gameObject.activeSelf || conversation == null || conversation.dialogueLines[conversationLineIndex].autoProgress)
            return;
        if (context.ReadValue<float>() >= 0.5f)
        {
            if(textCounter >= (conversation.dialogueLines[conversationLineIndex].dialogue).Length)
            {
                ProgressConversation();
            }
            else
            {
                textCounter = (uint)(conversation.dialogueLines[conversationLineIndex].dialogue).Length;
                RefreshLine();
            }
        }
    }
    public void OnSkip(InputAction.CallbackContext context)
    {
        if (!gameObject.activeSelf)
            return;
        EndConversation();
    }
    private void FixedUpdate()
    {
        UpdateAll();
    }
    public void UpdateAll()
    {
        if (textCounter >= conversation.dialogueLines[conversationLineIndex].dialogue.Length && conversation.dialogueLines[conversationLineIndex].autoProgress)
        {
            ProgressConversation();
        }
        bool convoIsLeft = conversation.dialogueLines[conversationLineIndex].speaker == DialogueLine.RelaventSpeaker.left;
        Color leftColor = convoIsLeft ? Color.white : new Color(.75f, .75f, .75f, .5f);
        Color rightColor = !convoIsLeft ? Color.white : new Color(.75f, .75f, .75f, .5f);
        leftIcon.color = Color.Lerp(leftIcon.color, leftColor, Time.deltaTime * 15);
        rightIcon.color = Color.Lerp(rightIcon.color, rightColor, Time.deltaTime * 15);
        if (convoIsLeft)
        {
            leftIcon.sprite = conversation.dialogueLines[conversationLineIndex].speakerImage;
            if (conversation.dialogueLines[conversationLineIndex].otherExpression)
            {
                rightIcon.sprite = conversation.dialogueLines[conversationLineIndex].otherExpression;
            }
        }
        else
        {
            rightIcon.sprite = conversation.dialogueLines[conversationLineIndex].speakerImage;
            if (conversation.dialogueLines[conversationLineIndex].otherExpression)
            {
                leftIcon.sprite = conversation.dialogueLines[conversationLineIndex].otherExpression;
            }
        }
        dialogue.horizontalAlignment = convoIsLeft ? HorizontalAlignmentOptions.Left : HorizontalAlignmentOptions.Right;
        speakerName.horizontalAlignment = convoIsLeft ? HorizontalAlignmentOptions.Left : HorizontalAlignmentOptions.Right;
        speakerName.color = conversation.dialogueLines[conversationLineIndex].speakerColor;
        speakerName.text = conversation.dialogueLines[conversationLineIndex].speakerName;
        if (textCounter < (uint)(conversation.dialogueLines[conversationLineIndex].dialogue).Length)
        {
            textCounter++;
            RefreshLine();
        }
    }

    public void RefreshLine()
    {
        if (conversationLineIndex < 0 || conversationLineIndex >= conversation.dialogueLines.Count)
        {
            Debug.LogWarning("Invalid conversation line index.");
            return;
        }

        string currentDialogue = (conversation.dialogueLines[conversationLineIndex].dialogue);

        if (textCounter > currentDialogue.Length)
        {
            textCounter = (uint)currentDialogue.Length; // Clamp to prevent overflow
        }

        dialogue.text = currentDialogue.Substring(0, (int)textCounter);
    }

    public void InitiateConversation(Conversation convo)
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.paralizePlayer = true;
        }
        else
        {
            MapMario mario = FindObjectOfType<MapMario>();
            if (mario != null)
            {
                mario.enabled = false;
            }
        }
        if(convo != null)
        {
            conversation = convo;
        }
        conversationLineIndex = 0;
        textCounter = 0;
        gameObject.SetActive(true);
        UpdateAll();
    }
    public void ProgressConversation()
    {
        conversationLineIndex++;
        if(conversationLineIndex >= conversation.dialogueLines.Count)
        {
            EndConversation();
            return;
        }
        textCounter = 0;
        RefreshLine();
    }
    public void EndConversation()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.paralizePlayer = false;
        }
        else
        {
            MapMario mario = FindObjectOfType<MapMario>();
            if (mario != null)
            {
                mario.enabled = true;
            }
        }
        gameObject.SetActive(false);
        onConvoEnd.Invoke();
    }
}
