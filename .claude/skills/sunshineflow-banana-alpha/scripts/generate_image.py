#!/usr/bin/env python
# -*- coding: UTF-8 -*-
"""
SunshineFlow Image Generator Script
从 .env 文件读取认证信息，调用 DreamMaker SunshineFlow API 完成生图任务。
"""

import argparse
import base64
import json
import mimetypes
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from http import client as httpclient
from pathlib import Path


BASEURL_PREFIX = "http://sunshineflow-api-server-prod.tmax.nie.netease.com"
BLUEPRINTS_FILE = Path(__file__).parent / "blueprints.json"

POLL_INTERVAL = 1
TIMEOUT = 300
RATE_LIMIT_RETRIES = 3
REQUEST_TIMEOUT = 30
AUTH_ERROR_KEYWORDS = ("401", "403", "token", "expired", "unauthorized", "auth")
CONTENT_TYPE_TO_EXT = {
    "image/png": "png",
    "image/jpeg": "jpeg",
    "image/jpg": "jpeg",
    "image/webp": "webp",
    "image/gif": "gif",
}


# ── 下载到本地（可选，仅 --output-dir 传入时生效）────────────────────────────

# output_keys 中常见关键词到文件名后缀的映射，用于双图蓝图区分主备
OUTPUT_KEY_LABELS = {
    "带alpha图": "alpha",
    "不带alpha图": "noalpha",
    "alpha-out": "alpha",
    "pixel-out": "",
    "output_textures": "",
    "output": "",
}


def slugify_for_filename(text: str, max_len: int = 30) -> str:
    """把 prompt 转成文件名安全的 slug：保留字母数字中文，其他换成 _。"""
    if not text:
        return "image"
    # 替换 Windows/Unix 都不允许的字符，并合并连续空白
    bad = '<>:"/\\|?*\n\r\t'
    cleaned = "".join("_" if c in bad else c for c in text.strip())
    cleaned = "_".join(cleaned.split())
    cleaned = cleaned.strip("._-")
    if len(cleaned) > max_len:
        cleaned = cleaned[:max_len].rstrip("._-")
    return cleaned or "image"


def label_for_index(blueprint: dict, output_textures: list, index: int) -> str:
    """
    根据蓝图 output_keys 顺序，给第 index 张图找一个语义化标签（如 alpha / noalpha）。
    单图蓝图返回空串。
    """
    output_keys = blueprint.get("output_keys") or []
    if len(output_textures) <= 1 or index >= len(output_keys):
        return ""
    return OUTPUT_KEY_LABELS.get(output_keys[index], output_keys[index]).strip()


def download_outputs(
    output_textures: list,
    output_dir: str,
    blueprint_id: str,
    blueprint: dict,
    prompt: str,
) -> list:
    """
    把生图返回的 URL 列表全部下载到 output_dir，按规范命名。
    返回每张图的本地绝对路径列表，顺序与 output_textures 一致。
    命名: <bp_id>_<YYYYMMDD_HHMMSS>_<slug>_<idx>[_<label>].<ext>
    """
    out_dir = Path(output_dir).expanduser().resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    from datetime import datetime
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    slug = slugify_for_filename(prompt)

    saved_paths: list = []
    for index, url in enumerate(output_textures):
        try:
            # 下载二进制并嗅探类型
            with urllib.request.urlopen(url, timeout=REQUEST_TIMEOUT) as resp:
                data = resp.read()
                content_type = resp.headers.get("Content-Type", "image/png").split(";")[0].strip().lower()
        except Exception as exc:
            print(f"[下载失败] {url}: {exc}", file=sys.stderr)
            saved_paths.append("")
            continue

        ext = CONTENT_TYPE_TO_EXT.get(content_type, "png")
        # URL 末尾如果带扩展名，用 URL 的更准
        url_path = urllib.parse.urlparse(url).path
        url_ext = Path(url_path).suffix.lstrip(".").lower()
        if url_ext in CONTENT_TYPE_TO_EXT.values():
            ext = url_ext

        label = label_for_index(blueprint, output_textures, index)
        parts = [blueprint_id, timestamp, slug, str(index)]
        if label:
            parts.append(label)
        filename = "_".join(parts) + "." + ext

        target = out_dir / filename
        target.write_bytes(data)
        size_kb = len(data) // 1024
        print(f"[已下载] {target} ({size_kb} KB)", file=sys.stderr)
        saved_paths.append(str(target))

    return saved_paths


