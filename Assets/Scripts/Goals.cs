using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnitType = Lookup.UnitType;

public class Goals : MonoBehaviour
{
    [SerializeField] private List<Text> goalCountTexts;
    [SerializeField] private GameObject goalReference;
    
    private GameMaster _gm;
    private List<UnitType> _goalTypes;
    private List<int> _goalCounts;
    public List<UnitType> GoalTypeTracker { get; private set; }

    private void Awake()
    {
        _goalCounts = new List<int>();
        _goalTypes = new List<UnitType>();
    }

    public void Initialize(GameMaster creator)
    {
        _gm = creator;
        _goalTypes.Add(UnitType.Cube2);
        _goalTypes.Add(UnitType.Balloon);
        _goalCounts.Add(5);
        _goalCounts.Add(5);
        
        GoalTypeTracker = new List<UnitType>();
        GoalTypeTracker.AddRange(_goalTypes);
        goalCountTexts = new List<Text>();

        for (var i = 0; i < _goalTypes.Count; i++)
        {
            var type = _goalTypes[i];
            var createdUnit = Instantiate(goalReference, Vector3.zero, Quaternion.identity);
            createdUnit.transform.SetParent(transform);
            goalCountTexts.Add(createdUnit.transform.GetComponentInChildren<Text>());
            goalCountTexts[i].text = _goalCounts[i].ToString();
            createdUnit.transform.GetComponentInChildren<Unit>().Initialize(_gm, givenType: type);

        }
    }

    public void UpdateGoals(ref List<Unit> units)
    {
        print("Updating Goals");
        foreach (var unit in units)
        {
            var index = GoalTypeTracker.IndexOf(unit.Type);
            if (index == -1)
            {
                _gm.animator.MoveUnitInstantly(unit, Lookup.Oblivion);
                continue;
            }

            var currentGoalCount = int.Parse(goalCountTexts[index].text);
            if (currentGoalCount > 0)
            {
                goalCountTexts[index].text = (--currentGoalCount).ToString();
                _gm.animator.AnimateUnit(unit, transform.position, true, speed:3f);
                _gm.animator.goalCountIn.Play();
            }
            else
            {
                _gm.animator.MoveUnitInstantly(unit, Lookup.Oblivion);
            }

            if (currentGoalCount != 0) continue;
            GoalTypeTracker.RemoveAt(index);
            goalCountTexts.RemoveAt(index);
        }
    }
}
