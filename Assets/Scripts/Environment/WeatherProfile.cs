using UnityEngine;

[System.Serializable]
public class WeatherProfile
{
    public string name;
    [Range(0, 100)] public int weight = 50;

    [Header("Visuals")]
    public Color ambientColor = Color.gray;
    public Color fogColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public float fogDensity = 0.01f;

    [Header("Sun")]
    public float lightIntensityMult = 1.0f;
    public Color lightColorTint = Color.white;

    [Header("Effects")]
    public ParticleSystem weatherEffect;
    public AudioClip ambientSound;
}