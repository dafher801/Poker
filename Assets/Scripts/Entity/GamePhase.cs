// GamePhase.cs
// 텍사스 홀덤 게임의 진행 단계를 나타내는 열거형.
// 한 핸드가 시작부터 결과 결정까지 거치는 다섯 가지 페이즈를 정의한다.

namespace TexasHoldem.Entity
{
    public enum GamePhase
    {
        PreFlop,   // 홀 카드 배분 후 첫 번째 베팅 라운드
        Flop,      // 커뮤니티 카드 3장 공개 후 베팅
        Turn,      // 커뮤니티 카드 4번째 공개 후 베팅
        River,     // 커뮤니티 카드 5번째 공개 후 베팅
        Showdown   // 패 비교 및 결과 결정
    }
}
