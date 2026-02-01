using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using UnityEngine;

public class PlotSet
{
    public Plot[] plots = new Plot[3];
    public bool[] isUsed = new bool[3];

    public PlotSet(Plot[] plots)
    {
        for(int i = 0; i < 3; i++)
        {
            this.plots[i] = plots[i];
            this.isUsed[i] = false;
        }
    }

    public void use(int slot)
    {
        isUsed[slot] = true;
    }

    public bool isAllUsed()
    {
        return isUsed[0] && isUsed[1] && isUsed[2];
    }
}


public class PlotManager : MonoBehaviour
{
    [Header("공작 SO 리스트")]
    [SerializeField] private List<Plot> plots;

    private List<Plot> usedPlots;

    void Awake()
    {
        usedPlots = new List<Plot>();
    }

    public PlotSet GeneratePlotSet()
    {
        float p = InGameManager.Instance.GetProgress(); // 진행도 (0~100 가정)
        Plot[] selectedPlots = new Plot[3];

        // 1. 가운데 슬롯 (index 1) : 희귀 / 전설
        float midRoll = Random.Range(0f, 100f);
        float midLegendaryProb = 10f + (p * 0.3f);
        
        PlotGrade midGrade = (midRoll < midLegendaryProb) ? PlotGrade.Legendary : PlotGrade.Rare;
        selectedPlots[1] = GetWeightedRandPlot(midGrade);

        // 2. 양 옆 슬롯 (index 0, 2) : 일반 / 희귀 / 전설
        float commonLimit = 70f - (p * 0.5f);
        float rareLimit = commonLimit + (20f + (p * 0.4f));

        for (int i = 0; i < 3; i++)
        {
            if (i == 1) continue; // 가운데는 이미 뽑음

            float sideRoll = Random.Range(0f, 100f);
            PlotGrade sideGrade;

            if (sideRoll < commonLimit) sideGrade = PlotGrade.Common;
            else if (sideRoll < rareLimit) sideGrade = PlotGrade.Rare;
            else sideGrade = PlotGrade.Legendary;

            selectedPlots[i] = GetWeightedRandPlot(sideGrade);
        }

        return new PlotSet(selectedPlots);
    }

    public void RefreshPlotManager()
    {
        usedPlots.Clear();
    }

    private Plot GetWeightedRandPlot(PlotGrade grade)
    {
        var candidates = plots.Where(p => p.plotGrade == grade && !usedPlots.Contains(p)).ToList();

        float weightSum = candidates.Sum(p => p.GetPlotWeight());
        
        float randChoice = Random.Range(0f, weightSum);
        float currentSum = 0f;

        foreach(var p in candidates)
        {
            currentSum += p.GetPlotWeight();
            if (currentSum >= randChoice) return p;
        }

        return candidates[0];
    }
}
