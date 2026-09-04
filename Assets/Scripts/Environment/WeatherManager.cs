using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Light sunLight;
    [SerializeField] private AudioSource audioSource;

    [Header("Configuration")]
    [SerializeField] private List<WeatherProfile> weatherProfiles;
    [SerializeField] private float transitionDuration = 5f;
    [SerializeField] private float minWeatherDuration = 10f;
    [SerializeField] private float maxWeatherDuration = 30f;

    private WeatherProfile currentProfile;

    void Start()
    {   
        RenderSettings.fog = true;
        currentProfile = weatherProfiles.FirstOrDefault();
        StartCoroutine(WeatherLoop());
    }

    IEnumerator WeatherLoop()
    {
        while (true)
        {
            WeatherProfile next = PickWeightedWeather();

            yield return TransitionWeather(currentProfile, next);
            currentProfile = next;

            yield return new WaitForSeconds(Random.Range(minWeatherDuration, maxWeatherDuration));
        
        }
    }

    IEnumerator TransitionWeather(WeatherProfile from, WeatherProfile to)
    {
        HandleEffectsSimple(to);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / transitionDuration;
            ApplyBlend(from, to, Mathf.Clamp01(t));
            yield return null;
        }
    }

    void ApplyBlend(WeatherProfile a, WeatherProfile b, float t)
    {
        // Fog & Ambient
        RenderSettings.fogColor = Color.Lerp(a.fogColor, b.fogColor, t);
        RenderSettings.fogDensity = Mathf.Lerp(a.fogDensity, b.fogDensity, t);
        

        float intensityMult = Mathf.Lerp(a.lightIntensityMult, b.lightIntensityMult, t);
        Color colorTint = Color.Lerp(a.lightColorTint, b.lightColorTint, t);

        sunLight.intensity = intensityMult;
        sunLight.color = colorTint;
    }

    void HandleEffectsSimple(WeatherProfile target)
    {
        foreach (var w in weatherProfiles)
        {
            if (w.weatherEffect != null)
                w.weatherEffect.Stop();
        }

        if (target.weatherEffect != null)
            target.weatherEffect.Play();

        if (audioSource != null && audioSource.clip != target.ambientSound)
        {
            audioSource.clip = target.ambientSound;
            audioSource.Play();
        }
    }

    WeatherProfile PickWeightedWeather()
    {
        int total = 0;
        foreach (var w in weatherProfiles)
            total += w.weight;

        int r = Random.Range(0, total);
        int cursor = 0;

        foreach (var w in weatherProfiles)
        {
            cursor += w.weight;
            if (r < cursor)
                return w;
        }

        return weatherProfiles[0];
    }
}
