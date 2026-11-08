# BALL-A-RETRO-Dev
BALL-A-RETRO Game Develope Repository

Unity Version : Unity 6.2 (6000.2.6f2)
->
Unity Version : Unity 6.0 (6000.0.60f1) [안정성 이슈로 변경]

## 출시 점검: 오디오 설정

로비와 게임의 오디오 매니저가 저장한 음량을 직접 복원하므로 설정 UI의 초기화 순서에
영향받지 않습니다. 로비 페이드 도중 음소거를 해도 이전 페이드가 음량을 되돌리지 않으며,
단발 효과음은 음량 배율을 한 번만 적용합니다.
`Tests/AudioSettings/Run-Tests.ps1 -OutputRoot <새 외부 폴더>`로 재현할 수 있습니다.
2026-11-08 검증: 수정 전 재현 검사 4개 실패, 수정 후 4개 모두 통과.

## 출시 점검: 개발용 단축키

F9 임의 점수 제출, Ctrl+R/M/L 재시작·버튼 전환·로비 이동, Q/Ctrl+Q 공 강제 이동은
Editor 및 Development Build에서만 컴파일합니다. 일반 출시 빌드에는 포함하지 않습니다.
일반 메뉴와 게임 조작은 유지합니다. `Tests/Release/Verify-Player.ps1`는 빌드된 DLL에서
개발용 입력 코드와 Steam 플랫폼 선택을 검사합니다. 2026-11-08 현재 소스를 STOVE 플레이어
define으로 컴파일하고 DLL 검사를 통과했습니다.

## 출시 점검: 전체 씬 초기화

범퍼 배수 조명은 외부 LED 컴포넌트의 `Start` 실행 순서와 무관하게 상태·발광·조명을
함께 갱신합니다. 비활성 닉네임 입력창은 씬 변경 때 코루틴을 시작하지 않으며,
정상 카메라 전환을 오류로 출력하던 디버그 로그를 제거했습니다. 외부 패키지 원본은 유지합니다.
2026-11-08 실제 로비 → 튜토리얼 → 로비 → 본게임 → 로비 통합 검사 22개 통과:
초기화 순서 재현, 비활성 입력창, 언어 3종, 설정 일시정지, 복귀 확인창, 개인 기록 유지,
Steam 비활성 상태를 포함합니다. 검증 코드는 `Tests/Release/BallARetroSceneProbe.cs`이며
개발 프로젝트 밖의 격리 프로젝트에서만 실행합니다.

## 출시 점검: 공 셰이더

`URP_MetallicMatCap`에서 전체 URP/Lit 셰이더로 연결하던 Fallback을 제거하고, URP의
ShadowCaster·DepthOnly·DepthNormals HLSL을 명시적인 패스로 컴파일합니다. Unity 6.0에서
실제 씬을 렌더링할 때 발생하던 `State comes from an incompatible keyword space` 오류를
해결했습니다. 기존 ForwardLit의 색상·금속성·MatCap 계산과 사용자 머테리얼 값은 유지합니다.
2026-11-08 수정 전 렌더링 오류 3건, 수정 후 실제 씬 5장 렌더링과 22개 통합 검사에서 오류 0건.

## 출시 점검: 목표 점수 HUD

목표 점수 HUD는 `Awake`에서 텍스트 참조를 준비하고, 튜토리얼 목표 설정 및 점수 매니저의
시작 알림을 받아 갱신합니다. HUD의 `Start`가 먼저 실행되어 튜토리얼 목표가 `0`으로
남던 문제를 수정했습니다. 실제 씬에서 공 발사 전 `점수:0 / 1000`, 본게임의 초기 목표,
전체 씬 이동과 렌더링을 포함한 24개 검사를 통과했습니다.

현 Repository에서는 Unity 6의 기본 Universal 3D의 기본 Packages 폴더를 예외로 처리하고 있습니다 (기본적인 패캐지는 로컬에서 자체적으로 추가해주셔야 합니다 [용량 간소화를 위함])

## 스토브 심사 / Steam 빌드

