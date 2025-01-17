using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;
    private CapsuleCollider cc;
    private bool isGrounded = true;

    private bool isCurrentlyInAction = false;
    private bool isCrouching = false;
    [SerializeField]
    private float maxSpeed = 8;
    [SerializeField]
    private float initialSpeed = 3f;
    private float currentSpeed = 0;
    private float speedUp = 6f;
    private float desiredLane = 1; //0:left 1:middle 2:right
    public float laneDistance = 4; //the distance between two lanes
    private Vector3 velocity = Vector3.zero;
    //private Vector3 eulerAngleVelocity;
    [SerializeField]
    private float jumpForce = 0.5f;
    public Animator animator;

    public Transform airShotSpawnPoint;
    public GameObject airShotPrefab;


    private AudioSource audioSource;

    public AudioClip jumpClip;
    public AudioClip beginCrouchClip;
    public AudioClip endCrouchClip;
    public AudioClip deathClip;
    public AudioClip attackClip;


    public static event Action<bool> isGameOver;

    private EventDataHook eventDataHook;
    //private List<GyroData> gyroData = new List<GyroData>();

    //private UdpSocket udpSocket;
    //private float crouch_x = -25000;
    //private float crouch_yaw = -100;
    private float crouch_pitch = -35;

    //private float shoot_x = -25000;

    private float shoot_pitch = 100;
    private float shoot_roll = -100;

    //private float jump_x = 25000;
    private float jump_roll = 20;


    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        cc = GetComponent<CapsuleCollider>();

        //udpSocket = UdpSocket.Instance;

        if (PortDataAccessor.Instance != null && PortDataAccessor.Instance.EventDataHook != null)
        {
            eventDataHook = PortDataAccessor.Instance.EventDataHook;

            //example Serial.print("left:1;") , important: no newline
            eventDataHook.registerDataHook("Left", (object sender, DataArrivedEventArgs args) =>
            {
                runOnLane(0);
            });

            //example Serial.print("middle:1;") , important: no newline
            eventDataHook.registerDataHook("Middle", (object sender, DataArrivedEventArgs args) =>
            {
                runOnLane(1);
            });

            //example Serial.print("right:1;") , important: no newline
            eventDataHook.registerDataHook("Right", (object sender, DataArrivedEventArgs args) =>
            {
                runOnLane(2);
            });
            //
            eventDataHook.registerDataHook("IMU", (object sender, DataArrivedEventArgs args) =>
            {
                Debug.Log("[IMU] " + args.Value);
                string[] splitValues = args.Value.Split(",");
                float x = float.Parse(splitValues[0], CultureInfo.InvariantCulture.NumberFormat);
                float z = float.Parse(splitValues[1], CultureInfo.InvariantCulture.NumberFormat);
                float yaw = float.Parse(splitValues[2], CultureInfo.InvariantCulture.NumberFormat);
                float pitch = float.Parse(splitValues[3], CultureInfo.InvariantCulture.NumberFormat);
                float roll = float.Parse(splitValues[4], CultureInfo.InvariantCulture.NumberFormat);
                Debug.Log("[parsed] " + x);
                Debug.Log("[parsed] " + z);
                Debug.Log("[parsed] " + yaw);
                Debug.Log("[parsed] " + pitch);
                Debug.Log("[parsed] " + roll);
                if (crouch_pitch > pitch && InputHandler.currentPredicted == Movements.Neutral)
                {
                    //InputHandler.currentPredicted = Movements.Crouch;

                    InputHandler.lastPredicted = Movements.Neutral;
                    InputHandler.currentPredicted = Movements.Jump;
                }/*
                else if (
                    shoot_roll > roll
                    && shoot_pitch < pitch
                    )
                {
                    InputHandler.currentPredicted = Movements.Shoot;
                } else if(
                    jump_roll < roll
                ){
                    InputHandler.currentPredicted = Movements.Jump;
                }*/ else if (crouch_pitch <= pitch){
                    InputHandler.lastPredicted = InputHandler.currentPredicted;
                    InputHandler.currentPredicted = Movements.Neutral;
                }

                Debug.Log("[Prediction] " +InputHandler.currentPredicted);

                //udpSocket.SendData(args.Value);
                //gyroData.Add(new GyroData(args.Value));
            });
            /*
            eventDataHook.registerDataHook("Accel", (object sender, DataArrivedEventArgs args) =>
            {
                Debug.Log("Accel" + args.Value);
                if (Input.GetKey(KeyCode.Space))
                {
                    args.Value += ",Jump";
                } else if (Input.GetKey(KeyCode.C)) {
                    args.Value += ",Crouch";
                }else if (Input.GetKey(KeyCode.X)) {
                    args.Value += ",Shoot";
                } else {
                    args.Value += ",Neutral";
                }
                gyroData.Add(new GyroData(args.Key, Time.time, args.Value));
            });

            eventDataHook.registerDataHook("Gyro", (object sender, DataArrivedEventArgs args) =>
            {
                Debug.Log("Gyro" + args.Value);
                if (Input.GetKey(KeyCode.Space))
                {
                    args.Value += ",Jump";
                } else if (Input.GetKey(KeyCode.C)) {
                    args.Value += ",Crouch";
                }else if (Input.GetKey(KeyCode.X)) {
                    args.Value += ",Shoot";
                } else {
                    args.Value += ",Neutral";
                }
                gyroData.Add(new GyroData(args.Key, Time.time, args.Value));
            });
            */

        }

        animator.SetBool("isGameStarted", true);
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 globalpos = new Vector3(transform.position.x, transform.position.y, transform.position.z);

        if (InputHandler.PlayerRunInput())
        {
            runOnLane(InputHandler.LaneInput());
        }
        else
        {
            currentSpeed -= 0.01f;
            if (currentSpeed <= 0.01)
                currentSpeed = 0;
            //direction.z = currentSpeed*Time.deltaTime;

        }


        if (InputHandler.JumpInput())
        {
           // Debug.Log("[Grounded] " + isGrounded);
            if ((isGrounded && !isCurrentlyInAction) || (!isGrounded && PlayerManager.doubleJumpPowerUp))
            {
                StartCoroutine(Jump());
            }
        }

        animator.SetBool("isCrouching", isCrouching);
        if (InputHandler.CrouchInput())
        {
            //eulerAngleVelocity.Set(0f, 0f, 90);
            StartCoroutine(Crouch());
            isCrouching = true;
        }

        if (InputHandler.AttackInput())
        {
            StartCoroutine(Attack());
        }

        /*else
        {
            //direction.y += gravity * Time.deltaTime;
        }*/
        //controller.Move(direction);
        globalpos.z += currentSpeed;
        transform.position = Vector3.SmoothDamp(transform.position, globalpos, ref velocity, 0.3f);
        //Calculate where we should be in the future

        Vector3 targetPosition = transform.position.z * transform.forward + transform.position.y * transform.up;

        if (desiredLane == 0)
        {
            targetPosition += Vector3.left * laneDistance;
        }
        else if (desiredLane == 2)
        {
            targetPosition += Vector3.right * laneDistance;
        }

        transform.position = Vector3.Lerp(transform.position, targetPosition, 80 * Time.fixedDeltaTime);
    }

    void runOnLane(float lane)
    {
        if (currentSpeed == 0)
        {
            currentSpeed += initialSpeed;
        }
        else
        {
            if (PlayerManager.speedPowerUp)
            {
                currentSpeed = Mathf.Min(currentSpeed + speedUp, maxSpeed);
                PlayerManager.speedPowerUp = false;
            }
            else
            {
            currentSpeed = Mathf.Min(currentSpeed + 1f, maxSpeed);
            }
        }

        //direction.z = currentSpeed*Time.deltaTime;
        desiredLane = lane;
    }

    private IEnumerator Jump()
    {
        isCurrentlyInAction = true;

        animator.SetBool("isJumping", true);
        rb.velocity += jumpForce * Vector3.up;
        audioSource.PlayOneShot(jumpClip);

        // Reset double jump power-up only if used
        if (!isGrounded) 
        {
            yield return new WaitForSeconds(0f);
            PlayerManager.doubleJumpPowerUp = false;
            isGrounded = false;
            isCurrentlyInAction = false;
        } else{
            yield return new WaitForSeconds(1f);
            isGrounded = false;
            isCurrentlyInAction = false;
        }


    }

    private IEnumerator Crouch()
    {
        if (isCurrentlyInAction == false)
        {
            isCurrentlyInAction = true;
            //Aktuell gel�st durch �nderung der H�he
            audioSource.PlayOneShot(beginCrouchClip);
            cc.height = 1;
            yield return new WaitForSeconds(1.5f);
            cc.height = 2;
            isCrouching = false;
            audioSource.PlayOneShot(endCrouchClip);
            /*var normScale = new Vector3(1f, 1f, 1f);
            var crouchScale = new Vector3(0.6f, 0.5f, 0.6f);
            transform.localScale = Vector3.Lerp(transform.localScale, crouchScale, 80 * Time.fixedDeltaTime);
            yield return new WaitForSeconds(1.5f);
            transform.localScale = Vector3.Lerp(transform.localScale, normScale, 80 * Time.fixedDeltaTime);
            */
            isCurrentlyInAction = false;
        }
    }

    private void OnCollisionStay()
    {
        isGrounded = true;
    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.transform.tag == "Ground")
        {
            isGrounded = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.tag == "Obstacle"/* || collision.transform.tag == "Breakable"*/)
        {
            if (!PlayerManager.Instance.HitShield())
            {
                isGameOver?.Invoke(true);
                DisableControls();
            }

        }
        if (collision.transform.tag == "Ground")
        {
            animator.SetBool("isJumping", false);
        }
        if (collision.transform.tag == "Breakable")
        {
            PlayerManager.Instance.Crate(collision.gameObject.transform);
            Destroy(collision.gameObject);
            //Destroy(gameObject);
            //gameObject.SetActive(false);

        }
    }
    public void DisableControls()
    {
        this.enabled = false;
        audioSource.PlayOneShot(deathClip);
        //SaveSystem.SaveGyroData(gyroData);
    }

    public IEnumerator Attack()
    {
        if (isCurrentlyInAction == false)
        {
            isCurrentlyInAction = true;
            Debug.Log("[Action] Shoot");
            var airShot = Instantiate(airShotPrefab, airShotSpawnPoint.position, airShotPrefab.transform.rotation);
            airShot.GetComponent<Rigidbody>().velocity = airShotSpawnPoint.forward * 20f;
            audioSource.PlayOneShot(attackClip);
            yield return new WaitForSeconds(0.5f);
            isCurrentlyInAction = false;
        }
    }
}
