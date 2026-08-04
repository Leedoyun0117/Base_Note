using GameName.Core.MemoryRooms;
using GameName.UI.Shared;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    // MemoryMapLayout은 화면 크기/해상도를 아예 인자로 받지 않는다 — 그
    // 계약 자체가 "화면 크기와 무관하다"는 증명이다. 이 테스트는 같은 좌표를
    // 몇 번을 물어도, 그리고 서로 다른 좌표라도 항상 같은 선형 공식
    // (origin + coordinate * cellSize)으로 일관되게 계산됨을 확인한다.
    public class MemoryMapLayoutTests
    {
        [Test]
        public void 같은_좌표는_몇_번을_변환해도_항상_같은_위치를_돌려준다()
        {
            var layout = new MemoryMapLayout(cellSize: 96f);
            var coordinate = new MemoryGraphCoordinate(2, 3);

            var first = layout.ToLocalPosition(coordinate);
            var second = layout.ToLocalPosition(coordinate);
            var third = layout.ToLocalPosition(coordinate);

            Assert.AreEqual(first, second);
            Assert.AreEqual(second, third);
        }

        [Test]
        public void 좌표는_칸_크기와_원점의_선형_공식으로_변환된다()
        {
            var layout = new MemoryMapLayout(cellSize: 100f, originX: 10f, originY: 20f);

            var position = layout.ToLocalPosition(new MemoryGraphCoordinate(2, 3));

            Assert.AreEqual(10f + 2 * 100f, position.x);
            Assert.AreEqual(20f + 3 * 100f, position.y);
        }

        [Test]
        public void 서로_다른_칸_크기를_가진_두_지도_인스턴스가_같은_좌표를_서로_다르게_변환해도_각자는_일관된다()
        {
            // "화면 크기"가 바뀌는 것이 아니라 지도 축척(CellSize) 자체가 다른
            // 두 인스턴스를 만들어도, 각 인스턴스는 자기 축척 하나로만
            // 일관되게 계산해야 한다 — 어느 쪽도 외부 화면 크기를 참조하지
            // 않는다.
            var smallLayout = new MemoryMapLayout(cellSize: 32f);
            var largeLayout = new MemoryMapLayout(cellSize: 256f);
            var coordinate = new MemoryGraphCoordinate(1, 1);

            var smallResult = smallLayout.ToLocalPosition(coordinate);
            var largeResult = largeLayout.ToLocalPosition(coordinate);

            Assert.AreEqual(32f, smallResult.x);
            Assert.AreEqual(256f, largeResult.x);
        }
    }
}