class SunshineFlowError(RuntimeError):
    def __init__(self, error: str, message: str, **details):
        super().__init__(message)
        self.error = error
        self.details = details


# ── 蓝图注册表 ────────────────────────────────────────────────────────────────

def load_blueprints() -> dict:
    """读取 blueprints.json，返回 {default, blueprints} 结构。"""
    if not BLUEPRINTS_FILE.exists():
        emit_error(
            "blueprints_missing",
            f"找不到蓝图注册表：{BLUEPRINTS_FILE}。请确认 skill 安装完整。"
        )
    try:
        with open(BLUEPRINTS_FILE, "r", encoding="utf-8") as f:
            data = json.load(f)
    except (json.JSONDecodeError, OSError) as exc:
        emit_error("blueprints_invalid", f"蓝图注册表解析失败：{exc}")

    blueprints = data.get("blueprints") or {}
    if not blueprints:
        emit_error("blueprints_empty", "blueprints.json 中未定义任何蓝图。")
    return data


def resolve_blueprint(registry: dict, blueprint_id: str = None, prompt: str = "") -> tuple:
    """
    选择要使用的蓝图：
      1. blueprint_id 显式指定 → 直接用
      2. 根据 prompt 关键词匹配，按 priority 排序，命中数量多 + 优先级高的赢
      3. 都不匹配 → 用 registry.default
    返回 (blueprint_id, blueprint_dict)
    """
    blueprints = registry["blueprints"]

    if blueprint_id:
        if blueprint_id not in blueprints:
            available = ", ".join(blueprints.keys())
            emit_error(
                "unknown_blueprint",
                f"未知蓝图 '{blueprint_id}'。可用蓝图：{available}"
            )
        return blueprint_id, blueprints[blueprint_id]

    prompt_lower = (prompt or "").lower()
    best = None  # (score, priority, bp_id)
    for bp_id, bp in blueprints.items():
        keywords = bp.get("trigger_keywords") or []
        hits = sum(1 for kw in keywords if kw.lower() in prompt_lower)
        if hits == 0:
            continue
        priority = bp.get("priority", 0)
        candidate = (hits, priority, bp_id)
        if best is None or candidate > best:
            best = candidate

    if best:
        bp_id = best[2]
        return bp_id, blueprints[bp_id]

    default_id = registry.get("default")
    if not default_id or default_id not in blueprints:
        emit_error("no_default_blueprint", "未匹配到任何蓝图，且 default 字段无效。")
    return default_id, blueprints[default_id]


def list_blueprints(registry: dict) -> None:
    """打印蓝图列表，用于 --list-blueprints 命令。"""
    default_id = registry.get("default")
    print(f"📋 已注册蓝图（default: {default_id}）：\n")
    for bp_id, bp in registry["blueprints"].items():
        marker = " ⭐ DEFAULT" if bp_id == default_id else ""
        print(f"  ▸ {bp_id}{marker}")
        print(f"      {bp.get('display_name', '-')}")
        print(f"      {bp.get('description', '-')}")
        print(f"      base_path        : {bp.get('base_path')}")
        print(f"      supports_image   : {bp.get('supports_image_input', False)}")
        print(f"      trigger_keywords : {', '.join(bp.get('trigger_keywords', []))}")
        print(f"      priority         : {bp.get('priority', 0)}")
        print()


