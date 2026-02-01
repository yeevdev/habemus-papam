using UnityEditor.Search;
using UnityEngine;

public class P002 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int maxInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int influenceDelta;


    void Reset()
    {
        // 설정 기본값
        plotID = "P002";
        plotGrade = PlotGrade.Common;
        
        // 텍스트 기본값
        plotName = "은밀한 논의";
        plotDescription = "...점심 뭐 먹지?";

        // 수치 기본값
        plotWeightBase = 20;
        plotWeightMultiplier = 0f;

        maxInfluence = 30;
        pietyCost = 20;
        influenceDelta = 15;
    }

    public override bool CanExecute(Cardinal performer)
    {
        return performer.Influence <= maxInfluence;
    }

    public override void Execute(Cardinal performer)
    {
        if(!CanExecute(performer)) return;

        performer.ChangePiety(-pietyCost);

        performer.ChangeInfluence(influenceDelta);
    }
    
}
