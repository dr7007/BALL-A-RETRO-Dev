"""Export the rating manual and referenced dialogue artwork, without altering game assets."""
import argparse
import hashlib
import json
import re
import shutil
from pathlib import Path
from xml.sax.saxutils import escape

from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Image, Table, TableStyle, PageBreak

ROOT = Path(__file__).resolve().parents[2]

def export_artwork(out):
    refs = []
    pattern = re.compile(r"propertyPath: '?((?:introSprites|ending1Sprites|ending2Sprites|tutorialSprites)\.Array\.data\[\d+\])'?\s+value:[^\n]*\n\s+objectReference: \{fileID: (-?\d+), guid: ([a-f0-9]{32}), type: \d+\}")
    for scene in ('Main_Scene_Game', 'Main_Scene_Tutorial'):
        text = (ROOT / 'Assets/MainScenes' / (scene + '.unity')).read_text(encoding='utf-8-sig')
        refs.extend(dict(scene=scene, slot=slot, fileID=file_id, guid=guid) for slot, file_id, guid in pattern.findall(text))
    needed = {r['guid'] for r in refs}
    paths = {}
    for base in ('Assets/Public', 'Assets/MemberFolder'):
        for meta in (ROOT / base).rglob('*.meta'):
            m = re.search(r'^guid: ([a-f0-9]{32})$', meta.read_text(encoding='utf-8-sig', errors='replace'), re.M)
            if m and m.group(1) in needed:
                paths[m.group(1)] = meta.with_suffix('')
    if needed - paths.keys():
        raise ValueError('Unresolved dialogue sprite GUIDs: ' + ', '.join(needed - paths.keys()))
    art = out / 'dialogue-artwork'
    art.mkdir(parents=True, exist_ok=True)
    for guid, source in sorted(paths.items()):
        if not source.is_file():
            raise ValueError(f'Not a dialogue image: {source}')
        dest = art / (guid[:8] + '-' + source.name)
        shutil.copy2(source, dest)
        for ref in refs:
            if ref['guid'] == guid:
                ref.update(source=str(source.relative_to(ROOT)).replace('\\','/'), file=dest.name,
                           sha256=hashlib.sha256(dest.read_bytes()).hexdigest())
    (art / 'manifest.json').write_text(json.dumps(refs, ensure_ascii=False, indent=2), encoding='utf-8')
    return paths, refs

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--screenshots', type=Path, required=True)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    artwork, refs = export_artwork(args.output)
    pdfmetrics.registerFont(TTFont('Malgun', 'C:/Windows/Fonts/malgun.ttf'))
    pdfmetrics.registerFont(TTFont('MalgunBold', 'C:/Windows/Fonts/malgunbd.ttf'))
    pdfmetrics.registerFontFamily('Malgun', normal='Malgun', bold='MalgunBold')
    ink = colors.HexColor('#16302c')
    accent = colors.HexColor('#25765b')
    body = ParagraphStyle('body', fontName='Malgun', fontSize=10, leading=16, textColor=ink, wordWrap='CJK', spaceAfter=9)
    small = ParagraphStyle('small', parent=body, fontSize=8, leading=12, textColor=colors.HexColor('#536663'))
    title = ParagraphStyle('title', parent=body, fontName='MalgunBold', fontSize=25, leading=32, spaceAfter=20)
    heading = ParagraphStyle('heading', parent=body, fontName='MalgunBold', fontSize=15, leading=22, textColor=accent, spaceBefore=6, spaceAfter=12)
    story = []
    def p(text, style=body): story.append(Paragraph(text, style))
    def shot(name, caption):
        path = args.screenshots / (name + '.png')
        if not path.is_file(): raise FileNotFoundError(path)
        story.append(Image(str(path), width=475, height=267.1875))
        p(caption, small)
    p('BALL-A-RETRO', title)
    p('게임 설명서 · STOVE 이용등급 심사 자료', heading)
    p('Windows 64비트 / 버전 0.1.0 / 한국어·영어·일본어 / 작성일 2026-11-08', small)
    p('1. 게임 개요', heading)
    p('공을 발사하고 플리퍼로 받아 점수를 얻는 레트로 스타일 핀볼 게임입니다. 점수를 관리하는 딜러의 안내를 따라 라운드 목표를 달성하고 강화 선택지로 공과 득점 능력을 바꿉니다. 핀볼 테이블과 함께 상층 미로, 룰렛, 슬롯 장치가 등장합니다.')
    p('딜러는 점수 승인·수수료·분배금을 이야기하며 플레이어에게 3라운드 도전을 제안합니다. 클리어 엔딩과 실패 엔딩이 있으며, 전체 대사는 별도 ALL-DIALOGUE-UI.txt에 세 언어로 제공합니다.')
    p('2. 조작 방법', heading)
    rows = [('입력', '동작'), ('Space 누르기 / 놓기', '발사 힘을 모은 후 공 발사'), ('Z / 슬래시(/)', '왼쪽 / 오른쪽 플리퍼'), ('W A S D / 방향키', '상층 미로에서 이동 방향 선택'), ('마우스 왼쪽 버튼', '메뉴·대사 진행·강화 선택'), ('Esc', '시스템창 열기 / 닫기')]
    table = Table([[Paragraph(escape(a), body), Paragraph(escape(b), body)] for a,b in rows], colWidths=[190,285])
    table.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,0),colors.HexColor('#e3efe9')),('VALIGN',(0,0),(-1,-1),'TOP'),('LINEBELOW',(0,0),(-1,-1),.3,colors.HexColor('#cddbd5')),('TOPPADDING',(0,0),(-1,-1),5),('BOTTOMPADDING',(0,0),(-1,-1),5)]))
    story.append(table)
    story.append(Spacer(1,16))
    p('3. 저장 및 실행', heading)
    p('언어·음량·이펙트 설정과 개인 최고 기록을 현재 PC에 저장합니다. 진행 중인 한 판의 중단 지점 저장이나 클라우드 저장은 제공하지 않습니다. STOVE 빌드는 Steam 로그인 없이 실행됩니다. 세계 랭킹은 Steam 전용 안내를 표시합니다.')
    story.append(PageBreak())
    p('시작 및 튜토리얼', title)
    shot('lobby','화면 1. 로비: 본게임, 튜토리얼, 기록 및 설정으로 이동합니다.')
    p('튜토리얼은 전용 대사 7개, 플리퍼 설명, 상층 입구, 발사, 점수 연습 순서로 진행됩니다. 발사 이후 Z와 /를 각각 사용하고 1,000점을 달성하면 완료합니다. 성공 또는 공 소진 시 튜토리얼 결과를 표시합니다.')
    shot('tutorial','화면 2. 튜토리얼: 목표 점수와 조작 안내를 확인합니다.')
    story.append(PageBreak())
    p('본게임 및 설정', title)
    shot('game','화면 3. 본게임: 현재 점수, 목표, 남은 공을 확인하며 라운드를 진행합니다.')
    p('라운드 목표 달성 시 다음 단계로 진행하고 강화 선택지를 고릅니다. 남은 공 또는 플리퍼 사용 횟수 등 종료 조건에 따라 결과 화면으로 이동합니다. 게임 내부 점수는 실제 돈이나 인출 가능한 재화로 표시되지 않습니다.')
    shot('lobby-settings','화면 4. 시스템창: 언어·음량·화면·이펙트 설정을 조절합니다.')
    story.append(PageBreak())
    p('기록 및 심의 참고 사항', title)
    shot('stove-ranking','화면 5. 세계 랭킹이 Steam 전용임을 알리는 STOVE 안내.')
    p('룰렛·슬롯', heading)
    p('룰렛·슬롯 외형과 무작위 결과에 따른 점수 보상이 있습니다. 기본 코드에서 룰렛은 나온 숫자 × 100점을, 슬롯은 세 칸 일치 5,000점 / 두 칸 일치 1,000점 / 그 외 500점을 부여합니다. 강화 효과로 점수가 달라질 수 있습니다. 심의에서 이 장치와 보상 구조를 함께 확인해 주세요.')
    p('대사·엔딩', heading)
    p('금전·수수료를 언급하는 허구의 딜러 대사와 “젠장”(영어 “Damn it”, 일본어 “くそっ”) 표현이 있습니다. 전체 대사와 UI 텍스트를 첨부합니다. 클리어 엔딩은 기계 붕괴 및 딜러의 퇴장, 실패 엔딩은 재도전 권유를 표현합니다.')
    p('결제·치트', heading)
    p('검토한 게임 코드와 패키지 구성에는 현금 결제·유료 재화 구매·환전 기능이 없습니다. 판매 가격과 STOVE 계정의 결제 설정은 별도 상품 설정입니다. 일반 출시 빌드에서 F9 임의 점수 제출, Ctrl+R/M/L 개발 메뉴, Q/Ctrl+Q 공 강제 이동 입력은 제외합니다.')
    story.append(PageBreak())
    p('대사 이미지 및 자료 구성', title)
    p(f'게임·튜토리얼 씬의 대사 스프라이트 참조 {len(refs)}개를 확인해 원본 이미지 {len(artwork)}개를 dialogue-artwork 폴더에 수록했습니다. manifest.json에 씬·대사 슬롯·원본 경로·파일 해시를 기록했습니다.')
    thumbnails = []
    for guid, source in sorted(artwork.items()):
        from PIL import Image as PILImage
        with PILImage.open(source) as im: w,h=im.size
        scale=min(135/w,130/h)
        thumbnails.append([Image(str(source),width=w*scale,height=h*scale),Paragraph(escape(source.name),small)])
    for index in range(0,len(thumbnails),3):
        cells=[Table([[item] for item in x], colWidths=[145]) for x in thumbnails[index:index+3]]
        cells += ['']*(3-len(cells))
        story.append(Table([cells],colWidths=[158,158,158]))
        story.append(Spacer(1,10))
    p('자료의 검증 범위', heading)
    p('본문 5개 이미지는 실제 게임 씬을 1920×1080으로 렌더링한 로컬 검증 이미지입니다. STOVE 클라이언트에서 촬영한 영상이나 DRM 시험 완료 증거는 아닙니다. 초반·중반·후반 각 7분 이상의 실제 플레이 영상과 모든 엔딩 영상은 별도 자료로 준비·검토해야 합니다.',small)
    p('등급은 신청자의 자체 확정이 아닌 STOVE 심의 결과에 따릅니다. 이 설명서는 개인·사업자 정보 등록 승인 또는 게임 이용등급 심사 접수 완료를 뜻하지 않습니다.',small)
    p('근거: STOVE Studio 자체 심의 체크리스트 및 BALL-A-RETRO 0.1.0 소스·로컬 검증.',small)
    p('https://studiodocs.onstove.com/release/ratings',small)
    def footer(c,doc):
        c.setStrokeColor(colors.HexColor('#cddbd5')); c.line(50,42,A4[0]-50,42)
        c.setFont('Malgun',8); c.setFillColor(colors.HexColor('#536663'))
        c.drawString(50,28,'BALL-A-RETRO  |  STOVE 심사 자료')
        c.drawRightString(A4[0]-50,28,str(doc.page))
    target=args.output/'BALL-A-RETRO-게임설명서.pdf'
    SimpleDocTemplate(str(target),pagesize=A4,rightMargin=60,leftMargin=60,topMargin=38,bottomMargin=56,title='BALL-A-RETRO 게임 설명서').build(story,onFirstPage=footer,onLaterPages=footer)
    print(json.dumps({'pdf':str(target),'dialogueImageCount':len(artwork),'references':len(refs)},ensure_ascii=False))

if __name__=='__main__': main()