2026-11-08 Windows 제출 후보와 검사 결과는
[`Docs/Release/STOVE-VALIDATION-20261108.md`](Docs/Release/STOVE-VALIDATION-20261108.md)에 기록했습니다.
재현 명령과 심의 자료 준비는 [`Docs/Release/STOVE-REVIEW.md`](Docs/Release/STOVE-REVIEW.md)를 확인합니다.
PowerShell 7에서 `Tests/Release/Build-StoveReview.ps1 -OutputRoot <새 외부 폴더>`를 실행하면
격리 복사본 생성, STOVE 빌드, 네이티브 Steam 파일 제외 및 게임 DLL 검사를 수행합니다.

기본 실행 모드는 스토브 심사용입니다. Steam 클라이언트를 초기화하지 않으며,
로비와 게임 결과에서 랭킹 화면을 열 수 있고, 온라인 조회는 Steam 버전에서만 수행합니다.
스토브에서는 Steam 전용 기능 안내를 표시합니다. 개인 최고 기록은 현재 PC에 모드·레벨별로
저장하며, 더 낮은 점수를 얻어도 유지합니다. 로비의 게임 시작 버튼은 닉네임 입력 없이
바로 게임 씬으로 이동하고, 튜토리얼 버튼은 튜토리얼 씬으로 이동합니다.

Unity의 `BALL-A-RETRO > Build > STOVE Review (Windows)`에서 출력 파일을 선택합니다.
플랫폼 선택은 빌드별 추가 define으로 전달하므로 프로젝트의 기본 설정을 바꾸지 않습니다.

Steam App ID는 `Assets/Resources/Release/SteamSettings.asset`에 `5193980`으로 설정했습니다.
`BALL-A-RETRO > Build > Steam (Windows)`를 사용합니다. 기본값 0과 테스트 ID 480은
출시용으로 허용하지 않습니다. Steam 출력 폴더에는 개발용 `steam_appid.txt`가 없어야 합니다.
Editor에서 Steam을 시험할 때만 `BALLARETRO_STEAM` define과 실제 ID를 담은 개발용
`steam_appid.txt`를 사용하고, 시험 후 define을 제거하면 스토브 기본 모드로 돌아갑니다.

열린 개발 프로젝트를 건드리지 않는 Steam Win64 배치 빌드는 PowerShell 7에서 실행합니다.
아래 출력 경로는 매번 새 폴더를 사용합니다. 빌드·압축·압축 해제 모두 실제 컴파일된
플랫폼, Steam 초기화 코드, Steamworks.NET과 일치하는 x64 DLL 하나, 개발용 App ID 제외를 검사합니다.

```powershell
./Tests/Release/Build-SteamReview.ps1 -OutputRoot D:/ball-a-retro-validation/steam-new
./Tests/Release/Package-StoveReview.ps1 -Platform Steam -PlayerRoot D:/ball-a-retro-validation/steam-new/player -OutputZip D:/ball-a-retro-validation/steam-new/BALL-A-RETRO_Steam_Win64.zip
./Tests/Release/Prepare-Upload.ps1 -Platform Steam -ArchivePath D:/ball-a-retro-validation/steam-new/BALL-A-RETRO_Steam_Win64.zip -OutputDirectory D:/ball-a-retro-validation/steam-new/upload
```

공통 패키징 스크립트의 기본 플랫폼은 기존대로 STOVE입니다. Steam에서는
`Assets/Resources/Release/SteamSettings.asset`를 포함하고 샘플 IAP 설정은 제외합니다.
`Build-SteamReview.ps1`은 본편 App ID와 전체 콘텐츠를 사용합니다. 데모는 아래의 별도
빌드 명령을 사용합니다. 자동 검사는 실제 Steam 계정 인증,
클라이언트 설치·실행, 오버레이와 서버 리더보드 검증을 대신하지 않습니다.

