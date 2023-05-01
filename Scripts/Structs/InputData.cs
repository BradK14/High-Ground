using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct InputData : INetworkInput
{
    public Vector2 mousePos;
    public bool leftMouse;
    public bool up;
    public bool left;
    public bool right;
    public bool down;
    public bool enter;
    public bool shift;
    
    public InputData(bool f)
    {
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        leftMouse = f;
        up = f;
        left = f;
        right = f;
        down = f;
        enter = f;
        shift = f;
    }
}
