using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is a bootstrapper. Nothing should run before this and almost all
/// game code should be driven through these *Update() functions.
/// </summary>

[DefaultExecutionOrder(-1001)]
public class BootSequence : MonoBehaviour
{
    Game m_Game;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        m_Game = new Game();
        m_Game.Init();
    }

    void OnDestroy()
    {
        m_Game.Shutdown();
    }

    void Update()      { m_Game.Update(); }
    void LateUpdate()  { m_Game.LateUpdate(); }
    void FixedUpdate() { m_Game.FixedUpdate(); }
}

