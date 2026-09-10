using System.Collections.Generic;
using GameName.UI.MemoryRoom.Space;
using NUnit.Framework;
using UnityEngine;

namespace GameName.UI.Tests.EditMode
{
    // 토글 규칙만 검증한다. 구독 배선(EventBus·Session)은 컴포넌트의 몫이고,
    // 여기서 확인할 것은 "활성 방의 오브젝트만 켜지는가"다.
    public class RoomArtSwitcherTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
                if (go != null)
                    Object.DestroyImmediate(go);
            _created.Clear();
        }

        private GameObject Obj(string name, bool startActive)
        {
            var go = new GameObject(name);
            go.SetActive(startActive);
            _created.Add(go);
            return go;
        }

        private RoomArtSwitcher.RoomArt Entry(string roomId, params GameObject[] objects) =>
            new RoomArtSwitcher.RoomArt { RoomId = roomId, Objects = objects };

        [Test]
        public void 활성_방의_오브젝트만_켜지고_나머지_방은_꺼진다()
        {
            var r1a = Obj("r1a", false);
            var r1b = Obj("r1b", false);
            var r2 = Obj("r2", true);
            var rooms = new[] { Entry("room-1", r1a, r1b), Entry("room-2", r2) };

            RoomArtSwitcher.Show(rooms, "room-1");

            Assert.IsTrue(r1a.activeSelf);
            Assert.IsTrue(r1b.activeSelf);
            Assert.IsFalse(r2.activeSelf);

            RoomArtSwitcher.Show(rooms, "room-2");

            Assert.IsFalse(r1a.activeSelf);
            Assert.IsFalse(r1b.activeSelf);
            Assert.IsTrue(r2.activeSelf);
        }

        [Test]
        public void 목록에_없는_방으로_넘어가면_전부_꺼진다()
        {
            var r1 = Obj("r1", true);
            var r2 = Obj("r2", true);
            var rooms = new[] { Entry("room-1", r1), Entry("room-2", r2) };

            RoomArtSwitcher.Show(rooms, "room-3");

            Assert.IsFalse(r1.activeSelf);
            Assert.IsFalse(r2.activeSelf);
        }

        [Test]
        public void 두_방_목록에_함께_든_오브젝트는_활성_방이_이긴다()
        {
            // 배경용 Global Light처럼 방마다 값이 다르지만 오브젝트 하나로
            // 공유되는 경우 — 활성 방 목록에 있으면 켜진 채로 끝나야 한다
            // (끄기 먼저 / 켜기 나중 순서).
            var shared = Obj("shared", false);
            var r2only = Obj("r2only", false);
            var rooms = new[]
            {
                Entry("room-1", shared),
                Entry("room-2", shared, r2only),
            };

            RoomArtSwitcher.Show(rooms, "room-2");
            Assert.IsTrue(shared.activeSelf, "활성 방(room-2) 목록에 있는데 꺼졌다.");
            Assert.IsTrue(r2only.activeSelf);

            RoomArtSwitcher.Show(rooms, "room-1");
            Assert.IsTrue(shared.activeSelf, "활성 방(room-1) 목록에 있는데 꺼졌다.");
            Assert.IsFalse(r2only.activeSelf);
        }

        [Test]
        public void 비어_있거나_null이어도_던지지_않는다()
        {
            Assert.DoesNotThrow(() => RoomArtSwitcher.Show(null, "room-1"));
            Assert.DoesNotThrow(() => RoomArtSwitcher.Show(new RoomArtSwitcher.RoomArt[0], "room-1"));

            var withHoles = new[]
            {
                new RoomArtSwitcher.RoomArt { RoomId = "room-1", Objects = null },
                new RoomArtSwitcher.RoomArt { RoomId = "room-2", Objects = new GameObject[] { null } },
            };
            Assert.DoesNotThrow(() => RoomArtSwitcher.Show(withHoles, "room-1"));
        }
    }
}
