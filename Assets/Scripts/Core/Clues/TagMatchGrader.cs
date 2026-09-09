using System.Collections.Generic;

namespace GameName.Core.Clues
{
    // 질문 태그 집합과 답(단서) 태그 집합이 얼마나 맞는지를 5단계로 매긴다.
    //
    // 순수하게 태그 구조만 본다 — 중심축(감정)이 맞는지, 곁축(장소·시간)이
    // 겹치는지, 축이 엇갈려 걸치는지. 심리 상태·안정 축에 따라 등급이 뒤집히는
    // 것은 여기 없다(그 조합은 별도 리졸버가 [10]에서 이 위에 얹는다).
    //
    // 태그 동치는 Axis를 보지 않고 Value로만 하므로, 축별 비교는 Value 집합을
    // 따로 갈라서 한다.
    public sealed class TagMatchGrader : ITagMatchGrader
    {
        public MatchGrade Grade(
            IReadOnlyList<ClueTag> questionTags, IReadOnlyList<ClueTag> answerTags)
        {
            var qCenter = Values(questionTags, ClueTagAxis.Center);
            var qSub = Values(questionTags, ClueTagAxis.Sub);
            var aCenter = Values(answerTags, ClueTagAxis.Center);
            var aSub = Values(answerTags, ClueTagAxis.Sub);

            if (Intersects(qCenter, aCenter))
            {
                // 답이 질문의 곁축을 (어느 축으로든) 전부 덮으면 완전적합. 질문에
                // 곁축이 없으면 중심축만으로 완전하다 — 빈 집합은 늘 덮인다.
                return CoversAll(qSub, aCenter, aSub) ? MatchGrade.Exact : MatchGrade.High;
            }

            // 중심축이 어긋났다 — 축을 엇갈려 걸치는지 본다.
            if (Intersects(aCenter, qSub) || Intersects(qCenter, aSub))
                return MatchGrade.Partial;

            if (Intersects(qSub, aSub))
                return MatchGrade.Low;

            return MatchGrade.None;
        }

        private static HashSet<string> Values(IReadOnlyList<ClueTag> tags, ClueTagAxis axis)
        {
            var set = new HashSet<string>();
            for (var i = 0; i < tags.Count; i++)
            {
                if (tags[i].Axis == axis)
                    set.Add(tags[i].Value);
            }

            return set;
        }

        private static bool Intersects(HashSet<string> a, HashSet<string> b)
        {
            foreach (var value in a)
            {
                if (b.Contains(value))
                    return true;
            }

            return false;
        }

        private static bool CoversAll(HashSet<string> needed, HashSet<string> a, HashSet<string> b)
        {
            foreach (var value in needed)
            {
                if (!a.Contains(value) && !b.Contains(value))
                    return false;
            }

            return true;
        }
    }
}
