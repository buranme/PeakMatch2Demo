using System.Collections.Generic;
using UnityEngine;

public class PushPull
{
    private Queue<Unit> _unitQueue;
    private Unit _tempUnit;
    
    public PushPull()
    {
        _unitQueue = new Queue<Unit>();
    }

    public Unit PullUnit(Vector3 unitPosition)
    {
        if (_unitQueue.Count < 1) return null;
        _tempUnit = _unitQueue.Dequeue();
        _tempUnit.transform.position = unitPosition;
        return _tempUnit;
    }

    public void PushUnit(Unit pushed)
    {
        pushed.transform.position = Lookup.Oblivion;
        _unitQueue.Enqueue(pushed);
    }
    
}