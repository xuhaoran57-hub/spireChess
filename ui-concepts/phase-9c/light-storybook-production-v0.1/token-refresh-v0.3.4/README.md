# Token Refresh v0.3.4

- 日期：2026-07-31
- 范围：3 张荒灵 Token 卡图
- 状态：`PROMOTED`
- Runtime：3/3 已原位替换

## 修正内容

v0.3.3 Runtime 晋级时，3 个 Token 的旧图已经正确接入
`PresentationSpriteCatalog.asset`，但它们没有按冻结的新美术风格重新生成。
“Catalog 精确解析”只证明接线正确，不能证明视觉风格已更新。

本目录补齐这 3 张 Token 的新风格候选。由于它会改变已经登记的 Runtime
基线，版本提升为 `v0.3.4 Token Refresh`，不回写或静默修改已经关闭的
v0.3.3 生产批次。

## 已晋级图片

| Token | 规格 | 候选 |
| --- | --- | --- |
| 幼灵 | 1/1；战斗结束后消失 | [token-young-spirit-v0.3.4.png](masters/token-young-spirit-v0.3.4.png) |
| 迅捷幼灵 | 2/1；召唤后立即攻击一次 | [token-swift-young-spirit-v0.3.4.png](masters/token-swift-young-spirit-v0.3.4.png) |
| 双尾狐影 | 2/2；亡语依次召唤两个幼灵 | [token-two-tailed-fox-shadow-v0.3.4-r3.png](masters/token-two-tailed-fox-shadow-v0.3.4-r3.png) |

## 生成边界

- 工具：Codex 内置 ImageGen。
- 模式：5 次独立的新图生成；没有定点编辑。首轮 3 张后，双尾狐影 r1 因尾部
  构图被否决，r2 因五腿错误被否决，当前候选为独立重生的 r3。
- 唯一图像参考：
  `../../../phase-9b/style-tiles/style-tile-d-wandering-storybook-v0.1.png`。
- 参考图只控制水彩 / 水粉媒介、象牙纤维纸、彩色墨线、明亮曝光与配色；
  不控制角色、物件或构图。
- 旧 Token 图仅用于发现差异和登记旧哈希，没有作为生成参考传入。
- 完整生成 Prompt 见
  [PROMPTS-v0.3.4.zh-CN.md](PROMPTS-v0.3.4.zh-CN.md)。

## 晋级验收

离线门禁检查：

- 3/3 文件身份与 SHA-256 一致；
- 3/3 画幅为约 5:4；
- 3/3 满足冻结亮度阈值；
- 3/3 候选与旧 Runtime 图不同；
- Runtime PNG 与候选 SHA-256 一致，`.meta` GUID 和正式 Catalog GUID 保持不变。

人工预审：

- 幼灵：单一幼鹿灵体、浅奶油色、苔绿叶纹、胸口琥珀生命光；
- 迅捷幼灵：与幼灵同物种，空中冲刺姿态和叶片速度轨迹清楚；
- 双尾狐影 r3：单只狐灵、侧向全身站姿；前腿 2、后腿 2，共 4 条腿和
  4 只爪；两个尾根从后腿上方骨盆处可见，一条低扫、一条上扬，恰好两个
  琥珀生命光点；
- 三张均为明亮日景，无夜景、月亮、暗角、文字、UI 或卡框。

双尾狐影 r1
`masters/token-two-tailed-fox-shadow-v0.3.4.png` 因两条尾巴像从肩背后方长出、
围住头部且受力关系不自然，于 2026-07-31 被用户否决。该文件仅保留审计，
不再是当前候选。

双尾狐影 r2
`masters/token-two-tailed-fox-shadow-v0.3.4-r2.png` 虽修正尾根，但腹部下方
生成了第三条后腿，合计五条腿，于 2026-07-31 被用户否决。该文件仅保留审计，
不得晋级。

3 张候选已随 `legacy-card-art-refresh-v0.3.4` 获得视觉确认并于
2026-08-01 晋级。Runtime 只使用双尾狐影 r3；r1 和 r2 继续作为否决审计，
不得重新接入。

## 验证

```powershell
powershell -ExecutionPolicy Bypass -File ui-concepts/phase-9c/light-storybook-production-v0.1/token-refresh-v0.3.4/validate.ps1
```

通过结果为 `PASS_RUNTIME_PROMOTED`，报告写入
`VALIDATION-REPORT-v0.3.4.json`。

## 已完成的晋级动作

1. 保留现有 `.meta` GUID，将 3 张候选覆盖到对应 Runtime PNG。
2. 将 3 张纹理导入策略统一为生产策略：无 mipmap、不可读、
   Default 1024 压缩、Standalone 1024 DXT1。
3. 保持 3 个 `artId` 和 Catalog GUID 不变。
4. 将 3 个横版图片的 Catalog 焦点统一为 0.5。
5. 纳入全部 83 个配置 ArtId 的批准来源、哈希与导入策略门禁。
