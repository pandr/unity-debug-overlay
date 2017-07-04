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

    public float mouse_sensitivity = 50.0f;
    public float player_speed = 5.0f;
    public float jump_speed = 5.0f;

    private float vertical_speed = 0.0f;

    CharacterController cc;
    void Start()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Move around with WASD
        var move = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        move = move * player_speed * Time.deltaTime;
        move = transform.TransformDirection(move);
        cc.Move(move);

        // Fall down / gravity
        vertical_speed = vertical_speed - 10.0f * Time.deltaTime;
        if (vertical_speed < -10.0f)
            vertical_speed = -10.0f; // max fall speed
        var vertical_move = new Vector3(0, vertical_speed * Time.deltaTime, 0);
        cc.Move(vertical_move);

        // Turn player
        var turn_player = new Vector3(0, Input.GetAxisRaw("Mouse X"), 0);
        turn_player = turn_player * mouse_sensitivity * Time.deltaTime;
        transform.localEulerAngles += turn_player;

        // Camera look up/down
        var turn_cam = new Vector3(-Input.GetAxisRaw("Mouse Y"), 0, 0);
        turn_cam = turn_cam * mouse_sensitivity * Time.deltaTime;
        player_cam.transform.localEulerAngles += turn_cam;

        // Jump
        if (Input.GetKeyDown(KeyCode.Space))
        {
            vertical_speed = jump_speed;
        }
    }
}
