using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LookupScriptableObject", menuName = "ScriptableObjects/Lookup")]
public class Lookup : ScriptableObject
{
    public enum UnitType
    {
        Cube1,
        Cube2,
        Cube3,
        Cube4,
        Cube5,
        Balloon,
        Duck,
        RocketX,
        RocketY,
        RocketRight,
        RocketLeft,
        RocketUp,
        RocketDown,
        None
    }
    public const int Width = 6;
    public const int Height = 6;
    public const int MoveCount = 8;
    public int cubeCount = 5;
    public static Vector2 Oblivion = Vector2.up * 10000;
    public List<Sprite> unitSprites;
    public float scaleX = Screen.width / 1080f;
    public float scaleY = Screen.height / 1920f;

}