using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.UI;
using UnityEngine.SceneManagement; // Fixed namespace

public class NewInputHandler : MonoBehaviour
{
    [SerializeField]
    NewCarHandler carHandler;

    void Update()
    {
        Vector2 input = new Vector2(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical")
        );

        carHandler.SetInput(input);
        carHandler.SetBraking(Input.GetKey(KeyCode.Space));

        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    }
}


