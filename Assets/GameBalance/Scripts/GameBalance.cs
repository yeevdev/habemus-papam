using UnityEngine;

[CreateAssetMenu(fileName = "GameBalance", menuName = "Scriptable Objects/GameBalance")]
public class GameBalance : ScriptableObject
{
    [Header("추기경 기본 설정")]
    [Tooltip("추기경 기본 체력")]
    [SerializeField] private int initialHp = 100;
    public int InitialHp => initialHp;

    [Tooltip("추기경 기본 정치력")]
    [SerializeField] private int initialInfluence = 20;
    public int InitialInfluence => initialInfluence;

    [Tooltip("추기경 기본 경건함")]
    [SerializeField] private int initialPiety = 20;
    public int InitialPiety => initialPiety;

    [Tooltip("추기경 기본 이동속도 계수")]
    [SerializeField] private float initialMoveSpeed = 3.0f;
    public float InitialMoveSpeed => initialMoveSpeed;


    [Header("추기경 행동 - 기도 설정")]
    [Tooltip("기도 성공 확률")]
    [SerializeField] private float praySuccessChance = 0.8f;
    public float PraySuccessChance => praySuccessChance;

    [Tooltip("기도 성공 시 경건함 변화량")]
    [SerializeField] private int praySuccessDeltaPiety = 15;
    public int PraySuccessDeltaPiety => praySuccessDeltaPiety;

    [Tooltip("기도 성공 시 체력 변화량")]
    [SerializeField] private int praySuccessDeltaHp = 10;
    public int PraySuccessDeltaHp => praySuccessDeltaHp;

    [Tooltip("기도 실패 시 경건함 변화량")]
    [SerializeField] private int prayFailDeltaPiety = 5;
    public int PrayFailDeltaPiety => prayFailDeltaPiety;

    [Tooltip("기도 실패 시 체력 변화량")]
    [SerializeField] private int prayFailDeltaHp = 20;
    public int PrayFailDeltaHp => prayFailDeltaHp;


    [Header("추기경 행동 - 연설 설정")]
    [Tooltip("연설 성공 확률")]
    [SerializeField] private float speechSuccessChance = 0.9f;
    public float SpeechSuccessChance => speechSuccessChance;

    [Tooltip("연설 성공 시 정치력 변화량(최소)")]
    [SerializeField] private int speechSuccessDeltaInfluenceMin = 3;
    public int SpeechSuccessDeltaInfluenceMin => speechSuccessDeltaInfluenceMin;

    [Tooltip("연설 성공 시 정치력 변화량(최대)")]
    [SerializeField] private int speechSuccessDeltaInfluenceMax = 7;
    public int SpeechSuccessDeltaInfluenceMax => speechSuccessDeltaInfluenceMax;

    [Tooltip("연설 성공 시 체력 변화량")]
    [SerializeField] private int speechSuccessDeltaHp = -5;
    public int SpeechSuccessDeltaHp => speechSuccessDeltaHp;

    [Tooltip("연설 실패 시 정치력 변화량")]
    [SerializeField] private int speechFailDeltaInfluence = -2;
    public int SpeechFailDeltaInfluence => speechFailDeltaInfluence;

    [Tooltip("연설 실패 시 체력 변화량")]
    [SerializeField] private int speechFailDeltaHp = -5;
    public int SpeechFailDeltaHp => speechFailDeltaHp;


    [Header("게임 진행 설정")]
    [Tooltip("한 콘클라베 당 자유 행동 시간")]
    [SerializeField] private float maxConclaveTime = 60.0f;
    public float MaxConclaveTime => maxConclaveTime;
}
