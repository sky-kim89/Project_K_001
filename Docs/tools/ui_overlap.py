"""
구워진 Unity UI 프리팹(YAML)의 RectTransform 을 재계산해 겹침을 찾는다.

  python ui_overlap.py <prefab> [canvasW canvasH]

검사:
  FRAME  — 글자/아이콘이 금테 프레임(Panel) 안쪽 여백 밖으로 나감
  BTN    — 버튼 그림 테두리 위에 글자/아이콘이 올라감
  TEXT   — 서로 다른 글자끼리 겹침
  FIT    — 글자가 자기 칸보다 넓음 (자동 축소 최소 크기로도)
레이아웃 그룹이 배치하는 자식은 저장된 값이 맞지 않을 수 있어 표시만 한다.
"""
import re, sys, glob, os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')).replace(os.sep, '/') + '/'
PT = ROOT + 'Assets/_project/3.Textures/UI/PixelTheme/'

# PixelTheme 스프라이트 guid → (이름, 보이는 테두리 두께 at scale 1)
VIS = {'ui_panel_frame_9slice': 16, 'ui_button_blue_9slice': 10, 'ui_button_gold_9slice': 12,
       'ui_button_teal_9slice': 10, 'ui_button_square_9slice': 12, 'ui_info_panel_9slice': 12,
       'ui_header_row': 10, 'ui_inset_panel_9slice': 10}
SPR = {}
for m in glob.glob(PT + '*.png.meta'):
    g = re.search(r'^guid: (\w+)', open(m, encoding='utf-8').read(), re.M).group(1)
    SPR[g] = os.path.basename(m)[:-9]


def parse(path):
    objs = {}
    cur = None
    for line in open(path, encoding='utf-8'):
        m = re.match(r'--- !u!(\d+) &(-?\d+)', line)
        if m:
            cur = {'_cls': int(m.group(1)), '_id': m.group(2), '_raw': []}
            objs[m.group(2)] = cur
            continue
        if cur is not None:
            cur['_raw'].append(line)
    for o in objs.values():
        raw = ''.join(o['_raw'])
        o['raw'] = raw
    return objs


def vec(raw, key):
    m = re.search(r'\n\s*' + key + r': \{x: ([-\d.e]+), y: ([-\d.e]+)', raw)
    return (float(m.group(1)), float(m.group(2))) if m else None


def field(raw, key, default=None):
    m = re.search(r'\n\s*' + key + r': (.*)', raw)
    return m.group(1).strip() if m else default


def text_width(s, fs):
    s = re.sub(r'<[^>]+>', '', s)
    s = s.replace('\\n', '\n')
    w = 0.0
    best = 0.0
    for ch in s:
        if ch == '\n':
            best = max(best, w); w = 0; continue
        o = ord(ch)
        if 0xAC00 <= o <= 0xD7A3 or 0x3040 <= o <= 0x9FFF: w += 0.95
        elif ch == ' ': w += 0.28
        elif ch.isdigit(): w += 0.58
        elif ch in 'il.,:;|!\'': w += 0.3
        elif ch.isupper(): w += 0.68
        else: w += 0.55
    return max(best, w) * fs


def decode(s):
    s = s.strip().strip('"')
    if '\\u' in s:
        try: s = s.encode('utf-8').decode('unicode_escape')
        except Exception: pass
    return s


