using UnityEngine;
using UnityEngine.InputSystem; // Input System 네임스페이스

public class PlayerController : MonoBehaviour, ICardinalController
{
    [Header("Interaction Settings")]
    [Tooltip("기도 대기열에 등록할 키 설정 (기본값 F)")]
    [SerializeField] private Key interactKey = Key.F;

    [Tooltip("Gamsil 매니저 참조 (자동으로 찾지만, 직접 할당 권장)")]
    [SerializeField] private Gamsil gamsilManager;

    private Vector2? targetPos;
    private StateController myStateController; // 내 상태 확인용

    private void Awake()
    {
        myStateController = GetComponent<StateController>();
    }

    private void Start()
    {
        // Gamsil 매니저가 할당되지 않았다면 씬에서 찾기
        if (gamsilManager == null)
        {
            gamsilManager = FindAnyObjectByType<Gamsil>();
        }
    }

    // Update에서 입력 감지 (단발성 입력은 Update가 적절)
    private void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        // 1. 마우스 입력 처리 (기존 로직)
        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 screenPos = mouse.position.ReadValue();
            Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -Camera.main.transform.position.z));
            targetPos = new Vector2(world.x, world.y);
        }

        // 2. [신규] 상호작용 키(F) 입력 처리 -> 감실 호출
        if (keyboard[interactKey].wasPressedThisFrame)
        {
            RequestPrayerEntry();
        }
    }

    // 감실에 기도 요청 보내기
    private void RequestPrayerEntry()
    {
        if (gamsilManager == null || myStateController == null) return;

        // 현재 상태가 Idle일 때만 호출 가능
        if (myStateController.CurrentState == CardinalState.Idle)
        {
            gamsilManager.RegisterPlayerToQueue(myStateController);
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
