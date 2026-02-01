using UnityEngine;

public class P001 : Plot
{
    [Header("해당 공작 설정")]
    [SerializeField] private int minInfluence;
    [SerializeField] private int pietyCost;
    [SerializeField] private int hpDelta;

    void Reset()
    {
        // 설정 기본값
        plotID = "P001";
        plotGrade = PlotGrade.Common;
        
        // 텍스트 기본값
        plotName = "한 숨 돌리기";
        plotDescription = "휴우...";

        // 수치 기본값
        plotWeightBase = 20;
        plotWeightMultiplier = 0f;

        minInfluence = 15;
        pietyCost = 20;
        hpDelta = 20;
    }

    public override bool CanExecute(Cardinal performer)
    {
        return performer.Influence >= minInfluence;
    }

    public override void Execute(Cardinal performer)
    {
        if(!CanExecute(performer)) return;

        performer.ChangePiety(-pietyCost);
        
        performer.ChangeHp(hpDelta);
    }
    
}
