using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Prevail.Model;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class PlayerNetCharacter : NetworkBehaviour
{
    [SyncVar]
    public float Health = 100f;
    [SyncVar]
    public float MaxHealth = 100f;

    public float ImpactMinDamage = 8f;
    public float ImpactMaxDamage = 50f;
    public float ImpactDamageMultiplier = 1f;

    public void OnCollisionEnter(Collision col)
    {
        Debug.Log("collision");
        if (isServer)
        {

            var dmg = Mathf.Round(Mathf.Clamp(col.relativeVelocity.magnitude * ImpactDamageMultiplier, 0f, ImpactMaxDamage));

            Debug.Log("Hurt: " + col.relativeVelocity.magnitude + ", " + dmg);

            if (dmg <= ImpactMinDamage)
            {
                return;
            }

            Health = Mathf.Clamp(Health - dmg, 0f, 100f);


            if (Health <= 0)
            {
                Controller.RpcDie();
                Destroy(this.gameObject);
            }
            else
            {
                Controller.RpcHurt();
            }
        }
    }

    public float MaxVelocity = 1f;
    public float MassMultiplier = 50f;

    PlayerNetController controller;
    [SyncVar]
    uint controller_nId;
    public PlayerNetController Controller
    {
        get
        {
            if (isClient && controller == null)
            {
                var obj = ClientScene.FindLocalObject(new NetworkInstanceId(controller_nId));
                return obj == null ? null : obj.GetComponent<PlayerNetController>();
            }
            return controller;
        }
        set
        {
            controller = value;
            controller_nId = controller.GetComponent<NetworkIdentity>().netId.Value;
        }
    }

    private Rigidbody m_RigidBody;
    private Renderer m_renderer;

    // Use this for initialization
    void Start()
    {
        m_Capsule = GetComponent<CapsuleCollider>();
        m_RigidBody = GetComponent<Rigidbody>();
        m_renderer = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Controller != null)
        {
            if (m_renderer.material.color != Controller.Color)
            {
                m_renderer.material.color = Controller.Color;
            }
            if (name != Controller.Name)
            {
                name = Controller.Name;
            }
        }
    }

    public float JumpForce = 30f;

    public void FixedUpdateInput(float vertical, float horizontal, bool jump, bool fire, bool reset, float rotation)
    {
        GroundCheck();

        if (reset)
        {
            m_RigidBody.MovePosition(Vector3.zero + Vector3.up * 2f);
            m_RigidBody.velocity = Vector3.zero;
        }
        m_RigidBody.MoveRotation(Quaternion.Euler(new Vector3(0, rotation, 0)));

        var localVelociy = transform.InverseTransformDirection(m_RigidBody.velocity);

        if (Mathf.Abs(localVelociy.x) < MaxVelocity || (localVelociy.x < 0 && horizontal > 0) || (localVelociy.x > 0 && horizontal < 0))
        {
            m_RigidBody.AddForce(MassMultiplier * (transform.right * horizontal));
        }
        if (Mathf.Abs(localVelociy.z) < MaxVelocity || (localVelociy.z < 0 && horizontal > 0) || (localVelociy.z > 0 && horizontal < 0))
        {
            m_RigidBody.AddForce(MassMultiplier * (transform.forward * vertical));
        }
        if (horizontal == 0 && vertical == 0)
        {
            m_RigidBody.velocity = new Vector3(m_RigidBody.velocity.x * 0.9f, m_RigidBody.velocity.y, m_RigidBody.velocity.z * 0.9f);
        }

        if (m_IsGrounded)
        {
            m_RigidBody.drag = 1f;

            if (jump)
            {
                m_RigidBody.drag = 0f;
                m_RigidBody.velocity = new Vector3(m_RigidBody.velocity.x, 0f, m_RigidBody.velocity.z);
                m_RigidBody.AddForce(new Vector3(0f, JumpForce, 0f), ForceMode.Impulse);
                m_Jumping = true;
            }
        }
        else
        {
            m_RigidBody.drag = 0f;
            if (m_PreviouslyGrounded && !m_Jumping)
            {
                StickToGroundHelper();
            }
        }
        
        var pos = m_RigidBody.position;
        pos.x = Mathf.Clamp(pos.x, -22f, 22f);
        pos.z = Mathf.Clamp(pos.z, -22f, 22f);
        m_RigidBody.position = pos;
    }

    private CapsuleCollider m_Capsule;
    private Vector3 m_GroundContactNormal;
    private bool m_PreviouslyGrounded, m_Jumping, m_IsGrounded;
    public float shellOffset = 0.1f;
    public float groundCheckDistance = 0.01f;

    /// sphere cast down just beyond the bottom of the capsule to see if the capsule is colliding round the bottom
    private void GroundCheck()
    {
        m_PreviouslyGrounded = m_IsGrounded;
        RaycastHit hitInfo;

        var origin = (transform.position + Vector3.up * -0.05f);
        var size = 0.1f * m_Capsule.radius * (1.0f - shellOffset);
        var direction = Vector3.down;
        var maxDistance = 0.1f * ((m_Capsule.height) - m_Capsule.radius) + groundCheckDistance;

        Debug.DrawLine(origin, origin + direction * maxDistance, Color.blue);
        Debug.DrawLine(origin, origin + direction * size, Color.red);

        if (Physics.SphereCast(origin, size, direction, out hitInfo, maxDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            m_IsGrounded = true;
            m_GroundContactNormal = hitInfo.normal;
        }
        else
        {
            m_IsGrounded = false;
            m_GroundContactNormal = Vector3.up;
        }
        if (!m_PreviouslyGrounded && m_IsGrounded && m_Jumping)
        {
            m_Jumping = false;
        }
    }

    public float stickToGroundHelperDistance = 0.5f; // stops the character

    private void StickToGroundHelper()
    {
        RaycastHit hitInfo;
        if (Physics.SphereCast(transform.position, m_Capsule.radius * (1.0f - shellOffset), Vector3.down, out hitInfo,
                               ((m_Capsule.height / 2f) - m_Capsule.radius) +
                               stickToGroundHelperDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            if (Mathf.Abs(Vector3.Angle(hitInfo.normal, Vector3.up)) < 85f)
            {
                m_RigidBody.velocity = Vector3.ProjectOnPlane(m_RigidBody.velocity, hitInfo.normal);
            }
        }
    }
}

