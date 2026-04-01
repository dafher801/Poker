# Poker2D

Unity 2D 포커 게임. 바이브 코딩의 한계를 검증하기 위해 Claude Code로 개발을 진행한다.
멀티플레이, AI 등 포커의 모든 기능을 구현할 예정.

## 환경

- Unity 6000.3.6f1
- 플랫폼: Windows
- 멀티플레이: Photon (최대 10인)

## 아키텍처

5-Layer 구조. 참조 방향: **View → Director → Usecase → Gateway → Entity**
같은 레이어, 상위 레이어, 두 계층 이상 차이나는 레이어 간 참조 금지. 각 계층은 바로 아래 계층만으로 구현 가능해야 한다.

| 레이어 | 위치 | 역할 | 비고 |
|---------|------|------|------|
| Entity | Assets/Scripts/Entity | 데이터 + 데이터 자체의 규칙 (예: 값 범위 제한) | 유니티 코드 X |
| Gateway | Assets/Scripts/Gateway | 데이터 입출력 (유저 입력, 네트워크, 메모리, 파일 I/O) | 유니티 코드 X |
| Usecase | Assets/Scripts/Usecase | 핵심 게임 로직 (패 판정, 베팅 처리 등) | 유니티 코드 X |
| Director | Assets/Scripts/Director | Usecase-View 중재, 흐름 제어, 상태 머신 | 유니티 코드 불가피한 경우만 허용, 최소화할 것 |
| View | Assets/Scripts/View | UI, 애니메이션 트리거, 유저 조작 | 유니티 코드 사용 가능 |

## 작업 규칙

- `ProjectPlan/` 폴더에 전체 계획이 있다.
  - Project → Milestone N → Epic N-M → Task 순의 계층 구조. (다수의 Task는 Epic N-M 파일 내에 JSON으로 존재: Task N-M-1 ~ Task N-M-Z)
- 모든 작업은 내 요구에 따라 **Task 단위 순서대로, 단계별로** 진행한다. 다른 Task를 멋대로 진행하면 안된다는 뜻이다.
- **소스 코드 작성이 아닌 작업**(UI 레이아웃 미세 조정, 에셋 배치 등)은 코드를 건드리지 않고 다음을 출력한다:
  > "이것은 소스 코드를 작성하는 업무가 아닙니다. 이것은 ____한 업무입니다."

## 컨벤션

- C# 네이밍: PascalCase (클래스, 메서드, 프로퍼티), _camelCase (private 필드)
- 한 파일에 한 클래스
- 모든 소스 파일 상단에 코드의 역할과 사용 방법을 주석으로 명시
- 테스트를 위해 필요한 코드는 `Assets/Tests/`내부 혹은 그 하위 폴더에 배치
- 불필요한 `using` 제거
- `Library/`, `Temp/`, `obj/` 수정 금지