Steam을 실행하지 않은 환경의 시작 검사는 `Tests/Release/Run-SteamOfflineSmoke.ps1`
`-PlayerRoot <upload 폴더> -EvidenceRoot <새 외부 증거 폴더>`로 수행합니다.
검사 도구는 원본과 해시가 일치하는 별도 복사본에만 개발용 App ID를 넣고 30초간 실행합니다.
로비·그래픽·현지화된 광과민성 안내·Steam DLL 로드와 클라이언트 부재에 따른 초기화 실패를 확인합니다.
배포 폴더는 변경하지 않으며, 이 결과도 실제 Steam 클라이언트 시험을 대신하지 않습니다.

2026-11-09 Steam 후보 `91fd920`은 빌드, 컴파일 결과 검사 8개, 포함 에셋 3,412개,
자동 회귀 검사 28개, 배포 파일 304개 해시 및 30초 오프라인 시작 검사를 통과했습니다.
상세 경로와 SHA-256, 검증 범위 및 남은 항목은
[`Docs/Release/steam-candidate-v1.json`](Docs/Release/steam-candidate-v1.json)에 기록했습니다.
이 후보는 본편 콘텐츠의 내부 검증용이며 데모 빌드가 아닙니다.

### Steam 무료 데모

본편 `5193980`에 연결한 데모 App ID는 `5585210`, 콘텐츠 디포는 `5585211`입니다.
데모는 3라운드와 누적 목표 2,000 → 4,000 → 6,000점, 라운드당 기본 공 2개로 구성합니다.
처음 발사하기 전과 1·2라운드 완료 후 각각 한 번씩, 총 세 번 강화합니다.
남은 공이 많아도 강화 선택 횟수는 늘어나지 않습니다.

- 블랙홀 축전기: 범퍼 적중당 100점 충전, 한도 600점/강화 시 1,000점. 블랙홀 진입으로 정산합니다.
- 마지막 공 역전: 마지막 공에서 직접 획득하는 점수에 +50%/강화 시 +75%. 충전 정산에는 배율을 적용하지 않습니다.
- 보조 강화: 플리퍼 강화 또는 현재·이후 라운드의 기본 공 +1.
- 데모의 블랙홀 기본 보상은 500점이며, 본편 규칙과 튜토리얼은 유지합니다.

게임 중 선택 내역은 기본적으로 닫혀 있으며 `Tab`으로 열고 닫습니다. 데모의 축전기·마지막 공
상태는 왼쪽 내역 패널 안에서만 표시합니다. 내역을 열어도 카메라·커서·게임 속도는 바뀌지 않으며,
선택창·설정창·종료·재시작 시 닫힙니다. 튜토리얼 딜러가 한국어·영어·일본어로 조작을 안내합니다.

데모 빌드에만 `BALLARETRO_STEAM;BALLARETRO_DEMO`를 적용하고, 격리 복사본의 제품명을
`Ball-A-Retro Demo`로 설정합니다. 설정·저장 경로와 개인 최고점 키는 본편과 분리하며,
데모에는 전용 온라인 랭킹과 이 PC의 개인 최고점을 함께 제공합니다. 온라인 랭킹은
데모 App ID로 조회·제출하므로 본편 기록과 섞이지 않으며, 개인 최고점은 온라인 연결과
무관하게 먼저 저장합니다. 본편 찜하기 버튼은 본편 상점 주소를 엽니다.
새 카드 두 장은 `Assets/Resources/Demo`의 아틀라스에 묶으며 본편/STOVE 복사본에서 제외합니다.
생성 도구와 프롬프트는 [`demo-icons-generation.json`](Docs/Release/demo-icons-generation.json)에 기록합니다.

```powershell
./Tests/Release/Build-SteamDemoReview.ps1 -OutputRoot D:/ball-a-retro-validation/demo-new -CacheProjectRoot <같은 Unity 버전의 이전 격리 프로젝트>
./Tests/Release/Package-StoveReview.ps1 -Platform SteamDemo -PlayerRoot D:/ball-a-retro-validation/demo-new/player -OutputZip D:/ball-a-retro-validation/demo-new/BALL-A-RETRO_SteamDemo_Win64.zip
./Tests/Release/Prepare-Upload.ps1 -Platform SteamDemo -ArchivePath D:/ball-a-retro-validation/demo-new/BALL-A-RETRO_SteamDemo_Win64.zip -OutputDirectory D:/ball-a-retro-validation/demo-new/upload
```

