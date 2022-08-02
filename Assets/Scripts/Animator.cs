using System;
using System.Collections.Generic;
using UnityEngine;

public class Animator : MonoBehaviour
{
    private GameMaster _gm;
    private List<Tuple<Unit, Vector3, bool, bool, float>> _unitsToBeAnimated;
    private List<Tuple<Unit, Vector3, bool, bool, float>> _unitsBeingAnimated;

    private bool _isRunningAnimation;
    private Tuple<Unit, Vector3, bool, bool, float> _cachedAnimation;
    private Vector3 _cachedVector;
    private Unit _cachedUnit;
    private Transform _cachedTransform;
    private int _cachedIndex;

    public AudioSource cubeExplode;
    public AudioSource duckExplode;
    public AudioSource goalCountIn;

    public void Initialize(GameMaster creator)
    {
        _gm = creator;
        _unitsToBeAnimated = new List<Tuple<Unit,Vector3,bool,bool,float>>();
        _unitsBeingAnimated = new List<Tuple<Unit,Vector3,bool,bool,float>>();
    }

    private void Update()
    {
        if (!_isRunningAnimation) return;
        
        _unitsBeingAnimated = new List<Tuple<Unit,Vector3,bool,bool,float>>();
        _unitsBeingAnimated.AddRange(_unitsToBeAnimated);
        
        foreach (var (unit, position, sendToOblivion, pushAtTheEnd, speed) in _unitsBeingAnimated)
        {
            _cachedTransform = unit.transform;
            _cachedVector = position - _cachedTransform.position;
            _cachedTransform.position += _cachedVector.normalized * speed;
            
            if (!(_cachedVector.magnitude < 5f)) continue;
            _cachedTransform.position = sendToOblivion ? Lookup.Oblivion : position;
            _cachedAnimation = new Tuple<Unit,Vector3,bool,bool,float>(unit, position, sendToOblivion, pushAtTheEnd, speed);
            _unitsToBeAnimated.Remove(_cachedAnimation);
            _cachedTransform.SetParent(_gm.transform);
            if (pushAtTheEnd)
            {
                _gm.PushPull.PushUnit(unit);
            }
        }
        if (_unitsToBeAnimated.Count < 1)
        {
            _isRunningAnimation = false;
            _gm.AnimationsFinished();
        }
    }

    public void AnimateUnit(Unit unit, Vector3 endPosition, bool sendToOblivion = false, bool pushAtTheEnd = false, float speed = 1)
    {
        if (!_isRunningAnimation)
        {
            _isRunningAnimation = true;
        }

        _cachedIndex = 0;
        foreach (var elem in _unitsToBeAnimated)
        {
            _cachedUnit = elem.Item1;
            _cachedVector = elem.Item2;
            if (unit == _cachedUnit)
            {
                if (!(endPosition.y < _cachedVector.y)) return;
                _unitsToBeAnimated.RemoveAt(_cachedIndex);
                break;
            }
            _cachedIndex++;
        }
        unit.transform.SetParent(transform);
        _cachedAnimation = new Tuple<Unit,Vector3,bool,bool,float>(unit, endPosition, sendToOblivion, pushAtTheEnd, speed);
        _unitsToBeAnimated.Add(_cachedAnimation);
    }
    
    public void MoveUnitInstantly(Unit movingUnit, Vector3 endPosition)
    {
        movingUnit.transform.position = endPosition;
    }
}