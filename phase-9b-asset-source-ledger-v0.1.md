# 阶段 9B 资产来源与许可台账 v0.1

- 日期：2026-07-22
- 更新：2026-07-23（登记拱顶旅团立牌、普通银灰立牌框与旅团绘本公共卡框 Unity 工程样板）
- 状态：G0 台账已建立；G1 工程样板持续登记
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
| `card_minion_forge_soul_shield_squire` | `sc/Assets/Art/Presentation/Cards/Minions/ForgeSoul/card_minion_forge_soul_shield_squire.png` | `e86786502868e3dbdd0ddf89a446a09e2ee184b2681615bdf6cce1b8ef6c9bc9` | `ui-concepts/phase-9b/size-validation-v0.1/art/forge-soul-shield-squire-storybook-v0.2.png`，与运行时导出哈希一致 | 内置 GPT Image 工作流；人工选择 v0.2、Unity Artwork Mask/立牌肖像裁切验证 | 生产许可待项目方确认；`工程样板` | 已接入 Catalog 和立牌/卡牌共享插画；不得用于最终发布 |
| `battle_standee_frame` | `sc/Assets/Art/Presentation/UI/Battle/Standee/standee_frame.png` | `72b8b9b006e2f1f77ffdbe13493ace5cbb0f8963ac09ade3660a9f10d14196d3` | `ui-concepts/phase-9b/arched-standee-runtime-assets-v0.5/runtime/standee-frame.png` | GPT Image 2 参考生成；色键抠图、软边去溢色、裁边缩放 | 生产许可待项目方确认；`工程样板` | 已接入 `PF_BattleStandee`；G1 人工评审待完成 |
| `battle_standee_frame_silver_v1` | `sc/Assets/Art/Presentation/UI/Battle/Standee/standee_frame_silver_v1.png` | `e9dade9130ea09b882e82968fbb9408b35284336cca060a5b42639025091e92c` | `ui-concepts/phase-9b/arched-standee-runtime-assets-v0.5/runtime/standee-frame-silver-chroma-v0.1.png`，源图 SHA-256 `7f4c9a857c9163be530399d2ddf1a24aa16deae01dd470225017b8948c3a4321` | 内置 GPT Image 工作流；以暖金立牌框为编辑目标、Style Tile v0.3 为材质参考；改为旧银/白镴与石墨凹槽，洋红色键经软边、收边和去溢色转真实 Alpha | 生产许可待项目方确认；`工程样板` | 已作为普通立牌独立框接入 Catalog；金色仍使用原暖金框；双分辨率对照通过 |
| `battle_standee_attack_medallion` | `sc/Assets/Art/Presentation/UI/Battle/Standee/attack_medallion.png` | `2a7f07fa49bed9d88cad34f321768ede157808ff8c93a89009d30091e39eb046` | `ui-concepts/phase-9b/arched-standee-runtime-assets-v0.5/runtime/attack-medallion.png` | GPT Image 2 参考生成；色键抠图、裁边缩放；数字由运行时绘制 | 生产许可待项目方确认；`工程样板` | 已接入；不含烘焙数值 |
| `battle_standee_health_medallion` | `sc/Assets/Art/Presentation/UI/Battle/Standee/health_medallion.png` | `99593265b528476d370d3f1204f798ca1b8655e5bbebb28d7439b9c8940b4848` | `ui-concepts/phase-9b/arched-standee-runtime-assets-v0.5/runtime/health-medallion.png` | GPT Image 2 参考生成；色键抠图、裁边缩放；数字由运行时绘制 | 生产许可待项目方确认；`工程样板` | 已接入；不含烘焙数值 |
| `battle_standee_shield_overlay` | `sc/Assets/Art/Presentation/UI/Battle/Standee/shield_overlay_screen.png` | `68937d94da14eac46912fbd2b31cf3c4768fd08cc10784923b175dd9ac8f0f10` | `ui-concepts/phase-9b/arched-standee-runtime-assets-v0.5/runtime/shield-shell-screen-2x.png` | GPT Image 2 参考生成；黑底裁切缩放；Unity Additive Material 合成 | 生产许可待项目方确认；`工程样板` | 已接入护盾获得/失去状态层 |
| `battle_standee_taunt_base` | `sc/Assets/Art/Presentation/UI/Battle/Standee/taunt_base.png` | `4b213e1284b0c63c40542b121ff2c9cc7113e8575f0a7d8e9e5961ae50b53fe0` | `ui-concepts/phase-9b/arched-standee-runtime-assets-v0.5/runtime/taunt-base.png` | GPT Image 2 参考生成；色键抠图、裁边缩放 | 生产许可待项目方确认；`工程样板` | 已接入立牌底座静默态 |
| `battle_standee_deathrattle_seal` | `sc/Assets/Art/Presentation/UI/Battle/Standee/deathrattle_seal.png` | `5a32f63a44a3d93c5d5f5d112fac7bce4ad10ef0185f26899e7246560f29001f` | `ui-concepts/phase-9b/arched-standee-runtime-assets-v0.5/runtime/deathrattle-seal.png` | GPT Image 2 参考生成；色键抠图、裁边缩放 | 生产许可待项目方确认；`工程样板` | 已接入拱顶上方静默态 |
| `battle_standee_splash_mark` | `sc/Assets/Art/Presentation/UI/Battle/Standee/splash_mark.png` | `d8106f396997f0c8775c5121dc95d89b4c9faa772d4b2393a1ca757cab481328` | `ui-concepts/phase-9b/arched-standee-runtime-assets-v0.5/runtime/splash-mark.png` | GPT Image 2 参考生成；色键抠图、裁边缩放 | 生产许可待项目方确认；`工程样板` | 已接入攻击圆章上方静默态 |
| `card_frame_normal` | `sc/Assets/Art/Presentation/UI/Common/card_frame_normal.png` | `c4a57c7388cfa6440250420079a0d9060186ed83014e307d3e5c53abea4d7c5a` | `ui-concepts/phase-9b/card-frames/shared-card-frame-normal-alpha-master-v0.1.png`，与运行时导出哈希一致 | ChatGPT 参考稿；`extract_card_frame_alpha.py` 完成棋盘格分离、真实 Alpha 和边缘去白 | 生产许可待项目方确认；`工程样板` | 可用于 G1/G2 技术验证；许可确认前不得进入最终发布 |
| `card_frame_golden` | `sc/Assets/Art/Presentation/UI/Common/card_frame_golden.png` | `e3eb8e2b9fa33639be0f47e403cfc40d0a91cfa291021c48710bf5ac9f0aa365` | `ui-concepts/phase-9b/card-frames/shared-card-frame-golden-alpha-master-v0.1.png`，与运行时导出哈希一致 | ChatGPT 参考稿；`create_golden_card_frame.py` 完成古金材质迁移并复用普通框 Alpha | 生产许可待项目方确认；`工程样板` | 静态框可用于 G1/G2；流光材质和生产许可均未完成 |
| `card_frame_storybook_normal_v2` | `sc/Assets/Art/Presentation/UI/Common/card_frame_storybook_normal_v2.png` | `5d4ba907deaf361af45df84d727ab7d35b814987647a6ec3b48c10048a29fbb5` | `ui-concepts/phase-9b/card-frames/shared-card-frame-storybook-normal-chroma-v0.2.png`，源图 SHA-256 `d8f93e81b31695970dbdb0a0d3b9f2022aa5a5ae139f1b1b0fecec55492fb0ce` | 内置 GPT Image 工作流；以旧框为几何目标、Style Tile v0.3 为风格参考；洋红色键、软边、去溢色转真实 Alpha | 生产许可待项目方确认；`工程样板` | G1 运行时共同风格/可读性通过；已替代旧黑银框，不得用于最终发布 |
| `card_frame_storybook_golden_v2` | `sc/Assets/Art/Presentation/UI/Common/card_frame_storybook_golden_v2.png` | `f21e410d1682fb910f4c4ed4c7a57f8babe126ae6996b48517ee723878d67baa` | `ui-concepts/phase-9b/card-frames/shared-card-frame-storybook-golden-chroma-v0.2.png`，源图 SHA-256 `8d06ed5627277d113a7e8d2082982a2d68be5a8d566937445d58e1f49bbc8941` | 内置 GPT Image 工作流；从普通框受控派生；洋红色键、软边、去溢色转真实 Alpha | 生产许可待项目方确认；`工程样板` | G1 金色对照通过；局部暖金未覆盖流派色与攻防数字 |

## 4. 新资产登记门禁

新增资产必须填写：稳定 Asset ID、类型、作者/负责人、工具或模型、来源链接/工程、许可证或商用范围、生成/购买日期、人工修改、源文件版本、运行时导出版本、SHA-256、评审人和评审结论。

缺少任一关键字段时只能进入概念参考或工程样板目录，不得标记为 `Runtime Ready`。素材库资产必须保留购买凭证或许可文本；生成式资产若工具不提供种子或后端模型版本，必须明确记录“不可获得”，不得填写推测值。
