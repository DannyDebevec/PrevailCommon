﻿using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Prevail.Model;
using UnityStandardAssets.Characters.FirstPerson;
using System;
using UnityEngine.UI;

public class PlayerNetController : NetworkBehaviour
{
    public Camera cam;

    PlayerNetCharacter character;
    [SyncVar]
    public uint character_nId;

    [ClientRpc]
    public void RpcHurt()
    {
        Debug.Log("Hurt!");
    }

    public PlayerNetCharacter Character
    {
        get
        {
            if (isClient && character == null)
            {
                var obj = ClientScene.FindLocalObject(new NetworkInstanceId(character_nId));
                return obj == null ? null : obj.GetComponent<PlayerNetCharacter>();
            }
            return character;
        }
        set
        {
            character = value;
            character_nId = character.GetComponent<NetworkIdentity>().netId.Value;
        }
    }

    internal void RpcDie()
    {
        HPText.text = "You are dead! :(";
    }

    public bool GameStarted
    {
        get
        {
            return GameScript.Instance.Started;
        }
    }

    [SyncVar]
    public Color Color;
    [SyncVar]
    public string Name;

    public float Vertical;

    public float Horizontal;

    public bool Jump;

    public bool Fire;

    public bool Reset;

    public float Rotation;

    public bool offline = false;

    public Text HPText;

    public bool IsLocalPlayer
    {
        get
        {
            return offline || base.isLocalPlayer;
        }
    }

    bool MouseLookInit = false;

    // Use this for initialization
    void Start()
    {
        if (IsLocalPlayer)
        {
            cam = Camera.main;
            HPText = GameObject.FindGameObjectWithTag("HPText").GetComponent<Text>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (IsLocalPlayer)
        {
            RotateView();

            Vertical = Input.GetAxis("Vertical");
            Horizontal = Input.GetAxis("Horizontal");
            Jump = Input.GetButton("Jump");
            Fire = Input.GetButton("Fire1");
            Reset = Input.GetKey(KeyCode.R);
            Rotation = Camera.main.transform.rotation.eulerAngles.y;

            InternalLockUpdate();

            if (Character != null)
            {
                Camera.main.transform.position = Character.transform.position + (Vector3.up * 0.08f);
                HPText.text = "HP: " + Character.Health + "/" + Character.MaxHealth;
            }

            if (!offline && GameStarted)
            {
                CmdInput(Vertical, Horizontal, Jump, Fire, Reset, Rotation);
            }

        }
    }

    bool m_cursorIsLocked;

    private void InternalLockUpdate()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            m_cursorIsLocked = false;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            m_cursorIsLocked = true;
        }

        if (m_cursorIsLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (!m_cursorIsLocked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    float rotX, rotY;
    float mouseSensitivity = 10f;

    private void RotateView()
    {
        rotX = cam.transform.rotation.eulerAngles.y;
        rotY = cam.transform.rotation.eulerAngles.x;

        rotX = rotX + Input.GetAxis("Mouse X") * mouseSensitivity;
        rotY = ClampAngle(rotY - (Input.GetAxis("Mouse Y") * mouseSensitivity), -80f, 80f);

        cam.transform.rotation = Quaternion.Euler(new Vector3(rotY, rotX, 0));
    }

    [Command]
    void CmdInput(float vertical, float horizontal, bool jump, bool fire, bool reset, float rotation)
    {
        Vertical = vertical;
        Horizontal = horizontal;
        Jump = jump;
        Fire = fire;
        Reset = reset;
        Rotation = rotation;
    }


    void FixedUpdate()
    {
        if (Character != null && GameStarted)
        {
            Character.FixedUpdateInput(Vertical, Horizontal, Jump, Fire, Reset, Rotation);
        }
    }

    float ClampAngle(float angle, float min, float max)
    {
        if (angle < 90 || angle > 270)
        {       // if angle in the critic region...
            if (angle > 180) angle -= 360;  // convert all angles to -180..+180
            if (max > 180) max -= 360;
            if (min > 180) min -= 360;
        }
        angle = Mathf.Clamp(angle, min, max);
        if (angle < 0) angle += 360;  // if angle negative, convert to 0..360
        return angle;
    }
}

