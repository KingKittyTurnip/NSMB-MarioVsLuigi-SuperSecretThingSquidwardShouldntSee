using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using static UnityEngine.GraphicsBuffer;

[CreateAssetMenu(fileName = "New Conversation", menuName = "Dialogue/Conversation")]
public class Conversation : ScriptableObject
{
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();
}

[Serializable]
public class DialogueLine
{
    public string speakerName;
    public Color speakerColor = Color.white;
    [TextArea]
    public string dialogue;
    public enum RelaventSpeaker
    {
        left, right
    }
    public RelaventSpeaker speaker;
    public Sprite speakerImage, otherExpression;
    public bool autoProgress;
}


#if UNITY_EDITOR
[CustomEditor(typeof(Conversation))]
public class ConversationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Conversation conversation = (Conversation)target;

        if (GUILayout.Button("Print Script"))
        {
            PrintConversation(conversation);
        }
    }

    private void PrintConversation(Conversation conversation)
    {
        string convoScript = "";
        convoScript += "=== Conversation Script ===\n";
        foreach (var line in conversation.dialogueLines)
        {
            convoScript += $"[{line.speakerName}] ({line.speaker}) (pose: {line.speakerImage.name}): {line.dialogue}" + "\n";
        }
        Debug.Log(convoScript);
    }
}

#endif