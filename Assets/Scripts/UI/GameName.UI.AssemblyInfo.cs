using System.Runtime.CompilerServices;

// 데모 저작 데이터(DemoGameData)는 internal이지만, 그 데이터가 검증기를 그대로
// 통과하는지는 저작 시점에 지켜져야 하는 계약이다. 테스트가 그 계약을 붙들 수
// 있도록 EditMode 테스트 어셈블리에만 internal을 연다.
[assembly: InternalsVisibleTo("GameName.UI.Tests.EditMode")]
