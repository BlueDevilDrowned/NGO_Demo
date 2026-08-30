from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path
import math
import textwrap

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(r"D:\UnityHub\Project\NGO")
DATA_SCRIPT = ROOT / "tools" / "build_complete_dialogue_doc.py"
OUTPUT_DIR = ROOT / "generated_story_graphs"


def load_data():
    spec = spec_from_file_location("dialogue_data", DATA_SCRIPT)
    module = module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def font(size, bold=False):
    candidates = [
        Path(r"C:\Windows\Fonts\msyhbd.ttc" if bold else r"C:\Windows\Fonts\msyh.ttc"),
        Path(r"C:\Windows\Fonts\simhei.ttf"),
        Path(r"C:\Windows\Fonts\simsun.ttc"),
    ]
    for candidate in candidates:
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size=size)
    return ImageFont.load_default()


F_TITLE = font(38, True)
F_SUBTITLE = font(20)
F_STAGE = font(25, True)
F_NODE_TITLE = font(23, True)
F_NODE = font(18)
F_SMALL = font(16)
F_LEGEND = font(17)


COLORS = {
    "bg": "#F7F8FA",
    "ink": "#1F2933",
    "muted": "#5E6C76",
    "line": "#A8B2BA",
    "base_fill": "#E9EEF2",
    "base_stroke": "#657783",
    "topic_fill": "#E3F0F7",
    "topic_stroke": "#2F6F91",
    "auto_fill": "#E8F3EA",
    "auto_stroke": "#3F7D52",
    "scene_fill": "#F8EEDB",
    "scene_stroke": "#996A25",
    "choice_fill": "#F1EAF7",
    "choice_stroke": "#72558A",
    "white": "#FFFFFF",
}


GRAPH_META = {
    "LiuChouStoryGraph": ("刘丑", "01-liuchou-story-graph.png"),
    "StudentGroupStoryGraph": ("周围学生", "02-student-group-story-graph.png"),
    "ZhouYaoStoryGraph": ("周曜", "03-zhouyao-story-graph.png"),
    "GuTeacherStoryGraph": ("顾老师", "04-gu-teacher-story-graph.png"),
    "HeTeacherStoryGraph": ("贺老师", "05-he-teacher-story-graph.png"),
    "SuHeStoryGraph": ("苏禾", "06-suhe-story-graph.png"),
    "ChenUncleStoryGraph": ("陈叔", "07-chen-uncle-story-graph.png"),
    "XuChengStoryGraph": ("许澄", "08-xucheng-story-graph.png"),
    "LuoTeacherStoryGraph": ("罗老师", "09-luo-teacher-story-graph.png"),
}


STAGE_ORDER = [
    "R1D1", "R1D2", "R1D3", "R1D4",
    "R2D1", "R2D2", "R2D3", "R2D4",
    "R3D1", "R3D2", "R3D3", "R3D4", "R4D2",
]


def stage_of(value):
    for stage in STAGE_ORDER:
        if stage in value:
            return stage
    return value


def wrap_chars(text, width):
    lines = []
    for paragraph in text.split("\n"):
        while len(paragraph) > width:
            lines.append(paragraph[:width])
            paragraph = paragraph[width:]
        if paragraph:
            lines.append(paragraph)
    return lines or [""]


def draw_multiline(draw, xy, text, font_obj, fill, width_chars, gap=5, max_lines=None):
    x, y = xy
    lines = wrap_chars(text, width_chars)
    if max_lines and len(lines) > max_lines:
        lines = lines[:max_lines]
        lines[-1] = lines[-1][:-1] + "…"
    for line in lines:
        draw.text((x, y), line, font=font_obj, fill=fill)
        bbox = draw.textbbox((x, y), line, font=font_obj)
        y = bbox[3] + gap
    return y


def draw_arrow(draw, start, end, color, width=3, dashed=False):
    x1, y1 = start
    x2, y2 = end
    if dashed:
        segments = 16
        for i in range(0, segments, 2):
            a = i / segments
            b = min((i + 1) / segments, 1)
            draw.line((x1 + (x2 - x1) * a, y1 + (y2 - y1) * a,
                       x1 + (x2 - x1) * b, y1 + (y2 - y1) * b), fill=color, width=width)
    else:
        draw.line((x1, y1, x2, y2), fill=color, width=width)
    angle = math.atan2(y2 - y1, x2 - x1)
    length = 14
    left = (x2 - length * math.cos(angle - 0.55), y2 - length * math.sin(angle - 0.55))
    right = (x2 - length * math.cos(angle + 0.55), y2 - length * math.sin(angle + 0.55))
    draw.polygon([end, left, right], fill=color)


def activation_palette(mode):
    if "NPC主动" in mode or "主动回应" in mode or "主动给予" in mode:
        return COLORS["auto_fill"], COLORS["auto_stroke"]
    if "现场" in mode or "播放" in mode or "系统" in mode:
        return COLORS["scene_fill"], COLORS["scene_stroke"]
    return COLORS["topic_fill"], COLORS["topic_stroke"]


def base_matches(base, role, stage):
    return role in base["key"] and stage in base["key"]


