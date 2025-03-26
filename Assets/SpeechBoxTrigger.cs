using NSMB.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Photon.Pun;

public class SpeechBoxTrigger : MonoBehaviour
{
    public SpeechBoxCanvas box;
    public Conversation[] conversations;
    public UnityEvent onConvoEnd;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == GameManager.Instance.localPlayer)
        {
            if (Utils.GetCharacterIndex() >= conversations.Length) //if there's no dialogue, make sure the game can continue
            {
                onConvoEnd.Invoke();
                Destroy(gameObject);
                Debug.Log("No dialogue for this character. You're either Deven, or there is a serialization error. ");
            }
            else
            {
                Destroy(gameObject);
                box.onConvoEnd = onConvoEnd;
                Conversation convo = null;
                convo = conversations[Utils.GetCharacterIndex()];
                Destroy(gameObject);
                if (convo == null)
                {
                    Debug.Log("Null dialogue for this character. There may be a serialization error. ");
                    onConvoEnd.Invoke();
                }
                else
                {
                    try
                    {
                        box.InitiateConversation(convo); //do this late, so that all the other stuff can happen, in case of worst case scenario. 
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"InitiateConversation call failed: {ex.Message}\n{ex.StackTrace}");
                    }
                }
            }
        }
        else
        {
            Debug.Log($"Object {collision.gameObject.name} is not local player");
        }
    }
}
