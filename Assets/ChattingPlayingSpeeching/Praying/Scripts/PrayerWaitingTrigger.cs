using UnityEngine;

public class PrayerWaitingTrigger : MonoBehaviour
{
    [Tooltip("플레이어를 등록할 Gamsil 매니저")]
    [SerializeField] private Gamsil gamsilManager;

    private bool isNpcInside = false;

    private StateController incomingNPC;
    public void SetIncomingNPC(StateController npc)
    {
        incomingNPC = npc;
        // Debug.Log($"NPC {npc.name}가 대기열로 이동 중입니다.");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {

        if (other.CompareTag("NPC"))
        {
            isNpcInside = true;
            Debug.Log("NPC가 대기열에 도착하여 자리를 선점했습니다.");

            // [추가] 도착한 NPC가 오고 있던 그 녀석이라면, 예약 변수 초기화
            StateController arrivedNPC = other.GetComponent<StateController>();
            if (incomingNPC == arrivedNPC)
            {
                incomingNPC = null;
            }

            return;
        }
        if (other.CompareTag("Player"))
        {
            // A. NPC가 이미 물리적으로 안에 있다면 플레이어는 무시
            if (isNpcInside)
            {
               
                return;
            }

            StateController playerSC = other.GetComponent<StateController>();

            // B. 조건 확인
            if (playerSC != null && playerSC.CurrentState == CardinalState.Idle && gamsilManager != null)
            {
               
                if (incomingNPC != null)
                {

                    incomingNPC.ChangeState(CardinalState.Idle);

                    incomingNPC = null;
                }

                // 플레이어를 대기열에 등록
                gamsilManager.RegisterPlayerToQueue(playerSC);
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