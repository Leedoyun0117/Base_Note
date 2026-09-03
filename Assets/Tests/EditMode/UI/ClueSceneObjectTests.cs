using GameName.Core.Clues;
using GameName.UI.MemoryRoom.Space;
using NUnit.Framework;
using UnityEngine;

namespace GameName.UI.Tests.EditMode
{
    // 마우스 오버 표시를 검증한다. 실제 마우스를 움직이지 않고 "가리켜졌다"는
    // 사실만 전달하는 이유: 어디를 가리켰는지 찾는 일은 ScenePointerInput의
    // 몫이고, 여기서 확인할 것은 그 사실을 받았을 때 테두리가 실제로 켜지는가다.
    public class ClueSceneObjectTests
    {
        private static (ClueSceneObject Clue, SpriteRenderer Outline, GameObject Root) MakeClueObject()
        {
            var root = new GameObject("Clue");
            var outlineObject = new GameObject("Outline");
            outlineObject.transform.SetParent(root.transform);
            var bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(root.transform);

            var outline = outlineObject.AddComponent<SpriteRenderer>();
            var body = bodyObject.AddComponent<SpriteRenderer>();
            var collider = root.AddComponent<BoxCollider2D>();
            var clue = root.AddComponent<ClueSceneObject>();
            clue.Initialize(new ClueId("clue-1"), outline, body, collider, drawOrder: 5);

            return (clue, outline, root);
        }

        [Test]
        public void 처음에는_테두리가_보이지_않는다()
        {
            var (clue, outline, root) = MakeClueObject();
            try
            {
                Assert.IsFalse(outline.enabled);
                Assert.IsFalse(clue.IsOutlineVisible);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void 마우스를_올리면_흰_테두리가_켜지고_벗어나면_꺼진다()
        {
            var (clue, outline, root) = MakeClueObject();
            try
            {
                clue.SetHovered(true);
                Assert.IsTrue(outline.enabled);
                Assert.IsTrue(clue.IsOutlineVisible);

                clue.SetHovered(false);
                Assert.IsFalse(outline.enabled);
                Assert.IsFalse(clue.IsOutlineVisible);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void 눌리면_자기_식별자만_알린다()
        {
            var (clue, _, root) = MakeClueObject();
            try
            {
                ClueId? activated = null;
                clue.Activated += id => activated = id;

                clue.Activate();

                Assert.IsTrue(activated.HasValue);
                Assert.AreEqual(new ClueId("clue-1"), activated.Value);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void 가시_영역_밖이면_콜라이더가_꺼지고_눌러도_알리지_않는다()
        {
            var root = new GameObject("Clue");
            try
            {
                var outline = new GameObject("Outline").AddComponent<SpriteRenderer>();
                outline.transform.SetParent(root.transform);
                var body = new GameObject("Body").AddComponent<SpriteRenderer>();
                body.transform.SetParent(root.transform);
                var collider = root.AddComponent<BoxCollider2D>();
                var clue = root.AddComponent<ClueSceneObject>();
                clue.Initialize(new ClueId("clue-1"), outline, body, collider, drawOrder: 5);

                clue.SetAccessible(false);

                Assert.IsFalse(collider.enabled, "가시 밖 단서의 콜라이더가 살아 있다.");
                Assert.IsFalse(clue.IsAccessible);

                var activated = false;
                clue.Activated += _ => activated = true;
                clue.Activate();
                Assert.IsFalse(activated, "가시 밖 단서를 눌렀는데 활성화 사건이 나갔다.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
