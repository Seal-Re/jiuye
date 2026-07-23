# -*- coding: utf-8 -*-
"""像素素材后处理——绿幕抠图 + Alpha 二值化 + 色阶量化 (pipeline v3)"""
import cv2, numpy as np, os, sys

def remove_chroma_key(img_path, output_path):
    """HSV 空间全图绿幕扣除——包含内部封闭区域"""
    img = cv2.imread(img_path, cv2.IMREAD_UNCHANGED)
    if img is None:
        print(f"ERROR: cannot read {img_path}")
        return False
    if img.shape[2] < 4:
        img = cv2.cvtColor(img, cv2.COLOR_BGR2BGRA)

    hsv = cv2.cvtColor(img[:,:,:3], cv2.COLOR_BGR2HSV)
    lower_green = np.array([35, 100, 100])
    upper_green = np.array([85, 255, 255])
    mask = cv2.inRange(hsv, lower_green, upper_green)

    # 形态学闭操作——填补内部小孔
    kernel = np.ones((3,3), np.uint8)
    mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, kernel)

    # 绿幕区域 Alpha→0
    img[mask > 0] = [0, 0, 0, 0]

    # Alpha 二值化: <128→0, >=128→255 (1-bit binary)
    alpha = img[:,:,3]
    alpha[alpha < 128] = 0
    alpha[alpha >= 128] = 255

    cv2.imwrite(output_path, img)
    return True

def quantize_colors(img_path, output_path, levels=5):
    """色阶量化收敛至 N-tone ramp"""
    img = cv2.imread(img_path, cv2.IMREAD_UNCHANGED)
    if img is None: return False
    rgb = img[:,:,:3]
    factor = 256 // levels
    quantized = (rgb // factor) * factor + factor // 2
    img[:,:,:3] = np.clip(quantized, 0, 255).astype(np.uint8)
    cv2.imwrite(output_path, img)
    return True

def post_process(input_path, output_path, do_quantize=True, do_chroma=True):
    """完整后处理管线"""
    tmp = input_path
    if do_chroma:
        tmp = output_path + ".chroma_tmp.png"
        if not remove_chroma_key(input_path, tmp):
            return False
    if do_quantize:
        final = output_path
        if not quantize_colors(tmp, final): return False
        if do_chroma: os.remove(tmp)
    return True

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python post_process.py <tiles_dir>")
        sys.exit(1)
    tiles_dir = sys.argv[1]
    for f in os.listdir(tiles_dir):
        if not f.endswith('_alpha.png'): continue
        src = os.path.join(tiles_dir, f)
        dst = os.path.join(tiles_dir, f.replace('_alpha.png', '_clean.png'))
        post_process(src, dst, do_quantize=True, do_chroma=True)
        print(f"processed: {f} → {os.path.basename(dst)}")
