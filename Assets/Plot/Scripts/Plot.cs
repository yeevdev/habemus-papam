using UnityEngine;

public enum PlotGrade { Common, Rare, Legendary }

public abstract class Plot : ScriptableObject
{
    [SerializeField] public string plotID;
    [SerializeField] public string plotName;
    [TextArea] public string plotDescription;
    [SerializeField] public Sprite plotImage;
    [SerializeField] public PlotGrade plotGrade;
    [SerializeField] public float plotWeightBase;
    [SerializeField] public float plotWeightMultiplier;

    public float GetPlotWeight()
    {
        float progressWeight = plotWeightMultiplier * InGameManager.Instance.GetProgress();
        
        return plotWeightBase + progressWeight;
    }

    // 조건 확인 함수, 구현은 자식 클래스에서 직접
    public abstract bool CanExecute(Cardinal performer);

    // 실제 실행시 로직 함수, 구현은 자식 클래스에서 직접
    public abstract void Execute(Cardinal performer);
}
