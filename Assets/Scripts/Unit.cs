using UnityEngine;
using Random = UnityEngine.Random;
using Image = UnityEngine.UI.Image;
using UnitType = Lookup.UnitType;

public class Unit : MonoBehaviour
{
    private GameMaster _gm;
    private Image _cardImage;
    public UnitType Type { get; private set; }

    private void Awake()
    {
        _cardImage = gameObject.GetComponent<Image>();
    }

    public void Initialize(GameMaster gm, bool isBottom = false, UnitType givenType = UnitType.None)
    {
        _gm = gm;
        if (givenType == UnitType.None)
            SetRandomType(isBottom);
        else
            Type = givenType;
        _cardImage.sprite = _gm.lookup.unitSprites[(int)Type];
    }

    public void DestroySelf()
    {
        if (!_gm.ClickAvailable ||
            Type is UnitType.Duck or UnitType.Balloon) return;
        _gm.UnitClicked(this);
    }

    public void Rocketify()
    {
        Type = Random.Range(0f,1f) < 0.5f ? UnitType.RocketX : UnitType.RocketY;
        _cardImage.sprite = _gm.lookup.unitSprites[(int)Type];
    }

    private void SetRandomType(bool isBottom)
    {
        var randomInt = isBottom ? Random.Range(0, _gm.lookup.cubeCount+1) : Random.Range(0, _gm.lookup.cubeCount+2);
        Type = (UnitType) randomInt;
    }
}