def load_env(env_path: str = None) -> dict:
    """
    .env 查找顺序（找到即停）：
      1. 命令行 --env 显式指定
      2. 当前工作目录   ./.env                       （工程内配置，兼容老用法）
      3. Skill 目录     <skill_dir>/config/.env      ⭐ 推荐：跟 skill 走，多工程复用
      4. 用户全局配置   ~/.config/sunshineflow/.env  （兜底：完全脱离工程目录）

    位置 3（<skill>/config/）是 skill 自带的私有配置目录，push 到云端 Hub 时
    skills_hub_api.py 会自动排除整个 config/ 目录，永远不会泄漏密钥。

    强烈建议把密钥放在第 3 处：跟着 skill 走，所有工程共用，不会被任何工程的 git 误传。
    """
    candidates = []
    if env_path:
        candidates.append(Path(env_path))
    candidates.append(Path.cwd() / ".env")
    # ⭐ skill 目录下专用配置目录（推荐）
    skill_root = Path(__file__).parent.parent
    candidates.append(skill_root / "config" / ".env")
    # 用户全局配置（兜底）
    candidates.append(Path.home() / ".config" / "sunshineflow" / ".env")
    # 兼容老路径：scripts/.env（之前 1.1.0 引入过）
    candidates.append(Path(__file__).parent / ".env")
    # 兼容更老的路径（向上 4 层）
    candidates.append(Path(__file__).parent.parent.parent.parent / ".env")

    for path in candidates:
        if path.exists():
            env = {}
            with open(path, "r", encoding="utf-8") as f:
                for line in f:
                    line = line.strip()
                    if not line or line.startswith("#") or "=" not in line:
                        continue
                    key, _, val = line.partition("=")
                    env[key.strip()] = val.strip()
            env["__loaded_from__"] = str(path)
            return env

    return {}


def get_required_config(env: dict) -> tuple:
    required_keys = ["SUNSHINEFLOW_AUTH_TOKEN", "SUNSHINEFLOW_AUTH_USER", "SUNSHINEFLOW_APP_CODE"]
    missing = [k for k in required_keys if not env.get(k)]
    if missing:
        skill_config_path = Path(__file__).parent.parent / "config" / ".env"
        global_env_path = Path.home() / ".config" / "sunshineflow" / ".env"
        print(json.dumps({
            "error": "missing_config",
            "missing_keys": missing,
            "message": (
                "缺少必要的认证配置。推荐把 .env 放在 skill 自带的 config 目录（跟 skill 走，所有工程共用，push 到云端时会自动排除）：\n"
                f"  {skill_config_path}\n\n"
                "也可以放在用户全局目录（彻底脱离 skill）：\n"
                f"  {global_env_path}\n\n"
                "文件内容示例：\n"
                "SUNSHINEFLOW_AUTH_TOKEN=你的v2 token\n"
                "SUNSHINEFLOW_AUTH_USER=你的企业邮箱前缀\n"
                "SUNSHINEFLOW_APP_CODE=你的app_code\n\n"
                "注意：蓝图 api_key 已迁移到 scripts/blueprints.json 统一管理，不再需要在 .env 中配置。\n\n"
                "查找优先级（找到即停）：\n"
                "  1) --env 显式指定\n"
                "  2) ./.env （当前工作目录，便于工程级覆盖）\n"
                "  3) <skill>/config/.env （⭐ 推荐：跟 skill 走，push 时自动排除）\n"
                "  4) ~/.config/sunshineflow/.env （兜底：全局共享）\n\n"
                "获取方式：\n"
                "  AUTH_TOKEN: https://console-auth.nie.netease.com/ 复制 v2 token\n"
                "  AUTH_USER: 企业邮箱前缀，例如 zhangsan01\n"
                "  APP_CODE: https://dreammaker.netease.com/permission 查看用户组 app_code"
            )
        }, ensure_ascii=False, indent=2))
        sys.exit(1)

    return (
        env["SUNSHINEFLOW_AUTH_TOKEN"],
        env["SUNSHINEFLOW_AUTH_USER"],
        env["SUNSHINEFLOW_APP_CODE"],
    )


def build_headers(auth_token: str, auth_user: str, app_code: str, api_key: str = "") -> dict:
    headers = {
        "X-Access-Token": auth_token,
        "X-Auth-User": auth_user,
        "X-Aigw-App": app_code,
    }
    if api_key:
        headers["X-Auth-API-key"] = api_key
    return headers


