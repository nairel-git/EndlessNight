using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterVehicleAnimationEvents : MonoBehaviour
{


    //[SerializeField] private CharacterVehicleInteraction characterVehicleInteractionScript;
    //[SerializeField] private NavMeshAgentVehicleInteraction navMeshAgentVehicleInteractionScript;
    //[SerializeField] private NavMeshAgentVehicleInteraction navMeshAgentSelfVehicleInteractionScript;
    //[SerializeField] private VehicleController vehicleMovementScript;
    //  [SerializeField] private VehicleFuelManager vehicleFuelManagerScript;
    [SerializeField] private Collider characterCollider;
    [SerializeField] private AudioAnimationEvents audioAnimationEventsScript;

    //animation events
    public void EnableVehicle()
    {
        //vehicleMovementScript.playerInputScript = playerInputScript;
        //playerInputScript.actions.Vehicle.Enable();

        // reset flags for footstep sound when exiting vehicle
    }
    public void DisableVehicle()
    {
        //playerInputScript.actions.Vehicle.Disable();

    }

    public void ExitVehicle()
    {
        //characterVehicleInteractionScript.preOrientExit = true;
       // playerInputScript.actions.Player.Enable();
    }
    public void SetConstraintTrue()
    {
        //characterVehicleInteractionScript.constraint = true;
        //characterVehicleInteractionScript.changeCameraRadiusFlag1 = true;
    }
    public void SetConstraintFalse()
    {
        //characterVehicleInteractionScript.constraint = false;
        //characterVehicleInteractionScript.changeCameraRadiusFlag2 = true;
    }
    public void EnableCharacterCollider()
    {
        
    }

    public void ExitRight()
    {
        //characterVehicleInteractionScript.exitRight = true;
    }
    public void ExitLeft()
    {
        //characterVehicleInteractionScript.exitLeft = true;
    }
    public void AgentEnter()
    {
        //navMeshAgentVehicleInteractionScript.enter = true;
    }
    public void AgentExitBack()
    {
        //navMeshAgentVehicleInteractionScript.exit = true;
        //navMeshAgentVehicleInteractionScript.agentAnim.SetTrigger("ket exit");
    }
    public void AgentExitVehicle()
    {
        //navMeshAgentSelfVehicleInteractionScript.preOrientExit = true;
    }
}
