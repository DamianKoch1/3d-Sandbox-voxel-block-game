using System;
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

    private Block targetBlock;

    public Block TargetBlock
    {
        private set
        {
            targetBlock = value;
            HighlightTarget();
        }
        get => targetBlock;
    }

    private LineRenderer lr;

    private RaycastHit interactHit;

    [SerializeField]
    private float maxPlaceCD = 0.2f;

    [SerializeField]
    private float maxBreakCD = 0.2f;

    float placeCD = 0;

    float breakCD = 0;

    private Fluid fluid;

    public void Initialize()
    {
        controller = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>();
        lr = GetComponent<LineRenderer>();
        motion = Vector3.zero;
        pitch = 0;
        yaw = 0;
        Cursor.lockState = CursorLockMode.Locked;
        TargetBlock = null;
        if (Physics.Raycast(transform.position - Vector3.up * 2, -Vector3.up, out var hit, Chunk.HEIGHT))
        {
            transform.position = hit.point + Vector3.up;
        }
    }

    private void Update()
    {
        if (!controller) return;

        CheckIfSwimming();

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
            TargetBlock = TerrainGenerator.Instance.GetBlock(interactHit.point - interactHit.normal * 0.01f);

            if (Input.GetButton("Fire1"))
            {
                Attack();
            }

            if (Input.GetButtonDown("Fire2"))
            {
                Use();
            }
            else if (Input.GetButton("Fire2"))
            {
                PlaceBlock();
            }
        }
        else
        {
            TargetBlock = null;
        }
    }

    private void CheckIfSwimming()
    {
        //TODO add Block.IsInside(Vector3 offset (0-1)) for eg water
        var headBlock = TerrainGenerator.Instance.GetBlock(cam.transform.position + Vector3.up * 0.2f);
        if (headBlock is Fluid)
        {
            fluid = (Fluid)headBlock;
            RenderSettings.fog = true;
        }
        else
        {
            RenderSettings.fog = false;
            var footBlock = TerrainGenerator.Instance.GetBlock(transform.position - Vector3.up * 0.4f);
            if (footBlock is Fluid)
            {
                fluid = (Fluid)footBlock;

            }
            else
            {
                fluid = null;
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

        if (fluid != null)
        {
            Swim();
        }
        else if (controller.isGrounded)
        {
            if (Input.GetButton("Jump"))
            {
                motion.y = jumpStrength;
            }
            else
            {
                motion.y = controller.velocity.y - gravity * Time.deltaTime;
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

    private void Swim()
    {
        if (Input.GetButton("Jump"))
        {
            motion.y = Mathf.Min(fluid.SinkSpeed, motion.y + 4 * gravity * Time.deltaTime);
        }
        else 
        {
            motion.y = Mathf.Lerp(motion.y, -fluid.SinkSpeed, Time.deltaTime);
        }
    }

    private void Attack()
    {
        if (targetBlock == null) return;
        DestroyBlock();
    }

    private void Use()
    {
        if (placeCD > 0) return;
        if (targetBlock == null) return;
        if (!(targetBlock is IUseable)) return;
        if (Input.GetButton("Fire3")) return;
        ((IUseable)targetBlock).OnUsed();
        placeCD = maxPlaceCD;
    }

    private bool PlaceBlock()
    {
        if (placeCD > 0) return false;
        if (targetBlock is IUseable)
        {
            if (!Input.GetButton("Fire3")) return false;
        }
        if (TerrainGenerator.Instance.PlaceBlock(Hotbar.Instance.GetSelected(), interactHit.point + interactHit.normal * 0.01f) != null)
        {
            placeCD = maxPlaceCD;
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
            breakCD = maxBreakCD;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Highlights targeted block face using a line renderer
    /// </summary>
    private void HighlightTarget()
    {
        if (targetBlock == null)
        {
            lr.positionCount = 0;
        }
        else
        {
            lr.positionCount = 4;
            Vector3Int dir1 = Vector3Int.zero;
            Vector3Int dir2 = Vector3Int.zero;

            //fixing float rounding errors that would sometimes floor near whole floats one int too much down
            var blockPos = Vector3Int.FloorToInt(interactHit.point + Vector3.one * 0.00001f);

            if (interactHit.normal.x != 0)
            {
                dir1 = new Vector3Int(0, 0, 1);
                dir2 = Vector3Int.up;
            }
            else if (interactHit.normal.y != 0)
            {
                dir1 = new Vector3Int(0, 0, 1);
                dir2 = Vector3Int.right;
            }
            else if (interactHit.normal.z != 0)
            {
                dir1 = Vector3Int.up;
                dir2 = Vector3Int.right;
            }

            lr.SetPositions(new Vector3[]
            {
                blockPos,
                blockPos + dir1,
                blockPos + dir1 + dir2,
                blockPos + dir2
            });
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!cam) cam = GetComponentInChildren<Camera>();
        Gizmos.color = Color.blue;
        if (targetBlock != null)
        {
            Gizmos.DrawLine(cam.transform.position, interactHit.point);
            Gizmos.DrawWireCube(targetBlock.Pos + Vector3.one * 0.5f, Vector3.one);
        }
        else
        {
            Gizmos.DrawLine(cam.transform.position, cam.transform.position + cam.transform.forward * interactRange);
        }
    }
}
