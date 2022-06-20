using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;
using UnityEngine.Rendering.HighDefinition;

public class DayNightSystem : MonoBehaviour
{
    [Header("Cycle")]
    public bool cycleEnabled = true;
    public float cycleLength = 200;

    [Header("Values")]
    [Range(0,360)] public float currentTime = 37;
    public float nightValue;
    public float lightIntensity;

    [Header("Curves")]
    public AnimationCurve timeCurve;
    public AnimationCurve lightIntensityCurve;
    public AnimationCurve nightVolumeCurve;

    [Header("Objects")]
    public Volume nightVolume;
    
    public Light sunLight;
    HDAdditionalLightData sunLightData;

    public Light moonLight;
    HDAdditionalLightData moonLightData;

    private Tween cycleTween;

    void Start()
    {
        sunLightData = sunLight.GetComponent<HDAdditionalLightData>();
        moonLightData = moonLight.GetComponent<HDAdditionalLightData>();

        if (cycleEnabled)
            StartCoroutine(StartTween());
    }

    private IEnumerator StartTween()
    {
        yield return new WaitForSeconds(.1f);

        cycleTween = DOTween
            .To(() => currentTime, x => currentTime = x, 360, cycleLength)
            .SetEase(timeCurve)
            .Play()
            .SetLoops(-1);
    }

    private void Update()
    {
        // Check if cycle enabled
        if (cycleEnabled)
            cycleTween.Play();
        else
            cycleTween.Pause();

        // Set lighting
        if (currentTime > 95 && currentTime < 265)
        {
            sunLight.shadows = LightShadows.None;
            sunLightData.SetIntensity(0);
            moonLight.shadows = LightShadows.Soft;
            moonLightData.SetIntensity(11);
        }
        else
        {
            sunLight.shadows = LightShadows.Soft;
            moonLight.shadows = LightShadows.None;
            moonLightData.SetIntensity(0);
        }

        // Update values
        nightValue = 1 - (Mathf.Cos(currentTime * Mathf.PI / 180) + 1) / 2;
        nightVolume.weight = nightVolumeCurve.Evaluate(nightValue);

        sunLightData.angularDiameter = Mathf.Lerp(1, 2, nightValue);
        sunLight.colorTemperature = Mathf.Lerp(6200, -1000, nightValue);

        lightIntensity = lightIntensityCurve.Evaluate(nightValue);
        sunLightData.SetIntensity(lightIntensity, LightUnit.Lux);

        sunLight.transform.rotation = Quaternion.Euler(currentTime + 90, -320, 0);
        moonLight.transform.rotation = Quaternion.Euler(currentTime - 90, -320, 0);
    }
}
