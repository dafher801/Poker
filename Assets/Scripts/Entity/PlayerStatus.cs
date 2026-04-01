// PlayerStatus.cs
// 텍사스 홀덤에서 플레이어의 현재 상태를 나타내는 열거형.
// 게임 진행 중 플레이어가 취할 수 있는 다섯 가지 상태를 정의한다.

namespace TexasHoldem.Entity
{
    public enum PlayerStatus
    {
        Waiting,    // 게임 시작 대기 중
        Active,     // 현재 게임에 참여 중
        Folded,     // 이번 핸드를 포기함
        AllIn,      // 모든 칩을 베팅한 상태
        Eliminated  // 칩이 없어 게임에서 제거됨
    }
}
