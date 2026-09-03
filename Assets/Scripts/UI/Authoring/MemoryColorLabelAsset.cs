using System;
using GameName.Core.Dialogue;
using GameName.Core.Memories;
using UnityEngine;

namespace GameName.UI.Authoring
{
    // 가려진 구간에 대신 그릴 문구를 저작 데이터로 들고 있는 자리.
    //
    // "[파란색]"이라는 글자가 코드 어디에도 없어야 하기 때문에 있는 에셋이다.
    // 색 이름은 번역 대상이고, 감싸는 표기도 연출에 따라 바뀐다(괄호 대신 기호로
    // 덮는다든지). 둘을 나눠 둔 것은 표기만 바꾸려고 세 이름을 다시 적는 일이
    // 없게 하기 위해서다.
    [CreateAssetMenu(menuName = "GameName/Memory Color Labels", fileName = "MemoryColorLabels")]
    public sealed class MemoryColorLabelAsset : ScriptableObject, ICensorMaskFormatter
    {
        [SerializeField] private string _redName;
        [SerializeField] private string _greenName;
        [SerializeField] private string _blueName;

        // 색 이름을 감쌀 형태. {0} 자리에 색 이름이 들어간다.
        [SerializeField] private string _maskFormat = "[{0}]";

        public string FormatMask(MemoryColor color) => string.Format(_maskFormat, GetName(color));

        // 감싸는 표기 없는 색 이름 그대로. 복원도의 색 뿌리 노드 라벨처럼 "[]"가
        // 붙으면 안 되는 자리에서 쓴다 — 같은 세 이름을 다시 적지 않도록 여기서
        // 함께 내준다.
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
