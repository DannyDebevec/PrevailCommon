using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Prevail.Model;
using UnityStandardAssets.Characters.FirstPerson;

public class PlayerNetController : NetworkBehaviour
{

    public Camera cam;
    public MouseLook mouseLook = new MouseLook();


    PlayerNetCharacter character;
    [SyncVar]
    public uint character_nId;

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

    [SyncVar]
    public bool GameStarted = false;

    [SyncVar]
    public Color Color;
    [SyncVar]
    public string Name;

    public float Vertical;

    public float Horizontal;

    public bool Jump;

    public bool Fire;

    public bool Reset;

    // Use this for initialization
    void Start()
    {
        if (isLocalPlayer)
        {
            cam = Camera.main;
            mouseLook.Init(transform, cam.transform);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isLocalPlayer && GameStarted)
        {
            RotateView();

            Vertical = Input.GetAxis("Vertical");
            Horizontal = Input.GetAxis("Horizontal");
            Jump = Input.GetButton("Jump");
            Fire = Input.GetButton("Fire1");
            Reset = Input.GetKey(KeyCode.R);

            CmdInput(Vertical, Horizontal, Jump, Fire, Reset);

            if (Character != null)
            {
                Camera.main.transform.position = Character.transform.position - (Character.transform.forward * 8f) + (Character.transform.up * 3f);
            }
        }
    }

    private void RotateView()
    {
        //avoids the mouse looking if the game is effectively paused
        if (Mathf.Abs(Time.timeScale) < float.Epsilon) return;

        // get the rotation before it's changed
        float oldYRotation = transform.eulerAngles.y;

        mouseLook.LookRotation(transform, cam.transform);
    }

    [Command]
    void CmdInput(float vertical, float horizontal, bool jump, bool fire, bool reset)
    {
        Vertical = vertical;
        Horizontal = horizontal;
        Jump = jump;
        Fire = fire;
        Reset = reset;
    }


    void FixedUpdate()
    {
        if (Character != null && GameStarted)
        {
            Character.FixedUpdateInput(Vertical, Horizontal, Jump, Fire, Reset, cam.transform.rotation);
        }
    }
}

