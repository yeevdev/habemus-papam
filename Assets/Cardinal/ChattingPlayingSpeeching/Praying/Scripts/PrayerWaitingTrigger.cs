using UnityEngine;

public class PrayerWaitingTrigger : MonoBehaviour
{
    [Tooltip("플레이어를 등록할 Gamsil 매니저")]
    [SerializeField] private Gamsil gamsilManager;

    // 현재 구역 안에 NPC가 물리적으로 있는지
    private bool isNpcInside = false;

    // 이 구역으로 오고 있는 NPC (예약자)
    private StateController incomingNPC;

    public void SetIncomingNPC(StateController npc)
    {
        incomingNPC = npc;
    }

    // =================================================================
    // [신규 기능] Gamsil에서 F키를 눌렀을 때 호출하는 함수
    // 플레이어가 대기열에 들어갈 수 있는지 확인하고, 오던 NPC가 있다면 취소시킴
    // =================================================================
    public bool TryReserveSpotForPlayer()
    {
        // 1. 이미 NPC가 물리적으로 서 있다면 플레이어는 갈 수 없음 (꽉 참)
        if (isNpcInside)
        {
            Debug.Log("누군가 서 있어서 기도를 신청할 수 없습니다.");
            return false;
        }

        // 2. 오고 있던 NPC가 있다면? -> 플레이어가 우선권을 가짐 (NPC 취소)
        if (incomingNPC != null)
        {
            // 오던 애는 Idle로 돌려보냄
            incomingNPC.ChangeState(CardinalState.Idle);
            incomingNPC = null;
            Debug.Log("오고 있던 NPC의 예약을 취소하고 플레이어가 자리를 차지합니다.");
        }

        return true; // 입장 가능
    }

    // =================================================================
    // 물리적 충돌 감지 (NPC만 체크)
    // =================================================================
    private void OnTriggerEnter2D(Collider2D other)
    {
        // [수정됨] 플레이어 자동 등록 로직 삭제!
        // 오직 NPC가 도착했는지만 확인

        if (other.CompareTag("NPC"))
        {
            isNpcInside = true;

            StateController arrivedNPC = other.GetComponent<StateController>();
            // 도착한 게 예약된 그 녀석이라면 예약 변수 비우기
            if (incomingNPC == arrivedNPC)
            {
                incomingNPC = null;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("NPC"))
        {
            isNpcInside = false;

            StateController exitingNPC = other.GetComponent<StateController>();
            if (incomingNPC == exitingNPC)
            {
                incomingNPC = null;
            }
        }
    }
}