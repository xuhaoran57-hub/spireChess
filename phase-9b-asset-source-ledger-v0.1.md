# 阶段 9B 资产来源与许可台账 v0.1

- 日期：2026-07-22
- 状态：G0 台账已建立
- 适用范围：Phase 9B 运行时美术、字体、音频、VFX 及其源文件
- 治理基线：`phase-9b-g0-baseline-v0.1.md`

## 1. 状态规则

| 状态 | 含义 |
| --- | --- |
| `工程样板` | 允许内部接线、截图和风格评审；不得作为最终生产发布资产 |
| `生产许可已确认` | 来源、作者/工具、商用范围和必要凭证已由项目方确认 |
| `Runtime Ready` | 生产许可已确认，并完成 Unity 接线、导入、自动化和视觉评审 |

没有具体模型版本、来源凭证或商用范围的生成式资产一律停留在 `工程样板`。项目不得根据文件已进入 `Assets` 推断其具有生产发布许可。

## 2. 存储策略

- 运行时 PNG、OGG、WAV、Unity Material、Prefab 和可复现导出脚本进入普通 Git。
- PSD、KRA、DAW 工程、高采样率母带及其他大体积分层源文件进入项目方管理的外部资产库。
- 当前 Git LFS 3.7.1 已安装但仓库未配置；G0 不提交 `.gitattributes`，未来启用 LFS 必须独立评审历史迁移、容量和协作者流程。
- 外部源文件必须在本表记录稳定资产 ID、负责人、版本、备份位置和运行时导出哈希；不在仓库中写入本机绝对路径或访问凭证。

## 3. 当前资产

| Asset ID | 运行时路径 | SHA-256 | 来源/源文件 | 工具与人工修改 | 许可/状态 | 评审结论 |
| --- | --- | --- | --- | --- | --- | --- |
| `font_noto_sans_cjk_sc_regular` | `sc/Assets/Art/Fonts/NotoSansCJKsc-Regular.otf` | `2c76254f6fc379fddfce0a7e84fb5385bb135d3e399294f6eeb6680d0365b74b` | 项目已有字体；许可证 `sc/Assets/Art/Fonts/OFL.txt`，SHA-256 `6a73f9541c2de74158c0e7cf6b0a58ef774f5a780bf191f2d7ec9cc53efe2bf2` | Unity 字体导入 | SIL Open Font License 1.1；`Runtime Ready` | 继续作为 9B 中文正式字体 |
| `card_minion_undying_furnace_king` | `sc/Assets/Art/Presentation/Cards/Minions/ForgeSoul/card_minion_undying_furnace_king.png` | `a6215ce0936a25c8900a822360f142e1bf8d2235a5723601cbdf9c4905acc5d3` | `ui-concepts/ChatGPT Image 2026年7月22日 16_23_30.png`，源图 SHA-256 `c780f16e2220fe2adac180ecf7d016fd4b4f28b6a265dafc347ccaf604431441` | ChatGPT 图像生成（后端具体模型版本未暴露）；Pillow 缩放、裁切和卡面合成验证 | 生产许可待项目方确认；`工程样板` | 可进入 G1 内部风格比较；不得标 Runtime Ready 或用于最终发布 |
| `card_frame_normal` | `sc/Assets/Art/Presentation/UI/Common/card_frame_normal.png` | `c4a57c7388cfa6440250420079a0d9060186ed83014e307d3e5c53abea4d7c5a` | `ui-concepts/phase-9b/card-frames/shared-card-frame-normal-alpha-master-v0.1.png`，与运行时导出哈希一致 | ChatGPT 参考稿；`extract_card_frame_alpha.py` 完成棋盘格分离、真实 Alpha 和边缘去白 | 生产许可待项目方确认；`工程样板` | 可用于 G1/G2 技术验证；许可确认前不得进入最终发布 |
| `card_frame_golden` | `sc/Assets/Art/Presentation/UI/Common/card_frame_golden.png` | `e3eb8e2b9fa33639be0f47e403cfc40d0a91cfa291021c48710bf5ac9f0aa365` | `ui-concepts/phase-9b/card-frames/shared-card-frame-golden-alpha-master-v0.1.png`，与运行时导出哈希一致 | ChatGPT 参考稿；`create_golden_card_frame.py` 完成古金材质迁移并复用普通框 Alpha | 生产许可待项目方确认；`工程样板` | 静态框可用于 G1/G2；流光材质和生产许可均未完成 |

## 4. 新资产登记门禁

新增资产必须填写：稳定 Asset ID、类型、作者/负责人、工具或模型、来源链接/工程、许可证或商用范围、生成/购买日期、人工修改、源文件版本、运行时导出版本、SHA-256、评审人和评审结论。

缺少任一关键字段时只能进入概念参考或工程样板目录，不得标记为 `Runtime Ready`。素材库资产必须保留购买凭证或许可文本；生成式资产若工具不提供种子或后端模型版本，必须明确记录“不可获得”，不得填写推测值。
