using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class KettenkradInteraction : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] Animator playerAnim;
    [SerializeField] PlayerMovement char_movement;
    [SerializeField] CharacterController char_controller;

    [Header("Vehicle Links")]
    [SerializeField] KettenkradStats targetKet;


    [Header("Seats / Entry Points")]
    [SerializeField] Transform driverSeat;
    [SerializeField] Transform passengerSeat;
    [SerializeField] Transform driverEntry;
    [SerializeField] Transform passengerEntry;

    [SerializeField] bool isInside;
    [SerializeField] bool isTransitioning;    

    void Start()
    {
        
    }

    private void Update()
    {

        if (InputManager.Instance.PlayerInteract())
            OnInteract();

        if (isInside && targetKet != null)
        {
            // Update Driver Animations
            Vector2 input = InputManager.Instance.VehicleMovement();
            playerAnim.SetFloat("steer", input.x, 0.1f, Time.deltaTime);

            // Constraint both to their seats
            LockToSeat(transform, driverSeat);
        }
    }

    public void LockToSeat(Transform target, Transform seat)
    {
        target.position = seat.position;
        target.rotation = seat.rotation;
    }

    private void OnInteract()
    {
        if (targetKet != null && !isInside && !isTransitioning)
            StartCoroutine(SequenceEntry());

        if (isInside && !isTransitioning)
            StartCoroutine(SequenceExit());
    }



    private IEnumerator SmoothAlign(Transform target, float duration)
    {
        float time = 0f;

        while (time < 1f)
        {          
            time += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(transform.position, target.position, time);
            transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, time);
            yield return null;
        }
    }

    private IEnumerator SequenceEntry()
    {
        isTransitioning = true;

        char_controller.enabled = false;
        char_movement.enabled = false;
        
        yield return StartCoroutine(SmoothAlign(driverEntry, 0.35f));
        
        playerAnim.SetTrigger("ket right");

        yield return StartCoroutine(SmoothAlign(driverSeat, 1.33f));

        targetKet.StartEngineSound();

        yield return new WaitForSeconds(1f);

        targetKet.StartEngine();
        
        isTransitioning = false;
        isInside = true;
    }

    private IEnumerator SequenceExit()
    {

        targetKet.StopEngine();
        
        playerAnim.SetTrigger("ket exit");

        yield return new WaitForSeconds(3.3f);


        isInside = false;
        targetKet = null;

        yield return StartCoroutine(SmoothAlign(driverEntry, 0.195f));      
   
        char_controller.enabled = true;
        char_movement.enabled = true;
    
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Vehicle") && !isInside)
        {
            targetKet = other.GetComponentInParent<KettenkradStats>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Vehicle") && !isInside)
        {
            targetKet = null;
        }
    }
}