using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    CharacterController controller;

    public float gravity = 9.81f;

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
        else
        {
            motion.y -= gravity * Time.deltaTime;
        }

        transform.eulerAngles = new Vector3(0, yaw, 0);
        cam.transform.eulerAngles = new Vector3(pitch, yaw, 0);

        controller.Move((motion.x * transform.right + motion.y * Vector3.up + motion.z * transform.forward) * Time.deltaTime);

        if (Input.GetButton("Fire1"))
        {
            DestroyBlock();
        }

        if (Input.GetButton("Fire2"))
        {
            PlaceBlock();
        }
    }

    private bool PlaceBlock()
    {
        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, interactRange)) return false;
        if (hit.point.y <= 0) return false;
        if (hit.point.y >= Chunk.HEIGHT) return false;
        return TerrainGenerator.Instance.PlaceBlock(hit.point + hit.normal * 0.01f);
    }

    private bool DestroyBlock()
    {
        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, interactRange)) return false;
        return TerrainGenerator.Instance.DestroyBlock(hit.point - hit.normal * 0.01f);
    }

    private void OnDrawGizmosSelected()
    {
        if (!cam) cam = GetComponentInChildren<Camera>();
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(cam.transform.position, cam.transform.position + cam.transform.forward * interactRange);
    }


}
