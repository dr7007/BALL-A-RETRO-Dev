# Next Fest 구현 상태

기준: 2026-11-11, `PSH_ALPHA`, 시작 HEAD `5fa6286`.

## 작업 범위

사용자 결정: 현재 데모 고정, 본편만 확장. 다시 뽑기는 선택 화면마다 3회.
프로젝트 및 상위 디렉터리에서 별도 `AGENTS.md`는 발견되지 않았다. README의 Unity 6000.0.60f1, 외부 격리 검사 및 다국어 테이블 동기화 절차를 따른다.

## 확인된 상태

- 초기 미커밋 사용자 변경: `Assets/Public/Font/Galmuri11 SDF.asset`, `Galmuri11 SDF_dialogue.asset`, `Galmuri11-Bold SDF.asset`, `Assets/MemberFolder/KHS/KHS_Materials/KHS_Mat_Ball.mat`.
- 보호 파일과 참고 출시 기록의 시작 SHA-256: `D:/ball-a-retro-validation/nextfest-20261111/protected-before.json`.
- 데모 코드 기준: 3라운드/총 3선택, 9효과+2전략에서 유효 3개 추첨, Tab, 3언어, 별도 Steam/개인 기록 확인. 실제 실행 회귀 결과는 아래에 추가한다.
- 2026-11-11 Steamworks 데모 페이지 실조회: 상점 및 빌드 모두 Review queue, Submitted on 8 Nov, Unavailable. 심사 완료나 공개 출시로 간주하지 않는다.
- 관찰 URL: https://partner.steamgames.com/apps/landing/5585210 . 출시 담당으로부터도 동일 상태 및 default `26525930`, Next Fest 미등록이라는 관찰을 전달받았다. 출시 기록은 이 작업에서 수정하지 않는다.

## 진행

| 기능 | 상태 | 커밋·검증 |
|---|---|---|
| 코드 진단·첫 범위 | 작성 | `PLAN.md` |
| 변경 전 성능 | 측정 | `9ab3b79`, baseline/performance.json: Roll3 중앙값 4.29945 μs, 직접 득점 0.02979 μs |
| 본편 축전/마지막 공 조합 | 모델 검사 통과 | `4934419`, build-combo-v1: 10/10. 실제 범퍼/블랙홀 콜백, 중첩 상한, 공 소진, 라운드 경계, 일시정지, 종료, 초기화 포함. 실제 씬 검사는 후속 |
| 본편 기록 v2 | 로컬/UI 검사 통과 | records-v2b: 16/16. `BALLARETRO_v2_Classic_1`, `BallARetro.STOVE.Best.v2.*`. 레거시 키 보존 검사. Steam 서버 인증은 별도 |
| 본편 등급별 추첨·화면당 3회 재추첨 | 예정 | 사용자 확정 범위 |
| 실제 씬·다국어·입력·종료 회귀 | 예정 | 외부 격리 프로젝트 |

## 알려진 문제 / 아직 주장하지 않는 것

- 보너스 맵 누적 총점 곱셈의 과도한 우위, 같은 트리거 재진입에 의한 중복 정산은 별도 검증이 필요하다.
- 현재 본편은 남은 공 수에 비례해 강화 횟수가 늘어나는 구조다. 이번에 임의로 새 라운드/영구 성장 규칙을 확정하지 않는다.
- 물리 플레이의 재미·숙련도별 밸런스·저사양 FPS는 자동 테스트 통과만으로 입증되지 않는다.
- Steam 인증·실제 서버 업로드와 공개 출시 승인은 별도 항목이다. 본편 로컬 검증을 현행 데모 출시 확인으로 보고하지 않는다.
- CPU 미세 측정은 11배치 × 추첨 2,000회/득점 10,000회. 이 Mono 환경의 `GC.GetAllocatedBytesForCurrentThread`가 할당이 있는 추첨에서도 0을 반환하므로 JSON의 0을 무할당 증거로 사용하지 않는다. FPS/프레임 할당은 별도 측정 대상이다.
- 첫 조합 추가 직후 미세 측정: Roll3 13.26725 μs, 직접 득점 0.10944 μs. 새로운 후보 2장과 조건 검사 비용이 포함되며 최종 추첨 구현 후 다시 비교한다.
- 고정 데모 회귀 frozen-demo-v1: 24/24 통과. 데모의 기존 균등 추첨, 3선택, 기존 충전 유지 규칙을 검사한다.
