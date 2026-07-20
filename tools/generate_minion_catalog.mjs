import { createHash } from "node:crypto";
import { readFileSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const repositoryRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const sourcePath = join(
  repositoryRoot,
  "sc",
  "Assets",
  "Resources",
  "Configs",
  "Json",
  "minions.v0.1.json",
);
const mirrorPath = join(repositoryRoot, "minions.v0.1.json");
const releasePath = join(
  repositoryRoot,
  "sc",
  "Assets",
  "Resources",
  "Configs",
  "Json",
  "content-release.v0.1.json",
);

const sourceBytes = readFileSync(sourcePath);
const mirrorBytes = readFileSync(mirrorPath);
if (!sourceBytes.equals(mirrorBytes)) {
  throw new Error("Root minion mirror does not match the runtime config.");
}

const config = JSON.parse(sourceBytes.toString("utf8"));
const release = JSON.parse(readFileSync(releasePath, "utf8"));
const minions = config.minions ?? [];
const ids = minions.map((minion) => minion.id);
if (new Set(ids).size !== ids.length) {
  throw new Error("Minion ids must be unique.");
}
if (
  ids.length !== release.minionIds.length ||
  ids.some((id) => !release.minionIds.includes(id))
) {
  throw new Error("The minion config does not match the content release manifest.");
}

for (const minion of minions) {
  if (!minion.name || !minion.description) {
    throw new Error(`${minion.id} is missing its normal card text.`);
  }
  if (
    !minion.isToken &&
    (!minion.goldenDescription || minion.goldenAttack <= 0 || minion.goldenHealth <= 0)
  ) {
    throw new Error(`${minion.id} is missing its golden card data.`);
  }
}

const raceNames = {
  ForgeSoul: "铸魂",
  WildSpirit: "荒灵",
  Starbound: "星契",
  Wayfarer: "旅团",
};
const keywordNames = {
  Battlecry: "战吼",
  Cleave: "溅射",
  Deathrattle: "亡语",
  Shield: "护盾",
  Taunt: "嘲讽",
};
const raceOrder = ["ForgeSoul", "WildSpirit", "Starbound", "Wayfarer"];
const originalOrder = new Map(minions.map((minion, index) => [minion.id, index]));
const hash = createHash("sha256").update(sourceBytes).digest("hex").toUpperCase();
const outputName = `minion-catalog-v${release.contentVersion}.md`;
const outputPath = join(repositoryRoot, outputName);

function cell(value) {
  return String(value ?? "—")
    .replaceAll("|", "\\|")
    .replace(/\r?\n/g, "<br>");
}

function keywords(minion) {
  if (!minion.keywords?.length) return "—";
  return minion.keywords.map((keyword) => keywordNames[keyword] ?? keyword).join("、");
}

function countFor(race, token) {
  return minions.filter((minion) => minion.race === race && minion.isToken === token).length;
}

const lines = [
  `# 全随从身材与效果图鉴（内容版本 ${release.contentVersion}）`,
  "",
  `生成日期：2026-07-20`,
  `配置版本：${config.version}`,
  `规则最低版本：${release.minimumRulesVersion}`,
  `随从配置 SHA256：\`${hash}\``,
  "",
  "本文档由运行时随从配置自动生成。身材均为卡牌基础身材，不包含阵容夹具成长、永久加成、繁茂、商店法术或战斗内临时增益。关键词栏表示普通与金色形态共享的基础关键词；效果授予的临时关键词以效果文案为准。",
  "",
  "## 总览",
  "",
  `- 随从总数：${minions.length}`,
  `- 可收集随从：${minions.filter((minion) => !minion.isToken).length}`,
  `- Token：${minions.filter((minion) => minion.isToken).length}`,
  `- 当前启用：${minions.filter((minion) => minion.enabled).length}`,
  "",
  "| 种族 | 可收集随从 | Token | 合计 |",
  "| --- | ---: | ---: | ---: |",
];

for (const race of raceOrder) {
  const collectible = countFor(race, false);
  const tokens = countFor(race, true);
  lines.push(`| ${raceNames[race]} | ${collectible} | ${tokens} | ${collectible + tokens} |`);
}

for (const tier of [1, 2, 3, 4, 5, 0]) {
  const tierMinions = minions
    .filter((minion) => minion.tier === tier)
    .sort((left, right) => {
      const raceDifference = raceOrder.indexOf(left.race) - raceOrder.indexOf(right.race);
      return raceDifference || originalOrder.get(left.id) - originalOrder.get(right.id);
    });
  const heading = tier === 0 ? "Token（等级0）" : `${tier}级随从`;
  lines.push(
    "",
    `## ${heading}`,
    "",
    "| 种族 | 随从 | 配置 ID | 基础关键词 | 普通身材 | 普通效果 | 金色身材 | 金色效果 |",
    "| --- | --- | --- | --- | ---: | --- | ---: | --- |",
  );
  for (const minion of tierMinions) {
    const goldenStats = minion.isToken ? "—" : `${minion.goldenAttack}/${minion.goldenHealth}`;
    const goldenText = minion.isToken ? "无金色形态（Token）" : minion.goldenDescription;
    lines.push(
      `| ${raceNames[minion.race] ?? minion.race} | ${cell(minion.name)} | \`${minion.id}\` | ${cell(keywords(minion))} | ${minion.attack}/${minion.health} | ${cell(minion.description)} | ${goldenStats} | ${cell(goldenText)} |`,
    );
  }
}

lines.push(
  "",
  "## 维护方式",
  "",
  "更新随从配置后运行：",
  "",
  "```powershell",
  "node tools/generate_minion_catalog.mjs",
  "```",
  "",
  "生成器会先校验运行时配置、根目录镜像和内容发布清单的一致性，再覆盖当前内容版本对应的图鉴。",
  "",
);

writeFileSync(outputPath, lines.join("\n"), "utf8");
console.log(`Generated ${outputName}: ${minions.length} minions, SHA256 ${hash}`);
