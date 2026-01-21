using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum CardinalState
{
    Idle,
    Praying,
    InSpeech,
    ChatMaster,
    Chatting,
    Scheme, // Plot..? 일단 기획서에는 Scheme 라 써있어서 남겨둠
    SchemeChatting,
    CutScene
}

public class StateController : MonoBehaviour
{
    [Header("상태 정보")]
    [SerializeField] private CardinalState currentState = CardinalState.CutScene;

    // 컴포넌트 참조
    private Cardinal cardinal;
    private NavMeshAgent agent;
    private ICardinalController inputController; // 입력 처리를 위해 가져옴

    // 이동 경로 큐 (컷씬용)
    private Queue<Vector3> waypoints = new Queue<Vector3>();

    // 코루틴
    private Coroutine pathCoroutine;
    private Coroutine aiWanderCoroutine;

    // 프로퍼티
    public bool IsMoving => pathCoroutine != null;
    public CardinalState CurrentState => currentState;
    public bool ConClaving { get; set; } = false;

    void Awake()
    {
        cardinal = GetComponent<Cardinal>();
        agent = GetComponent<NavMeshAgent>();
        inputController = GetComponent<ICardinalController>(); 
    }

    void Start()
    {
        ChangeState(CardinalState.CutScene);
    }

    void Update()
    {
        switch (currentState)
        {
            case CardinalState.Idle:
                HandleIdleState();
                break;

            case CardinalState.CutScene:
                HandleCutSceneState();
                break;

                // 다른 상태들...
        }
    }

    // ---------------------------------------------------------
    // 상태별 로직
    // ---------------------------------------------------------

    void HandleIdleState()
    {
        // Player 태그일 때: 직접 조작 
        if (CompareTag("Player"))
        {
            HandlePlayerInput();
        }
        else
        {
            // AI일 때: 배회 코루틴이 돌고 있지 않다면 시작 
            if (aiWanderCoroutine == null)
            {
                aiWanderCoroutine = StartCoroutine(AIWanderRoutine());
            }
        }
    }

    void HandleCutSceneState()
    {
        // 컷씬 중 필요한 로직 작성.. 아직 작성하지 않아 남겨둠
    }

    // ---------------------------------------------------------
    // 플레이어 입력 처리 (이동속도는 Cardinal.cs 참조)
    // ---------------------------------------------------------
    void HandlePlayerInput()
    {
        if (inputController == null || agent == null) return;

        CardinalInputData input = inputController.GetInput();

        // 1순위 키보드 이동 
        if (input.moveDirection != Vector2.zero)
        {
            MoveByKeyboard(input.moveDirection);
        }
        // 2순위 마우스 이동
        else if (input.targetPos.HasValue)
        {
            MoveToTargetPos(input.targetPos.Value);
        }
        else
        {
            // 입력이 없고 경로도 없다면 정지
            if (!agent.hasPath && agent.velocity.sqrMagnitude > 0.01f)
            {
                agent.velocity = Vector3.zero;
            }
        }
    }

    // 키보드 이동 실행
    private void MoveByKeyboard(Vector2 direction)
    {
        if (agent.hasPath) agent.ResetPath();

        // Cardinal의 MoveSpeed를 참조하여 이동
        agent.velocity = new Vector3(direction.x, direction.y, 0) * cardinal.MoveSpeed;
    }

    // 마우스/타겟 이동 실행
    private void MoveToTargetPos(Vector2 targetPos)
    {
        Vector3 destination = new Vector3(targetPos.x, targetPos.y, transform.position.z);
        if (agent.isOnNavMesh) agent.SetDestination(destination);
    }

    // ---------------------------------------------------------
    // [이동 로직] AI 배회 (Idle)
    // ---------------------------------------------------------
    private IEnumerator AIWanderRoutine()
    {
        while (currentState == CardinalState.Idle)
        {
            float waitTime = Random.Range(1f, 3f);
            yield return new WaitForSeconds(waitTime);

            if (currentState != CardinalState.Idle) yield break;

            Vector3 randomOffset = new Vector3(
                Random.Range(-2f, 2f),
                Random.Range(-2f, 2f),
                0
            );

            Vector3 randomDest = transform.position + randomOffset;
            NavMeshHit hit;

            if (NavMesh.SamplePosition(randomDest, out hit, 1.0f, NavMesh.AllAreas))
            {
                if (agent.isOnNavMesh) agent.SetDestination(hit.position);
            }
            else
            {
                continue;
            }

            yield return new WaitUntil(() =>
                currentState != CardinalState.Idle ||
                (!agent.pathPending &&
                 agent.remainingDistance <= agent.stoppingDistance &&
                 agent.velocity.sqrMagnitude <= 0.1f)
            );
        }
    }

    // ---------------------------------------------------------
    // [이동 로직] 컷씬 강제 이동 
    // ---------------------------------------------------------
    public void MoveToWaypoints(Transform[] pathNodes)
    {
        ChangeState(CardinalState.CutScene);

        waypoints.Clear();
        foreach (Transform t in pathNodes)
        {
            waypoints.Enqueue(t.position);
        }

        if (pathCoroutine != null) StopCoroutine(pathCoroutine);
        pathCoroutine = StartCoroutine(ProcessMoveQueue());
    }

    private IEnumerator ProcessMoveQueue()
    {
        while (waypoints.Count > 0)
        {
            Vector3 nextPos = waypoints.Dequeue();

            // 좌표 오차 적용 -> 무작위를 위한 로직 입장때만 실행 됨
            if (waypoints.Count == 0 && ConClaving == false)
            {
                if (CompareTag("Player"))
                {
                    nextPos.y -= 1f;
                }
                else
                {
                    float randomX = Random.Range(-1.5f, 1.5f);
                    float randomY = Random.Range(-4f, 7f);
                    nextPos.x += randomX;
                    nextPos.y += randomY;
                }
            }

            if (agent.isOnNavMesh) agent.SetDestination(nextPos);
            agent.avoidancePriority = 1;

            yield return new WaitUntil(() =>
                !agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance &&
                (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            );
        }

        if (CompareTag("Player")) agent.avoidancePriority = 10;
        else agent.avoidancePriority = 50;

        pathCoroutine = null;
    }

    // ---------------------------------------------------------
    // 상태 변경 관리
    // ---------------------------------------------------------
    public void ChangeState(CardinalState newState)
    {
        if (currentState == newState) return;
        ExitState(currentState);
        currentState = newState;
        EnterState(currentState);
    }

    private void EnterState(CardinalState state)
    {
        switch (state)
        {
            case CardinalState.Idle:
                // AI일 경우 배회 시작
                if (!CompareTag("Player") && aiWanderCoroutine == null)
                {
                    aiWanderCoroutine = StartCoroutine(AIWanderRoutine());
                }
                break;

            case CardinalState.CutScene:
                if (agent != null && agent.hasPath) agent.ResetPath();
                break;
        }
    }

    private void ExitState(CardinalState state)
    {
        switch (state)
        {
            case CardinalState.Idle:
                if (aiWanderCoroutine != null)
                {
                    StopCoroutine(aiWanderCoroutine);
                    aiWanderCoroutine = null;
                }
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.ResetPath();
                    agent.velocity = Vector3.zero;
                }
                break;
        }
    }
}