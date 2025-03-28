using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class CreditsReel : MonoBehaviour
{
    private void Start()
    {
        InputSystem.controls.UI.Pause.performed += OnPause;
    }

    public void OnDestroy()
    {
        InputSystem.controls.UI.Pause.performed -= OnPause;
    }
    public RectTransform rect;
    void Update()
    {
        rect.pivot -= Vector2.up * Time.deltaTime / 10;
    }


    public void OnPause(InputAction.CallbackContext context)
    {
        SceneManager.LoadScene(0);
    }
}
