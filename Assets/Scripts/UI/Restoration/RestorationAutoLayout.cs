using GameName.Core.Memories;
using GameName.Core.Restoration;

namespace GameName.UI.Restoration
{
    // 아직 자리를 잡지 않은 노드(Position이 원점)에 겹치지 않는 초기 자리를
    // 정해 준다. 이 값은 곧바로 MoveNode로 Core에 적어 넣으므로(컨트롤러가
    // 처리), 한 번 놓인 뒤로는 플레이어 드래그가 진실이 된다 — 여기서 다시
    // 계산하지 않는다.
    //
    // 색마다 캔버스에 나란한 세로 띠 하나씩. 스케치처럼 세 클러스터가 떨어져
    // 있다. Core는 좌표계를 모르므로 이 픽셀 값 계산은 전부 화면 쪽에만 있다.
    public static class RestorationAutoLayout
    {
        // 캔버스 크기와 클러스터 간격. USS의 .restoration-canvas 크기와 맞춘다.
        public const float CanvasWidth = 1740f;
        public const float CanvasHeight = 1180f;

        private const float BandWidth = 560f;
        private const float BandLeftPadding = 40f;

        // 한 색 띠 안에서 종류·순번에 따른 자리.
        //   · 뿌리: 띠 위쪽 가운데
        //   · 단서 노드: 뿌리 아래로 2열 격자
        //   · 플레이어 노드: 그 아래로 살짝씩 어긋나게 쌓는다(겹침 방지)
        public static BoardPosition SlotFor(MemoryColor color, RestorationNodeKind kind, int indexWithinKind)
        {
            var bandLeft = BandLeftPadding + BandIndex(color) * BandWidth;

            switch (kind)
            {
                case RestorationNodeKind.ColorRoot:
                    return new BoardPosition(bandLeft + 180f, 48f);

                case RestorationNodeKind.ClueLinked:
                {
                    var col = indexWithinKind % 2;
                    var rowIndex = indexWithinKind / 2;
                    return new BoardPosition(bandLeft + 20f + col * 250f, 210f + rowIndex * 132f);
                }

                default: // PlayerAuthored
                    return new BoardPosition(
                        bandLeft + 90f + indexWithinKind * 28f,
                        640f + indexWithinKind * 30f);
            }
        }

        private static int BandIndex(MemoryColor color)
        {
            switch (color)
            {
                case MemoryColor.Red: return 0;
                case MemoryColor.Green: return 1;
                case MemoryColor.Blue: return 2;
                default: return 0;
            }
        }
    }
}
