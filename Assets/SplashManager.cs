using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SplashManager : MonoBehaviour
{
    [SerializeField]
    private string[] splashes;
    private TMP_Text text;
    [SerializeField]
    private float scaleSize = 1;
    [SerializeField]
    private float scaleSpeed = 1;
    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }
    // Start is called before the first frame update
    void Start()
    {
        text.text = splashes[Random.Range(0, splashes.Length)];
    }

    private void OnEnable()
    {
        text.text = splashes[Random.Range(0, splashes.Length)];
    }

    // Update is called once per frame
    void Update()
    {
        text.fontSize = 20 + Mathf.Sin(Mathf.Deg2Rad * Time.time * scaleSpeed) * scaleSize;
    }
}