def emit_error(error: str, message: str, **extra):
    payload = {
        "status": "error",
        "error": error,
        "message": message,
    }
    payload.update(extra)
    print(json.dumps(payload, ensure_ascii=False, indent=2))
    sys.exit(1)


def classify_response_error(status_code: int, body_text: str) -> SunshineFlowError:
    body_lower = (body_text or "").lower()
    if status_code == httpclient.TOO_MANY_REQUESTS or "rate limit" in body_lower:
        return SunshineFlowError("rate_limited", "SunshineFlow API 触发速率限制，请稍后重试", status_code=status_code, response=body_text)
    if status_code in (httpclient.UNAUTHORIZED, httpclient.FORBIDDEN) or any(keyword in body_lower for keyword in AUTH_ERROR_KEYWORDS):
        return SunshineFlowError("auth_error", "Token 可能已过期或认证失败，请更新 .env 后重试", status_code=status_code, response=body_text)
    return SunshineFlowError("request_failed", f"请求接口错误，状态码：{status_code}，内容：{body_text}", status_code=status_code, response=body_text)


def urlopen_json(method: str, url: str, req_headers: dict, body=None):
    headers = dict(req_headers)
    data = None
    if body is not None:
        data = json.dumps(body).encode("utf-8")
        headers["Content-Type"] = "application/json"

    request = urllib.request.Request(url, data=data, headers=headers, method=method.upper())
    try:
        with urllib.request.urlopen(request, timeout=REQUEST_TIMEOUT) as response:
            status_code = response.getcode()
            text = response.read().decode("utf-8")
            return status_code, text
    except urllib.error.HTTPError as exc:
        body_text = exc.read().decode("utf-8", errors="replace")
        return exc.code, body_text


def fetch_binary(url: str):
    request = urllib.request.Request(url, method="GET")
    with urllib.request.urlopen(request, timeout=REQUEST_TIMEOUT) as response:
        return response.read(), response.headers.get_content_type()


def guess_content_type(source: str, content_type: str = None) -> str:
    if content_type in CONTENT_TYPE_TO_EXT:
        return content_type

    guessed, _ = mimetypes.guess_type(str(source))
    if guessed in CONTENT_TYPE_TO_EXT:
        return guessed

    path = urllib.parse.urlparse(str(source)).path.lower()
    for mime_type, extension in CONTENT_TYPE_TO_EXT.items():
        if path.endswith(f".{extension}"):
            return mime_type

    return "image/png"


def binary_to_data_url(binary: bytes, content_type: str) -> str:
    encoded = base64.b64encode(binary).decode("ascii")
    return f"data:{content_type};base64,{encoded}"


def image_url_to_data_url(url: str) -> str:
    binary, content_type = fetch_binary(url)
    resolved_content_type = guess_content_type(url, content_type)
    return binary_to_data_url(binary, resolved_content_type)


def image_file_to_data_url(path: str) -> str:
    file_path = Path(path)
    binary = file_path.read_bytes()
    content_type = guess_content_type(file_path)
    return binary_to_data_url(binary, content_type)


def image_source_to_data_url(source: str) -> str:
    if not source:
        raise ValueError("参考图来源为空")
    if source.startswith("data:image/"):
        return source
    if source.startswith("http://") or source.startswith("https://"):
        return image_url_to_data_url(source)

    file_path = Path(source)
    if file_path.exists() and file_path.is_file():
        return image_file_to_data_url(source)

    raise FileNotFoundError(f"无法识别参考图来源：{source}")


