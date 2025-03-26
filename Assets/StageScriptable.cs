using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New StageData", menuName = "CharMod/StageData", order = 1)]
public class StageScriptable : ScriptableObject //what if I told you this script is originally from my Character Mod?
{
    public string displayName = "New Stage", displayOrigin = "NSMBVS: HyperCat's Character Mod";
    public uint sceneIndex = 2;
    public Conversation[] endConvo = new Conversation[4];
}
