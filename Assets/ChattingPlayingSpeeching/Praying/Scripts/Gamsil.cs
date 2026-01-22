using System.Collections.Generic;
using UnityEngine;

public class Gamsil : MonoBehaviour
{
    [Header("위치 설정")]
    [Tooltip("실제 기도를 수행할 위치")]
    [SerializeField] private Transform prayTargetPoint;

    [Tooltip("기도 순서를 기다릴 대기 장소 (줄 서는 곳)")]
    [SerializeField] private Transform waitingPoint;

    [Header("시간 설정")]
    [Tooltip("대기열이 비었을 때 다음 NPC를 호출하기까지 걸리는 시간 (초)")]
    [SerializeField] private float callInterval = 3.0f; // [요청 반영] 기본값 3초로 변경

    [Tooltip("개별 NPC 재호출 대기 시간 (초)")]
    [SerializeField] private float individualCooldownDuration = 30.0f;

    // 감지된 NPC 리스트 (후보군)
    private List<StateController> candidates = new List<StateController>();

    // 개별 쿨타임 관리
    private Dictionary<StateController, float> npcLastCalledTime = new Dictionary<StateController, float>();

    // 대기열 큐
    private Queue<StateController> prayerQueue = new Queue<StateController>();

    // 현재 기도를 수행 중인 NPC
    private StateController currentPrayerNPC = null;

    // 호출 타이머
    private float timer = 0f;

    void Update()
    {
        // 1. [소비자] 기도 자리가 비었고, 대기열에 사람이 있다면 입장시킴
        // (먼저 처리해야 대기열이 비면서 아래 생산자 로직이 돌 수 있음)
        ProcessQueue();

        // 2. [생산자] 대기열(waitingPoint) 관리 로직 수정
        // [변경점] 무조건 타이머를 돌리는게 아니라, '대기열에 사람이 없을 때만' 타이머를 돌림
        if (prayerQueue.Count == 0)
        {
            timer += Time.deltaTime;

            if (timer >= callInterval) // 3초가 지나면
            {
                CallNewNPCToQueue();
                timer = 0f;
            }
        }
        else
        {
            // 대기열에 사람이 있으면 타이머를 0으로 리셋 (대기자가 떠나야 다시 카운트 시작)
            timer = 0f;
        }
    }

    // --- [1] 새로운 NPC 호출 로직 ---
    private void CallNewNPCToQueue()
    {
        if (candidates.Count == 0 || waitingPoint == null) return;

        StateController bestCandidate = null;
        float minDistance = float.MaxValue;

        // 후보군 검색
        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            StateController sc = candidates[i];

            // 유효성 체크
            if (sc == null || sc.CompareTag("Player"))
            {
                candidates.RemoveAt(i);
                if (sc != null && npcLastCalledTime.ContainsKey(sc)) npcLastCalledTime.Remove(sc);
                continue;
            }

            // 개별 쿨타임 체크
            if (npcLastCalledTime.ContainsKey(sc))
            {
                if (Time.time - npcLastCalledTime[sc] < individualCooldownDuration) continue;
            }

            // 이미 대기열에 있거나 기도 중인 NPC는 제외
            if (prayerQueue.Contains(sc) || sc == currentPrayerNPC) continue;

            // Idle 상태인 NPC만
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

        // 호출
        if (bestCandidate != null)
        {
            // 큐에 등록 (이제 prayerQueue.Count가 1이 되므로 Update의 타이머가 멈춤)
            prayerQueue.Enqueue(bestCandidate);

            // 쿨타임 갱신
            if (npcLastCalledTime.ContainsKey(bestCandidate)) npcLastCalledTime[bestCandidate] = Time.time;
            else npcLastCalledTime.Add(bestCandidate, Time.time);

            // 대기 장소로 이동 명령
            bestCandidate.OrderToPray(waitingPoint.position, true);

            Debug.Log($"Gamsil added {bestCandidate.name} to the prayer queue.");
        }
    }

    // --- [2] 대기열 처리 로직 ---
    private void ProcessQueue()
    {
        // 1. 현재 기도 중인 사람이 있는지 확인
        if (IsPrayerSpotOccupied()) return;

        // 2. 자리가 비었는데 대기열에 사람이 있다면 입장!
        if (prayerQueue.Count > 0)
        {
            // 대기열에서 꺼냄 (이제 prayerQueue.Count가 0이 되므로 Update의 타이머가 다시 돌기 시작)
            StateController nextNPC = prayerQueue.Dequeue();

            if (nextNPC != null && nextNPC.CurrentState == CardinalState.ReadyPraying)
            {
                currentPrayerNPC = nextNPC;
                nextNPC.ProceedToRealPrayer(prayTargetPoint.position);
                Debug.Log($"{nextNPC.name} is moving to the prayer spot.");
            }
        }
    }

    private bool IsPrayerSpotOccupied()
    {
        if (currentPrayerNPC == null) return false;

        // 아직 ReadyPraying(기도하러 가는 중) 이거나 Praying(기도 중) 이면 차있음
        if (currentPrayerNPC.CurrentState == CardinalState.ReadyPraying ||
            currentPrayerNPC.CurrentState == CardinalState.Praying)
        {
            return true;
        }

        currentPrayerNPC = null;
        return false;
    }

    // --- Trigger 감지 ---
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