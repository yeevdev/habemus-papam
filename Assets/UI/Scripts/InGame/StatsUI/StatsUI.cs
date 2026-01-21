using System;
using UnityEngine;
//상단 UI
//초상화와 능력치를 표시하는 Stats 블록의 위치와 세부 능력치를 결정
//Stats에서는 단순 표시만을 담당
public class StatsUI : MonoBehaviour
{
    [SerializeField] private Stats[] StatsList = new Stats[4];
    private int[] MaxStats = new int[4];
    [SerializeField] private Closeup closeup;
    private int top = 0;
    private int playerLength = 100;
    private int length = 100;
    void Start()
    {
        
    }
    void Update()
    {
        
    }
    void SetStats()
    {
        foreach(Stats stats in StatsList)
        {
            //stats.SetHP();
            //stats.SetPiety();
            //stats.SetPol();
        }
    }
    void GetMaxStat()
    {
        int maxStat = Math.Max(1,2);
    }
}