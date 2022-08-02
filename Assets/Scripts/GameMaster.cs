using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameMaster : MonoBehaviour
{
    public Animator animator;
    [SerializeField] private Goals goals;
    [SerializeField] private GameObject endScreen;
    [SerializeField] private Text moves;
    public GameObject unitReference;
    
    public Unit[,] Units { get; private set; }
    public bool ClickAvailable { get; private set; }
    
    private int _moveCounter;
    private Vector2 _startingPosition;
    private FloodFill _floodFill;
    public PushPull PushPull;
    public Helper Helper;
    public Lookup lookup;
    
    //CACHE
    private List<Unit> _units;
    private List<Tuple<int, int>> _indices;
    private List<Vector2> _positions;
    private int _index;
    private Vector2 _vector2;

    private void Awake()
    {
        _units = new List<Unit>();
    }
    
    private void Start()
    {
        PreInitialize();
        InitializeGrid();
        CreateUnits();
    }

    private void PreInitialize()
    {
        ClickAvailable = false;

        _moveCounter = Lookup.MoveCount;
        moves.text = _moveCounter.ToString();
        Units = new Unit[Lookup.Width, Lookup.Height];
        _floodFill = new FloodFill(this);
        PushPull = new PushPull();
        Helper = new Helper(this);
        animator.Initialize(this);
        goals.Initialize(this);
    }
    
    private void InitializeGrid()
    {
        print(lookup.scaleX + ", " + lookup.scaleY);
        var rectTransform = transform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.sizeDelta *= new Vector2(Lookup.Width*lookup.scaleX, Lookup.Height*lookup.scaleY);
            rectTransform.sizeDelta += new Vector2(20, 40);
        }
        transform.position += new Vector3((520 + (-50 * Lookup.Width))*lookup.scaleX, (870 + (-50 * Lookup.Height))*lookup.scaleY, 0);
        
        _startingPosition = transform.position + new Vector3(10, 10, 0);
    }

    private void CreateUnits()
    {
        var position = _startingPosition;
        var offsetX = 100 * lookup.scaleX;
        var offsetY = 100 * lookup.scaleY;
        for (var j = 0; j < Lookup.Height; j++)
        {
            for (var i = 0; i < Lookup.Width; i++)
            {
                Units[i, j] = CreateUnitOnPosition(position + new Vector2(offsetX * i, 0), j == 0);
            }
            position.y += offsetY;
        }
    }

    public void UnitClicked(Unit clickedUnit)
    {
        SelectUnitsToBeDestroyed(clickedUnit); // modifies _units

        if (_units.Count == 0) return;
        
        ClickAvailable = false;
        animator.cubeExplode.Play();
        
        RegisterMove();
        ParseUnits();
        FallDown();
        goals.UpdateGoals(ref _units);
    }

    private void SelectUnitsToBeDestroyed(Unit clickedUnit)
    {
        _units = new List<Unit>();
        
        if (clickedUnit.Type is Lookup.UnitType.RocketX or Lookup.UnitType.RocketY)
        {
            _units.AddRange(Helper.GetUnitsOnLine(clickedUnit));
            Helper.RunRocketEffect(clickedUnit);
        }
        else
        {
            var (cubeCluster, neighboringBalloons) = _floodFill.Calculate(ref clickedUnit);

            if (cubeCluster.Count < 2) return;
            
            if (cubeCluster.Count > 4)
            {
                cubeCluster.Remove(clickedUnit);
                clickedUnit.Rocketify();
            }
            
            _units.AddRange(cubeCluster);
            _units.AddRange(neighboringBalloons);
        }
    }

    private void RegisterMove()
    {
        _moveCounter--;
        moves.text = _moveCounter.ToString();
    }

    private void ParseUnits()
    {
        _indices = new List<Tuple<int, int>>();
        foreach (var unit in _units)
        {
            var (x, y) = Helper.GetIndices(unit);
            _indices.Add(new Tuple<int, int>(x,y));
        }
        foreach (var (x, y) in _indices)
        {
            Units[x, y] = null;
        }
    }

    private void FallDown()
    {
        for (var x = 0; x < Lookup.Width; x++)
        {
            var position = _startingPosition + Vector2.right * (100* lookup.scaleX * x);
            var emptySpots = 0;
            for (var y = 0; y < Lookup.Height; y++)
            {
                if (Units[x, y] == null)
                {
                    emptySpots++;
                    continue;
                }
                if (emptySpots > 0)
                {
                    animator.AnimateUnit(Units[x,y], position);
                    Units[x, y - emptySpots] = Units[x, y];
                    Units[x, y] = null;
                }
                position += Vector2.up * (100 * lookup.scaleY);
            }

            for (_index = 0; _index < emptySpots; _index++)
            {
                var createdUnit = CreateUnitOnPosition(position);
                position += Vector2.up * (100 * lookup.scaleY);
                Units[x, _index + Lookup.Height - emptySpots] = createdUnit;
            }
        }
    }

    public void AnimationsFinished()
    {
        foreach (var unit in _units)
        {
            PushPull.PushUnit(unit);
        }
        
        DestroyDucks();

        if(goals.GoalTypeTracker.Count < 1)
            EndGame(true);
        else if (_moveCounter <= 0)
            EndGame(false);
        else
            ClickAvailable = true;
    }

    private Unit CreateUnitOnPosition(Vector2 endPosition, bool isBottom = false)
    {
        _vector2 = new Vector2(endPosition.x, endPosition.y + 100*lookup.scaleY);
        var createdUnit = Helper.GetUnit(_vector2);
        createdUnit.Initialize(this, isBottom);
        
        animator.AnimateUnit(createdUnit, endPosition);
        
        return createdUnit;
    }

    private void DestroyDucks()
    {
        _units = new List<Unit>();
        for (var i = 0; i < Lookup.Width; i++)
        {
            if (Units[i, 0].Type != Lookup.UnitType.Duck) continue;
            _units.Add(Units[i, 0]);
        }

        if (_units.Count < 1) return;
            
        animator.duckExplode.Play();
        ParseUnits();
        FallDown();
        goals.UpdateGoals(ref _units);
    }

    private void EndGame(bool won)
    {
        StopAllCoroutines();
        ClickAvailable = false;
        endScreen.transform.GetComponentInChildren<Text>().text = won ? "You Won!" : "You Lost";
        endScreen.SetActive(true);
    }

    public void Restart()
    {
        endScreen.SetActive(false);
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }
}


