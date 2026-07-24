# 阶段 9B 资产来源与许可台账 v0.1

- 日期：2026-07-22
- 更新：2026-07-24（补齐 8 张锚点、6 张新增核心样板、4 个轻量数值组件的当前版本与哈希）
- 状态：G1 已通过；18 项成本样本获项目方接受，28 项活动生产候选已确认个人版 OpenAI 服务生产许可
- 适用范围：Phase 9B 运行时美术、字体、音频、VFX 及其源文件
- 治理基线：`phase-9b-g0-baseline-v0.1.md`
- G1 签字包：`phase-9b-g1-production-license-signoff-v0.1.md`

## 1. 状态规则

| 状态 | 含义 |
| --- | --- |
| `工程样板` | 允许内部接线、截图和风格评审；不得作为最终生产发布资产 |
| `生产许可已确认` | 来源、作者/工具、商用范围和必要凭证已由项目方确认，并完成 G1 签字包第 4–5 节 |
| `Runtime Ready` | 生产许可已确认，并完成 Unity 接线、导入、自动化和视觉评审 |

未记录具体模型版本（或工具未提供版本时未明确写明“不可获得”）、来源凭证或商用
范围的生成式资产一律停留在 `工程样板`。项目不得根据文件已进入 `Assets` 推断其
具有生产发布许可。

## 2. 存储策略

- 运行时 PNG、OGG、WAV、Unity Material、Prefab 和可复现导出脚本进入普通 Git。
- PSD、KRA、DAW 工程、高采样率母带及其他大体积分层源文件进入项目方管理的外部资产库。
- 当前 Git LFS 3.7.1 已安装但仓库未配置；G0 不提交 `.gitattributes`，未来启用 LFS 必须独立评审历史迁移、容量和协作者流程。
- 外部源文件必须在本表记录稳定资产 ID、负责人、版本、备份位置和运行时导出哈希；不在仓库中写入本机绝对路径或访问凭证。

## 3. 当前资产

