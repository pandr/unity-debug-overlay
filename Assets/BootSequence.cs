using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

