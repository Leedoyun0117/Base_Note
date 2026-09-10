using System;
using System.Collections.Generic;
using System.Text;
using GameName.Core.Complexes;
using GameName.Core.Events;
using UnityEngine;

namespace GameName.UI.MemoryRoom
{
    // 단서를 읽을 때마다 "원본 태그 → 각 컴플렉스 단계 → 최종 태그"를 콘솔에
    // 한 덩어리로 찍는다. 해석 로그 UI를 따로 만들지 않는 대신, 컴플렉스가
    // 태그를 실제로 바꾸는지 플레이 중 눈으로 확인하는 창구다.
    //
    // 규칙은 하나도 판단하지 않는다 — ClueInterpretedEvent를 문자열로 옮길 뿐.
    public sealed class ChainStepConsoleLog : IDisposable
    {
        private const string Tag = "[해석]";

        private readonly IDisposable _subscription;

        public ChainStepConsoleLog(IEventBus eventBus)
        {
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));
            _subscription = eventBus.Subscribe<ClueInterpretedEvent>(OnInterpreted);
        }

        private static void OnInterpreted(ClueInterpretedEvent e)
        {
            var sb = new StringBuilder(Tag);
            sb.Append(" 원본 ").Append(Join(e.SourceTags));

            if (e.Steps != null && e.Steps.Count > 0)
            {
                foreach (var step in e.Steps)
                    sb.Append("\n  → ").Append(step.ComplexId.Value)
                      .Append(" [").Append(step.Kind).Append("]  ")
                      .Append(Join(step.TagsBefore)).Append("  ⇒  ").Append(Join(step.TagsAfter));
            }
            else
            {
                sb.Append("\n  (작용한 컴플렉스 없음)");
            }

            sb.Append("\n최종 ").Append(Join(e.FinalTags));
            Debug.Log(sb.ToString());
        }

        private static string Join(IReadOnlyList<StoryTag> tags)
        {
            if (tags == null || tags.Count == 0) return "—";

            var sb = new StringBuilder();
            for (var i = 0; i < tags.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(tags[i]);
            }

            return sb.ToString();
        }

        public void Dispose() => _subscription.Dispose();
    }
}
