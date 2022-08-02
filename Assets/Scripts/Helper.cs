using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;
using UnitType = Lookup.UnitType;

public class Helper
{
    private GameMaster _gm;
    
    public Helper(GameMaster creator)
    {
        _gm = creator;
    }

    public List<Unit> GetUnitsOnLine(Unit startingUnit)
    {
        var toReturn = new List<Unit>();
        var (x, y) = GetIndices(startingUnit);
        if (startingUnit.Type == UnitType.RocketX)
        {
            for (var i = 0; i < Lookup.Width; i++)
            {
                toReturn.Add(_gm.Units[i,y]);
            }
        }
        else
        {
            for (var j = 0; j < Lookup.Height; j++)
            {
                toReturn.Add(_gm.Units[x,j]);
            }
        }

        return toReturn;
    }

    public Tuple<int, int> GetIndices(Unit toSearch)
    {
        for (var j = 0; j < Lookup.Height; ++j)
        {
            for (var i = 0; i < Lookup.Width; ++i)
            {
                if (_gm.Units[i, j].Equals(toSearch))
                {
                    return Tuple.Create(i, j);
                }
            }
        }
        return Tuple.Create(-1, -1);
    }

    public Unit GetUnit(Vector3 unitPosition)
    {
        var unit = _gm.PushPull.PullUnit(unitPosition);
        if (unit) return unit;
        
        unit = Object.Instantiate(_gm.unitReference, unitPosition, Quaternion.identity).GameObject().GetComponent<Unit>();
        unit.transform.localScale = new Vector3(_gm.lookup.scaleX,_gm.lookup.scaleY,1);
        unit.transform.SetParent(_gm.transform);
        return unit;
    }

    public void RunRocketEffect(Unit rocket)
    {
        var startingPosition = rocket.transform.position;
        var rocket1 = _gm.Helper.GetUnit(startingPosition);
        var rocket2 = _gm.Helper.GetUnit(startingPosition);
        var speed = 5f * _gm.lookup.scaleX;
        var dist = 1000 * _gm.lookup.scaleX;
        if (rocket.Type == UnitType.RocketX)
        {
            rocket1.Initialize(_gm, givenType: UnitType.RocketRight);
            _gm.animator.AnimateUnit(rocket1, startingPosition + Vector3.right*dist, pushAtTheEnd: true, speed: speed);
            
            rocket2.Initialize(_gm, givenType: UnitType.RocketLeft);
            _gm.animator.AnimateUnit(rocket2, startingPosition + Vector3.left*dist, pushAtTheEnd: true, speed: speed);
        }
        else
        {
            rocket1.Initialize(_gm, givenType: UnitType.RocketUp);
            _gm.animator.AnimateUnit(rocket1, startingPosition + Vector3.up*dist, pushAtTheEnd: true, speed: speed);
            
            rocket2.Initialize(_gm, givenType: UnitType.RocketDown);
            _gm.animator.AnimateUnit(rocket2, startingPosition + Vector3.down*dist, pushAtTheEnd: true, speed: speed);
        }
    }
}