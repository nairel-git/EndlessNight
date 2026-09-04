using UnityEngine;

public class KettenkradStats : MonoBehaviour
{
    [Header("Engine & Fuel")]
    [SerializeField] float Fuel = 100f;
    [SerializeField] float ConsumptionRate = 0.5f;
    [SerializeField] bool EngineOn;

    [Header("Maintenance")]
    [SerializeField] float Condition = 100f;
    public bool IsEngineOn => EngineOn;
    public bool HasFuel => Fuel > 0f;
    public bool IsOperational => Condition > 0f && HasFuel;
    public bool CanDrive => EngineOn && IsOperational;




    [Header("Sound Effects")]
    [SerializeField] AudioSource source;

    [SerializeField] AudioClip VehicleStart;
    [SerializeField] AudioClip LightsOn;
    [SerializeField] AudioClip LightsOff;
    

    void Start()
    {
        source = GetComponent<AudioSource>();
    }

    void Update()
    {
       
    }


    public void StartEngineSound()
    {
        source.PlayOneShot(VehicleStart);
    }

    public void StartEngine()
    {       
        EngineOn = true;
        source.Play();
    }

    public void StopEngine()
    {
        EngineOn = false;
        source.Stop();
    }

    public void ConsumeFuel(float inputMagnitude)
    {
        if (!IsEngineOn) 
            return;

        float drain = inputMagnitude > 0.1f ? ConsumptionRate : ConsumptionRate * 0.2f;

        Fuel = Mathf.Max(0, Fuel - drain * Time.deltaTime);

        if (Fuel <= 0)
            StopEngine();
    }
}
