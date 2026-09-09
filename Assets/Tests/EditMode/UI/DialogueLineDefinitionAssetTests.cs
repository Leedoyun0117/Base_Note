using System.Linq;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.UI.Authoring;
using NUnit.Framework;
using UnityEngine;

namespace GameName.UI.Tests.EditMode
{
    // DialogueLineDefinitionAsset의 ToDefinition()이 손으로 만든 Core 타입과
    // 같은 결과를 내는지 붙든다 — TextChoice든 ClueSelection이든.
    //
    // 직렬화 필드를 JsonUtility로 채운다. 리플렉션으로 private 필드를 하나씩
    // 건드리는 것보다 인스펙터가 저장하는 모양에 가깝고, 중첩 [Serializable]
    // ChoiceEntry 리스트도 그대로 들어간다.
    public class DialogueLineDefinitionAssetTests
    {
        private static DialogueLineDefinitionAsset FromJson(string json)
        {
            var asset = ScriptableObject.CreateInstance<DialogueLineDefinitionAsset>();
            JsonUtility.FromJsonOverwrite(json, asset);
            return asset;
        }

        [Test]
        public void TextChoice_라인은_기존_경로와_같은_결과를_낸다()
        {
            var asset = FromJson(@"{
                ""_lineId"": ""line-1"",
                ""_speaker"": ""유키"",
                ""_authoredText"": ""우리 그 시절 기억나?"",
                ""_lineKind"": 0,
                ""_choices"": [
                    { ""_choiceId"": ""c-yes"", ""_authoredText"": ""응"", ""_isCorrect"": true, ""_nextLineId"": ""line-2"", ""_conditionKind"": 0 },
                    { ""_choiceId"": ""c-gated"", ""_authoredText"": ""..."", ""_isCorrect"": false, ""_nextLineId"": """", ""_conditionKind"": 1, ""_requiredClueId"": ""ribbon"" }
                ]
            }");

            var line = asset.ToDefinition();

            Assert.AreEqual(DialoguePromptKind.TextChoice, line.PromptKind);
            Assert.AreEqual(new DialogueLineId("line-1"), line.Id);
            Assert.AreEqual("유키", line.Speaker);
            Assert.AreEqual("우리 그 시절 기억나?", line.AuthoredText);

            Assert.AreEqual(2, line.Choices.Count);
            Assert.AreEqual(new ChoiceId("c-yes"), line.Choices[0].Id);
            Assert.IsTrue(line.Choices[0].IsCorrect);
            Assert.AreEqual(new DialogueLineId("line-2"), line.Choices[0].Next);
            Assert.AreEqual(ChoiceConditionKind.None, line.Choices[0].Condition.Kind);

            Assert.IsFalse(line.Choices[1].Next.HasValue, "빈 nextLineId는 null이어야 한다.");
            Assert.AreEqual(ChoiceConditionKind.ClueUsed, line.Choices[1].Condition.Kind);
            Assert.AreEqual(new ClueId("ribbon"), line.Choices[1].Condition.RequiredClue.Value);

            CollectionAssert.IsEmpty(line.RequiredTags);
        }

        [Test]
        public void ClueSelection_라인은_팩토리_경로와_같은_결과를_낸다()
        {
            var asset = FromJson(@"{
                ""_lineId"": ""r2-q"",
                ""_speaker"": ""유키"",
                ""_authoredText"": ""그때 뭘 쥐고 있었어?"",
                ""_lineKind"": 1,
                ""_requiredTags"": [ ""room2.heldItem"", ""room2.other"", """" ],
                ""_correctNextLineId"": ""r2-q-right"",
                ""_incorrectNextLineId"": ""r2-q-wrong""
            }");

            var line = asset.ToDefinition();
            var expected = DialogueLineDefinition.ClueSelection(
                new DialogueLineId("r2-q"), "유키", "그때 뭘 쥐고 있었어?",
                new[] { new ClueTag("room2.heldItem"), new ClueTag("room2.other") },
                new DialogueLineId("r2-q-right"), new DialogueLineId("r2-q-wrong"));

            Assert.AreEqual(expected.PromptKind, line.PromptKind);
            Assert.AreEqual(DialoguePromptKind.ClueSelection, line.PromptKind);
            Assert.AreEqual(expected.Id, line.Id);
            Assert.AreEqual(expected.Speaker, line.Speaker);
            Assert.AreEqual(expected.AuthoredText, line.AuthoredText);
            Assert.AreEqual(expected.CorrectNext, line.CorrectNext);
            Assert.AreEqual(expected.IncorrectNext, line.IncorrectNext);
            CollectionAssert.AreEqual(
                expected.RequiredTags.Select(t => t.Value).ToArray(),
                line.RequiredTags.Select(t => t.Value).ToArray());
            Assert.AreEqual(2, line.RequiredTags.Count, "빈 태그 칸은 걸러진다.");
            CollectionAssert.IsEmpty(line.Choices);
        }

        [Test]
        public void 분기_id를_안_채운_ClueSelection_라인은_터지지_않고_null_분기로_들어온다()
        {
            var asset = FromJson(@"{
                ""_lineId"": ""q"",
                ""_speaker"": ""유키"",
                ""_authoredText"": ""?"",
                ""_lineKind"": 1,
                ""_requiredTags"": [],
                ""_correctNextLineId"": """",
                ""_incorrectNextLineId"": """"
            }");

            DialogueLineDefinition line = null;
            Assert.DoesNotThrow(() => line = asset.ToDefinition());
            Assert.AreEqual(DialoguePromptKind.ClueSelection, line.PromptKind);
            Assert.IsFalse(line.CorrectNext.HasValue);
            Assert.IsFalse(line.IncorrectNext.HasValue);
            CollectionAssert.IsEmpty(line.RequiredTags);
        }
    }
}
