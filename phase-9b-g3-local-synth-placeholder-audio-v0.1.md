# 阶段 9B G3 本地程序合成占位音频 v0.1

- 日期：2026-07-26
- 状态：`Local Synth Placeholder`；可用于本地试听、运行时接线与交互调试
- 生产边界：不是正式 AI 生成成品，不是正式母带，不得标记为 `Runtime Ready`
- 正式制作真源：`phase-9b-g3-ai-audio-production-spec-v0.1.md`
- 生成器：`tools/generate_g3_placeholder_audio.py`
- 校验器：`tools/validate_g3_placeholder_audio.py`
- 机器清单：`sc/Assets/Audio/Presentation/Placeholder/placeholder_audio_manifest.json`

## 1. 本轮输出

本地生成器只使用振荡器、滤波噪声和数学包络，不读取第三方采样。固定代码、种子及
锁定运行环境可重复生成相同 PCM 内容；本轮位级复现环境为 CPython 3.12.13 /
NumPy 2.3.5，版本已写入 Manifest。跨 Python、NumPy 或平台版本不承诺逐位一致，
应以 Manifest 的逐文件 SHA-256 检测漂移。清单同时记录采样数、声道、响度指标和
循环点。

| 范围 | Cue | Clip | 输出 |
| --- | ---: | ---: | --- |
| BGM | 3 | 3 | 完整长度、立体声、可循环 WAV |
| UI | 4 | 9 | 以单声道为主的短反馈 |
| Shop | 9 | 20 | 刷新与发现展开使用立体声，其余以单声道为主 |
| Battle / Result | 10 | 30 | 攻击、受击、护盾、成长、死亡、召唤与结果反馈 |
| Run | 2 | 5 | 节点选择与奖励反馈 |
| 合计 | 28 | 67 | 3 BGM + 25 个 P0 音效 Cue / 64 个音效变体 |

三首 BGM：

| Cue | 调式 / BPM | 时长 | Loop start | Loop end（exclusive） |
| --- | --- | ---: | ---: | ---: |
| `bgm_main_menu` | D Dorian / 72 | 106.666667 秒 | 0 | 5,120,000 |
| `bgm_run_shop` | G Dorian / 90 | 128 秒 | 0 | 6,144,000 |
| `bgm_battle_normal` | D Aeolian / 120 | 96 秒 | 0 | 4,608,000 |

源 WAV 统一为 48 kHz / 24-bit PCM。样本峰值上限为 -3 dBFS；BGM 为 stereo，
普通 SFX 为 mono，`shop_refresh` 与 `shop_discover_open` 保留明确的左右展开。
三个 BGM 以 sample 精确排程、跨边界尾音与循环卷积混响生成，清单中的首尾
`seamDelta` 为 0；文件级重新解码校验允许最多 `0.00001` 的 24-bit 抖动误差。

Unity 导入设置：

- BGM：`Streaming`、Vorbis、quality 0.55、保留 48 kHz、后台加载、不预载；
- SFX：`Decompress On Load`、PCM、保留 48 kHz、预载；
- Catalog：每个 Cue 显式标记 `PresentationAudioCueAssetStatus.Placeholder`。

## 2. 可复现命令

生成：

```powershell
python -B tools/generate_g3_placeholder_audio.py
```

文件级校验：

```powershell
python -B tools/validate_g3_placeholder_audio.py
```

Unity 导入与 Catalog 挂接：

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe' `
  -batchmode -quit `
  -projectPath 'D:\code\spireChess\spireChess\sc' `
  -executeMethod SpireChess.Editor.G3PlaceholderAudioAssetBuilder.AttachFromCommandLine `
  -logFile 'D:\code\spireChess\spireChess\sc\Logs\G3-Placeholder-Attach.log'
```

生成器再次覆盖 WAV 时不会删除 Unity `.meta`，因此既有 GUID 保持稳定；随后重新
执行挂接命令即可刷新导入设置与引用。挂接器不会覆盖已标记为
`ProductionApproved` 的 Cue，只补齐 `Pending/Placeholder` Cue。