`CacheProjectRoot`는 선택 사항입니다. 빌드는 새 폴더에서 수행하며 원본의 볼 머테리얼과
폰트 변경을 복사하되 원본을 저장하거나 수정하지 않습니다. 실제 Steam 클라이언트 설치·실행,
물리 플레이와 재미·난이도 평가는 별도이며, 이 절차만으로 공개 출시나 심사 신청을 하지 않습니다.

2026-11-09 후보 `aca6470`은 Steam 빌드 `26523575`로 업로드해 미출시 데모의 `default`에 적용했습니다.
ZIP 304개 파일 무결성, 3,417개 포함 자산, 오프라인 시작, 실제 씬 흐름 70개 및 결과 상태 29개 검사를 통과했습니다.
실제 Steam 설치본 304개 파일이 원본과 일치하고, 사용자 설치·실행·랭킹 확인 및 Steamworks 서버의
데모 랭킹 7,150점 등록까지 확인했습니다. 이후 Steamworks의 5분 실행 항목 및 데모 빌드 체크리스트 완료를
확인했습니다. 오버레이의 실제 조작은 별도 확인 대상입니다.
클라이언트 증거와 사용자 확인 범위는 [`steam-demo-client-validation.json`](Docs/Release/steam-demo-client-validation.json)에 기록했습니다.
해시·보존 자산·검사 범위와 제한은 [`steam-demo-candidate-v1.json`](Docs/Release/steam-demo-candidate-v1.json)에 기록했습니다.

Tab 선택 내역과 3개 언어 튜토리얼 안내를 반영한 후보 `d84a4d4`는 Steam 빌드 `26524160`으로
미출시 데모의 `default`에 적용했습니다. 실제 씬에서 내역·모달·재시작·결과 흐름 108개 검사와
304개 업로드 파일 무결성 검사를 통과했습니다. 라운드·선택 횟수는 기존 3회로 유지합니다.
새 설치본에서의 직접 Tab/Steam 오버레이 조작은 사용자 확인 대상이며, 빌드·검사 증거는
[`steam-demo-candidate-v2.json`](Docs/Release/steam-demo-candidate-v2.json)에 기록했습니다.

