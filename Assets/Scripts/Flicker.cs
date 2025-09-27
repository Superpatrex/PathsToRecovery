using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flicker : MonoBehaviour
{
    // Properties
    public String waveFunction = "tri"; // possible values: sin, tri(angle), sqr(square), saw(tooth), inv(verted sawtooth), noise (random)
    public float based = 0.0f; // start
    public float amplitude = 1.0f; // amplitude of the wave
    public float phase = 0.0f; // start point inside on wave cycle
    public float frequency = 0.9f; // cycle frequency per second

    // Keep a copy of the original color
    private Color originalColor;

    // Store the original color 
    void Start()
    {
        originalColor = GetComponent<Light>().color;
    }

    void Update()
    {
        Light light = GetComponent<Light>();
        light.color = originalColor * (EvalWave());
    }

    float EvalWave()
    {
        float x = (Time.time + phase) * frequency;
        float y = 0.0f;

        x = x - Mathf.Floor(x); // normalized value (0..1)

        if (waveFunction == "sin")
        {
            y = Mathf.Sin(x * 2 * Mathf.PI);
        }
        else if (waveFunction == "tri")
        {
            if (x < 0.5)
                y = 4.0f * x - 1.0f;
            else
                y = -4.0f * x + 3.0f;
        }
        else if (waveFunction == "sqr")
        {
            if (x < 0.5f)
                y = 1.0f;
            else
                y = -1.0f;
        }
        else if (waveFunction == "saw")
        {
            y = x;
        }
        else if (waveFunction == "inv")
        {
            y = 1.0f - x;
        }
        else if (waveFunction == "noise")
        {
            //y = 1 - (Random.value * 2);
        }
        else
        {
            y = 1.0f;
        }
        return (y * amplitude) + based;
    }
}
