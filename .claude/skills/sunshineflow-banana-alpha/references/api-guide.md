# SunshineFlow API 使用指南

## 服务信息

| 项目 | 值 |
|------|-----|
| 服务域名 (BASEURL) | `http://sunshineflow-api-server-prod.tmax.nie.netease.com/pixel-art-fs` |
| 生图接口 | `POST /api/v1/run` |
| 状态查询 | `GET /api/v1/status?uuid={uuid}` |
| ETS 信息 | `POST /api/v1/ets_info` |

## 输入参数

| 参数名 | 类型 | 是否必填 | 说明 |
|--------|------|----------|------|
| `prompt` | `string` | 是 | 图片描述提示词 |
| `reference_textures` | `list[string]` | 否 | 参考图来源列表，支持 URL、本地文件路径或 data URL |

## 图片输入作为参数

当蓝图节点的输入类型是“图片”时，API 侧不能直接传图片 URL 字符串，而应传 base64 data URL。

示例：

```json
{
  "params": {
    "prompt": "Convert this image into pixel art style",
    "image01": "data:image/png;base64,iVBORw0KGgoAAA..."
  }
}
```

如果上层拿到的是图片 URL，正确流程是：
1. 先下载图片内容
2. 转成 `data:image/<type>;base64,...`
3. 再写入 `image01` / `image02` 等图片类型变量

如果上层拿到的是本地文件路径，正确流程是：
1. 直接读取本地图片文件内容
2. 转成 `data:image/<type>;base64,...`
3. 再写入 `image01` / `image02` 等图片类型变量

## 注意事项

1. 聊天附件图片通常更适合作为本地文件路径传给脚本，而不是强制转换成 URL。
2. 脚本仅使用 Python 标准库 HTTP 能力，不依赖 `requests` 或 `pip install`。