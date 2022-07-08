using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Grid : MonoBehaviour
{
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private int givenMoveCount;
    [SerializeField] private List<string> goalTypes;
    [SerializeField] private List<int> goalCounts;
    
    [SerializeField] private GameObject goalsPanel;
    [SerializeField] private GameObject endScreen;
    [SerializeField] private GameObject errorScreen;
    [SerializeField] private Text errorText;
    [SerializeField] private Text moves;
    [SerializeField] private List<Text> goalCountTexts;
    [SerializeField] private GameObject unitReference;
    [SerializeField] private GameObject goalReference;
    [SerializeField] private AudioSource cubeExplode;
    [SerializeField] private AudioSource duckExplode;
    [SerializeField] private AudioSource goalCountIn;
    
    private Unit[,] _units;
    private int _moveCounter;
    private List<string> _goalTypeTracker;
    private Vector2 _startingPosition;
    private int _globalAnimationStepCount;
    private float _animationTime;
    private Vector3 _oblivion;
    
    private void Start()
    {
        if (!AreGivenGoalsLegal()) return;
        width = Math.Clamp(width, 1, 10);
        height = Math.Clamp(height, 1, 12);
        
        _globalAnimationStepCount = 50;
        _animationTime = 0.005f;
        _oblivion = new Vector3(10000, 0, 0);

        PreInitialize();
        InitializeGrid();
    }

    private void PreInitialize()
    {
        PlayerPrefs.SetInt("Enabled", 0);

        _moveCounter = givenMoveCount;
        moves.text = _moveCounter.ToString();
        _units = new Unit[width, height];
        InitializeGoals();
    }
    
    private void InitializeGrid()
    {
        var rectTransform = transform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.sizeDelta *= new Vector2(width, height);
            rectTransform.sizeDelta += new Vector2(20, 40);
        }
        transform.position += new Vector3(-50 * width, -50 * height, 0);

        _startingPosition = transform.position + new Vector3(10, 10, 0);
        
        CreateUnits();
    }

    private void CreateUnits()
    {
        var unitPosition = _startingPosition;
        for (var j = 0; j < height; j++)
        {
            for (var i = 0; i < width; i++)
            {
                _units[i, j] = CreateUnitOnPosition(unitPosition + new Vector2(100 * i, 0), j == 0);
            }
            unitPosition.y += 100;
        }
        PlayerPrefs.SetInt("Enabled", 1);
    }

    public IEnumerator UnitClicked(Unit clickedUnit)
    {
        var unitsToBeDestroyed = SelectUnitsToBeDestroyed(clickedUnit);
        
        PlayerPrefs.SetInt("Enabled", 0);
        RegisterMove();
        
        yield return StartCoroutine(ParseUnits(unitsToBeDestroyed));
        cubeExplode.Play();
        
        yield return StartCoroutine(UpdateGoals(unitsToBeDestroyed));
        yield return new WaitForSeconds(1f);
        
        yield return StartCoroutine(DestroyDucks());

        if(_goalTypeTracker.Count < 1)
            EndGame(true);
        else if (_moveCounter <= 0)
            EndGame(false);
        else
            PlayerPrefs.SetInt("Enabled", 1);

        foreach (var unit in unitsToBeDestroyed)
        {
            Destroy(unit.gameObject);
        }
    }

    private List<Unit> SelectUnitsToBeDestroyed(Unit clickedUnit)
    {
        var unitsToBeDestroyed = new List<Unit>();
        
        if (clickedUnit.Type is "rocket_x" or "rocket_y")
        {
            unitsToBeDestroyed.AddRange(GetUnitsOnLine(clickedUnit));
            StartCoroutine(RunRocketEffect(clickedUnit));
        }
        else
        {
            var (cubeCluster, neighboringBalloons) = BfsFloodFill(clickedUnit);

            if (cubeCluster.Count < 2) return unitsToBeDestroyed;
            
            if (cubeCluster.Count > 4)
            {
                cubeCluster.Remove(clickedUnit);
                clickedUnit.Rocketify();
            }
            
            unitsToBeDestroyed.AddRange(cubeCluster);
            unitsToBeDestroyed.AddRange(neighboringBalloons);
        }

        return unitsToBeDestroyed;

    }

    private IEnumerator UpdateGoals(List<Unit> toBeUpdated)
    {
        foreach (var unit in toBeUpdated)
        {
            var index = _goalTypeTracker.IndexOf(unit.Type);
            if (index == -1)
            {
                MoveUnitInstantly(unit, _oblivion);
                continue;
            }

            var currentGoalCount = int.Parse(goalCountTexts[index].text);
            if (currentGoalCount > 0)
            {
                goalCountTexts[index].text = (--currentGoalCount).ToString();
                StartCoroutine(MoveUnitGradually(unit, goalsPanel.transform.position, true));
                yield return new WaitForSeconds(0.5f);
                goalCountIn.Play();
            }
            else
            {
                MoveUnitInstantly(unit, _oblivion);
            }

            if (currentGoalCount != 0) continue;
            _goalTypeTracker.RemoveAt(index);
            goalCountTexts.RemoveAt(index);
        }

        yield break;
    }

    private IEnumerator ParseUnits(List<Unit> toBeDestroyed)
    {
        var indices = new List<Tuple<int, int>>();
        var positions = new List<Vector2>();
        foreach (var unit in toBeDestroyed)
        {
            var (x, y) = GetIndices(unit);
            indices.Add(new Tuple<int, int>(x,y));
        }

        indices = indices.OrderByDescending(t => t.Item2).ToList();
        foreach (var (x,y) in indices)
        {
            positions.Add(_units[x, y].transform.position);
        }

        yield return StartCoroutine(FallDown(indices, positions));
    }

    private IEnumerator FallDown(IEnumerable<Tuple<int, int>> indices, IReadOnlyList<Vector2> positions)
    {
        foreach (var ((x,y), i) in indices.Select((value, i) => ( value, i )))
        {
            var fallPosition = positions[i];
            for (var j = y+1; j < height; j++)
            {
                var currentUnit = _units[x, j];
                _units[x, j - 1] = currentUnit;
                StartCoroutine(MoveUnitGradually(currentUnit, fallPosition));
                fallPosition += new Vector2(0, 100);
            }
            _units[x, height - 1] = CreateUnitOnPosition(fallPosition);
        }
        yield break;
    }

    private Unit CreateUnitOnPosition(Vector2 endPosition, bool isBottom = false)
    {
        var startPosition = new Vector2(endPosition.x, endPosition.y + 100);
        var createdUnit = Instantiate(unitReference, startPosition, Quaternion.identity).GetComponent<Unit>();
        createdUnit.transform.SetParent(transform);
        createdUnit.Initialize(this, isBottom);
        
        StartCoroutine(MoveUnitGradually(createdUnit, endPosition));
        StartCoroutine(createdUnit.FadeIn(_animationTime));
        
        return createdUnit;
    }

    private IEnumerator DestroyDucks()
    {
        while (true)
        {
            var ducksFound = new List<Unit>();
            for (var i = 0; i < width; i++)
            {
                if (_units[i, 0].Type != "duck") continue;
                
                ducksFound.Add(_units[i, 0]);
            }

            if (ducksFound.Count < 1) yield break;
            
            yield return StartCoroutine(ParseUnits(ducksFound));
            duckExplode.Play();
            yield return StartCoroutine(UpdateGoals(ducksFound));
            yield return new WaitForSeconds(1);
            
            foreach (var duck in ducksFound)
            {
                Destroy(duck.gameObject);
            }
        }
    }

    private List<Unit> GetUnitsOnLine(Unit startingUnit)
    {
        var toReturn = new List<Unit>();
        var (x, y) = GetIndices(startingUnit);
        if (startingUnit.Type == "rocket_x")
        {
            for (var i = 0; i < width; i++)
            {
                toReturn.Add(_units[i,y]);
            }
        }
        else
        {
            for (var j = 0; j < height; j++)
            {
                toReturn.Add(_units[x,j]);
            }
        }

        return toReturn;
    }

    private Tuple<int, int> GetIndices(Unit toSearch)
    {
        for (var j = 0; j < height; ++j)
        {
            for (var i = 0; i < width; ++i)
            {
                if (_units[i, j].Equals(toSearch))
                {
                    return Tuple.Create(i, j);
                }
            }
        }
        return Tuple.Create(-1, -1);
    }

    private Tuple<List<Unit>, List<Unit>> BfsFloodFill(Unit startingUnit)
    {
        var (x, y) = GetIndices(startingUnit);
        var searchedType = startingUnit.Type;
        var cubes = new List<Unit>();
        var balloons = new List<Unit>();

        var queue = new Queue<Tuple<int, int>>();
        queue.Enqueue(new Tuple<int, int>(x,y));
        while (queue.Count != 0)
        {
            var (i, j) = queue.Dequeue();
            if (j < 0 || j >= height || i < 0 || i >= width) continue;
            
            var currentUnit = _units[i, j];
            if(cubes.Contains(currentUnit)) continue;

            if (currentUnit.Type == "balloon" && !balloons.Contains(currentUnit))
            {
                balloons.Add(currentUnit);
                continue;
            }
            
            if(currentUnit.Type != searchedType) continue;
            
            cubes.Add(_units[i,j]);
            queue.Enqueue(new Tuple<int, int>(i-1,j));
            queue.Enqueue(new Tuple<int, int>(i+1,j));
            queue.Enqueue(new Tuple<int, int>(i,j-1));
            queue.Enqueue(new Tuple<int, int>(i,j+1));
        }

        return new Tuple<List<Unit>, List<Unit>>(cubes, balloons);
    }

    private bool AreGivenGoalsLegal()
    {
        var message = "";
        foreach (var count in goalCounts)
        {
            if (count is < 1 or > 100)
                message = "Please make sure the goal count is between 1 and 100";
        }
        foreach (var type in goalTypes)
        {
            if (type is "balloon" or "duck" or "cube_1" or "cube_2" or "cube_3" or "cube_4" or "cube_5") continue;
            message = "Please use one of the following goal types: balloon, duck, cube_1, cube_2, cube_3, cube_4, cube_5";
        }
        if (goalCounts.Count != goalTypes.Count)
            message = "Please make sure you input equal quantity of goal counts and types.";
        if (goalCounts.Count == 0 || goalTypes.Count == 0)
            message = "Please make sure you input goal counts and types.";
        if (goalCounts.Count > 3 || goalTypes.Count > 3)
            message = "Please make sure you input at most 3 goal counts and types.";
        
        if (message != "")
        {
            errorText.text = message;
            errorScreen.SetActive(true);
            return false;
        }
        return true;
    }

    private void InitializeGoals()
    {
        _goalTypeTracker = new List<string>();
        _goalTypeTracker.AddRange(goalTypes);
        goalCountTexts = new List<Text>();
        
        foreach (var (type, i) in goalTypes.Select((value, i) => (value, i)))
        {
            var createdUnit = Instantiate(goalReference, Vector3.zero, Quaternion.identity);
            createdUnit.transform.SetParent(goalsPanel.transform);
            goalCountTexts.Add(createdUnit.transform.GetComponentInChildren<Text>());
            goalCountTexts[i].text = goalCounts[i].ToString();
            createdUnit.transform.GetComponentInChildren<Unit>().Initialize(this, givenType: type);
            
        }
    }

    private void RegisterMove()
    {
        _moveCounter--;
        moves.text = _moveCounter.ToString();
    }

    private void EndGame(bool won)
    {
        StopAllCoroutines();
        PlayerPrefs.SetInt("Enabled", 0);
        endScreen.transform.GetComponentInChildren<Text>().text = won ? "You Won!" : "You Lost";
        endScreen.SetActive(true);
    }

    public void Restart()
    {
        endScreen.SetActive(false);
        foreach (var unit in _units)
        {
            Destroy(unit.gameObject);
        }
        for (var i = 0; i < goalsPanel.transform.childCount; i++)
        {
            Destroy(goalsPanel.transform.GetChild(i).gameObject);
            
        }
        PreInitialize();
        CreateUnits();
    }

    private void MoveUnitInstantly(Unit movingUnit, Vector3 endPosition)
    {
        movingUnit.transform.position = endPosition;
    }

    private IEnumerator MoveUnitGradually(Unit movingUnit, Vector3 movingPosition, bool sendToOblivion = false)
    {
        var movingVector = (movingPosition - movingUnit.transform.position) / _globalAnimationStepCount;
        while (Vector2.Distance(movingUnit.transform.position, movingPosition) > 20f)
        {
            movingUnit.transform.position += movingVector;
            yield return new WaitForSeconds(_animationTime);
        }
        movingUnit.transform.position = sendToOblivion ? _oblivion : movingPosition;
    }

    private IEnumerator RunRocketEffect(Unit rocket)
    {
        var startingPosition = rocket.transform.position;
        var rocket1 = Instantiate(unitReference, startingPosition, Quaternion.identity).GetComponent<Unit>();
        var rocket2 = Instantiate(unitReference, startingPosition, Quaternion.identity).GetComponent<Unit>();
        if (rocket.Type == "rocket_x")
        {
            rocket1.transform.SetParent(transform);
            rocket1.Initialize(this, givenType: "rocket_right");
            StartCoroutine(MoveUnitGradually(rocket1, startingPosition + Vector3.right*1000));
            
            rocket2.transform.SetParent(transform);
            rocket2.Initialize(this, givenType: "rocket_left");
            StartCoroutine(MoveUnitGradually(rocket2, startingPosition + Vector3.left*1000));
        }
        else
        {
            rocket1.transform.SetParent(transform);
            rocket1.Initialize(this, givenType: "rocket_up");
            StartCoroutine(MoveUnitGradually(rocket1, startingPosition + Vector3.up*1000));
            
            rocket2.transform.SetParent(transform);
            rocket2.Initialize(this, givenType: "rocket_down");
            StartCoroutine(MoveUnitGradually(rocket2, startingPosition + Vector3.down*1000));
        }

        yield return new WaitForSeconds(1);
        Destroy(rocket1.gameObject);
        Destroy(rocket2.gameObject);
    }
}


