using System;
using System.Collections.Generic;
using UnitType = Lookup.UnitType;

[Serializable]
public class FloodFill
{
    private GameMaster _gm;

    private int x, y, i, j;
    private UnitType _searchedType;
    private List<Unit> _cubes;
    private List<Unit> _balloons;
    private Queue<Tuple<int, int>> _queue;
    private Unit _currentUnit;

    public FloodFill(GameMaster creator)
    {
        _gm = creator;
    }
    
    public Tuple<List<Unit>, List<Unit>> Calculate(ref Unit startingUnit)
    {
        (x, y) = _gm.Helper.GetIndices(startingUnit);
        _searchedType = startingUnit.Type;
        _cubes = new List<Unit>();
        _balloons = new List<Unit>();
        _queue = new Queue<Tuple<int, int>>();
        
        _queue.Enqueue(new Tuple<int, int>(x,y));
        while (_queue.Count != 0)
        {
            (i, j) = _queue.Dequeue();
            if (j < 0 || j >= Lookup.Height || i < 0 || i >= Lookup.Width) continue;
            
            _currentUnit = _gm.Units[i, j];
            if(_cubes.Contains(_currentUnit)) continue;

            if (_currentUnit.Type == UnitType.Balloon && !_balloons.Contains(_currentUnit))
            {
                _balloons.Add(_currentUnit);
                continue;
            }
            
            if(_currentUnit.Type != _searchedType) continue;
            
            _cubes.Add(_gm.Units[i,j]);
            _queue.Enqueue(new Tuple<int, int>(i-1,j));
            _queue.Enqueue(new Tuple<int, int>(i+1,j));
            _queue.Enqueue(new Tuple<int, int>(i,j-1));
            _queue.Enqueue(new Tuple<int, int>(i,j+1));
        }

        return new Tuple<List<Unit>, List<Unit>>(_cubes, _balloons);
    }
    
}
