using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    CharacterController controller;

    public float gravity = 9.81f;
    private float swimGravity = 0;

    public float speed;

    public float sprintSpeed;

    public float jumpStrength;

    private Vector3 motion;

    float yaw;
    float pitch;

    public float camSpeedH = 2.0f;

    public float camSpeedV = 2.0f;

    public float interactRange;

    private Camera cam;

    private Block targetBlock;

    private RaycastHit interactHit;

    float placeCD = 0;

    float breakCD = 0;

    private Fluid fluid;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>();
        motion = Vector3.zero;
        pitch = 0;
        yaw = 0;
        Cursor.lockState = CursorLockMode.Locked;

        if (Physics.Raycast(transform.position - Vector3.up * 2, -Vector3.up, out var hit, Chunk.SIZE))
        {
            transform.position = hit.point + Vector3.up;
        }
    }

    private void Update()
    {
        var footBlock = TerrainGenerator.Instance.GetBlock(transform.position - Vector3.up * 0.4f);
        if (footBlock is Fluid)
        {
            fluid = (Fluid)footBlock;
        }
        else
        {
            fluid = null;
        }

        Movement();

        if (breakCD > 0)
        {
            breakCD = Mathf.Max(0, breakCD - Time.deltaTime);
        }

        if (placeCD > 0)
        {
            placeCD = Mathf.Max(0, placeCD - Time.deltaTime);
        }


        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out interactHit, interactRange))
        {
            targetBlock = TerrainGenerator.Instance.GetBlock(interactHit.point - interactHit.normal * 0.01f);

            if (Input.GetButton("Fire1"))
            {
                DestroyBlock();
            }

            if (Input.GetButton("Fire2"))
            {
                PlaceBlock();
            }
        }
    }

    private void Movement()
    {
        var input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized;

        if (Input.GetButton("Fire3"))
        {
            input *= sprintSpeed;
        }
        else
        {
            input *= speed;
        }

        yaw += camSpeedH * Input.GetAxis("Mouse X");
        pitch -= camSpeedV * Input.GetAxis("Mouse Y");

        pitch = Mathf.Clamp(pitch, -90, 90);

        motion.x = input.x;
        motion.z = input.y;

        if (controller.isGrounded)
        {
            if (Input.GetButton("Jump"))
            {
                motion.y = jumpStrength;
            }
            else
            {
                motion.y = 0;
            }
        }
        else if (fluid != null)
        {
            if (Input.GetButton("Jump"))
            {
                motion.y = fluid.fallSpeed;
            }
            else if (motion.y > -fluid.fallSpeed)
            {
                motion.y = Mathf.Max(-fluid.fallSpeed, motion.y - gravity * Time.deltaTime);
            }
        }
        else
        {
            motion.y -= gravity * Time.deltaTime;
        }

        transform.eulerAngles = new Vector3(0, yaw, 0);
        cam.transform.eulerAngles = new Vector3(pitch, yaw, 0);

        controller.Move((motion.x * transform.right + motion.y * Vector3.up + motion.z * transform.forward) * Time.deltaTime);
    }

    private bool PlaceBlock()
    {
        if (placeCD > 0) return false;
        if (TerrainGenerator.Instance.PlaceBlock(interactHit.point + interactHit.normal * 0.01f))
        {
            placeCD = 0.1f;
            return true;
        }
        return false;
    }

    private bool DestroyBlock()
    {
        if (breakCD > 0) return false;
        if (targetBlock == null) return false;
        if (TerrainGenerator.Instance.DestroyBlock(interactHit.point - interactHit.normal * 0.01f))
        {
            breakCD = 0.1f;
            return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!cam) cam = GetComponentInChildren<Camera>();
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(cam.transform.position, cam.transform.position + cam.transform.forward * interactRange);
    }


}
