# BALL-A-RETRO-Dev
BALL-A-RETRO Game Develope Repository

Unity Version : Unity 6.2 (6000.2.6f2)
->
Unity Version : Unity 6.0 (6000.0.60f1) [안정성 이슈로 변경]

현 Repository에서는 Unity 6의 기본 Universal 3D의 기본 Packages 폴더를 예외로 처리하고 있습니다 (기본적인 패캐지는 로컬에서 자체적으로 추가해주셔야 합니다 [용량 간소화를 위함])

## 스토브 심사 / Steam 빌드

기본 실행 모드는 스토브 심사용입니다. Steam 클라이언트를 초기화하지 않으며,
랭킹은 현재 PC에 저장된 본인의 최고 기록 한 건을 표시합니다. 기록은 모드·레벨별로
구분하고, 닉네임을 바꾸거나 더 낮은 점수를 얻어도 최고 기록을 유지합니다.

Unity의 `BALL-A-RETRO > Build > STOVE Review (Windows)`에서 출력 파일을 선택합니다.
플랫폼 선택은 빌드별 추가 define으로 전달하므로 프로젝트의 기본 설정을 바꾸지 않습니다.

Steam App ID는 `Assets/Resources/Release/SteamSettings.asset`에 `5193980`으로 설정했습니다.
`BALL-A-RETRO > Build > Steam (Windows)`를 사용합니다. 기본값 0과 테스트 ID 480은
출시용으로 허용하지 않습니다. Steam 출력 폴더에는 개발용 `steam_appid.txt`가 없어야 합니다.
Editor에서 Steam을 시험할 때만 `BALLARETRO_STEAM` define과 실제 ID를 담은 개발용
`steam_appid.txt`를 사용하고, 시험 후 define을 제거하면 스토브 기본 모드로 돌아갑니다.

Steam 닉네임은 로그인 상태와 유효한 Steam ID를 확인한 뒤 현재 계정에서 읽습니다.
실제 Steam 클라이언트 계정으로 연결하는 검증은 남아 있습니다.
기존 Steam 온라인 랭킹 서버의 운영·서버 인증 검증도 별도로 필요합니다.
언어 추가 및 시스템창의 언어 전환은 후속 작업입니다. 번역 자체에는 Steam 키가 필요하지 않습니다.

## 재현 가능한 검증

검증 프로젝트는 원본 Unity 프로젝트 밖의 **새 폴더**에 생성합니다. 열려 있는 개발 씬을
다른 Unity 프로세스에서 열거나 저장하지 않습니다. 아래 명령은 저장소 루트에서 실행합니다.

```powershell
./Tests/RunProgression/Run-Tests.ps1 -OutputRoot D:/ball-a-retro-validation/gameplay-new -IncludeTutorial -IncludeBallRescue -IncludeScorePopups
./Tests/TutorialProgression/Run-CanvasTests.ps1 -OutputRoot D:/ball-a-retro-validation/ui-new -IncludeHover -IncludeEffects -IncludePersonalRecords -PythonPath python
./Tests/TutorialProgression/Run-CanvasTests.ps1 -OutputRoot D:/ball-a-retro-validation/steam-new -IncludeSteamLogin -PythonPath python
```

각 출력 폴더의 `results.xml`과 `unity.log`에서 결과를 확인합니다. Steam 검증은 실제
SteamManager와 모의 Steam API를 사용하며 실제 Steam 계정 인증 시험을 대신하지 않습니다.

2026-11-06 확인: 게임 진행 59개, UI·설정·개인 기록 21개, Steam 로그인 경계 7개 통과.
실제 프로젝트 소스는 스토브/Steam 양쪽 define으로 C# 컴파일을 확인했습니다.
이 결과는 전체 플레이어 빌드와 모든 씬의 수동 플레이 검증을 의미하지 않습니다.

심사용 빌드에서 확인할 항목:

- 튜토리얼 시작 카메라, 발사 이후 Z와 / 각각 사용, 목표 점수 달성 후 완료창.
- 완료창·선택지·닫기·랭킹 버튼의 호버와 클릭, 시스템창을 닫은 뒤 커서 복원.
- 이펙트 OFF 즉시 적용, 다시 켜기, 로비·게임 이동과 재실행 후 설정 유지.
- 1920×1080 및 2560×1440에서 튜토리얼 안내와 결과창 표시.
- 네트워크 없이 개인 기록 저장, 닉네임 변경 및 낮은 점수 제출 후 최고 기록 유지.

