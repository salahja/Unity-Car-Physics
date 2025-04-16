using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Fixed namespace

public class InputHandler : MonoBehaviour
{
    [SerializeField]
    CarHandler carHandler;

    void Update()
    {
        Vector2 input = Vector2.zero;

        input.x = Input.GetAxis("Horizontal"); // Fixed string literals with quotes
        input.y = Input.GetAxis("Vertical");   // Changed to input.y for vertical axis

        carHandler.SetInput(input);

        if (Input.GetKeyDown(KeyCode.R)) // Fixed Input class and removed extra parenthesis
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