def build_graph(data, graph_name, role, filename):
    clues = []
    for clue in data.CLUES:
        assignment, node_id = data.GRAPH_ASSIGNMENTS[clue["id"]]
        if graph_name not in assignment:
            continue
        mode, condition, topic, note = data.ACTIVATION[clue["id"]]
        clues.append({
            "id": clue["id"],
            "stage": stage_of(clue["round"]),
            "item": clue["item"],
            "mode": mode,
            "condition": condition,
            "topic": topic,
            "choices": [label for label, _ in clue["choices"]],
            "node_id": node_id,
        })

    stages = sorted({clue["stage"] for clue in clues}, key=lambda s: STAGE_ORDER.index(s) if s in STAGE_ORDER else 999)
    if not stages:
        return None

    groups = {stage: [clue for clue in clues if clue["stage"] == stage] for stage in stages}
    col_width = 350
    left = 90
    top = 145
    base_h = 104
    node_h = 245
    node_gap = 34
    column_gap = 42
    width = max(1500, left * 2 + len(stages) * col_width + (len(stages) - 1) * column_gap)
    max_nodes = max(len(items) for items in groups.values())
    height = top + base_h + 72 + max_nodes * (node_h + node_gap) + 130

    image = Image.new("RGB", (width, height), COLORS["bg"])
    draw = ImageDraw.Draw(image)
    draw.text((left, 34), f"{role}｜角色故事图", font=F_TITLE, fill=COLORS["ink"])
    draw.text((left, 87), "横向：轮次 / 天次　纵向：该时间入口下可解锁的线索话题", font=F_SUBTITLE, fill=COLORS["muted"])

    base_boxes = {}
    clue_boxes = {}
    for col_index, stage in enumerate(stages):
        x = left + col_index * (col_width + column_gap)
        draw.text((x, top - 46), stage, font=F_STAGE, fill=COLORS["ink"])
        base_box = (x, top, x + col_width, top + base_h)
        draw.rounded_rectangle(base_box, radius=10, fill=COLORS["base_fill"], outline=COLORS["base_stroke"], width=3)
        draw.text((x + 18, top + 14), "普通会话入口", font=F_NODE_TITLE, fill=COLORS["ink"])
        matching = next((base for base in data.BASE_CONVERSATIONS if base_matches(base, role, stage)), None)
        repeat = matching["repeat"] if matching else f"{role}：有具体线索再选话题。"
        repeat = repeat.replace(f"{role}：", "")
        draw_multiline(draw, (x + 18, top + 52), repeat, F_SMALL, COLORS["muted"], 19, max_lines=2)
        base_boxes[stage] = base_box

        for row_index, clue in enumerate(groups[stage]):
            y = top + base_h + 72 + row_index * (node_h + node_gap)
            box = (x, y, x + col_width, y + node_h)
            fill, stroke = activation_palette(clue["mode"])
            draw.rounded_rectangle(box, radius=10, fill=fill, outline=stroke, width=4)
            draw.text((x + 18, y + 14), f"{clue['id']}｜{clue['item']}", font=F_NODE_TITLE, fill=COLORS["ink"])
            current_y = y + 52
            current_y = draw_multiline(draw, (x + 18, current_y), f"入口：{clue['topic']}", F_NODE, COLORS["ink"], 19, max_lines=2)
            current_y = draw_multiline(draw, (x + 18, current_y + 3), f"条件：{clue['condition']}", F_SMALL, COLORS["muted"], 21, max_lines=2)
            choice_text = "主角选择：" + " / ".join(clue["choices"])
            draw_multiline(draw, (x + 18, current_y + 6), choice_text, F_SMALL, COLORS["choice_stroke"], 21, max_lines=3)
            clue_boxes[(stage, clue["id"])] = box
            draw_arrow(draw, ((box[0] + box[2]) / 2, base_box[3]), ((box[0] + box[2]) / 2, box[1]), stroke, width=3)

    for index in range(len(stages) - 1):
        left_box = base_boxes[stages[index]]
        right_box = base_boxes[stages[index + 1]]
        draw_arrow(draw, (left_box[2], (left_box[1] + left_box[3]) / 2),
                   (right_box[0], (right_box[1] + right_box[3]) / 2), COLORS["line"], width=3, dashed=True)

    legend_y = height - 62
    legend = [
        (COLORS["topic_fill"], COLORS["topic_stroke"], "主角主动话题"),
        (COLORS["auto_fill"], COLORS["auto_stroke"], "NPC主动对白"),
        (COLORS["scene_fill"], COLORS["scene_stroke"], "拾取 / 场景事件"),
    ]
    lx = left
    for fill, stroke, label in legend:
        draw.rounded_rectangle((lx, legend_y, lx + 34, legend_y + 24), radius=5, fill=fill, outline=stroke, width=2)
        draw.text((lx + 46, legend_y - 2), label, font=F_LEGEND, fill=COLORS["muted"])
        lx += 240

    output = OUTPUT_DIR / filename
    image.save(output, format="PNG", optimize=True, dpi=(180, 180))
    return output


def make_contact_sheet(paths):
    thumbs = []
    for path in paths:
        image = Image.open(path).convert("RGB")
        image.thumbnail((700, 430))
        thumbs.append((path, image.copy()))
    sheet = Image.new("RGB", (1460, math.ceil(len(thumbs) / 2) * 500), "#F7F8FA")
    draw = ImageDraw.Draw(sheet)
    for index, (path, image) in enumerate(thumbs):
        col = index % 2
        row = index // 2
        x = 25 + col * 720
        y = 30 + row * 500
        sheet.paste(image, (x, y + 35))
        draw.text((x, y), path.stem, font=F_LEGEND, fill=COLORS["ink"])
    output = OUTPUT_DIR / "contact-sheet.png"
    sheet.save(output, format="PNG", optimize=True)
    return output


def main():
    data = load_data()
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    paths = []
    for graph_name, (role, filename) in GRAPH_META.items():
        output = build_graph(data, graph_name, role, filename)
        if output:
            paths.append(output)
            print(output)
    print(make_contact_sheet(paths))


if __name__ == "__main__":
    main()