def build_params(blueprint: dict, prompt: str, reference_textures: list = None) -> dict:
    prompt_param = blueprint.get("prompt_param", "提示词")
    params = {prompt_param: prompt}

    image_keys = blueprint.get("image_input_keys") or []
    refs = reference_textures or []

    # 蓝图不支持图片输入但用户传了参考图 → 报错明确告知
    if refs and not blueprint.get("supports_image_input", False):
        raise SunshineFlowError(
            "blueprint_no_image_input",
            f"蓝图 '{blueprint.get('display_name', '?')}' 仅支持文生图，不接受参考图。"
            "请改用支持图生图的蓝图（如 pixel-art-fs），或去掉参考图。"
        )

    for index, image_source in enumerate(refs):
        if index >= len(image_keys):
            break
        params[image_keys[index]] = image_source_to_data_url(image_source)
    return params


def build_url(blueprint: dict, endpoint: str) -> str:
    """根据蓝图 base_path 拼装请求 URL。endpoint 形如 'api/v1/run' 或 'api/v1/status?uuid=xxx'。"""
    base_path = blueprint.get("base_path", "").strip("/")
    return f"{BASEURL_PREFIX}/{base_path}/{endpoint}"


def dm_flow_req(method: str, url: str, req_headers: dict, **kwargs):
    body = kwargs.get("json")
    for attempt in range(1, RATE_LIMIT_RETRIES + 1):
        status_code, response_text = urlopen_json(method, url, req_headers, body=body)
        if httpclient.OK <= status_code <= httpclient.NON_AUTHORITATIVE_INFORMATION:
            try:
                return json.loads(response_text)
            except ValueError as exc:
                raise SunshineFlowError("invalid_response", f"解析返回内容异常，原始内容：{response_text}") from exc

        error = classify_response_error(status_code, response_text)
        if error.error == "rate_limited" and attempt < RATE_LIMIT_RETRIES:
            time.sleep(attempt)
            continue
        raise error


def execute_sf(blueprint: dict, headers: dict, params: dict = None) -> dict:
    payload = {}
    if params:
        payload["params"] = params
    url = build_url(blueprint, "api/v1/run")
    return dm_flow_req("post", url, headers, json=payload)


def get_status(blueprint: dict, headers: dict, uuid: str) -> dict:
    url = build_url(blueprint, f"api/v1/status?uuid={uuid}")
    return dm_flow_req("get", url, headers)


def normalize_prompt(prompt: str) -> str:
    return (prompt or "").strip()


def extract_output_textures(blueprint: dict, status_info: dict) -> list:
    """
    按蓝图 output_keys 顺序，收集所有命中字段的图片 URL。
    - 每个字段可能是单个 URL 字符串或 URL 列表
    - 多个字段全部命中时，按 output_keys 列表顺序拼接（第一个字段的图排前面）
    - 自动去重，但保留原始顺序
    - 蓝图 output_keys 全部缺失时，回退到通用兜底字段
    """
    output = status_info.get("output") or {}
    image_output = status_info.get("image_output") or {}

    collected: list = []
    seen: set = set()

    def _add(val):
        if isinstance(val, list):
            for item in val:
                if isinstance(item, str) and item.startswith("http") and item not in seen:
                    collected.append(item)
                    seen.add(item)
        elif isinstance(val, str) and val.startswith("http") and val not in seen:
            collected.append(val)
            seen.add(val)

    # 1) 优先按蓝图配置的 output_keys 顺序收集
    for key in blueprint.get("output_keys", []) or []:
        _add(output.get(key))
        _add(image_output.get(key))

    if collected:
        return collected

    # 2) 兜底：尝试通用字段（兼容未来未知蓝图）
    for candidate in (
        output.get("output_textures"),
        output.get("output"),
        output.get("1K图片输出"),
        image_output.get("output_textures"),
        image_output.get("output"),
        image_output.get("1K图片输出"),
    ):
        _add(candidate)

    return collected


