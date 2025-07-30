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

    // Config variables for player settings
    static ConfigVarFloat s_MouseSensitivity = new ConfigVarFloat("mousesensitivity", 75.0f, "Mouse sensitivity");
    static ConfigVarFloat s_Fov = new ConfigVarFloat("fov", 70.0f, "Field of view");
    static ConfigVarFloat s_JumpSpeed = new ConfigVarFloat("jumpspeed", 5.0f, "Jump speed");
    static ConfigVarFloat s_PlayerFriction = new ConfigVarFloat("playerfriction", 8.0f, "Player ground friction");
    static ConfigVarFloat s_PlayerAirFriction = new ConfigVarFloat("playerairfriction", 1.0f, "Player air friction");
    static ConfigVarFloat s_PlayerAccel = new ConfigVarFloat("playeraccel", 100.0f, "Player ground acceleration");
    static ConfigVarFloat s_PlayerAirAccel = new ConfigVarFloat("playerairaccel", 30.0f, "Player air acceleration");
    static ConfigVarFloat s_PlayerSpeed = new ConfigVarFloat("playerspeed", 7.0f, "Player movement speed");

    private float cam_yaw = 0.0f;
    private Vector3 velocity = Vector3.zero;

    CharacterController cc;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        
        // Register config variables
        ConfigVarRegistry.Register(ref s_MouseSensitivity);
        ConfigVarRegistry.Register(ref s_Fov);
        ConfigVarRegistry.Register(ref s_JumpSpeed);
        ConfigVarRegistry.Register(ref s_PlayerFriction);
        ConfigVarRegistry.Register(ref s_PlayerAirFriction);
        ConfigVarRegistry.Register(ref s_PlayerAccel);
        ConfigVarRegistry.Register(ref s_PlayerAirAccel);
        ConfigVarRegistry.Register(ref s_PlayerSpeed);
    }

    void Update()
    {
        if(Game.console.IsOpen())
            return;

        // Turn player
        var turn_player = new Vector3(0, Mouse.current.delta.x.value, 0);

        turn_player = turn_player * s_MouseSensitivity.Value * Time.deltaTime;
        transform.localEulerAngles += turn_player;

        // Fall down / gravity
        velocity.y = velocity.y - 10.0f * Time.deltaTime;
        if (velocity.y < -10.0f)
            velocity.y = -10.0f; // max fall speed
        var vertical_move = new Vector3(0, velocity.y * Time.deltaTime, 0);
        cc.Move(vertical_move);

        bool isGrounded = cc.isGrounded;

        var friction = isGrounded ? s_PlayerFriction.Value : s_PlayerAirFriction.Value;
        var accel = isGrounded ? s_PlayerAccel.Value : s_PlayerAirAccel.Value;

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
        var wantedGroundVel = move * s_PlayerSpeed.Value;
        var wantedGroundDir = moveMagnitude > 0.001f ? move / moveMagnitude : Vector3.zero;
        var speedMadeGood = Vector3.Dot(groundVelocity, wantedGroundDir);
        var deltaSpeed = moveMagnitude * s_PlayerSpeed.Value - speedMadeGood;
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
            velocity.y = s_JumpSpeed.Value;
        }

        // Camera look up/down
        cam_yaw += -Mouse.current.delta.y.value * s_MouseSensitivity.Value * Time.deltaTime;
        cam_yaw = Mathf.Clamp(cam_yaw, -70.0f, 70.0f);
        player_cam.transform.localEulerAngles = new Vector3(cam_yaw, 0, 0);
        
        // Apply FOV from config
        if (player_cam != null)
        {
            player_cam.fieldOfView = s_Fov.Value;
        }
    }
}
