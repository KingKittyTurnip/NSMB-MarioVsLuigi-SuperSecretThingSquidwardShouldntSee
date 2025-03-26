using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveManager : Singleton<SaveManager>
{
    public StageScriptable currentStage;
    public List<StageScriptable> completedStages = new List<StageScriptable>();
    public Enums.PowerupState playerState = Enums.PowerupState.Small;
    public Powerup playerReserve;

    private void Awake()
    {
        if(Instance == null)
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void CompleteStage()
    {
        if (GameManager.Instance.localPlayer)
        {
            playerReserve = GameManager.Instance.localPlayer.GetComponent<PlayerController>().storedPowerup;
            playerState = GameManager.Instance.localPlayer.GetComponent<PlayerController>().state;
        }
        if (!completedStages.Contains(currentStage))
        {
            completedStages.Add(currentStage);
        }
    }
}
