using System;
using GameName.Core.Memories;
using UnityEngine;

namespace GameName.UI.Authoring
{
    // 색 이름을 저작 데이터로 들고 있는 자리.
    //
    // "[파란색]"이라는 글자가 코드 어디에도 없어야 하기 때문에 있는 에셋이다.
    // 색 이름은 번역 대상이고, 복원도의 색 뿌리 노드 라벨·추리 지원 UI 등이
    // 같은 세 이름을 쓴다.
    [CreateAssetMenu(menuName = "GameName/Memory Color Labels", fileName = "MemoryColorLabels")]
    public sealed class MemoryColorLabelAsset : ScriptableObject
    {
        [SerializeField] private string _redName;
        [SerializeField] private string _greenName;
        [SerializeField] private string _blueName;

        public string DisplayName(MemoryColor color) => GetName(color);

        private string GetName(MemoryColor color)
        {
            switch (color)
            {
                case MemoryColor.Red: return _redName;
                case MemoryColor.Green: return _greenName;
                case MemoryColor.Blue: return _blueName;
                default: throw new ArgumentOutOfRangeException(nameof(color), color, null);
            }
        }
    }
}
