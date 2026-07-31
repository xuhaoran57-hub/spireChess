# Phase 9C v0.3.3 Runtime 晋级签字包

- 候选：`PresentationSpriteCatalog_LightStorybookProductionV033Batch06`
- 候选状态：`UNITY_BATCH_RELEASE`
- 晋级状态：`Runtime Ready`
- 关闭时间：`2026-07-31T22:10:48+08:00`
- 验收摘要：
  `ui-concepts/phase-9c/light-storybook-production-v0.1/runtime-promotion-v0.3.3/acceptance-summary.md`
- 机器契约：`phase-9c-v0.3.3-runtime-promotion-contract.json`
- 技术门禁：`Spire Chess/Release/Validate Phase 9C v0.3.3 Runtime Promotion Gate`

## 1. 晋级范围

本次只允许将 v0.3.3 最终隔离候选中的 51 项量产美术晋级到正式 Runtime：

- 42 张非 Token 随从；
- 9 张法术；
- 与 v0.3.2 基线及既有 Runtime 资源合并后，正式 Catalog 应包含 86 个条目，
  并精确覆盖当前配置中的 83 个 ArtId。

本签字不批准正式音频、遗珍/事件/背景的后续全量生产，也不关闭 G3/G4。

## 2. 技术门禁

| ID | 条件 | 当前状态 |
| --- | --- | --- |
| RPG-01 | Batch 06 Catalog GUID 固定、86 条目、83 个配置 ArtId Exact | 自动校验 |
| RPG-02 | 51 项生产清单身份唯一，源文件和引用源 SHA-256 完整 | 自动校验 |
| RPG-03 | Unity 2022.3.62f3c1、373/373 EditMode、30/30 PlayMode、42 图证据完整 | 自动校验 |
| RPG-04 | 晋级前 Runtime Catalog 保持 24 条目，51 项量产 ArtId 全部隔离 | 自动校验 |
| RPG-05 | 正式目标路径、Windows DXT1、Max 1024、禁用 Mipmap/Readable 等策略已冻结 | 自动校验 |
| RPG-06 | 项目负责人完成权利、披露、视觉和 Runtime 晋级确认 | 已批准 |

技术门禁通过只证明候选具备晋级条件，不会修改 Runtime。RPG-06 未通过时，
命令行门禁必须返回失败。

## 3. 晋级后强制复验

晋级实现必须保留正式 Catalog GUID，并满足：

1. 51 项正式纹理只能引用
   `Assets/Art/Presentation/Runtime/LightStorybookV033/`，不得引用 Calibration；
2. Windows 使用 DXT1、Max 1024、Compression Quality 50、MipMap 关闭、
   Read/Write 关闭；
3. 83 个配置 ArtId 在正式 Runtime Catalog 中全部 Exact；
4. 全量 EditMode/PlayMode、Clean Windows Player 和 G4 正式链复跑；
5. 记录构建体积、纹理内存、首次 Shop 尖峰和稳定期内存；
6. 完成双分辨率逐图复核后，才能把资产状态改为 `Runtime Ready`。

如果压缩后出现不可接受的文字外主体失真、色带或裁切，不得临时放宽策略；应更新
本契约版本并重新签字。

## 4. 项目负责人确认

- 适用账号/协议：`Personal OpenAI services / Terms of Use`
- 签字人：`项目负责人（Codex 任务内确认）`
- 签字时间：`2026-07-31T10:53:54+08:00`
- [x] 对 Style Tile、项目配置和生成输入拥有必要权利或许可。
- [x] 接受 AI 输出可能不唯一，并同意按发行地和平台要求披露 AI 参与。
- [x] 已复核 v0.3.3 的 51 项量产资产和 Unity 批次放行证据。
- [x] 同意按第 3 节目标策略建立正式 Runtime 候选。

只有收到项目负责人的明确确认后，才可将机器契约中的 `approval` 更新为：

```json
{
  "status": "Approved",
  "approvedBy": "<负责人>",
  "approvedAt": "<ISO-8601 时间>",
  "accountAgreement": "Personal OpenAI services / Terms of Use",
  "inputRightsConfirmed": true,
  "aiDisclosureAccepted": true,
  "visualReviewAccepted": true,
  "runtimePromotionAccepted": true
}
```

可直接回复：

```text
我确认对 v0.3.3 使用的全部输入拥有必要权利或许可，接受并披露 AI 参与；
我已复核 51 项量产资产和 Unity 批次证据，同意按
phase-9c-v0.3.3-runtime-promotion-signoff.md 第 3 节策略建立正式 Runtime 候选。
```

项目负责人于 2026-07-31 在 Codex 任务中回复：

```text
确认签字包
```

该回复构成对本签字包第 4 节全部项目的明确确认；机器契约的 `approval` 已更新为
`Approved`，RPG-06 关闭。

## 5. Runtime 晋级关闭

项目负责人于 2026-07-31 在 Codex 任务中指示：

```text
关闭 v0.3.3 Runtime 晋级
```

Promotion Builder 已从干净工作树执行，生成物与晋级清单提交为
`8fc61a5472b2ca4eb14e02b88c427e1dd8b089fb`。复核结论：

- 晋级清单状态为 `PROMOTED`；正式 Catalog GUID 保持
  `75d638606a8084146524a35a317a2cca`；
- Runtime Catalog 为 86 条，当前 83 个配置 ArtId 全部 Exact；
- 51 / 51 量产资产已进入
  `Assets/Art/Presentation/Runtime/LightStorybookV033/`，Calibration 引用为 0；
- 66 张晋级纹理全部满足 Windows DXT1、Max 1024、Quality 50、Mipmap/Readable
  关闭的冻结策略；
- EditMode 383 / 383、PlayMode 30 / 30 通过；
- 干净 Windows Development Player、双分辨率 10 图、内存与 10 次 Stress
  证据通过，未发现本次晋级引入的视觉阻断项或单调内存增长。

首次 10 卡 Shop 仍有 0.49–0.55 秒的一次性激活尖峰，已作为本次 Runtime 晋级的
已知性能事实接受；不据此关闭 G4 第二机/跨机器门槛。完整构建身份、证据哈希和范围
边界见验收摘要。

据此，v0.3.3 的 42 张非 Token 随从与 9 张法术状态更新为
`Runtime Ready`，本 Runtime 晋级关闭。正式音频、背景生产许可及 G3/G4 总门禁
保持开放。
