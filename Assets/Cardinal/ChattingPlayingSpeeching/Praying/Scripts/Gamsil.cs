using System.Collections.Generic;
using UnityEngine;

public class Gamsil : MonoBehaviour
{
    [Header("위치 설정")]
    [Tooltip("실제 기도를 수행할 위치")]
    [SerializeField] private Transform prayTargetPoint;

    [Tooltip("기도 순서를 기다릴 대기 장소 (줄 서는 곳)")]
    [SerializeField] private Transform waitingPoint;

    [Tooltip("자리가 꽉 찼을 때 플레이어가 대기할 3번째 장소")]
    [SerializeField] private Transform playerOverflowPoint;

    [Tooltip("WaitingPoint 오브젝트에 붙어있는 PrayerWaitingTrigger 컴포넌트")]
    [SerializeField] private PrayerWaitingTrigger waitingTrigger;

    [Header("시간 설정")]
    [Tooltip("대기열이 비었을 때 다음 NPC를 호출하기까지 걸리는 시간 (초)")]
    [SerializeField] private float callInterval = 3.0f;

    [Tooltip("개별 NPC 재호출 대기 시간 (초)")]
    [SerializeField] private float individualCooldownDuration = 30.0f;

    // 감지된 NPC 리스트 
    private List<StateController> candidates = new List<StateController>();

    // 개별 쿨타임 관리
    private Dictionary<StateController, float> npcLastCalledTime = new Dictionary<StateController, float>();

    // 대기열 큐 
    private List<StateController> prayerList = new List<StateController>();
    // 현재 기도를 수행 중인 대상
    private StateController currentPrayerNPC = null;
    
    // 3번째 자리에 대기 중인 플레이어 저장용
    private StateController overflowPlayer = null;

    // 호출 타이머
    private float timer = 0f;

    void Update()
    {
        ProcessQueue();

        if (prayerList.Count == 0 && overflowPlayer == null)
        {
            timer += Time.deltaTime;

            if (timer >= callInterval)
            {
                CallNewNPCToQueue();
                timer = 0f;
            }
        }
        else
        {
            timer = 0f;
        }
    }

    public void CancelPlayerRegistration(StateController playerSC)
    {
        bool removed = false;

        // 1. 3번째 자리(Overflow)에 있었다면 제거
        if (overflowPlayer == playerSC)
        {
            overflowPlayer = null;
            removed = true;
            Debug.Log("3번째 대기석 예약 취소됨.");
        }
        // 2. 대기열(List)에 있었다면 제거
        else if (prayerList.Contains(playerSC))
        {
            prayerList.Remove(playerSC);
            removed = true;
            Debug.Log("대기열 예약 취소됨.");

            // 만약 대기열 1번이었고 트리거 예약자였다면 트리거도 비워줘야 함
            if (waitingTrigger != null) waitingTrigger.SetIncomingNPC(null);
        }

        // 플레이어에게 취소 명령 (하던 행동 멈추기)
        if (removed)
        {
            playerSC.CancelApproach();
        }
    }

    public void RegisterPlayerToQueue(StateController playerSC)
    {
        if (prayerList.Contains(playerSC) || currentPrayerNPC == playerSC || overflowPlayer == playerSC) return;
        if (playerSC.CurrentState != CardinalState.Idle) return;

        bool isMainSpotAvailable = false;
        if (waitingTrigger != null)
        {
            isMainSpotAvailable = waitingTrigger.TryReserveSpotForPlayer();
        }

        if (isMainSpotAvailable)
        {
            Debug.Log("Player entered Waiting Zone! Added to Queue.");
            prayerList.Add(playerSC); // 리스트 끝에 추가
            playerSC.OrderToPray(waitingPoint.position, true);
        }
        else
        {
            if (playerOverflowPoint != null)
            {
                Debug.Log("대기석이 꽉 차서 플레이어 전용 대기석(3순위)으로 이동합니다.");
                overflowPlayer = playerSC;
                playerSC.OrderToPray(playerOverflowPoint.position, true);
            }
        }
    }