| Asset ID | 运行时路径 | SHA-256 | 来源/源文件 | 工具与人工修改 | 许可/状态 | 评审结论 |
| --- | --- | --- | --- | --- | --- | --- |
| `font_noto_sans_cjk_sc_regular` | `sc/Assets/Art/Fonts/NotoSansCJKsc-Regular.otf` | `2c76254f6fc379fddfce0a7e84fb5385bb135d3e399294f6eeb6680d0365b74b` | 项目已有字体；许可证 `sc/Assets/Art/Fonts/OFL.txt`，SHA-256 `6a73f9541c2de74158c0e7cf6b0a58ef774f5a780bf191f2d7ec9cc53efe2bf2` | Unity 字体导入 | SIL Open Font License 1.1；`Runtime Ready` | 继续作为 9B 中文正式字体 |
| `card_minion_forge_soul_shield_squire` | `sc/Assets/Art/Presentation/Cards/Minions/ForgeSoul/card_minion_forge_soul_shield_squire.png` | `6e85be99b0dac591e9c81d8adb5397839b14e7af8f4b366d9774c6d5e9c68664` | `ui-concepts/phase-9b/archetype-anchor-illustrations-v0.2/masters/forge-soul-shield-squire.png`，与 Runtime 哈希一致 | 内置 GPT Image 工作流；后端模型版本/种子不可获得；按“铸魂非人”规则重做；完整提示词与历史版本已留档 | 个人版 OpenAI Terms of Use；`生产许可已确认` | G1 八图盲测通过；G2 候选已复制到 Unity，当前未运行 Unity 验证 |
| `card_minion_undying_furnace_king` | `sc/Assets/Art/Presentation/Cards/Minions/ForgeSoul/card_minion_undying_furnace_king.png` | `912e6ab1763993df087d7436b3784533df3b665028461fa9e27c6a90a9ebdf41` | `ui-concepts/phase-9b/archetype-anchor-illustrations-v0.2/masters/forge-undying-furnace-king.png`，与 Runtime 哈希一致 | 内置 GPT Image 工作流；后端模型版本/种子不可获得；非人纠正、端坐王者与威严定向编辑版本均已留档 | 个人版 OpenAI Terms of Use；`生产许可已确认` | G1 八图盲测通过；G2 候选已复制到 Unity，当前未运行 Unity 验证 |
| `card_minion_young_deer_spirit` | `sc/Assets/Art/Presentation/Cards/Minions/WildSpirit/card_minion_young_deer_spirit.png` | `34ced80f6a3428a0ddc41f8b4ae80ead5d5a039f8b14d4a0d738b9329a80f85b` | `ui-concepts/phase-9b/archetype-anchor-illustrations-v0.2/masters/wild-young-deer-spirit.png`，与 Runtime 哈希一致 | 内置 GPT Image 工作流；后端模型版本/种子不可获得；提示词与 160×240 评审留档 | 个人版 OpenAI Terms of Use；`生产许可已确认` | G1 八图盲测通过；G2 候选已复制到 Unity，当前未运行 Unity 验证 |
| `card_minion_ten_thousand_hoof_surge` | `sc/Assets/Art/Presentation/Cards/Minions/WildSpirit/card_minion_ten_thousand_hoof_surge.png` | `32835879a5a470159d25e4240d3e2714defce89bca688e1ffc85e1735e0355ee` | `ui-concepts/phase-9b/archetype-anchor-illustrations-v0.2/masters/wild-ten-thousand-hoof-surge.png`，与 Runtime 哈希一致 | 内置 GPT Image 工作流；后端模型版本/种子不可获得；提示词与 160×240 评审留档 | 个人版 OpenAI Terms of Use；`生产许可已确认` | G1 八图盲测通过；G2 候选已复制到 Unity，当前未运行 Unity 验证 |
| `card_minion_astrolabe_calibrator` | `sc/Assets/Art/Presentation/Cards/Minions/Starbound/card_minion_astrolabe_calibrator.png` | `05eef768d11f2f509e4d0a48d732ad46ced8dd4939b8f91bc62c8e39899d87e6` | `ui-concepts/phase-9b/archetype-anchor-illustrations-v0.2/masters/star-astrolabe-calibrator.png`，与 Runtime 哈希一致 | 内置 GPT Image 工作流；后端模型版本/种子不可获得；提示词与 160×240 评审留档 | 个人版 OpenAI Terms of Use；`生产许可已确认` | G1 八图盲测通过；G2 候选已复制到 Unity，当前未运行 Unity 验证 |
| `card_minion_sky_covenant_bearer` | `sc/Assets/Art/Presentation/Cards/Minions/Starbound/card_minion_sky_covenant_bearer.png` | `88755c4da82cbdc45ca4b5c4b259a28e892fa7cd022d5443c79dddfd413a502c` | `ui-concepts/phase-9b/archetype-anchor-illustrations-v0.2/masters/star-sky-covenant-bearer.png`，与 Runtime 哈希一致 | 内置 GPT Image 工作流；后端模型版本/种子不可获得；降低星饰密度的受控编辑与提示词已留档 | 个人版 OpenAI Terms of Use；`生产许可已确认` | G1 八图盲测通过；G2 候选已复制到 Unity，当前未运行 Unity 验证 |
| `card_minion_traveling_physician` | `sc/Assets/Art/Presentation/Cards/Minions/Wayfarer/card_minion_traveling_physician.png` | `f6fab09efe0c9e76c426c12ff1f6f46545201a222278cf4efef441df9d3a1f36` | `ui-concepts/phase-9b/archetype-anchor-illustrations-v0.2/masters/wayfarer-traveling-physician.png`，与 Runtime 哈希一致 | 内置 GPT Image 工作流；后端模型版本/种子不可获得；提示词与 160×240 评审留档 | 个人版 OpenAI Terms of Use；`生产许可已确认` | G1 附加盲测锚点；不计入 12 张 G2 核心样板；Unity 候选未验证 |
| `card_minion_many_arts_apprentice` | `sc/Assets/Art/Presentation/Cards/Minions/Wayfarer/card_minion_many_arts_apprentice.png` | `aeacbf49f5c25b95512fde0b4693677dd2e26822d09a217df2c09cec6519883e` | `ui-concepts/phase-9b/archetype-anchor-illustrations-v0.2/masters/wayfarer-many-arts-apprentice.png`，与 Runtime 哈希一致 | 内置 GPT Image 工作流；后端模型版本/种子不可获得；钝化训练装备的受控编辑与提示词已留档 | 个人版 OpenAI Terms of Use；`生产许可已确认` | G1 附加盲测锚点；不计入 12 张 G2 核心样板；Unity 候选未验证 |
| `card_minion_tempering_mender` | 未接入；G2 预定 `sc/Assets/Art/Presentation/Cards/Minions/ForgeSoul/card_minion_tempering_mender.png` | `0f394d2a8f131b59b40fcc68a5d7a19e6a071758f4a46fe2ccce15d50d9505a4` | `ui-concepts/phase-9b/sample-minion-illustrations-v0.1/masters/forge-tempering-mender.png` | 内置 GPT Image 工作流；后端模型版本/种子不可获得；共享参考与完整提示词留档；无额外生成调用 | 个人版 OpenAI Terms of Use；`生产许可已确认` | G1 离线样板 160×240 检查通过；G2 Runtime/卡面/Unity 验收未开始 |
| `card_minion_cracked_armor_avenger` | 未接入；G2 预定 `sc/Assets/Art/Presentation/Cards/Minions/ForgeSoul/card_minion_cracked_armor_avenger.png` | `7908ae6793ed9e34f7bc2045df4a9c40b170dc05e00a79b5a39c037ff733f1af` | `ui-concepts/phase-9b/sample-minion-illustrations-v0.1/masters/forge-cracked-armor-avenger.png` | 内置 GPT Image 工作流；后端模型版本/种子不可获得；共享参考与完整提示词留档；无额外生成调用 | 个人版 OpenAI Terms of Use；`生产许可已确认` | G1 离线样板 160×240 检查通过；G2 Runtime/卡面/Unity 验收未开始 |
| `card_minion_rotleaf_heir` | 未接入；G2 预定 `sc/Assets/Art/Presentation/Cards/Minions/WildSpirit/card_minion_rotleaf_heir.png` | `fba3a071d839c1e09a7f1619b7687a9e49206c3ba6f20e2a7750f8163aa64403` | `ui-concepts/phase-9b/sample-minion-illustrations-v0.1/masters/wild-rotleaf-heir.png` | 内置 GPT Image 工作流；后端模型版本/种子不可获得；共享参考与完整提示词留档；无额外生成调用 | 个人版 OpenAI Terms of Use；`生产许可已确认` | G1 离线样板 160×240 检查通过；G2 Runtime/卡面/Unity 验收未开始 |
| `card_minion_fox_den_matriarch` | 未接入；G2 预定 `sc/Assets/Art/Presentation/Cards/Minions/WildSpirit/card_minion_fox_den_matriarch.png` | `a914d77279a6b50c3463d111a5cb87bd1671ac638cd3daec9268ec6e37825af9` | `ui-concepts/phase-9b/sample-minion-illustrations-v0.1/masters/wild-fox-den-matriarch.png` | 内置 GPT Image 工作流；后端模型版本/种子不可获得；共享参考与完整提示词留档；无额外生成调用 | 个人版 OpenAI Terms of Use；`生产许可已确认` | G1 离线样板 160×240 检查通过；G2 Runtime/卡面/Unity 验收未开始 |
| `card_minion_secret_page_refractor` | 未接入；G2 预定 `sc/Assets/Art/Presentation/Cards/Minions/Starbound/card_minion_secret_page_refractor.png` | `75eb2709a7900a1f23c8e6fe336593bba92306b64c30a0b6f3b8c3c6e79b5789` | `ui-concepts/phase-9b/sample-minion-illustrations-v0.1/masters/star-secret-page-refractor.png` | 内置 GPT Image 工作流；后端模型版本/种子不可获得；共享参考与完整提示词留档；无额外生成调用 | 个人版 OpenAI Terms of Use；`生产许可已确认` | G1 离线样板 160×240 检查通过；G2 Runtime/卡面/Unity 验收未开始 |
| `card_minion_star_map_broker` | 未接入；G2 预定 `sc/Assets/Art/Presentation/Cards/Minions/Starbound/card_minion_star_map_broker.png` | `f0e395a5483061e99e06dff8fa1d3455b60e6be45c2315cca6f89c22e2ad80f8` | `ui-concepts/phase-9b/sample-minion-illustrations-v0.1/masters/star-star-map-broker.png` | 内置 GPT Image 工作流；后端模型版本/种子不可获得；共享参考与完整提示词留档；无额外生成调用 | 个人版 OpenAI Terms of Use；`生产许可已确认` | G1 离线样板 160×240 检查通过；G2 Runtime/卡面/Unity 验收未开始 |
| `card_ui_cost_coin` | `sc/Assets/Art/Presentation/UI/Card/card_cost_coin_v1.png` | 源 `ea28d04c1e03a75a2980eba5761f1e1ba3d9a2e0c57ee6fc5f60fe03e46ed518`；Runtime `7d0b80c8108b212e1122d70af9e66e66b5580c638ad1420fb74a2ac9869dbd2b` | `ui-concepts/phase-9b/card-components-number-tags-v0.2/components/cost-coin.png`；绿底源在相邻 `source-chroma/` | 内置 GPT Image 工作流；`remove_chroma_key.py` 去绿；`prepare_card_runtime_assets.py` 确定性裁切/缩放 | 个人版 OpenAI Terms of Use；`生产许可已确认` | Full/Compact/四位数离线评审通过；G2 Unity 候选未运行验证 |
| `card_ui_tier_bookmark` | `sc/Assets/Art/Presentation/UI/Card/card_tier_bookmark_v1.png` | 源 `a08940f4515390a3b610b6d80d726d8a32e37e78d982181df747791947fd4120`；Runtime `bc9ecef13c9a7229485701302dcb2758f55958ea5b5cf116574e2f45e6beb6c4` | `ui-concepts/phase-9b/card-components-number-tags-v0.2/components/tier-bookmark.png`；绿底源在相邻 `source-chroma/` | 内置 GPT Image 工作流；`remove_chroma_key.py` 去绿；`prepare_card_runtime_assets.py` 确定性裁切/缩放 | 个人版 OpenAI Terms of Use；`生产许可已确认` | Full/Compact/四位数离线评审通过；G2 Unity 候选未运行验证 |
| `card_ui_attack_tag` | `sc/Assets/Art/Presentation/UI/Card/card_attack_tag_v1.png` | 源 `655c7f7239dad21286a816a919470fec72b487edaa97a9b80aa3630c4b3e4588`；Runtime `1775a2c0b0fa1b13e89790f2cb18fe6abb2163b0b467a8758318ad83aaa960e9` | `ui-concepts/phase-9b/card-components-number-tags-v0.2/components/attack-tag.png`；绿底源在相邻 `source-chroma/` | 内置 GPT Image 工作流；`remove_chroma_key.py` 去绿；`prepare_card_runtime_assets.py` 生成横向 9-slice Runtime | 个人版 OpenAI Terms of Use；`生产许可已确认` | Full/Compact/四位数离线评审通过；G2 Unity 候选未运行验证 |
| `card_ui_health_tag` | `sc/Assets/Art/Presentation/UI/Card/card_health_tag_v1.png` | 源 `48a597e3d5087ded5e5efa992eb396e9fd09a60e511720160cfd11f55bd28e53`；Runtime `5b8fdfc7a83e1bfa1bb08a923f7427c264b44d2ef8b59b78f4e77873936942f3` | `ui-concepts/phase-9b/card-components-number-tags-v0.2/components/health-tag.png`；绿底源在相邻 `source-chroma/` | 内置 GPT Image 工作流；`remove_chroma_key.py` 去绿；`prepare_card_runtime_assets.py` 生成横向 9-slice Runtime | 个人版 OpenAI Terms of Use；`生产许可已确认` | Full/Compact/四位数离线评审通过；G2 Unity 候选未运行验证 |
| `battle_standee_frame` | `sc/Assets/Art/Presentation/UI/Battle/Standee/standee_frame.png` | `72b8b9b006e2f1f77ffdbe13493ace5cbb0f8963ac09ade3660a9f10d14196d3` | `ui-concepts/phase-9b/arched-standee-runtime-assets-v0.5/runtime/standee-frame.png` | GPT Image 2 参考生成；色键抠图、软边去溢色、裁边缩放 | 个人版 OpenAI Terms of Use；`生产许可已确认` | 已接入 `PF_BattleStandee`；G1 运行时共同风格与双分辨率评审通过 |
| `battle_standee_frame_silver_v1` | `sc/Assets/Art/Presentation/UI/Battle/Standee/standee_frame_silver_v1.png` | `e9dade9130ea09b882e82968fbb9408b35284336cca060a5b42639025091e92c` | `ui-concepts/phase-9b/arched-standee-runtime-assets-v0.5/runtime/standee-frame-silver-chroma-v0.1.png`，源图 SHA-256 `7f4c9a857c9163be530399d2ddf1a24aa16deae01dd470225017b8948c3a4321` | 内置 GPT Image 工作流；以暖金立牌框为编辑目标、Style Tile v0.3 为材质参考；改为旧银/白镴与石墨凹槽，洋红色键经软边、收边和去溢色转真实 Alpha | 个人版 OpenAI Terms of Use；`生产许可已确认` | 已作为普通立牌独立框接入 Catalog；金色仍使用原暖金框；双分辨率对照通过 |
| `battle_standee_attack_medallion` | `sc/Assets/Art/Presentation/UI/Battle/Standee/attack_medallion.png` | `2a7f07fa49bed9d88cad34f321768ede157808ff8c93a89009d30091e39eb046` | `ui-concepts/phase-9b/arched-standee-runtime-assets-v0.5/runtime/attack-medallion.png` | GPT Image 2 参考生成；色键抠图、裁边缩放；数字由运行时绘制 | 个人版 OpenAI Terms of Use；`生产许可已确认` | 已接入；不含烘焙数值 |
| `battle_standee_health_medallion` | `sc/Assets/Art/Presentation/UI/Battle/Standee/health_medallion.png` | `99593265b528476d370d3f1204f798ca1b8655e5bbebb28d7439b9c8940b4848` | `ui-concepts/phase-9b/arched-standee-runtime-assets-v0.5/runtime/health-medallion.png` | GPT Image 2 参考生成；色键抠图、裁边缩放；数字由运行时绘制 | 个人版 OpenAI Terms of Use；`生产许可已确认` | 已接入；不含烘焙数值 |
| `battle_standee_shield_overlay` | `sc/Assets/Art/Presentation/UI/Battle/Standee/shield_overlay_screen.png` | `68937d94da14eac46912fbd2b31cf3c4768fd08cc10784923b175dd9ac8f0f10` | `ui-concepts/phase-9b/arched-standee-runtime-assets-v0.5/runtime/shield-shell-screen-2x.png` | GPT Image 2 参考生成；黑底裁切缩放；Unity Additive Material 合成 | 个人版 OpenAI Terms of Use；`生产许可已确认` | 已接入护盾获得/失去状态层 |
| `battle_standee_taunt_base` | `sc/Assets/Art/Presentation/UI/Battle/Standee/taunt_base.png` | `4b213e1284b0c63c40542b121ff2c9cc7113e8575f0a7d8e9e5961ae50b53fe0` | `ui-concepts/phase-9b/arched-standee-runtime-assets-v0.5/runtime/taunt-base.png` | GPT Image 2 参考生成；色键抠图、裁边缩放 | 个人版 OpenAI Terms of Use；`生产许可已确认` | 已接入立牌底座静默态 |
| `battle_standee_deathrattle_seal` | `sc/Assets/Art/Presentation/UI/Battle/Standee/deathrattle_seal.png` | `5a32f63a44a3d93c5d5f5d112fac7bce4ad10ef0185f26899e7246560f29001f` | `ui-concepts/phase-9b/arched-standee-runtime-assets-v0.5/runtime/deathrattle-seal.png` | GPT Image 2 参考生成；色键抠图、裁边缩放 | 个人版 OpenAI Terms of Use；`生产许可已确认` | 已接入拱顶上方静默态 |
| `battle_standee_splash_mark` | `sc/Assets/Art/Presentation/UI/Battle/Standee/splash_mark.png` | `d8106f396997f0c8775c5121dc95d89b4c9faa772d4b2393a1ca757cab481328` | `ui-concepts/phase-9b/arched-standee-runtime-assets-v0.5/runtime/splash-mark.png` | GPT Image 2 参考生成；色键抠图、裁边缩放 | 个人版 OpenAI Terms of Use；`生产许可已确认` | 已接入攻击圆章上方静默态 |
| `card_frame_normal` | `sc/Assets/Art/Presentation/UI/Common/card_frame_normal.png` | `c4a57c7388cfa6440250420079a0d9060186ed83014e307d3e5c53abea4d7c5a` | `ui-concepts/phase-9b/card-frames/shared-card-frame-normal-alpha-master-v0.1.png`，与运行时导出哈希一致 | ChatGPT 参考稿；`extract_card_frame_alpha.py` 完成棋盘格分离、真实 Alpha 和边缘去白 | 不申请本轮生产许可；历史 `工程样板` | 已被 `card_frame_storybook_normal_v2` 替代；只保留作技术与版本证据 |
| `card_frame_golden` | `sc/Assets/Art/Presentation/UI/Common/card_frame_golden.png` | `e3eb8e2b9fa33639be0f47e403cfc40d0a91cfa291021c48710bf5ac9f0aa365` | `ui-concepts/phase-9b/card-frames/shared-card-frame-golden-alpha-master-v0.1.png`，与运行时导出哈希一致 | ChatGPT 参考稿；`create_golden_card_frame.py` 完成古金材质迁移并复用普通框 Alpha | 不申请本轮生产许可；历史 `工程样板` | 已被 `card_frame_storybook_golden_v2` 替代；只保留作技术与版本证据 |
| `card_frame_storybook_normal_v2` | `sc/Assets/Art/Presentation/UI/Common/card_frame_storybook_normal_v2.png` | `5d4ba907deaf361af45df84d727ab7d35b814987647a6ec3b48c10048a29fbb5` | `ui-concepts/phase-9b/card-frames/shared-card-frame-storybook-normal-chroma-v0.2.png`，源图 SHA-256 `d8f93e81b31695970dbdb0a0d3b9f2022aa5a5ae139f1b1b0fecec55492fb0ce` | 内置 GPT Image 工作流；以旧框为几何目标、Style Tile v0.3 为风格参考；洋红色键、软边、去溢色转真实 Alpha | 个人版 OpenAI Terms of Use；`生产许可已确认` | G1 运行时共同风格/可读性通过；已替代旧黑银框；G2 验收前不标 Runtime Ready |
| `card_frame_storybook_golden_v2` | `sc/Assets/Art/Presentation/UI/Common/card_frame_storybook_golden_v2.png` | `f21e410d1682fb910f4c4ed4c7a57f8babe126ae6996b48517ee723878d67baa` | `ui-concepts/phase-9b/card-frames/shared-card-frame-storybook-golden-chroma-v0.2.png`，源图 SHA-256 `8d06ed5627277d113a7e8d2082982a2d68be5a8d566937445d58e1f49bbc8941` | 内置 GPT Image 工作流；从普通框受控派生；洋红色键、软边、去溢色转真实 Alpha | 个人版 OpenAI Terms of Use；`生产许可已确认` | G1 金色对照通过；局部暖金未覆盖流派色与攻防数字 |

