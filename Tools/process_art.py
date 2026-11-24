"""把生成的 PNG 抠图、切图，输出为 Resources 下的透明精灵。
运行时用 Texture2D + Sprite.Create 加载，因此不依赖 Unity 的 Sprite 导入设置。
"""
import os
import cv2
import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, "Assets", "Art")
OUT = os.path.join(ROOT, "Assets", "Resources", "Art")

VARIANTS = {
    "Characters": [
        ("2D_side_view_game_character_sp_2026-11-22T03-11-47.png", "warrior", 34),
        ("2D_side_view_game_character_sp_2026-11-22T03-12-10.png", "ranger", 34),
        ("2D_side_view_game_character_sp_2026-11-22T03-12-29.png", "mage", 30),
    ],
    "Enemies": [
        ("2D_game_sprite_sheet__four_dis_2026-11-22T03-13-12.png", None, 26),
        ("2D_side_view_game_boss_sprite__2026-11-22T03-13-30.png", "boss_minor", 32),
        ("2D_side_view_game_final_boss_s_2026-11-22T03-13-48.png", "boss_major", 32),
    ],
    "UI": [
        ("2D_game_UI_kit_sheet__medieval_2026-11-22T03-14-12.png", None, 30),
    ],
    "Icons": [
        ("2D_game_item_icon_sheet__medie_2026-11-22T03-14-32.png", None, 28),
    ],
}

ICON_NAMES = [
    "sword", "axe", "hammer", "shield",
    "bow", "crossbow", "staff", "book",
    "helmet", "armor", "amulet", "bandage",
    "herb", "potion_green", "potion_purple", "potion_gold",
]

UI_RECTS = {
    "panel_large": (20, 10, 620, 465),
    "panel_small": (635, 20, 1010, 465),
    "bar_hp": (25, 485, 505, 585),
    "bar_stamina": (520, 485, 1005, 585),
    "frame_gray": (20, 605, 215, 795),
    "frame_green": (225, 605, 420, 795),
    "frame_blue": (430, 605, 625, 795),
    "frame_purple": (225, 805, 420, 1000),
    "frame_orange": (430, 805, 625, 1000),
    "btn_round": (640, 605, 815, 790),
    "btn_close": (820, 805, 1005, 990),
}


def bg_color(bgr):
    h, w = bgr.shape[:2]
    border = np.concatenate([
        bgr[0, :, :], bgr[-1, :, :], bgr[:, 0, :], bgr[:, -1, :]
    ], axis=0)
    return np.median(border, axis=0).astype(np.uint8)


def transparent(bgr, tolerance):
    """从四角 flood fill 背景，转成 RGBA。"""
    from scipy import ndimage

    h, w = bgr.shape[:2]
    c = bg_color(bgr).astype(np.int16)
    dist = np.max(np.abs(bgr.astype(np.int16) - c), axis=2)
    # 与背景色接近的像素做连通域，凡碰到图像边界的都判为背景
    labels, _ = ndimage.label(dist <= tolerance)
    border = np.unique(np.concatenate([
        labels[0, :], labels[-1, :], labels[:, 0], labels[:, -1]
    ]))
    filled = np.isin(labels, border[border != 0]).astype(np.uint8) * 255

    # 水印区域：右下角浅灰字形，按背景色相近度清掉
    wm_x0, wm_y0 = int(w * 0.84), int(h * 0.93)
    sub = filled[wm_y0:, wm_x0:]
    sub[dist[wm_y0:, wm_x0:] < 110] = 255

    alpha = 255 - filled
    # 收 1 像素去掉浅色背景渗边，再轻度抗锯齿
    alpha = cv2.erode(alpha, np.ones((3, 3), np.uint8), iterations=1)
    alpha = cv2.GaussianBlur(alpha, (3, 3), 0.4)
    # imwrite 按 BGRA 写出，不能转成 RGBA，否则红蓝互换
    bgra = cv2.cvtColor(bgr, cv2.COLOR_BGR2BGRA)
    bgra[:, :, 3] = alpha
    return bgra


def autocrop(rgba, pad=6):
    ys, xs = np.where(rgba[:, :, 3] > 12)
    if len(xs) == 0:
        return rgba
    x0, x1 = max(0, xs.min() - pad), min(rgba.shape[1], xs.max() + pad + 1)
    y0, y1 = max(0, ys.min() - pad), min(rgba.shape[0], ys.max() + pad + 1)
    return rgba[y0:y1, x0:x1]


def save(rgba, subdir, name):
    path = os.path.join(OUT, subdir)
    os.makedirs(path, exist_ok=True)
    cv2.imwrite(os.path.join(path, name + ".png"), rgba)
    print(f"  {subdir}/{name}.png  {rgba.shape[1]}x{rgba.shape[0]}")


def process():
    for subdir, variants in VARIANTS.items():
        for filename, name, tol in variants:
            src = os.path.join(ART, subdir, filename)
            bgr = cv2.imread(src, cv2.IMREAD_COLOR)
            if bgr is None:
                print("MISSING", src)
                continue
            print(subdir, filename)

            if subdir == "Enemies" and name is None:
                h, w = bgr.shape[:2]
                for i, ename in enumerate(["grunt", "archer", "spore", "shield"]):
                    r, c = divmod(i, 2)
                    cell = bgr[r * h // 2:(r + 1) * h // 2,
                               c * w // 2:(c + 1) * w // 2]
                    save(autocrop(transparent(cell, tol)), subdir, "enemy_" + ename)

            elif subdir == "Icons" and name is None:
                h, w = bgr.shape[:2]
                for i, iname in enumerate(ICON_NAMES):
                    r, c = divmod(i, 4)
                    cell = bgr[r * h // 4:(r + 1) * h // 4,
                               c * w // 4:(c + 1) * w // 4]
                    save(autocrop(transparent(cell, tol)), subdir, "icon_" + iname)

            elif subdir == "UI" and name is None:
                for uname, (x0, y0, x1, y1) in UI_RECTS.items():
                    cell = bgr[y0:y1, x0:x1]
                    save(transparent(cell, tol), subdir, uname)

            else:
                save(autocrop(transparent(bgr, tol)), subdir, name)

    # 背景不走透明，直接复制
    bg = os.path.join(ART, "Backgrounds")
    for f in os.listdir(bg):
        if f.endswith(".png"):
            dst = os.path.join(OUT, "Backgrounds")
            os.makedirs(dst, exist_ok=True)
            img = cv2.imread(os.path.join(bg, f))
            cv2.imwrite(os.path.join(dst, "wasteland.png"), img)
            print("  Backgrounds/wasteland.png")


if __name__ == "__main__":
    process()
