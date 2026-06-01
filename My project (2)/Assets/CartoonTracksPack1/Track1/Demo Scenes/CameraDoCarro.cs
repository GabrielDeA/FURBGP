using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using System;

public class CameraVolante : MonoBehaviour
{
    [Header("Mouse")]
    public float sensibilidadeMouse = 2f;
    public float limiteVertical = 80f;

    //[Header("Modo")]
    public enum ModoForcado { Auto, Mouse, VR }
    public ModoForcado modoForcado = ModoForcado.Auto;

    private float rotX = 0f;
    private float rotY = 0f;
    private bool emVR = false;

    void Start()
    {
        switch (modoForcado)
        {
            case ModoForcado.VR: emVR = true; break;
            case ModoForcado.Mouse: emVR = false; break;
            default: emVR = VerificarVR(); break;
        }

        if (!emVR)
        {
            rotY = transform.localEulerAngles.y;
            rotX = transform.localEulerAngles.x;
        }
        else
        {
            XRInputSubsystem subsystem = null;
            var subsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            if (subsystems.Count > 0)
            {
                subsystem = subsystems[0];
                subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Device);
                subsystem.TryRecenter();
            }
            
            var inputSubsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(inputSubsystems);
            foreach (var s in inputSubsystems)
                s.TrySetTrackingOriginMode(TrackingOriginModeFlags.Device);
        }

        Debug.Log("Modo câmera: " + (emVR ? "VR" : "Mouse"));
    }

    void Update()
    {
        if (!emVR) { AtualizarMouse(); return; }

        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.HeadMounted, devices);

        if (devices.Count > 0 &&
            devices[0].TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
        {
            transform.localRotation = rot;
        }
    }

    void LateUpdate()
    {
        // Em VR: não faz NADA.
        if (emVR) return;
        Debug.Log("Late  Update");
        if (Input.GetKeyDown(KeyCode.R))
        {
            rotX = 0f;
            rotY = 0f;
            transform.localRotation = Quaternion.identity;
        }
    }

    void AtualizarMouse()
    {
        if (!Input.GetMouseButton(1)) return;

        float mouseX = Input.GetAxis("Mouse X") * sensibilidadeMouse;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidadeMouse;

        rotY += mouseX;
        rotX -= mouseY;
        rotX = Mathf.Clamp(rotX, -limiteVertical, limiteVertical);

        transform.localRotation = Quaternion.Euler(rotX, rotY, 0f);
    }

    bool VerificarVR()
    {
        var displays = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);
        foreach (var d in displays)
            if (d.running) return true;
        return false;
    }
}