def run(prompt: str, reference_textures: list = None, env_path: str = None, blueprint_id: str = None, output_dir: str = None):
    normalized_prompt = normalize_prompt(prompt)
    if not normalized_prompt:
        emit_error("empty_prompt", "prompt 不能为空，已停止调用生图接口")

    # 加载蓝图注册表 + 选蓝图
    registry = load_blueprints()
    bp_id, blueprint = resolve_blueprint(registry, blueprint_id, normalized_prompt)
    print(
        f"[蓝图选择] {bp_id} ({blueprint.get('display_name', '?')}) "
        f"→ {build_url(blueprint, 'api/v1/run')}",
        file=sys.stderr,
    )

    env = load_env(env_path)
    auth_token, auth_user, app_code = get_required_config(env)
    api_key = blueprint.get("api_key", "")
    headers = build_headers(auth_token, auth_user, app_code, api_key)

    try:
        params = build_params(blueprint, normalized_prompt, reference_textures)
    except SunshineFlowError as exc:
        emit_error(exc.error, str(exc), **exc.details)
    except Exception as exc:
        emit_error("reference_image_error", f"参考图处理失败：{exc}")

    try:
        execute_info = execute_sf(blueprint, headers, params)
    except SunshineFlowError as exc:
        emit_error(exc.error, str(exc), **exc.details)

    uuid = execute_info.get("uuid")
    if not uuid:
        emit_error(
            "no_uuid",
            "API 未返回任务 UUID，请检查参数或认证信息",
            raw_response=execute_info,
        )

    elapsed = 0
    while elapsed < TIMEOUT:
        try:
            status_info = get_status(blueprint, headers, uuid)
        except SunshineFlowError as exc:
            emit_error(exc.error, str(exc), **exc.details)

        status = status_info.get("status")

        if status == "Finish":
            output_textures = extract_output_textures(blueprint, status_info)
            result = {
                "status": "success",
                "blueprint": bp_id,
                "blueprint_display_name": blueprint.get("display_name"),
                "output_textures": output_textures,
            }
            if output_dir and output_textures:
                local_paths = download_outputs(
                    output_textures,
                    output_dir,
                    bp_id,
                    blueprint,
                    normalized_prompt,
                )
                result["local_paths"] = local_paths
                result["output_dir"] = str(Path(output_dir).expanduser().resolve())
            result["raw_output"] = status_info
            print(json.dumps(result, ensure_ascii=False, indent=2))
            return

        if status == "Error":
            emit_error(
                "task_error",
                status_info.get("message", "未知错误"),
                raw=status_info,
            )

        progress = status_info.get("progressPercent", 0)
        print(f"[生图中] 状态: {status}，进度: {progress}%", file=sys.stderr)
        time.sleep(POLL_INTERVAL)
        elapsed += POLL_INTERVAL

    print(json.dumps({
        "status": "timeout",
        "error": "timeout",
        "message": f"生图超时（超过 {TIMEOUT} 秒），请稍后重试",
    }, ensure_ascii=False, indent=2))
    sys.exit(1)


def main():
    parser = argparse.ArgumentParser(description="SunshineFlow 生图脚本（多蓝图路由版）")
    parser.add_argument("--prompt", help="图片描述提示词（生图时必填）")
    parser.add_argument(
        "--reference_textures",
        nargs="*",
        default=None,
        help="参考图来源列表（可选，支持 URL、本地文件路径或 data URL）",
    )
    parser.add_argument("--env", default=None, help="指定 .env 文件路径（可选）")
    parser.add_argument(
        "--blueprint",
        default=None,
        help="显式指定蓝图 ID（可选）。不指定时按 prompt 关键词自动匹配，否则用 default。",
    )
    parser.add_argument(
        "--output-dir",
        default=None,
        help="可选。指定后会把生图结果自动下载到该目录，并按规范命名。"
             "命名格式: <blueprint>_<时间戳>_<prompt slug>_<idx>[_<label>].<ext>",
    )
    parser.add_argument(
        "--list-blueprints",
        action="store_true",
        help="列出所有已注册蓝图后退出",
    )

    args = parser.parse_args()

    if args.list_blueprints:
        list_blueprints(load_blueprints())
        return

    if not args.prompt:
        parser.error("--prompt 为必填参数（除非使用 --list-blueprints）")

    run(
        prompt=args.prompt,
        reference_textures=args.reference_textures,
        env_path=args.env,
        blueprint_id=args.blueprint,
        output_dir=args.output_dir,
    )


if __name__ == "__main__":
    main()