// BlindResult.cs
// 블라인드 징수 결과를 담는 데이터 클래스.
// 갱신된 GameRoundState, SB/BB 실제 징수 금액, 발생한 BlindPostedEvent 목록을 포함한다.
// 사용법: BlindPostingUsecase.PostBlinds()의 반환값으로 사용된다.

using System.Collections.Generic;

namespace TexasHoldem.Entity
{
    public class BlindResult
    {
        public GameRoundState State { get; }
        public int SbActual { get; }
        public int BbActual { get; }
        public IReadOnlyList<BlindPostedEvent> Events { get; }

        public BlindResult(GameRoundState state, int sbActual, int bbActual, IReadOnlyList<BlindPostedEvent> events)
        {
            State = state;
            SbActual = sbActual;
            BbActual = bbActual;
            Events = events;
        }
    }
}