Steam 닉네임은 로그인 상태와 유효한 Steam ID를 확인한 뒤 현재 계정에서 읽습니다.
로비 오른쪽 아래 기존 랭킹 아이콘과 게임 결과의 기존 랭킹 버튼이 같은 랭킹 화면을 엽니다.
검은 배경·초록 이중 테두리·순위별 색상으로 `RANK / NAME / STEP / SCORE`를 표시하며 기존 폰트를 유지합니다.
NAME은 Steam 계정 이름, STEP은 최고 점수를 얻은 라운드입니다. 라운드 정보가 없는 예전 기록은 `—`로 표시합니다.
본편 화면 제목은 **SCORE RANKING**이며, 데모 제목·범위·버튼은 선택한 언어로 표시합니다.
열 이름은 고정 영어이고, 상태 안내는 선택한 언어로 표시합니다.
닫으면 기존 로비/결과 화면으로 돌아가며 게임오버의 일시정지 상태는 유지됩니다.
Steam 버전에서 세계 TOP 10을 조회합니다.
Steamworks 네이티브 리더보드를 사용하며, 기존 localhost PHP 주소와 API 키 설정은 제거했습니다.
기본 보드 이름은 `BALLARETRO_Classic_1`이며 모드·레벨별로 구분합니다.
첫 요청에서 보드를 찾거나 생성하고, 내림차순 정렬과 `KeepBest` 업로드로 계정별 최고 점수와 그 라운드를 유지합니다.
조회 실패·로그인 필요·기록 없음은 각각 화면에 표시하며, 창을 닫은 뒤 도착한 응답은 UI를 다시 열지 않습니다.
실제 App ID 소유 계정으로 점수 업로드와 다른 계정의 조회를 검증하는 과정은 남아 있습니다.
Steam 커뮤니티 페이지에도 보드를 노출하려면 Steamworks 관리 화면에서 표시 이름을 설정해야 합니다.
([Steamworks 리더보드 안내](https://partner.steamgames.com/doc/features/leaderboards))
한국어·영어·일본어는 스토브와 Steam에서 동일하게 지원하며, 번역에는 Steam 키가 필요하지 않습니다.

## 튜토리얼 진행

시작 시 기존 `Tutorial_01`부터 `Tutorial_07`까지의 전용 대사를 재생한 뒤
플리퍼 → 2층 입구 → 공 발사 → 점수 연습 순서로 진행합니다. 일반 게임의 인트로와 재시도 플래그는 공유하지 않습니다.
공 발사 이후 Z와 /를 각각 사용하고 1,000점을 달성해야 완료합니다. HUD 목표도 1,000점으로 표시합니다.
연습 중 목표 안내는 왼쪽 위의 작은 연녹색 텍스트로 표시하여 중앙 플레이 영역을 비웁니다.
성공 또는 공 소진 시 튜토리얼 결과창만 표시하며, 일반 게임의 라운드 선택·엔딩 대사·랭킹 제출은 실행하지 않습니다.
남은 공 HUD는 `Awake`에서 참조를 준비하여 튜토리얼의 `Start`가 먼저 실행돼도 초기화를 중단하지 않습니다.
씬의 임시 안내 문구는 비우고 결과창은 비활성 상태로 저장합니다. 발사 단계에서 튜토리얼 잠금을 명시적으로 해제하며,
일시정지·시스템창·공 없음 상태에서는 발사 이벤트를 발생시키지 않습니다.
2026-11-08 추가 회귀 검사 32개 통과: HUD보다 먼저 시작하는 튜토리얼, 실제 플런저의 공 발사 힘,
전용 대사 7개와 각 연습 단계의 한국어·영어·일본어 전환, 연습 진행 상태 보존을 포함합니다.

## 언어 설정

로비·게임·튜토리얼의 시스템창에서 **언어 (Language)**를 선택하면 열린 화면에도 즉시 적용됩니다.
선택한 언어는 저장되어 다음 실행에도 유지됩니다. 최초 실행은 지원되는 시스템 언어를 사용하고,
지원하지 않는 언어인 경우 영어로 표시합니다.

- 시스템창, 화면 모드, 이펙트 세부 설정, 로비 버튼과 세계 랭킹 안내.
- 튜토리얼 설명·연습 진행·성공/실패 안내, 인트로·라운드·엔딩 대사.
- 점수·남은 공·목표 표시, 강화 선택지 9종과 희귀도·보관함 툴팁·다시 뽑기 횟수.
- 최고 기록 도전 안내, 종료 사유, 결과·기록 화면 및 나가기 확인창.

언어 변경은 진행 중인 대사 순서, 튜토리얼 연습 기록, 선택한 강화, 점수와 설정값을 초기화하지 않습니다.
랭킹의 플레이어 이름은 Steam 계정 이름을 그대로 표시합니다.

번역 원본은 `Assets/Resources/ui.csv`와 `Assets/Resources/dialogues.csv`입니다.
문구를 수정한 뒤 `python Tests/Localization/Sync-Tables.py`로 Unity StringTable을 동기화합니다.
`--check` 옵션으로 CSV와 에셋의 불일치를 검사할 수 있습니다. 새 UI에는
`PSH_Script_LocalizedText`와 키를 지정하고, 동적 문구는 `PSH_Script_Localization.Text`를 사용합니다.
일본어 글자는 Galmuri11 폰트 폴백과 동적 다중 아틀라스로 표시합니다.

## 재현 가능한 검증

검증 프로젝트는 원본 Unity 프로젝트 밖의 **새 폴더**에 생성합니다. 열려 있는 개발 씬을
다른 Unity 프로세스에서 열거나 저장하지 않습니다. 아래 명령은 저장소 루트에서 실행합니다.

```powershell
./Tests/RunProgression/Run-Tests.ps1 -OutputRoot D:/ball-a-retro-validation/gameplay-new -IncludeTutorial -IncludeBallRescue -IncludeScorePopups
./Tests/TutorialProgression/Run-CanvasTests.ps1 -OutputRoot D:/ball-a-retro-validation/ui-new -IncludeHover -IncludeEffects -IncludePersonalRecords -PythonPath python
./Tests/TutorialProgression/Run-CanvasTests.ps1 -OutputRoot D:/ball-a-retro-validation/steam-new -IncludeSteamLogin -PythonPath python
./Tests/Localization/Run-Tests.ps1 -OutputRoot D:/ball-a-retro-validation/localization-new
./Tests/TutorialProgression/Run-CanvasTests.ps1 -OutputRoot D:/ball-a-retro-validation/localized-ui-new -IncludeHover -IncludeEffects -IncludeLocalization -PythonPath python
./Tests/TutorialProgression/Run-CanvasTests.ps1 -OutputRoot D:/ball-a-retro-validation/menu-input-new -IncludeHover -IncludeEffects -IncludeLocalization -IncludeMenuInteractions -PythonPath python
./Tests/TutorialProgression/Run-CanvasTests.ps1 -OutputRoot D:/ball-a-retro-validation/lobby-stove-new -IncludeLobby -IncludeEffects -IncludeLocalization -IncludePersonalRecords -PythonPath python
./Tests/TutorialProgression/Run-CanvasTests.ps1 -OutputRoot D:/ball-a-retro-validation/lobby-steam-new -IncludeLobby -IncludeEffects -IncludeLocalization -IncludeSteamLogin -PythonPath python
./Tests/DialogueInput/Run-Tests.ps1 -OutputRoot D:/ball-a-retro-validation/dialogue-input-new
```

각 출력 폴더의 `results.xml`과 `unity.log`에서 결과를 확인합니다. Steam 검증은 실제
SteamManager와 모의 Steam API를 사용하며 실제 Steam 계정 인증 시험을 대신하지 않습니다.

2026-11-06 확인: 게임 진행 59개, UI·설정·개인 기록 21개, Steam 로그인 경계 7개 통과.
튜토리얼 UI를 추출한 검증 씬에서 FHD·QHD 및 카메라 전환 전후의 버튼 렌더링과 클릭도 통과했습니다.
실제 프로젝트 소스는 스토브/Steam 양쪽 define으로 C# 컴파일을 확인했습니다.
이 결과는 전체 플레이어 빌드와 모든 씬의 수동 플레이 검증을 의미하지 않습니다.

다국어 변경 검증: 번역·저장·글꼴 8개, 게임 진행·튜토리얼·대사 전환 26개,
실제 UI·호버·이펙트 설정 32개 통과. FHD·QHD에서 세 언어의 시스템창과 이펙트창,
선택지 9종의 글자 잘림, 비활성 화면을 다시 열었을 때의 번역도 확인했습니다.

심사용 빌드에서 확인할 항목:

- 튜토리얼 전용 대사, 시작 카메라, 발사 이후 Z와 / 각각 사용, 목표 점수 달성 후 완료창.
- 완료창·선택지·닫기·랭킹 버튼의 호버와 클릭, 시스템창을 닫은 뒤 커서 복원.
- 이펙트 OFF 즉시 적용, 다시 켜기, 로비·게임 이동과 재실행 후 설정 유지.
- 1920×1080 및 2560×1440에서 튜토리얼 안내와 결과창 표시.
- 스토브에서 네트워크 없이 개인 기록 저장과 Steam 전용 안내, Steam에서 세계 랭킹 조회·최고 기록 유지.
- 세 언어 전환 후 열린 시스템창·튜토리얼·대사·강화·결과 표시, 재실행 후 언어 유지.

## 이펙트 세부 설정

시스템창의 **이펙트 효과 → 세부 설정**에서 카메라 흔들림, 충돌·폭죽 파티클,
공 궤적, 섬광·효과 조명, 글리치·화면 움직임, 빛 번짐·후처리를 각각 조절합니다.

- **기본 모드 · 모두 켜기**: 여섯 효과를 모두 ON으로 설정합니다. 저장값이 없는 신규 사용자도 이 상태로 시작합니다.
- **광과민성 배려 · 모두 끄기**: 여섯 효과와 표시된 스위치를 함께 OFF로 설정합니다.
- 이후 원하는 효과만 켜면 **사용자 설정**으로 표시합니다. 예를 들어 다른 효과를 끈 채 카메라 흔들림만 유지할 수 있습니다.
- 개별 선택은 즉시 적용·저장하며, 기존 버전에서 효과를 모두 꺼 둔 설정도 보존합니다.
- 시스템창의 기존 전체 스위치는 효과가 하나라도 켜져 있으면 ON 위치에 있고, 일부만 켜진 상태는 `일부`로 표시합니다. 전체 스위치를 끄면 모두 OFF, 다시 켜면 모두 ON입니다.

설정은 공 본체와 충돌·점수 판정을 숨기거나 비활성화하지 않습니다.
이펙트 검증에는 설정 저장·이전 버전 값 이전·독립 동작·공 본체 보존·일시정지 중 조작과
실제 설정창 버튼 연결, FHD·QHD 표시 및 클릭을 포함합니다.


## 시스템창 닫기와 마우스 커서

시스템창 X는 닫기 전용 `CloseMenu`를 호출하며, 중복 클릭으로 다시 열리지 않습니다.
나가기 확인창의 X와 취소는 확인창만 닫아 시스템창의 일시정지를 유지합니다.
시스템창을 닫으면 열기 전의 시간 배율과 커서 표시 상태로 돌아갑니다.

커서는 최상단 UI의 실제 클릭 가능 상태로 판정합니다. 버튼·슬라이더·드롭다운과
생성된 선택 항목·토글·입력창·커스텀 클릭 영역에 손 모양을 표시합니다.
비활성 버튼이나 다른 창에 가려진 UI에는 표시하지 않으며, 일시정지 중이거나
마우스가 정지한 상태에서 창을 여닫아도 다음 프레임에 갱신합니다.
기존 버튼의 확대·색상 호버 연출은 유지합니다.

2026-11-08 검증: 실제 로비·게임 공용 프리팹의 닫기/취소 연결과 커서 판정을 포함한
UI 검사 41개, 대사·시스템창 입력과 커서 복원 회귀 검사 29개 통과.
검증용 UI 추출의 `--interactions` 모드는 실제 버튼 콜백을 보존합니다.

2026-11-08 로비·튜토리얼 변경 검증: 게임 진행·튜토리얼 29개, Steam 로비·로그인·랭킹
19개, 스토브 로비·개인 기록 9개 통과. 실제 로비 프리팹의 게임/튜토리얼 클릭 경로,
플랫폼별 랭킹 노출, 지연 응답 후 닫힌 창 유지, 로그인/빈 목록/통신 실패 표시를 확인했습니다.
세계 랭킹은 한국어·영어·일본어의 FHD·QHD 렌더링과 글자 잘림을 검사했습니다.
Steam API는 모의 클라이언트로 검증했으며 실제 SDK를 포함한 양쪽 플랫폼 C# 컴파일도 통과했습니다.

2026-11-08 후속 수정 검증: 튜토리얼·게임 진행 32개, 다국어·시스템창·호버 41개,
Steam 로비·결과 랭킹 20개, 스토브 로비·결과 랭킹·개인 기록 9개로 총 102개 통과.
결과 화면의 실제 랭킹 버튼 콜백, 한 프레임 뒤 생성된 랭킹창 닫기, 종료 상태 유지,
순위별 색상·STEP 데이터·기존 랭킹 아이콘을 확인했습니다. UI 테스트는 씬의 CanvasScaler와
시스템창 참조 오버라이드를 반영합니다. 실제 Steam 서버에 점수를 올리는 검증은 포함하지 않습니다.
