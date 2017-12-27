// Simple fps controller for unity
// To use, build this:
//
//   Capsule
//     Camera
//
// Put this script AND a CharacterController on the Capsule
// Link up camera to the "player_cam" property of this script
// Make sure Camera is positioned at (locally) 0, 0, 0 and with no rotation and scale 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fpscontroller : MonoBehaviour
{
    public Camera player_cam;

    public float mouse_sensitivity = 75.0f;
    public float jump_speed = 5.0f;
    public float player_friction = 8.0f;
    public float player_air_friction = 1.0f;
    public float player_accel = 100.0f;
    public float player_air_accel = 30.0f;
    public float player_speed = 7.0f;

    private Vector3 velocity = Vector3.zero;

    CharacterController cc;

    void Start()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Turn player
        var turn_player = new Vector3(0, Input.GetAxisRaw("Mouse X"), 0);
        turn_player = turn_player * mouse_sensitivity * Time.deltaTime;
        transform.localEulerAngles += turn_player;

        // Fall down / gravity
        velocity.y = velocity.y - 10.0f * Time.deltaTime;
        if (velocity.y < -10.0f)
            velocity.y = -10.0f; // max fall speed
        var vertical_move = new Vector3(0, velocity.y * Time.deltaTime, 0);
        cc.Move(vertical_move);

        bool isGrounded = cc.isGrounded;

        var friction = isGrounded ? player_friction : player_air_friction;
        var accel = isGrounded ? player_accel : player_air_accel;

        // WASD movement
        var move = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        var moveMagnitude = move.magnitude;
        if (moveMagnitude > 1.0f)
            move /= moveMagnitude;
        // Transform to world space
        move = transform.TransformDirection(move);

        // Apply friction
        var groundVelocity = new Vector3(velocity.x, 0, velocity.z);
        float g_speed = groundVelocity.magnitude;
        if (g_speed > 0)
        {
            g_speed -= Mathf.Max(g_speed, 1.0f) * friction * Time.deltaTime;
            if (g_speed < 0)
                g_speed = 0;
            groundVelocity = groundVelocity.normalized * g_speed;
        }

        // Horizontal movement
        var wantedGroundVel = move * player_speed;
        var wantedGroundDir = moveMagnitude > 0.001f ? move / moveMagnitude : Vector3.zero;
        var speedMadeGood = Vector3.Dot(groundVelocity, wantedGroundDir);
        var deltaSpeed = moveMagnitude * player_speed - speedMadeGood;
        if (deltaSpeed > 0.001)
        {
            var velAdjust = Mathf.Clamp(accel * Time.deltaTime, 0.0f, deltaSpeed) * wantedGroundDir;
            groundVelocity += velAdjust;
        }

        velocity.x = groundVelocity.x;
        velocity.z = groundVelocity.z;
        cc.Move(velocity * Time.deltaTime);

        // Jump
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            velocity.y = jump_speed;
        }

        // Camera look up/down
        var turn_cam = new Vector3(-Input.GetAxisRaw("Mouse Y"), 0, 0);
        turn_cam = turn_cam * mouse_sensitivity * Time.deltaTime;
        player_cam.transform.localEulerAngles += turn_cam;
    }
}
