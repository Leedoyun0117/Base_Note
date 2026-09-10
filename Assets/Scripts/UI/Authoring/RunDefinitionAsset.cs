using System.Collections.Generic;
using GameName.Core.Authoring;
using UnityEngine;

namespace GameName.UI.Authoring
{
    // 한 판 전체의 저작 데이터를 에셋으로 들고 있는 자리. 이 에셋 하나가 곧
    // "이번에 플레이할 내용"이다.
    [CreateAssetMenu(menuName = "GameName/Run Definition", fileName = "RunDefinition")]
    public sealed class RunDefinitionAsset : ScriptableObject
    {
        [SerializeField] private List<RoomDefinitionAsset> _rounds = new List<RoomDefinitionAsset>();

        [Header("결정적 난수")]
        [SerializeField] private int _seed;

        [Header("안정 축")]
        [SerializeField] private int _startingStability;
        [SerializeField] private int _stabilityMin = -100;
        [SerializeField] private int _stabilityMax = 100;

        public IReadOnlyList<RoomDefinitionAsset> Rounds => _rounds;

        public RunDefinition ToDefinition()
        {
            var rounds = new List<RoomDefinition>(_rounds.Count);
            foreach (var round in _rounds)
            {
                if (round != null)
                    rounds.Add(round.ToDefinition());
            }

            return new RunDefinition(rounds, _seed, _startingStability, _stabilityMin, _stabilityMax);
        }
    }
}
