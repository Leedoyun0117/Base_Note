using GameName.Core.MemoryRooms;
using GameName.UI.Session;
using NUnit.Framework;
using UnityEngine;

namespace GameName.UI.Tests.EditMode
{
    // RoomSceneMap의 조회(SceneFor)와 전환 결정(Plan)만 검증한다. 실제 씬
    // 로드/언로드는 RoomSceneLoader의 몫이고, 여기서 볼 것은 "지금 방에 맞추려면
    // 무엇을 내리고 무엇을 올려야 하는가"가 옳게 나오는가다.
    public class RoomSceneMapTests
    {
        private static RoomSceneMap Map(params (string room, string scene)[] entries)
        {
            var asset = ScriptableObject.CreateInstance<RoomSceneMap>();
            var json = new System.Text.StringBuilder("{\"_entries\":[");
            for (var i = 0; i < entries.Length; i++)
            {
                if (i > 0) json.Append(',');
                json.Append($"{{\"RoomId\":\"{entries[i].room}\",\"SceneName\":\"{entries[i].scene}\"}}");
            }
            json.Append("]}");
            JsonUtility.FromJsonOverwrite(json.ToString(), asset);
            return asset;
        }

        [Test]
        public void SceneFor_대소문자와_공백을_무시하고_찾는다()
        {
            var map = Map(("room-1", "Room_Greenroom"), ("room-2", "Room_ClockTower"));

            Assert.AreEqual("Room_Greenroom", map.SceneFor("room-1"));
            Assert.AreEqual("Room_Greenroom", map.SceneFor("  ROOM-1 "));
            Assert.AreEqual("Room_ClockTower", map.SceneFor(new MemoryRoomId("room-2")));
        }

        [Test]
        public void SceneFor_매핑이_없거나_비면_null()
        {
            var map = Map(("room-1", "Room_Greenroom"), ("room-2", ""));

            Assert.IsNull(map.SceneFor("room-3"));
            Assert.IsNull(map.SceneFor("room-2"), "SceneName이 비면 null이어야 한다.");
            Assert.IsNull(map.SceneFor(""));
        }

        [Test]
        public void Plan_첫_진입은_올리기만_한다()
        {
            var map = Map(("room-1", "Room_Greenroom"));

            var swap = map.Plan(currentScene: null, nextRoomId: "room-1");

            Assert.IsNull(swap.ToUnload);
            Assert.AreEqual("Room_Greenroom", swap.ToLoad);
            Assert.IsFalse(swap.NoChange);
        }

        [Test]
        public void Plan_같은_방이면_아무것도_안_한다()
        {
            var map = Map(("room-1", "Room_Greenroom"));

            var swap = map.Plan(currentScene: "Room_Greenroom", nextRoomId: "room-1");

            Assert.IsTrue(swap.NoChange);
        }

        [Test]
        public void Plan_다른_방이면_내리고_올린다()
        {
            var map = Map(("room-1", "Room_Greenroom"), ("room-2", "Room_ClockTower"));

            var swap = map.Plan(currentScene: "Room_Greenroom", nextRoomId: "room-2");

            Assert.AreEqual("Room_Greenroom", swap.ToUnload);
            Assert.AreEqual("Room_ClockTower", swap.ToLoad);
        }

        [Test]
        public void Plan_매핑_없는_방으로_가면_내리기만_한다()
        {
            var map = Map(("room-1", "Room_Greenroom"));

            var swap = map.Plan(currentScene: "Room_Greenroom", nextRoomId: "room-3");

            Assert.AreEqual("Room_Greenroom", swap.ToUnload);
            Assert.IsNull(swap.ToLoad, "아직 안 만든 방 씬 — 배경 없이 진행한다.");
        }

        [Test]
        public void Plan_매핑도_현재_씬도_없으면_아무것도_안_한다()
        {
            var map = Map(("room-1", "Room_Greenroom"));

            var swap = map.Plan(currentScene: null, nextRoomId: "room-3");

            Assert.IsTrue(swap.NoChange);
        }
    }
}
