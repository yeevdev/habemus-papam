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
    private Queue<StateController> prayerQueue = new Queue<StateController>();

    // 현재 기도를 수행 중인 대상
    private StateController currentPrayerNPC = null;
    
    // 3번째 자리에 대기 중인 플레이어 저장용
    private StateController overflowPlayer = null;

    // 호출 타이머
    private float timer = 0f;

    void Update()
    {
        ProcessQueue();

        if (prayerQueue.Count == 0 && overflowPlayer == null)
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

    public void RegisterPlayerToQueue(StateController playerSC)
    {
        // 중복 체크 (이미 큐에 있거나, 기도 중이거나, 3번째 자리에 있거나)
        if (prayerQueue.Contains(playerSC) || currentPrayerNPC == playerSC || overflowPlayer == playerSC) return;
        if (playerSC.CurrentState != CardinalState.Idle) return;

        // 1. 기본 대기석(WaitingPoint) 자리가 비었는지 확인
        bool isMainSpotAvailable = false;
        if (waitingTrigger != null)
        {
            isMainSpotAvailable = waitingTrigger.TryReserveSpotForPlayer();
        }

        // 2. 분기 처리
        if (isMainSpotAvailable)
        {
            // A. 자리가 있음 -> 바로 2번째 대기석(WaitingPoint)으로 이동 및 큐 등록
            Debug.Log("Player entered Waiting Zone! Added to Queue.");
            prayerQueue.Enqueue(playerSC);
            playerSC.OrderToPray(waitingPoint.position, true);
        }
        else
        {
            // B. 자리가 없음(꽉 참) -> 3번째 대기석(Overflow)으로 이동
            // 큐(prayerQueue)에는 아직 넣지 않음! (2번째 자리로 갈 때 넣을 예정)

            if (playerOverflowPoint != null)
            {
                Debug.Log("대기석이 꽉 차서 플레이어 전용 대기석(3순위)으로 이동합니다.");
                overflowPlayer = playerSC;

                // 로직은 기도 로직을 그대로 사용하되, 좌표만 3번째 자리로 설정
                // isQueueing = true로 설정하여 대기 상태(왼쪽 보기 등) 유지
                playerSC.OrderToPray(playerOverflowPoint.position, true);
            }
            else
            {
                Debug.LogWarning("PlayerOverflowPoint가 할당되지 않아 갈 곳이 없습니다.");
            }
        }
    }

    private void CallNewNPCToQueue()
    {
        // 만약 플레이어가 3번째 자리에 있다면, 새로운 NPC를 부르지 않음 (플레이어 우선)
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

            if (prayerQueue.Contains(sc) || sc == currentPrayerNPC) continue;

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
            prayerQueue.Enqueue(bestCandidate);

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
        // 1. 기도석(1번)이 꽉 찼는지 확인
        bool isSpot1Occupied = IsPrayerSpotOccupied();

        // -------------------------------------------------------------
        // Case A: 대기열(2번)에 사람이 있고, 기도석(1번)이 비었을 때
        // -------------------------------------------------------------
        if (!isSpot1Occupied && prayerQueue.Count > 0)
        {
            // 2번 -> 1번 이동
            StateController nextCandidate = prayerQueue.Dequeue();

            if (nextCandidate != null)
            {
                // ReadyPraying인지 체크하지 않고 보내거나, 상태 강제 (이동 중일 수 있으므로)
                // 만약 이동 중이라면 도착 후 StateController 로직에 의해 처리되겠지만
                // 여기서는 확실하게 1번으로 보냄
                currentPrayerNPC = nextCandidate;
                nextCandidate.ProceedToRealPrayer(prayTargetPoint.position);

                // ★ [기존 로직] 2번이 비었으니 3번(플레이어)을 2번으로 당김
                if (overflowPlayer != null)
                {
                    MoveOverflowPlayerToWaitingSpot();
                }
            }
            return; // 이번 프레임 처리 끝
        }

        // -------------------------------------------------------------
        // Case B: [버그 수정] 대기열(2번)은 비어있는데, 3번에 플레이어가 기다릴 때
        // (멀리서 오느라 늦게 도착했거나, 앞사람들이 다 빠져나간 경우)
        // -------------------------------------------------------------
        if (prayerQueue.Count == 0 && overflowPlayer != null)
        {
            // 2번 자리가 확실히 비어있으므로(Queue가 0이니까), 3번 -> 2번으로 당김
            MoveOverflowPlayerToWaitingSpot();

            // 만약 기도석(1번)도 비어있다면?
            // 다음 프레임에 ProcessQueue가 다시 돌면서 Case A에 걸려 
            // 2번(방금 도착한 플레이어) -> 1번으로 자연스럽게 이동됨.
        }
    }

    private void MoveOverflowPlayerToWaitingSpot()
    {
        if (overflowPlayer == null) return;

        Debug.Log("대기석이 비어 플레이어가 3순위 -> 2순위로 이동합니다.");

        // 1. 진짜 대기열 큐에 등록
        prayerQueue.Enqueue(overflowPlayer);

        // 2. 트리거에 "내가 간다"고 예약 (NPC가 못 채게)
        if (waitingTrigger != null)
        {
            // 강제로 예약 (SetIncomingNPC 사용)
            waitingTrigger.SetIncomingNPC(overflowPlayer);
        }

        // 3. 이동 명령 갱신 (목적지를 WaitingPoint로 변경)
        // 이미 ReadyPraying 상태이므로 OrderToPray를 다시 호출하면 목적지만 바뀜
        overflowPlayer.OrderToPray(waitingPoint.position, true);

        // 4. 변수 비우기
        overflowPlayer = null;
    }

    // 자리가 찼는지 확인
    private bool IsPrayerSpotOccupied()
    {
        if (currentPrayerNPC == null) return false;

        if (currentPrayerNPC.CurrentState == CardinalState.ReadyPraying ||
            currentPrayerNPC.CurrentState == CardinalState.Praying)
        {
            return true;
        }

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