def analyze(path, cw=1920, ch=1080):
    objs = parse(path)
    go = {k: o for k, o in objs.items() if o['_cls'] == 1}
    rts = {k: o for k, o in objs.items() if o['_cls'] == 224}
    mbs = [o for o in objs.values() if o['_cls'] == 114]
    rt_of_go = {}
    for k, o in rts.items():
        g = re.search(r'm_GameObject: \{fileID: (-?\d+)', o['raw']).group(1)
        rt_of_go[g] = k
        o['go'] = g
        o['father'] = re.search(r'm_Father: \{fileID: (-?\d+)', o['raw']).group(1)
    for g, o in go.items():
        o['name'] = field(o['raw'], 'm_Name', '?')
        o['active'] = field(o['raw'], 'm_IsActive', '1') == '1'
    comps = {}
    for m in mbs:
        g = re.search(r'm_GameObject: \{fileID: (-?\d+)', m['raw'])
        if g: comps.setdefault(g.group(1), []).append(m)

    # 레이아웃 그룹이 있는 부모 (자식 좌표는 런타임이 정한다)
    layout_parent = set()
    for g, cl in comps.items():
        for c in cl:
            if re.search(r'm_ChildAlignment:', c['raw']) and 'm_Spacing' in c['raw']:
                layout_parent.add(g)

    rect = {}

    def calc(k):
        if k in rect: return rect[k]
        o = rts[k]
        f = o['father']
        if f == '0' or f not in rts:
            px, py, pw, ph = 0, 0, cw, ch
        else:
            px, py, pw, ph = calc(f)
        amin = vec(o['raw'], 'm_AnchorMin'); amax = vec(o['raw'], 'm_AnchorMax')
        ap = vec(o['raw'], 'm_AnchoredPosition'); sd = vec(o['raw'], 'm_SizeDelta')
        pv = vec(o['raw'], 'm_Pivot'); sc = vec(o['raw'], 'm_LocalScale') or (1, 1)
        ax0, ay0 = px + pw * amin[0], py + ph * amin[1]
        ax1, ay1 = px + pw * amax[0], py + ph * amax[1]
        w = (ax1 - ax0) + sd[0]; h = (ay1 - ay0) + sd[1]
        cx = ax0 + (ax1 - ax0) * pv[0] + ap[0]
        cy = ay0 + (ay1 - ay0) * pv[1] + ap[1]
        r = (cx - w * pv[0], cy - h * pv[1], w, h)
        rect[k] = r
        return r

    def path_of(k):
        names = []
        while k in rts:
            names.append(go[rts[k]['go']]['name'])
            k = rts[k]['father']
        return '/'.join(reversed(names))

    def visible(k):
        while k in rts:
            if not go[rts[k]['go']]['active']: return False
            k = rts[k]['father']
        return True

    def ancestors(k):
        out = []
        k = rts[k]['father']
        while k in rts:
            out.append(k); k = rts[k]['father']
        return out

    def in_layout(k):
        while k in rts:
            f = rts[k]['father']
            if f in rts and rts[f]['go'] in layout_parent: return True
            k = f
        return False

    # 구성 요소 분류
    texts, images, skinned = [], [], {}
    for k, o in rts.items():
        if not visible(k): continue
        for c in comps.get(o['go'], []):
            raw = c['raw']
            if field(raw, 'm_Enabled', '1') != '1': continue
            if '\n  m_text:' in raw:
                s = decode(field(raw, 'm_text', ''))
                fs = float(field(raw, 'm_fontSize', '36'))
                auto = field(raw, 'm_enableAutoSizing', '0') == '1'
                fmin = float(field(raw, 'm_fontSizeMin', str(fs))) if auto else fs
                ha = int(field(raw, 'm_HorizontalAlignment', '2'))
                wrap = field(raw, 'm_TextWrappingMode', '0') in ('1', '3')
                texts.append(dict(k=k, s=s, fs=fs, fmin=fmin, auto=auto, ha=ha, wrap=wrap))
            elif 'm_Sprite:' in raw and 'm_FillMethod' in raw:
                g = re.search(r'm_Sprite: \{fileID: -?\d+(?:, guid: (\w+))?', raw)
                sp = SPR.get(g.group(1)) if g and g.group(1) else None
                mult = float(field(raw, 'm_PixelsPerUnitMultiplier', '1'))
                ca = re.search(r'm_Color: \{r: [-\d.e]+, g: [-\d.e]+, b: [-\d.e]+, a: ([-\d.e]+)', raw)
                images.append(dict(k=k, sprite=sp, mult=mult, a=float(ca.group(1)) if ca else 1.0))
                if sp in VIS:
                    skinned[k] = (sp, VIS[sp] / mult)

    for k in rts: calc(k)
    issues = []

    def ink(t):
        x, y, w, h = rect[t['k']]
        need = text_width(t['s'], t['fs'])
        if t['auto'] and need > w:
            need = max(text_width(t['s'], t['fmin']), w)
        if t['wrap']: need = min(need, w)
        if t['ha'] in (2, 32):   # center
            x0 = x + (w - need) / 2
        elif t['ha'] == 4:      # right
            x0 = x + w - need
        else:
            x0 = x
        return (x0, y, need, h), need > w + 1

    for t in texts:
        if not t['s'].strip(): continue
        r, over = ink(t)
        t['ink'] = r
        if over and not in_layout(t['k']):
            issues.append(('FIT', path_of(t['k']), f"'{t['s'][:24]}' 폭 {r[2]:.0f} > 칸 {rect[t['k']][2]:.0f}"))

    def inside(r, box, m):
        x, y, w, h = r; bx, by, bw, bh = box
        return x >= bx + m - 0.5 and y >= by + m - 0.5 and x + w <= bx + bw - m + 0.5 and y + h <= by + bh - m + 0.5

    def check_against_frames(k, r, label):
        for a in ancestors(k):
            if a in skinned:
                sp, vis = skinned[a]
                box = rect[a]
                # 프레임 위·아래로 꽉 찬 칸(본문 영역)은 세로는 비교하지 않는다 (글자 높이 추정 오차)
                if not inside((r[0], box[1] + vis + 1, r[2], 1), box, vis):
                    issues.append(('BTN' if 'button' in sp else 'FRAME', path_of(k),
                                   f"{label} 가로가 '{go[rts[a]['go']]['name']}'({sp[3:]}) 테두리 {vis:.0f}px 를 침범"))
                return

    for t in texts:
        if 'ink' in t and not in_layout(t['k']):
            check_against_frames(t['k'], t['ink'], f"'{t['s'][:20]}'")
    for im in images:
        if im['k'] in skinned or im['sprite'] in VIS: continue
        if in_layout(im['k']): continue
        n = go[rts[im['k']]['go']]['name']
        if n not in ('Icon', 'Mark') and not n.endswith('Icon'): continue
        check_against_frames(im['k'], rect[im['k']], f"아이콘 {n}")

    # COVER — 프레임 패널의 자식 배경이 금테 띠를 덮는다
    frames = [k for k, (sp, v) in skinned.items() if sp == 'ui_panel_frame_9slice']
    for im in images:
        if im['sprite'] is not None or im['a'] < 0.5 or in_layout(im['k']): continue
        n = go[rts[im['k']]['go']]['name']
        if n in ('AccentLine',): continue
        for f in frames:
            if f not in ancestors(im['k']): continue
            fx, fy, fw, fh = rect[f]; vis = skinned[f][1]
            x, y, w, h = rect[im['k']]
            if w < 2 or h < 2: continue
            hit = []
            if x < fx + vis - 1 and x + w > fx: hit.append('왼')
            if x + w > fx + fw - vis + 1 and x < fx + fw: hit.append('오른')
            if y < fy + vis - 1 and y + h > fy: hit.append('아래')
            if y + h > fy + fh - vis + 1 and y < fy + fh: hit.append('위')
            if hit:
                issues.append(('COVER', path_of(im['k']), f"배경이 금테({vis:.0f}px) {'·'.join(hit)}쪽을 덮음"))
            break

    # 글자끼리 겹침
    T = [t for t in texts if 'ink' in t and not in_layout(t['k'])]
    for i in range(len(T)):
        for j in range(i + 1, len(T)):
            a, b = T[i]['ink'], T[j]['ink']
            ox = min(a[0] + a[2], b[0] + b[2]) - max(a[0], b[0])
            oy = min(a[1] + a[3], b[1] + b[3]) - max(a[1], b[1])
            na = path_of(T[i]['k']).split('/')[-1]; nb = path_of(T[j]['k']).split('/')[-1]
            if 'Shadow' in na or 'Shadow' in nb: continue
            if ox > 4 and oy > min(a[3], b[3]) * 0.4:
                issues.append(('TEXT', path_of(T[i]['k']), f"'{T[i]['s'][:16]}' ↔ '{T[j]['s'][:16]}' ({path_of(T[j]['k']).split('/')[-1]})"))
    import os as _o
    if _o.environ.get('DBG'):
        print('layout parents:', sorted({go[g]['name'] for g in layout_parent})[:30])
        print('texts', len(texts), 'in_layout', sum(in_layout(t['k']) for t in texts), 'images', len(images), 'frames', len([k for k,(sp,v) in skinned.items() if sp=='ui_panel_frame_9slice']))
    return issues


if __name__ == '__main__':
    p = sys.argv[1]
    cw, ch = (int(sys.argv[2]), int(sys.argv[3])) if len(sys.argv) > 3 else (1920, 1080)
    res = analyze(p, cw, ch)
    from collections import Counter
    print(os.path.basename(p), Counter(r[0] for r in res))
    for kind, path, msg in res:
        print(f'  [{kind}] {path.split("/", 1)[-1][:70]}  — {msg}')
