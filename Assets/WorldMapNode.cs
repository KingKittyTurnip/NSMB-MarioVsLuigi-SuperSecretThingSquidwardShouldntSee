using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WorldMapNode : MonoBehaviour
{
    public WorldMapNode right, left, up, down;

    public StageScriptable map;

    public bool open = true;

    public bool rightBlocked, leftBlocked, upBlocked, downBlocked;

    public UnityEvent WhenLevelCleared;

    private void Start()
    {
        if (SaveManager.Instance.completedStages.Contains(map))
        {
            WhenLevelCleared.Invoke();
        }
    }
    public void SetNodeRight(WorldMapNode node)
    {
        right = node;
    }

    public void SetNodeLeft(WorldMapNode node)
    {
        left = node;
    }
    public void SetNodeUp(WorldMapNode node)
    {
        up = node;
    }
    public void SetNodeDown(WorldMapNode node)
    {
        down = node;
    }

    public static WorldMapNode FindNodeByStage(StageScriptable stage)
    {
        foreach(WorldMapNode node in FindObjectsOfType<WorldMapNode>())
        {
            if(node.map == stage)
            {
                return node;
            }
        }
        return null;
    }
}
