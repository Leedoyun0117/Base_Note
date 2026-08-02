using GameName.Core.Commissions;
using GameName.Core.MemoryRooms;

namespace GameName.UI.Flow
{
    // "지금 위치 + 의뢰 단계"에서 "어느 화면을 보여줄지"를 고르는 순수 판정.
    // SceneScreenSwitcher(MonoBehaviour)가 이 결과대로 GameObject를 켜고 끌
    // 뿐이다 — 판정 자체를 MonoBehaviour 안에 묻어두면 씬/컴포넌트 없이는
    // 검증할 수 없어, 이 로직만 따로 뽑아 둔다.
    public static class ActiveScreenSelector
    {
        public static ActiveScreen Select(
            CommissionStage stage,
            MemoryGraphNodeId currentPosition,
            MemoryGraphNodeId perfumeryRoomNodeId,
            MemoryGraphNodeId analysisRoomNodeId)
        {
            if (stage == CommissionStage.ReturnedToReality)
                return ActiveScreen.FinalCrafting;

            if (stage == CommissionStage.Completed)
                return ActiveScreen.Completion;

            if (stage != CommissionStage.InMemory)
                return ActiveScreen.None;

            if (currentPosition.Equals(perfumeryRoomNodeId))
                return ActiveScreen.Perfumery;

            if (currentPosition.Equals(analysisRoomNodeId))
                return ActiveScreen.AnalysisRoom;

            return ActiveScreen.MemoryRoom;
        }
    }
}
