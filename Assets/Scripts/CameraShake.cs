using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public bool isShaking;

    public IEnumerator Shake(float duration, float magnitude)
    {
        isShaking = true;
        Vector3 originalPos = transform.localPosition;

        float elapsed = 0f;

        // Kamera tärisee x ja y akselilla
        while (elapsed < duration)
        {
            // Luodaan satunnaisluvut millä heilutetaan kameraa
            float x = Random.Range(1f, 2f) * magnitude;
            float y = Random.Range(1f, 5f) * magnitude;

            transform.localPosition = new Vector3(x, y, originalPos.z);
            elapsed += Time.deltaTime * 5; 
            yield return null;
        }

        transform.localPosition = originalPos;
        isShaking = false;
    }
}