    private void CallNewNPCToQueue()
    {
        if (overflowPlayer != null) return;
        if (candidates.Count == 0 || waitingPoint == null) return;

        StateController bestCandidate = null;
        float minDistance = float.MaxValue;

        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            StateController sc = candidates[i];
            if (sc.CurrentState == CardinalState.Scheme || sc.IsSchemer) continue;

            if (sc == null || sc.CompareTag("Player"))
            {
                candidates.RemoveAt(i);
                if (sc != null && npcLastCalledTime.ContainsKey(sc)) npcLastCalledTime.Remove(sc);
                continue;
            }

            if (npcLastCalledTime.ContainsKey(sc))
            {
                if (Time.time - npcLastCalledTime[sc] < individualCooldownDuration) continue;
            }

            // List로 변경됨에 따라 Contains 체크
            if (prayerList.Contains(sc) || sc == currentPrayerNPC) continue;

            if (sc.CurrentState == CardinalState.Idle)
            {
                float dist = Vector3.Distance(transform.position, sc.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestCandidate = sc;
                }
            }
        }

        if (bestCandidate != null)
        {
            prayerList.Add(bestCandidate); // List Add

            if (npcLastCalledTime.ContainsKey(bestCandidate)) npcLastCalledTime[bestCandidate] = Time.time;
            else npcLastCalledTime.Add(bestCandidate, Time.time);

            bestCandidate.OrderToPray(waitingPoint.position, true);

            if (waitingTrigger != null)
            {
                waitingTrigger.SetIncomingNPC(bestCandidate);
            }
        }
    }

    // 대기열 처리
    private void ProcessQueue()
    {
        bool isSpot1Occupied = IsPrayerSpotOccupied();

        // Case A: 대기열(List)에 사람이 있고, 기도석(1번)이 비었을 때
        if (!isSpot1Occupied && prayerList.Count > 0)
        {
            // List의 0번째 요소 가져오기 (Queue.Dequeue 역할)
            StateController nextCandidate = prayerList[0];
            prayerList.RemoveAt(0);

            if (nextCandidate != null)
            {
                currentPrayerNPC = nextCandidate;
                nextCandidate.ProceedToRealPrayer(prayTargetPoint.position);

                if (overflowPlayer != null)
                {
                    MoveOverflowPlayerToWaitingSpot();
                }
            }
            return;
        }

        // Case B: 대기열은 비어있는데, 3번에 플레이어가 기다릴 때
        if (prayerList.Count == 0 && overflowPlayer != null)
        {
            MoveOverflowPlayerToWaitingSpot();
        }
    }

    private void MoveOverflowPlayerToWaitingSpot()
    {
        if (overflowPlayer == null) return;

        Debug.Log("대기석이 비어 플레이어가 3순위 -> 2순위로 이동합니다.");

        prayerList.Add(overflowPlayer); // List Add

        if (waitingTrigger != null)
        {
            waitingTrigger.SetIncomingNPC(overflowPlayer);
        }

        overflowPlayer.OrderToPray(waitingPoint.position, true);
        overflowPlayer = null;
    }

    // 자리가 찼는지 확인
    private bool IsPrayerSpotOccupied()
    {
        if (currentPrayerNPC == null) return false;

        // [수정] 단순히 상태만 보는 게 아니라, 이동 중인지(IsPerformingPrayerAction)까지 확인
        // StateController에 추가한 프로퍼티 사용
        if (currentPrayerNPC.IsPerformingPrayerAction)
        {
            return true;
        }

        // 여기까지 왔다면 기도가 진짜 끝난 것임
        currentPrayerNPC = null;
        return false;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("NPC"))
        {
            StateController sc = other.GetComponent<StateController>();
            if (sc != null && !candidates.Contains(sc)) candidates.Add(sc);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("NPC"))
        {
            StateController sc = other.GetComponent<StateController>();
            if (sc != null && candidates.Contains(sc)) candidates.Remove(sc);
        }
    }
}