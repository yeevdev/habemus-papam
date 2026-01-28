using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, ICardinalController
{
    [Header("Interaction Settings")]
    [Tooltip("기도 대기열에 등록할 키 설정")]
    [SerializeField] private Key interactKey = Key.F;

    [Tooltip("상호작용 쿨타임 (초)")]
    [SerializeField] private float interactCooldown = 5.0f; // [신규] 기본값 5초

    [Tooltip("Gamsil 매니저 참조")]
    [SerializeField] private Gamsil gamsilManager;

    private float currentCooldownTimer = 0f; // 현재 남은 쿨타임
    private Vector2? targetPos;
    private StateController myStateController;

    private void Awake()
    {
        myStateController = GetComponent<StateController>();
    }

    private void Start()
    {
        if (gamsilManager == null)
        {
            gamsilManager = FindAnyObjectByType<Gamsil>();
        }
    }

    private void Update()
    {
        // 1. 쿨타임 감소 (상태와 무관하게 돌아감)
        if (currentCooldownTimer > 0)
        {
            currentCooldownTimer -= Time.deltaTime;
        }

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        // =========================================================
        // [중요] 상태 체크: 오직 'Idle' 상태일 때만 입력 처리 (이동, 취소, 신청)
        // =========================================================
        if (myStateController != null && myStateController.CurrentState == CardinalState.Idle)
        {
            // 2. 이동 입력 감지
            bool isMovingInput = false;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 screenPos = mouse.position.ReadValue();
                Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -Camera.main.transform.position.z));
                targetPos = new Vector2(world.x, world.y);
                isMovingInput = true;
            }

            if (keyboard.wKey.isPressed || keyboard.sKey.isPressed || keyboard.aKey.isPressed || keyboard.dKey.isPressed ||
                keyboard.upArrowKey.isPressed || keyboard.downArrowKey.isPressed || keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                isMovingInput = true;
            }

            // 3. 취소 로직 (이동 입력이 있고 + 기도하러 가는 중이라면)
            // [수정] Idle 상태 안에서 체크하므로, 이미 도착해서 기도 중일 때는 취소되지 않음
            if (isMovingInput && myStateController.IsHeadingToQueue)
            {
                if (gamsilManager != null)
                {
                    gamsilManager.CancelPlayerRegistration(myStateController);
                    // 취소 시 타겟 포지션도 초기화하여 튀는 현상 방지
                    targetPos = null;
                }
            }

            // 4. 상호작용 (F키)
            if (keyboard[interactKey].wasPressedThisFrame)
            {
                // [조건] 기도하러 가는 중(IsHeadingToQueue)이 아닐 때만 신청 가능
                if (!myStateController.IsHeadingToQueue)
                {
                    if (currentCooldownTimer <= 0)
                    {
                        RequestPrayerEntry();
                    }
                    else
                    {
                        Debug.Log($"쿨타임 중입니다. {currentCooldownTimer:F1}초 남음");
                    }
                }
            }
        }
    }

    private void RequestPrayerEntry()
    {
        if (gamsilManager == null || myStateController == null) return;

        if (myStateController.CurrentState == CardinalState.Idle)
        {
            gamsilManager.RegisterPlayerToQueue(myStateController);
            currentCooldownTimer = interactCooldown; // 쿨타임 시작
        }
    }

    public CardinalInputData GetInput()
    {
        CardinalInputData inputData = new CardinalInputData { targetPos = this.targetPos };
        targetPos = null;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            Vector2 moveDir = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveDir.y += 1;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveDir.y -= 1;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveDir.x -= 1;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveDir.x += 1;

            inputData.moveDirection = moveDir.normalized;
        }
        return inputData;
    }
}