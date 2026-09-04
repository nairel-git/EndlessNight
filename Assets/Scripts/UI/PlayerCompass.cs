using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class PlayerCompass : MonoBehaviour
{
    [SerializeField]
    private Transform _camera;
    [SerializeField]
    private RectTransform compass;

    void Start()
    {
        _camera = Camera.main.transform;
    }

    void Update()
    {
        compass.localRotation =  Quaternion.Euler(0,0,_camera.eulerAngles.y);
    }
}
