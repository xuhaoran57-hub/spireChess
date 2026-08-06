# Battle Showcase VFX candidate manifest v0.1

- Generated: 2026-08-06
- Tool: OpenAI ImageGen
- Runtime destination: `sc/Assets/Art/Presentation/UI/Battle/Vfx/`
- Source destination: `ui-concepts/phase-9c/battle-showcase-v0.1/source/`
- Transparency workflow: generate against `#00FF00`, then process with
  `remove_chroma_key.py --soft-matte --transparent-threshold 35 --opaque-threshold 90 --edge-feather 1 --despill`.
- Shared prompt guardrails: isolated 2D game VFX element; bright wandering
  storybook; hand-painted watercolor and ink; no character, card frame, UI,
  text, letter, logo, number, or recognizable third-party asset.

These are Showcase candidates, not production-approved sound or art assets.
All 14 final PNGs were manually sampled after alpha conversion; the generated
source images are retained outside `Assets` so Unity only imports final RGBA
sprites.

| ID | Runtime use | Source prompt focus | SHA-256 (final PNG) |
| --- | --- | --- | --- |
| `FX_ATK_TRAIL_LIGHT` | Normal attack trail | Thin gold ink slash with tapered tail | `1A2AB8D110F0C6F6C181735AA0376ADB3FC868307CC979CD35E2627F5BCD49A3` |
| `FX_ATK_TRAIL_HEAVY` | Cleave / immediate heavy trail | Dense warm-white and gold slash | `07D205318B6196D2D78ABC71927B1E55843A11418B60653CE90E9A129192F93E` |
| `FX_CLEAVE_ARC` | Cleave relationship branch | Single branching arc, no targets | `6444F5645814CB97B45663D8B2C16EE590D4997B281DC81CA2409CF5E1D4B629` |
| `FX_HIT_LIGHT` | Normal hit contact | Small radial gold paper-ink burst | `B30D7136892D441063A6455D2B451B3594EEDF813DD0378754E68D34796829BE` |
| `FX_HIT_HEAVY` | Strong / critical hit contact | Dense flower-shaped impact and fracture strokes | `1E6EFBF44DCA020776DE1569FA3F32835820C481C63B15FB4E718DF4BC5EAFE7` |
| `FX_WARCRY_SEAL` | OnPlay source seal | Warm gold and teal text-free seal | `E5E3AFA72318284EFF83D38EC7CF9E45CE085B3C4AF2B1DBF26516F69C4B65EF` |
| `FX_WARCRY_LINK` | OnPlay source-to-target link | Thin gold / teal pulsing ink tether | `4E8034759ABE7467B79B3A95AC2B69914EB3962930FB7D6310FCFE1456AEC7C2` |
| `FX_STAT_GROWTH` | Positive stat change | Rising gold ticks, leaves, and sparkles | `338EF187B6FC06C13CF035E4FD8702D335F4892BD659A3BF00D3DCA6571F7121` |
| `FX_DEATH_DISSOLVE` | Non-token death | Ash-paper fragments and inward collapse | `AC2CCDE1116038A7E808DF3C93B43A9611555FB7F4CB33CC5E24E04B2EEA843C` |
| `FX_TOKEN_POOF` | Token death | Compact pale paper dust and spirit wisps | `ABEE02BF0126B62C7B7AC89B3CD0DEA45C2A5E5FEAA6A02F6FDDB21A7354EDE7` |
| `FX_SUMMON_PORTAL` | Summon entrance base | Circular watercolor portal / seal ring | `0837F2932F4146D8FFE6BDF771641CBAE93A14F9CC61A52712D7873244DEF2A7` |
| `FX_SUMMON_BEAM` | Summon entrance vertical light | Narrow gold-blue light column | `6ADFC9309A7447267DD93A9B29934B762465ACD600A70783D0420D59183BF1A1` |
| `FX_SUMMON_DUST` | Summon entrance dust | Small upward paper-dust burst | `D62254DB2E7CADA1B697ECF9F0341E82BF18C20682492C4164BE5612FFC5122E` |
| `FX_EFFECT_BOLT` | Non-OnPlay effect relationship link | Short gold-teal magic bolt | `CEAAFC0B244800674E6BEE36DE5F01F85F5424AF66663AC97CAF8E51F5A88704` |

Source files use the same ID plus `_source.png`. Runtime wiring is performed
by `BattleUiPrefabBuilder.ConfigureShowcaseVfx`; all missing sprite references
fall back to the existing UGUI pool sprite until the prefab is rebuilt.
