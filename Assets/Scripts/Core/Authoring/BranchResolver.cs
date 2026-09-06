using System.Collections.Generic;
using GameName.Core.Dialogue;

namespace GameName.Core.Authoring
{
    // 랜덤 분기 풀을 런 시작 시 한 번 확정하는 자리.
    //
    // 각 BranchPool마다 후보 하나를 시드로 골라 그 방의 DialogueLines에 병합한
    // 새 RunDefinition을 돌려준다. 확정본에는 풀이 남지 않는다 — 플레이 코드는
    // 고정 라인과 뽑힌 라인을 구분하지 않고 하나의 대화 그래프로 본다.
    //
    // 추첨은 (시드, 풀 슬롯 id)만으로 결정된다. 풀 순회 순서나 앞선 풀 개수에
    // 딸려가지 않으므로, 풀을 더하거나 자리를 바꿔도 기존 풀들의 결과는 그대로다.
    // 그래서 System.Random 대신 슬롯 id별 독립 해시를 쓴다.
    public static class BranchResolver
    {
        public static RunDefinition Resolve(RunDefinition raw, int seed)
        {
            if (raw == null) throw new System.ArgumentNullException(nameof(raw));

            var rooms = new List<RoomDefinition>(raw.Rooms.Count);
            foreach (var room in raw.Rooms)
                rooms.Add(ResolveRoom(room, seed));

            return new RunDefinition(
                rooms, raw.StartingTrust, raw.StartingHiromi, raw.Seed, raw.CensorKeyTagRequirements,
                raw.StartingChance, raw.MoveHiromiCost);
        }

        private static RoomDefinition ResolveRoom(RoomDefinition room, int seed)
        {
            if (room.BranchPools.Count == 0)
                return room;

            var lines = new List<DialogueLineDefinition>(room.DialogueLines);

            foreach (var pool in room.BranchPools)
            {
                // 후보가 없는 풀은 확정할 것이 없다. 저작 오류지만(검증기가 잡는다)
                // 여기서 터뜨리지 않고 조용히 건너뛴다 — 확정은 검증과 별개 단계다.
                if (pool.Candidates.Count == 0)
                    continue;

                var slotId = pool.Candidates[0].Id.Value;
                var index = (int)(StableHash(seed, slotId) % (uint)pool.Candidates.Count);
                lines.Add(pool.Candidates[index]);
            }

            // 풀은 확정본에서 비운다 — 이미 라인으로 녹아 들어갔다.
            return new RoomDefinition(room.Id, room.Clues, room.StartLineId, lines);
        }

        // FNV-1a. 시드 4바이트 + 슬롯 id 문자들을 순서대로 섞는다. 플랫폼·런타임에
        // 무관하게 같은 입력이면 같은 값이 나온다.
        private static uint StableHash(int seed, string slotId)
        {
            unchecked
            {
                const uint offsetBasis = 2166136261;
                const uint prime = 16777619;

                var hash = offsetBasis;
                var s = (uint)seed;
                for (var i = 0; i < 4; i++)
                {
                    hash = (hash ^ (s & 0xFF)) * prime;
                    s >>= 8;
                }

                foreach (var ch in slotId)
                {
                    hash = (hash ^ ((uint)ch & 0xFF)) * prime;
                    hash = (hash ^ (((uint)ch >> 8) & 0xFF)) * prime;
                }

                return hash;
            }
        }
    }
}