### 3.1 本次 G1 生产许可签字范围

本次签字只覆盖上表中的 28 项活动生产候选：14 张当前立绘、4 个轻量数值组件、
8 个在用战斗立牌部件和 2 个绘本 v2 卡框。`card_frame_normal`、
`card_frame_golden` 及其他被否决或被替代的历史输出不在签字范围内。权威清单、
成本样本边界和确认文本见 `phase-9b-g1-production-license-signoff-v0.1.md` 第 2、4–6 节。

项目负责人已于 2026-07-24 确认适用个人版 OpenAI 服务、全部输入参考权利、输出
可能不唯一及第 3 节建议成本门槛；上述 28 项状态已统一更新为
`生产许可已确认`。该状态不代表 G2 Unity 导入、自动化或双分辨率运行验收完成。

## 4. 新资产登记门禁

新增资产必须填写：稳定 Asset ID、类型、作者/负责人、工具或模型、来源链接/工程、许可证或商用范围、生成/购买日期、人工修改、源文件版本、运行时导出版本、SHA-256、评审人和评审结论。

缺少任一关键字段时只能进入概念参考或工程样板目录，不得标记为 `Runtime Ready`。素材库资产必须保留购买凭证或许可文本；生成式资产若工具不提供种子或后端模型版本，必须明确记录“不可获得”，不得填写推测值。
