using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }    
    
    void OnEnable()
    {
        Instance = this;
    }


    public Vector2 Look()
    {
        var x = Input.GetAxisRaw("Mouse X");
        var y = Input.GetAxisRaw("Mouse Y");
        
        //Dont Normalize
        return new Vector2(x,y);
    }



    public Vector2 VehicleMovement()
    {
        var x = Input.GetAxisRaw("Horizontal");
        var y = Input.GetAxisRaw("Vertical");

        return new Vector2(x,y).normalized;
    }

    public bool VehicleBreak()
    {
        return Input.GetButton("Break");
    }

    public Vector2 PlayerMovement()
    {
        var x = Input.GetAxisRaw("Horizontal");
        var y = Input.GetAxisRaw("Vertical");

        return new Vector2(x,y).normalized;
    }

    public bool PlayerSprint()
    {
        return Input.GetButton("Sprint");
    }
    
    public bool PlayerInteract()
    {
        return Input.GetButtonDown("Interact");
    }

    public bool PlayerPause()
    {
        return Input.GetButtonDown("Pause");
    }



}

