# BALL-A-RETRO 스토브 심사 준비

검토일: 2026-11-08. Windows 64비트, 제품 버전 0.1.0, Unity 6000.0.60f1.
이 문서는 소스와 로컬 검증에 근거한 제출 준비 자료다. 스토브 DRM 적용·클라이언트 시험 및
등급 심의 접수·승인 상태는 별도로 확인한다.

## 심사용 게임 설명 초안

BALL-A-RETRO는 공을 발사하고 플리퍼로 받아 점수를 얻는 레트로 스타일 핀볼 게임이다.
점수를 관리하는 딜러의 안내를 따라 라운드 목표에 도전하고, 강화 선택지로 공과 득점 능력을
바꾼다. 핀볼 테이블 외에 상층의 미로·룰렛·슬롯 요소가 등장한다.

로비에서 튜토리얼 또는 본게임을 시작한다. 튜토리얼은 대사, 플리퍼, 상층 입구, 발사,
점수 연습 순서로 진행한다. 발사 이후 양쪽 플리퍼를 각각 사용하고 1,000점을 달성하면
완료한다. 본게임에서는 라운드 목표와 남은 공을 확인하며 진행하고, 공 소진과 목표 달성
여부에 따라 결과 또는 다음 단계로 이동한다. 스토리에는 3라운드 도전 및 딜러와의 대사가 있다.

| 입력 | 기능 |
| --- | --- |
| Space 누른 뒤 놓기 | 공 발사 힘을 모아 발사 |
| Z / 슬래시(`/`) | 왼쪽 / 오른쪽 플리퍼 |
| W A S D 또는 방향키 | 미로 구간 이동 |
| 마우스 왼쪽 버튼 | 대사 진행, 메뉴·강화 선택 |
| Esc | 시스템창 열기 / 닫기 |

시스템창에서 음량, 화면 모드·해상도, 언어, 이펙트를 바꿀 수 있다.
한국어·영어·일본어 텍스트를 지원한다. 언어·음량·이펙트와 개인 최고 기록은 현재 PC에 저장한다.
진행 중인 한 판의 중단 지점 저장·클라우드 저장을 제공한다고 안내하지 않는다.
스토브 빌드는 Steam 로그인 없이 실행하며, 세계 랭킹은 Steam 전용 안내를 표시한다.
개인 최고 기록은 네트워크 없이 저장한다.

## 심의 내용 확인

희망 등급을 아직 확정하지 않는다. 룰렛·슬롯 외형과 점수 보상, 딜러 대사의 금전 표현을
심의 자료에 포함한다. 슬롯은 일치 개수에 따른 점수, 룰렛은 나온 숫자에 따른 점수를 부여한다.
이를 생략하고 단순히 일반 핀볼이라고만 설명하지 않는다.
현금 결제·환전·유료 재화와의 연동 여부는 최종 상품 설정과 함께 확인해야 한다.

대사에는 `젠장`, `Damn it`, `くそっ` 및 딜러·분배금·전액 관련 표현이 있다.
심사자에게 `Assets/Resources/dialogues.csv`와 `ui.csv` 전체를 제공하여 세 언어를 함께 확인하게 한다.
최종 등급은 스토브 심의 결과에 따른다.

일반 출시 빌드에서는 F9 임의 점수 제출, Ctrl+R/M/L 개발 메뉴, Q/Ctrl+Q 공 강제 이동을
컴파일에서 제외한다. 이 단축키는 Editor 및 Development Build에서만 사용한다.
심사 진행용 치트가 필요하면 일반 업로드 파일과 구별되는 별도 빌드 및 사용법을 준비한다.

## 빌드와 검증 재현

개발 Unity 프로젝트를 닫거나 씬을 저장할 필요 없이, 저장소 루트에서 실행한다.
PowerShell 7 이상과 Unity 6000.0.60f1 Windows Build Support를 사용한다. 문구 추출에는 Python 3이 필요하다.
외부의 **새 폴더**를 사용해야 한다. 원본 Assets/Packages/ProjectSettings/Library를 복사하고,
복사본에서만 임포트·빌드한다. 미커밋 에셋도 복사되므로 커밋과 파일 해시를 함께 기록한다.