## 3. 自动化与门禁结果

2026-07-26 在 Unity 2022.3.62f3c1 验证：

| 门禁 | 结果 |
| --- | --- |
| 文件矩阵 | 67 / 67；3 BGM、25 SFX Cue、64 SFX 变体 |
| WAV 规格 | 全部 48 kHz / 24-bit；声道、采样数、峰值、RMS、循环接缝通过 |
| 完整性 | 清单路径与磁盘集合精确相等；67 个 SHA-256 全部匹配 |
| Commissioning | 通过；28 个 Cue 均可播放，并各报告 1 条占位 warning |
| ProductionStrict | 按设计失败；Unity 退出码 1，精确报告 28 个未获生产批准的 Cue |
| Catalog 重建保留 | `BuildFromCommandLine` 通过；28 个 Cue 的状态、Clip 数量和逐变体引用保持不变 |
| EditMode | 346 / 346，通过，0 失败、0 跳过 |
| PlayMode | 25 / 25，通过，0 失败、0 跳过 |

严格门禁不再把“存在 Clip”等同于“生产就绪”。它要求冻结的 28 个 Cue / 67 个
变体精确齐全、无 null、引用不重复且状态为 `ProductionApproved`；正式 BGM 必须
精确位于 `Music/<cue>_v01.ogg`，正式 SFX 必须精确位于
`SFX/<Domain>/sfx_<cue>_<NN>.wav`。门禁还验证 48 kHz、语义声道数、Importer
加载/压缩策略，以及 SFX 源 WAV 的 24-bit PCM 格式。`Pending`、`Placeholder`、
数量错误、引用空洞、重复引用、错路径或错格式都会失败。`G3AudioAssetBuilder`
重建时按 Cue ID 保留状态和逐变体引用，并在写回后自检，不会因重建而自动批准或
静默抹掉资源。

本地 WAV 共 100,097,828 bytes（约 100.10 MB），最大单文件为
`placeholder_bgm_run_shop_v01.wav`，36,864,044 bytes。仓库当前没有
`.gitattributes`，这些文件不受 Git LFS 管理；提交前必须明确接受普通 Git 仓库的
体积增长，或另行决定改为受控生成/配置 LFS。本轮未自动提交。

## 4. 人工试听清单

程序和自动化不能代替主观听审。正式 AI 生成开始前，可用这套占位音确认节奏、事件密度
和语义边界；建议按以下顺序试听：

1. 三首 BGM 是否能立即区分 MainMenu、Run/Shop 与 Battle，长循环是否疲劳；
2. `ui_click / ui_confirm / ui_cancel / ui_error` 是否分别呈现单击、上行、下行、
   低频错误轮廓；
3. `battle_attack_light` 是否只有运动前摇，`battle_hit` 是否只有身体撞击；
4. `battle_shield_gain / battle_shield_break / battle_hit` 是否不会互相混淆；
5. `battle_death` 是否比 `battle_token_death` 更低、更重、更长；
6. `shop_play` 是否立即落位，`battle_summon` 是否先有吸入式魔法前摇；
7. 连续刷新、快速受击、嵌套召唤/亡语和四路音量调整时是否刺耳或遮蔽。

人工试听结论只用于调整本地占位或反哺 AI 自制音频生产规范，不会改变其
`Placeholder` 状态。

## 5. 正式 AI 音频替换

1. 按 AI 自制音频生产规范整理原始生成文件、母带、Runtime 文件、loop sample 点、
   生成记录、来源/许可和 SHA-256。
2. 正式文件进入非 `Placeholder` 的 `Music/` 与对应 `SFX/` 目录。
3. 按既有 Cue ID 替换全部变体，并显式将完成验收的 Cue 设置为
   `ProductionApproved`；不得根据目录或 Clip 非空自动批准。
4. 执行文件规格、`ProductionStrict`、Unity 全量测试、无缝循环、快速切场、
   嵌套亡语峰值和项目负责人人工听审。
5. 更新资产盘点与来源台账后，才可删除对应占位引用并申请 `Runtime Ready`。
