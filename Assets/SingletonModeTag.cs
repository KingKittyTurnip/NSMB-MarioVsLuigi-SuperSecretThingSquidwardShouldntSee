using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonModeTag
{
    public enum Mode
    {
        None,
        Singleplayer
    }
    public static Mode mode;
}