```powershell
./Tests/Release/Build-StoveReview.ps1 -OutputRoot D:/ball-a-retro-validation/stove-new
./Tests/Release/Verify-Player.ps1 -PlayerRoot D:/ball-a-retro-validation/stove-new/player -EvidencePath D:/ball-a-retro-validation/stove-new/evidence/player-audit.json
./Tests/Release/Run-PlayerSmoke.ps1 -PlayerRoot D:/ball-a-retro-validation/stove-new/player -EvidenceRoot D:/ball-a-retro-validation/stove-new/evidence/startup
./Tests/Release/Package-StoveReview.ps1 -PlayerRoot D:/ball-a-retro-validation/stove-new/player -OutputZip D:/ball-a-retro-validation/stove-new/BALL-A-RETRO-STOVE.zip
python Tests/Release/Export-ReviewText.py --output D:/ball-a-retro-validation/stove-new/ALL-DIALOGUE-UI.txt
```

빌드는 로비를 첫 씬으로 요구하며, 출력 폴더가 비어 있는지 검사한다. STOVE define과
빌드별 플러그인 제외 규칙을 사용하여 Steam 네이티브 DLL이 섞이지 않게 한다.
Steam 프로젝트 설정, 플러그인 meta, 사용자의 볼 머테리얼·폰트를 변경하지 않는다.

`Verify-Player.ps1`는 실제 완성된 Assembly-CSharp.dll을 읽어 플랫폼 상수와 개발용 입력의
제거를 확인한다. 이 검사는 게임 플레이 및 STOVE 클라이언트 시험을 대신하지 않는다.

`BallARetroSceneProbe.cs`는 전체 씬 통합 점검용이다. 원본 Assets에는 넣지 않고 격리 프로젝트의
Assets/ReleaseValidation에만 복사한다. `-batchmode -executeMethod BallARetroSceneAudit.Run
-auditOutput <새 증거 폴더>`로 실행하며 `-quit`는 붙이지 않는다. 완료되면 자동 종료한다.
로비 설정, 세 언어, 오프라인 기록, 튜토리얼·게임 씬 이동 및 로비 복귀를 검사한다.
목표 HUD와 초기화 순서 재현을 포함하여 24개 항목을 검사한다. 배치 에디터에는 Game View가 없으므로,
원래 씬 카메라에 UI를 임시 연결하여 1920×1080 렌더링 5장을 기록한 뒤 UI 연결을 복원한다.
이 이미지는 렌더링 검증 자료이며, STOVE 클라이언트의 실제 플레이 영상과 구별한다.

2026-11-08 검증 증거 루트: `D:/ball-a-retro-validation/stove-release-20261108`.
`evidence/regression-results.json`에 6개 회귀 테스트 모음의 167개 통과 결과,
`full-scene-approved/scene-audit.json`에 최종 24개 통과 및 오류 0건을 기록했다.
음량 복원·음소거, 임의 점수 제출 단축키, 씬 초기화, 셰이더 키워드 충돌,
튜토리얼 목표 HUD 문제를 기능별 커밋으로 수정했다.

## 제출 전 남은 외부 절차

1. 스토브 스튜디오의 그룹·프로젝트·상품, 개인/사업자 및 정산 정보를 확인한다.
2. 로컬 Windows 플레이어를 업로드하고 DRM Maker를 적용한다. 기본 실행 파일은
   `BALL-A-RETRO.exe`다. DRM 적용 후 스토브 PC 클라이언트에서 설치·실행·종료·재실행 및
   언어·설정·개인 기록을 다시 확인한다.
3. 설명서에 실제 플레이 스크린샷 5장 이상, 등장 인물·조작·진행·심의 요소를 정리한다.
   자동 UI 검사 이미지만으로 모든 플레이 장면 자료를 대체하지 않는다.
4. 초반·중반·후반 각각 7분 이상의 플레이 영상 및 모든 엔딩을 준비한다. 룰렛·슬롯,
   문제가 될 수 있는 대사 장면의 위치를 표시한다. 현재 자동 검증은 이 영상 제작을 포함하지 않는다.
5. 전체 대사·UI 문구와 필요한 이미지 자료, 콘텐츠 체크리스트를 첨부하여 자체 심의를 신청한다.
   결과가 나오기 전에는 스토브 또는 Steam의 국내 심의 요건을 충족했다고 표시하지 않는다.

절차 근거: [스토브 빠른 시작](https://studiodocs.onstove.com/quick-guide),
[자체 등급 분류](https://studiodocs.onstove.com/release/ratings),
[자체 심의 체크리스트](https://studiodocs.onstove.com/release/ratings/자체-심의-체크리스트),
[DRM Maker](https://studiodocs.onstove.com/build-management/drm-maker).
계정의 최신 스튜디오 안내에 따라 최종 요구 자료를 확인한다.
