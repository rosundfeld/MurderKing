using System.Collections.Generic;
using TMPro;
using UnityEngine;

//Reusable list of short messages (newest on top) that fade out, used by the ammo log and the money feed
public class FeedLinesUI : MonoBehaviour
{
    [SerializeField] private Transform container;
    [SerializeField] private TextMeshProUGUI lineTemplate;
    [SerializeField] private int maxLines = 4;
    [SerializeField] private float lineDuration = 6f;

    private class Line
    {
        public TextMeshProUGUI text;
        public float time;
    }

    private readonly List<Line> lineList = new List<Line>();

    private void Awake()
    {
        lineTemplate.gameObject.SetActive(false);
    }

    public void AddLine(string message)
    {
        TextMeshProUGUI lineText = Instantiate(lineTemplate, container);
        lineText.gameObject.SetActive(true);
        lineText.text = message;
        lineText.transform.SetAsFirstSibling();
        lineList.Add(new Line { text = lineText, time = Time.time });

        if (lineList.Count > maxLines)
        {
            RemoveLineAt(0);
        }
    }

    private void Update()
    {
        for (int i = lineList.Count - 1; i >= 0; i--)
        {
            float age = Time.time - lineList[i].time;
            if (age > lineDuration)
            {
                RemoveLineAt(i);
                continue;
            }
            lineList[i].text.alpha = Mathf.Lerp(1f, 0.25f, age / lineDuration);
        }
    }

    private void RemoveLineAt(int index)
    {
        Destroy(lineList[index].text.gameObject);
        lineList.RemoveAt(index);
    }
}
