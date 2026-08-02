using System;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Judging;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Ampoules
{
    // 기억 방에서 앰플을 시향하는 처리기.
    //
    // 정답(MemoryRoomAnswer)을 인자로 받지 않는다 — 목표 방은 앰플이 이미 알고
    // 있으므로(Ampoule.TargetRoomId), 정답은 IMemoryRoomAnswerRepository에서
    // 그 식별자로 직접 조회한다. 호출부(UI 등)가 정답을 손에 쥔 채로 넘겨야만
    // 시향을 요청할 수 있는 구조를 원천적으로 없애기 위함이다.
    //
    // 조향(제작)과는 장소도, 조건도, 책임도 달라 별도 타입으로 분리한다. 판정
    // 공식은 IScentJudge에 위임하고, 복원 여부 판단과 그에 따른 정신력 회복·
    // 사다리 해금은 전혀 알지 못한다 — IMemoryRoomRestorationTracker에 판정
    // 결과만 보고하면, 복원 판정과 이어지는 정신력 회복(RoomRestorationRecoveryAdapter)은
    // 전부 이벤트로 흘러간다.
    //
    // 이미 복원된 방에 다시 시향해도 막지 않는다. 막으려면 "차단할 때 앰플을
    // 소모할지 보존할지"라는 새 분기가 필요해지고, 이는 "사용한 앰플은 판정
    // 결과와 무관하게 사라진다"는 단순한 불변식을 깨뜨린다. 복원 자체는
    // IMemoryRoomRestorationTracker가 이미 멱등적으로 처리하므로(같은 방을 다시
    // 보고해도 이벤트가 중복 발행되지 않는다) 재시향을 허용해도 정신력 회복이나
    // 사다리 해금을 중복으로 얻을 방법은 없다 — 재시향의 유일한 대가는 플레이어
    // 스스로 선택해 소모하는 앰플 하나뿐이며, 그 선택을 막을 이유가 없다.
    public sealed class ScentTestingProcessor
    {
        private readonly IPlayerLocation _playerLocation;
        private readonly IPlayerInventory _inventory;
        private readonly IScentJudge _scentJudge;
        private readonly IMemoryRoomRestorationTracker _restorationTracker;
        private readonly IMemoryRoomAnswerRepository _answerRepository;
        private readonly IEventBus _eventBus;

        public ScentTestingProcessor(
            IPlayerLocation playerLocation,
            IPlayerInventory inventory,
            IScentJudge scentJudge,
            IMemoryRoomRestorationTracker restorationTracker,
            IMemoryRoomAnswerRepository answerRepository,
            IEventBus eventBus)
        {
            _playerLocation = playerLocation ?? throw new ArgumentNullException(nameof(playerLocation));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _scentJudge = scentJudge ?? throw new ArgumentNullException(nameof(scentJudge));
            _restorationTracker = restorationTracker ?? throw new ArgumentNullException(nameof(restorationTracker));
            _answerRepository = answerRepository ?? throw new ArgumentNullException(nameof(answerRepository));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public ScentTestResult Test(Ampoule ampoule)
        {
            if (ampoule == null) throw new ArgumentNullException(nameof(ampoule));

            if (!ContainsAmpoule(ampoule))
                return ScentTestResult.Failure(ScentTestFailureReason.AmpouleNotInInventory);

            if (!_playerLocation.Current.Equals(MemoryGraphNodeId.OfRoom(ampoule.TargetRoomId)))
                return ScentTestResult.Failure(ScentTestFailureReason.WrongRoom);

            // 목표 방에 정답이 등록되어 있지 않은 것은 플레이어가 어찌할 수 있는
            // 정상적인 실패가 아니다 — 앰플의 목표 방은 조향 시점에 이미
            // IMemoryRoomPublicInfoRepository로 존재가 확인된 방이어야 하므로,
            // 여기서 정답이 없다는 것은 데이터 정합성이 깨졌다는 뜻이다. 그래서
            // ScentTestFailureReason에 새 값을 추가하는 대신 예외로 드러낸다.
            if (!_answerRepository.TryGetAnswer(ampoule.TargetRoomId, out var answer))
            {
                throw new InvalidOperationException(
                    $"목표 방({ampoule.TargetRoomId})에 등록된 정답이 없다.");
            }

            var judgement = _scentJudge.Judge(ampoule.Scent, answer);

            // 판정 결과와 무관하게 앰플은 사라진다.
            _inventory.TryRemove(ampoule);

            _restorationTracker.ReportJudgement(answer.RoomId, judgement);
            _eventBus.Publish(new ScentJudgedEvent(ampoule.Id, answer.RoomId, ampoule.Scent, judgement));

            return ScentTestResult.Success(judgement);
        }

        // IPlayerInventory.Items는 IReadOnlyList라 LINQ 없이 직접 순회한다.
        private bool ContainsAmpoule(Ampoule ampoule)
        {
            foreach (var item in _inventory.Items)
            {
                if (item.Equals(ampoule))
                    return true;
            }

            return false;
        }
    }
}
