// RoundPhase.cs
// 한 핸드(게임 라운드)의 진행 단계를 나타내는 열거형.
// GameRoundUsecase가 현재 라운드의 단계를 추적하는 데 사용한다.

namespace TexasHoldem.Entity
{
    public enum RoundPhase
    {
        None,      // 라운드 시작 전 초기 상태
        PreFlop,   // 홀 카드 배분 후 첫 번째 베팅 라운드
        Flop,      // 커뮤니티 카드 3장 공개 후 베팅
        Turn,      // 커뮤니티 카드 4번째 공개 후 베팅
        River,     // 커뮤니티 카드 5번째 공개 후 베팅
        Showdown,  // 패 비교 및 승자 결정
        Complete   // 라운드 종료, 칩 분배 완료
    }
}
