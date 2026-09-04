using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    [Tooltip("Duration of a full day in seconds")]
    public float dayDuration = 120f;
    [Range(0, 1)] public float currentTime = 0.5f;

    [Header("Sun Positioning")]
    public Light sunLight;
    [Tooltip("Compass direction (0 = North, 90 = East)")]
    public float sunHeading = - 90; 
    [Tooltip("The tilt of the sun's path. Use 0 for straight up/down, 45 for a nice arc.")]
    public float sunLatitude = 45f; 

    [Header("Day/Night Colors")]
    public Gradient skyColor;
    public Gradient ambientColor;
    public Gradient starsFade;

    void Start()
    {   

        GenerateStarsSkybox();
        GenerateWorldSkybox();
    }


    public Material WorldSkybox;
    public Material StarsSkybox;

    void Update()
    {

        stars_sphere.transform.position = Camera.main.transform.position;
        world_sphere.transform.position = Camera.main.transform.position;

        currentTime += Time.deltaTime / dayDuration;
        if (currentTime >= 1) 
            currentTime = 0;

        // 2. Rotate Sun
        // We calculate the sun's position on a 360 loop based on time
        float sunAngle = (currentTime * 360f) - 90f;
        
        // APPLY ROTATION:
        // We create a base rotation (Time)
        Quaternion timeRotation = Quaternion.Euler(sunAngle, 0, 0);
        
        // We create a tilt rotation (Heading + Latitude)
        // Rotating the "Universe" around the sun allows for the arc effect
        Quaternion tiltRotation = Quaternion.Euler(0, sunHeading, sunLatitude);
        
        // Combine them: Apply the tilt, THEN the time rotation
        sunLight.transform.localRotation = tiltRotation * timeRotation;

        WorldSkybox.SetColor("_Tint", skyColor.Evaluate(currentTime));
        

        StarsSkybox.SetFloat("_Intensity", starsFade.Evaluate(currentTime).a);

        RenderSettings.ambientLight = ambientColor.Evaluate(currentTime);

    }

    private GameObject stars_sphere;
    private GameObject world_sphere;

    void GenerateWorldSkybox()
    {
        world_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        world_sphere.transform.parent = transform;
        world_sphere.name = "World_Skybox";

        // 2. Remove the Collider so the player doesn't walk into it
        Destroy(world_sphere.GetComponent<SphereCollider>());

        // 3. Set Scale and Material
        world_sphere.transform.localScale = new Vector3(1000, 1000, 1000);
        world_sphere.GetComponent<MeshRenderer>().material = WorldSkybox;

        // 4. Optimization: Don't let it cast shadows or interact with lightprobes
        var renderer = world_sphere.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }


    void GenerateStarsSkybox()
    {
          // 1. Create a standard Unity Sphere
        stars_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        stars_sphere.transform.parent = transform;
        stars_sphere.name = "Stars_Sphere";

        // 2. Remove the Collider so the player doesn't walk into it
        Destroy(stars_sphere.GetComponent<SphereCollider>());

        // 3. Set Scale and Material
        stars_sphere.transform.localScale = new Vector3(999, 999, 999);
        stars_sphere.GetComponent<MeshRenderer>().material = StarsSkybox;

        // 4. Optimization: Don't let it cast shadows or interact with lightprobes
        var renderer = stars_sphere.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }
}