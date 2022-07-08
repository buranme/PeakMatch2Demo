using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;
using Image = UnityEngine.UI.Image;

public class Unit : MonoBehaviour
{
    private Grid _grid;
    public string Type { get; private set; }
    private Image _cardImage;

    private void Awake()
    {
        _cardImage = gameObject.GetComponent<Image>();
    }

    public void Initialize(Grid grid, bool isBottom = false, string givenType = "")
    {
        _grid = grid;
        
        if (givenType == "")
            Type = GetRandomType(isBottom);
        else
            Type = givenType;
        
        if (Type == "cube")
        {
            var color = Random.Range(1, 6);
            Type += "_" + color;
        }
        
        var imagePath = $"Images/{Type}";


        _cardImage.sprite = Resources.Load<Sprite>(imagePath);
    }

    public void DestroySelf()
    {
        if (PlayerPrefs.GetInt("Enabled") == 0 ||
            Type is "duck" or "balloon") return;
        StartCoroutine(_grid.UnitClicked(this));
    }

    public void Rocketify()
    {
        Type = Random.Range(0f,1f) < 0.5f ? "rocket_x" : "rocket_y";
        _cardImage.sprite = Resources.Load<Sprite>($"Images/{Type}");
    }

    public IEnumerator FadeIn(float animationTime)
    {
        var tempColor = _cardImage.color;
        for (var i = 0f; i < 1; i += 0.02f)
        {
            tempColor.a = i;
            _cardImage.color = tempColor;
            yield return new WaitForSeconds(animationTime);
        }
    }

    private string GetRandomType(bool isBottom)
    {
        const float cube = 0.8f;
        var balloon = isBottom ? 0.2f : 0.1f;
        
        var randomFloat = Random.Range(0f, 1f);
        if (randomFloat <= cube)
            return "cube";
        return randomFloat <= cube + balloon ? "balloon" : "duck";
    }
}
