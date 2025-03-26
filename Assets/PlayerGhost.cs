using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NSMB.Utils;
using Photon.Pun;
using System;

//fun fact! I stole this script from Wonder mod :)
public class PlayerGhost : MonoBehaviour
{
    // New
    public PlayerGhostParameter[] parameters = new PlayerGhostParameter[4]; // Params to copy

    // Old
    public int delay;
    public Animator me;
    public PlayerController target;

    private List<PlayerGhostFrame> frameBuffer = new List<PlayerGhostFrame>(); // List to store frames with delay

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!target)
        {
            return;
        }

        // Capture the target's animator and other parameters
        Animator targetAnim = target.gameObject.GetComponent<Animator>();

        // Create a new frame with position, rotation, and animator parameters
        PlayerGhostFrame newFrame = new PlayerGhostFrame
        {
            position = target.transform.position,
            rotation = target.AnimationController.models.transform.rotation.eulerAngles.y
        };

        // Capture animator parameters and create a new copy for each parameter
        foreach (PlayerGhostParameter param in parameters)
        {
            // Create a new copy for the frame
            PlayerGhostParameter paramCopy = new PlayerGhostParameter
            {
                name = param.name,
                type = param.type
            };

            if (param.type == PlayerGhostParameter.PlayerGhostParameterType.FLT)
            {
                paramCopy.valueFloat = targetAnim.GetFloat(param.name);
            }
            else if (param.type == PlayerGhostParameter.PlayerGhostParameterType.INT)
            {
                paramCopy.valueFloat = targetAnim.GetInteger(param.name);
            }
            else if (param.type == PlayerGhostParameter.PlayerGhostParameterType.BOL)
            {
                paramCopy.valueBool = targetAnim.GetBool(param.name);
            }

            // Add the copy to the new frame
            newFrame.parameters.Add(paramCopy);
        }


        // Add the captured frame to the buffer (implementing delay)
        frameBuffer.Add(newFrame);

        // Apply delayed frame to this ghost
        if (frameBuffer.Count > delay)
        {
            PlayerGhostFrame delayedFrame = frameBuffer[0];
            transform.position = new Vector3(delayedFrame.position.x, delayedFrame.position.y, -2);
            transform.rotation = Quaternion.Euler(0, delayedFrame.rotation, 0);

            // Apply delayed animator parameters
            foreach (var param in delayedFrame.parameters)
            {
                if (param.type == PlayerGhostParameter.PlayerGhostParameterType.FLT)
                {
                    me.SetFloat(param.name, param.valueFloat);
                }
                else if (param.type == PlayerGhostParameter.PlayerGhostParameterType.INT)
                {
                    me.SetInteger(param.name, (int)param.valueFloat);
                }
                else if (param.type == PlayerGhostParameter.PlayerGhostParameterType.BOL)
                {
                    me.SetBool(param.name, param.valueBool);
                }
            }
            frameBuffer.RemoveAt(0);
        }
    }
}

[Serializable]
public class PlayerGhostFrame
{
    public Vector3 position;
    public float rotation;
    public List<PlayerGhostParameter> parameters = new List<PlayerGhostParameter>();
}

[Serializable]
public class PlayerGhostParameter
{
    public string name;
    public enum PlayerGhostParameterType
    {
        FLT, // float
        INT, // integer
        BOL  // bool
    }
    public PlayerGhostParameterType type;
    public float valueFloat; // reuse for ints
    public bool valueBool;
}
