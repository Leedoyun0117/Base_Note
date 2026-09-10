using GameName.Core.Complexes;
using GameName.UI.Authoring;
using NUnit.Framework;
using UnityEngine;

namespace GameName.UI.Tests.EditMode
{
    // ComplexDefinitionAsset → ComplexDefinition 라운드트립. 직렬화 필드는
    // JsonUtility.FromJsonOverwrite로 채운다(리플렉션 대신 — 다른 SO 테스트와 동일).
    public class ComplexDefinitionAssetTests
    {
        private static ComplexDefinitionAsset Make(string json)
        {
            var asset = ScriptableObject.CreateInstance<ComplexDefinitionAsset>();
            JsonUtility.FromJsonOverwrite(json, asset);
            return asset;
        }

        [Test]
        public void 이름과_설명과_규칙이_정의로_넘어간다()
        {
            var asset = Make(@"{
                ""_complexId"": ""complex-sink"",
                ""_displayName"": ""가라앉다"",
                ""_description"": ""결국 후회로 가라앉는 경향이 있습니다."",
                ""_priority"": 10,
                ""_durationTurns"": 3,
                ""_kind"": 0,
                ""_rules"": [ { ""_matchAxis"": 1, ""_matchValue"": """", ""_kind"": 0, ""_resultAxis"": 1, ""_resultValue"": ""후회"" } ]
            }");

            var definition = asset.ToDefinition();

            Assert.AreEqual("complex-sink", definition.Id.Value);
            Assert.AreEqual("가라앉다", definition.DisplayName);
            StringAssert.Contains("후회로 가라앉는", definition.Description);
            Assert.AreEqual(3, definition.DurationTurns);
            Assert.AreEqual(1, definition.Rules.Count);
            Assert.AreEqual(ComplexKind.Transform, definition.Rules[0].Kind);
        }

        [Test]
        public void 설명이_비면_빈_문자열이다()
        {
            var asset = Make(@"{ ""_complexId"": ""complex-x"", ""_durationTurns"": 2 }");

            Assert.AreEqual(string.Empty, asset.ToDefinition().Description);
        }
    }
}
