using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioAnimationEvents : MonoBehaviour
{

    public AudioSource source;


    [Header("Audio Clips")]
    [SerializeField] AudioClip MetalStepOne;
    [SerializeField] AudioClip MetalStepTwo;
    [SerializeField] AudioClip ConcreteStepOne;
    [SerializeField] AudioClip ConcreteStepTwo;



    public bool isGroundedConcrete;
    public bool isGroundedMetal;


    public void PlayAudioClipOneShot(AudioClip _audioClip)
    {
        source.PlayOneShot(_audioClip);
    }

    public void PlayFootstepOneAudioClipOneShot(AnimationEvent animEvent)
    {
        if (animEvent.animatorClipInfo.weight > 0.5f)
        {
            if (isGroundedConcrete)
                source.PlayOneShot(ConcreteStepOne);
            else if (isGroundedMetal)
                source.PlayOneShot(MetalStepOne);
        }
    }
    public void PlayFootstepTwoAudioClipOneShot(AnimationEvent animEvent)
    {
        if (animEvent.animatorClipInfo.weight > 0.5f)
        {
            if (isGroundedConcrete)
                source.PlayOneShot(ConcreteStepTwo);
            else if (isGroundedMetal)
                source.PlayOneShot(MetalStepTwo);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Metal") || other.gameObject.CompareTag("Vehicle"))
            isGroundedMetal = true;
        else
            isGroundedConcrete = true;

    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Metal") || other.gameObject.CompareTag("Vehicle"))
            isGroundedMetal = false;
        else
            isGroundedConcrete = false;
    }
}