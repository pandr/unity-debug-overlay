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
using UnityEngine.InputSystem;

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

    private float cam_yaw = 0.0f;
    private Vector3 velocity = Vector3.zero;

    CharacterController cc;

    void Start()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        if(Game.console.IsOpen())
            return;

        // Turn player
        var turn_player = new Vector3(0, Mouse.current.delta.x.value, 0);

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
        float horizontal = Keyboard.current.aKey.isPressed ? -1.0f : 0.0f;
        horizontal += Keyboard.current.dKey.isPressed ? 1.0f : 0.0f;
        float vertical = Keyboard.current.sKey.isPressed ? -1.0f : 0.0f;
        vertical += Keyboard.current.wKey.isPressed ? 1.0f : 0.0f;
        var move = new Vector3(horizontal, 0, vertical);
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
        if (isGrounded && Keyboard.current.spaceKey.isPressed)
        {
            velocity.y = jump_speed;
        }

        // Camera look up/down
        cam_yaw += -Mouse.current.delta.y.value * mouse_sensitivity * Time.deltaTime;
        cam_yaw = Mathf.Clamp(cam_yaw, -70.0f, 70.0f);
        player_cam.transform.localEulerAngles = new Vector3(cam_yaw, 0, 0);
    }
}
