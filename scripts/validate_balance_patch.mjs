import fs from "node:fs";
import path from "node:path";
import process from "node:process";

const root = process.cwd();
const read = (relativePath) => fs.readFileSync(path.join(root, relativePath), "utf8");
const parse = (relativePath) => JSON.parse(read(relativePath));
const failures = [];

function check(condition, message) {
  if (!condition) {
    failures.push(message);
  }
}

function includes(haystack, needle, message) {
  check(haystack.includes(needle), message);
}

function excludes(haystack, needle, message) {
  check(!haystack.includes(needle), message);
}

const enCards = parse("Diviner/localization/eng/cards.json");
const zhCards = parse("Diviner/localization/zhs/cards.json");
const enKeywords = parse("Diviner/localization/eng/card_keywords.json");
const zhKeywords = parse("Diviner/localization/zhs/card_keywords.json");
const enPowers = parse("Diviner/localization/eng/powers.json");
const zhPowers = parse("Diviner/localization/zhs/powers.json");

const enKeys = Object.keys(enCards).sort();
const zhKeys = Object.keys(zhCards).sort();
check(
  JSON.stringify(enKeys) === JSON.stringify(zhKeys),
  "English and Simplified Chinese card localization keys differ."
);

const requiredLocalization = [
  [enKeywords, "DIVINER-DESTINY.description", "lose 1 Countdown of Destiny instead"],
  [enKeywords, "DIVINER-DREDGE.description", "lose 1 Countdown of Destiny instead"],
  [zhKeywords, "DIVINER-DESTINY.description", "改为失去1层命运倒计时"],
  [zhKeywords, "DIVINER-DREDGE.description", "改为失去1层命运倒计时"],
  [enPowers, "DIVINER-DESTINY_POWER.description", "lose 1 Countdown of Destiny instead"],
  [zhPowers, "DIVINER-DESTINY_POWER.description", "改为失去 1 层命运倒计时"],
  [enCards, "DIVINER-DIVULGE.description", "draw !Cards! card. Lose 1 Destiny"],
  [zhCards, "DIVINER-DIVULGE.description", "占卜并抽 !Cards! 张牌。失去 1 点命运"],
  [enCards, "DIVINER-CURSED_PREDICTION.description", "Doomed:"],
  [zhCards, "DIVINER-CURSED_PREDICTION.description", "劫兆："],
  [enCards, "DIVINER-THREAD_CUT.upgradedDesc", "then deal 12 damage"],
  [zhCards, "DIVINER-THREAD_CUT.upgradedDesc", "再造成 12 点伤害"],
  [enCards, "DIVINER-DOOMSCRIPT.upgradedDesc", "Deal !Damage! damage"],
  [enCards, "DIVINER-NARROW_ESCAPE.upgradedDesc", "gain 2 Countdown of Destiny"],
  [zhCards, "DIVINER-NARROW_ESCAPE.upgradedDesc", "获得 2 层命运倒计时"],
  [enCards, "DIVINER-STAR_NEEDLE.description", "Damage increases by 1 for every recorded divination"],
  [enCards, "DIVINER-STAR_NEEDLE.upgradedDesc", "apply 2 Vulnerable"],
  [enCards, "DIVINER-INEVITABILITY.description", "every 7 recorded divinations"],
  [enCards, "DIVINER-INEVITABILITY.upgradedDesc", "every 5 recorded divinations"],
  [enCards, "DIVINER-READ_AHEAD.description", "Foretell: draw 1 card"],
  [enCards, "DIVINER-OMEN_OF_PESTILENCE.description", "Apply 1 Weak and 9 Poison"],
  [enCards, "DIVINER-OMEN_OF_PESTILENCE.upgradedDesc", "Apply 1 Weak and 13 Poison"],
  [enCards, "DIVINER-PREDESTINED_PATH.description", "Foretell:"],
  [enCards, "DIVINER-HEXAGRAM.upgradedDesc", "increased by 11"],
  [enCards, "DIVINER-WHITE_ROOM.upgradedDesc", "up to 6 cards"],
  [enCards, "DIVINER-LEDGER_OF_SIGNS.description", "gain 4 Block"],
  [enCards, "DIVINER-LEDGER_OF_SIGNS.upgradedDesc", "gain 6 Block"],
  [enCards, "DIVINER-APOCALYPSE.description", "After other cost modifiers"],
  [enCards, "DIVINER-APOCALYPSE.upgradedDesc", "random enemy 10 times"],
  [enCards, "DIVINER-UNAVOIDABLE_END.description", "damage actually dealt"],
  [zhCards, "DIVINER-UNAVOIDABLE_END.title", "终局"],
  [enCards, "DIVINER-PERFECT_FORECAST.description", "every 7 recorded divinations"],
  [enCards, "DIVINER-PERFECT_FORECAST.upgradedDesc", "every 5 recorded divinations"],
  [enCards, "DIVINER-THE_FINAL_STRAND.description", "Set Destiny to 0"],
  [enCards, "DIVINER-OMEN_OF_TRANSCENDENCE.description", "{Energy:energyIcons()}"],
  [zhCards, "DIVINER-OMEN_OF_TRANSCENDENCE.description", "{Energy:energyIcons()}"]
];

for (const [table, key, expected] of requiredLocalization) {
  includes(table[key] ?? "", expected, `${key} is missing "${expected}".`);
}

const staleLocalization = [
  [enCards, "DIVINER-DIVULGE.description", "If not Doomed"],
  [enCards, "DIVINER-CURSED_PREDICTION.description", "Bad Omen:"],
  [enCards, "DIVINER-THREAD_CUT.upgradedDesc", "14 damage"],
  [enCards, "DIVINER-INEVITABILITY.description", "every 4 recorded"],
  [enCards, "DIVINER-READ_AHEAD.description", "Put up to"],
  [enCards, "DIVINER-OMEN_OF_PESTILENCE.description", "3 Weak"],
  [enCards, "DIVINER-LEDGER_OF_SIGNS.description", "gain 5 Block"],
  [enCards, "DIVINER-THE_FINAL_STRAND.description", "Lose 5 Destiny"],
  [enCards, "DIVINER-PERFECT_FORECAST.description", "unique category"],
  [zhCards, "DIVINER-UNAVOIDABLE_END.title", "无可避免的结局"]
];

for (const [table, key, forbidden] of staleLocalization) {
  excludes(table[key] ?? "", forbidden, `${key} still contains stale text "${forbidden}".`);
}

const energyCardKeys = [
  "DIVINER-SMALL_RITUAL.description",
  "DIVINER-ESCAPE_FROM_DESTINY.description",
  "DIVINER-FORTUNE.description",
  "DIVINER-EVIL_EYE.description",
  "DIVINER-READ_THE_ASHES.description",
  "DIVINER-BORROWED_TOMORROW.description",
  "DIVINER-THE_WRITTEN_HOUR.description",
  "DIVINER-OMEN_OF_VIGOR.description",
  "DIVINER-OMEN_OF_TRANSCENDENCE.description",
  "DIVINER-SACRIFICE_OF_CERTAINTY.description",
  "DIVINER-PERFECT_FORECAST.description"
];

for (const key of energyCardKeys) {
  check(
    (enCards[key] ?? "").includes("energyIcons(") &&
      (zhCards[key] ?? "").includes("energyIcons("),
    `${key} does not use native energy-icon markup in both localizations.`
  );
}

for (const [key, value] of [...Object.entries(enCards), ...Object.entries(zhCards)]) {
  check(!/\[E(?:E+|\?)?\]/.test(value), `${key} contains obsolete literal energy markup.`);
}

const duplicatePatterns = [
  /\b(Innate|Retain|Exhaust|Ethereal|Unplayable)\.(?:.|\n)*\b\1\./i,
  /(固有|保留|消耗|虚无|无法打出)。(?:.|\n)*\1。/
];

for (const [key, value] of [...Object.entries(enCards), ...Object.entries(zhCards)]) {
  for (const pattern of duplicatePatterns) {
    check(!pattern.test(value), `${key} repeats an automatic card keyword.`);
  }
}

function findCsFiles(directory) {
  return fs.readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const entryPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      return findCsFiles(entryPath);
    }

    return entry.name.endsWith(".cs") ? [entryPath] : [];
  });
}

const autoKeywords = [
  ["Exhaust", "Exhaust.", "消耗。"],
  ["Retain", "Retain.", "保留。"],
  ["Innate", "Innate.", "固有。"],
  ["Ethereal", "Ethereal.", "虚无。"],
  ["Unplayable", "Unplayable.", "无法打出。"]
];

for (const file of findCsFiles(path.join(root, "DivinerCode/Cards"))) {
  const fileSource = fs.readFileSync(file, "utf8");
  const classPattern = /public class (\w+)\s*:\s*DivinerCard/g;
  let match;
  while ((match = classPattern.exec(fileSource)) !== null) {
    const braceStart = fileSource.indexOf("{", classPattern.lastIndex);
    let depth = 0;
    let classEnd = braceStart;
    for (; classEnd < fileSource.length; classEnd += 1) {
      if (fileSource[classEnd] === "{") {
        depth += 1;
      } else if (fileSource[classEnd] === "}" && --depth === 0) {
        classEnd += 1;
        break;
      }
    }

    const classSource = fileSource.slice(match.index, classEnd);
    for (const [keyword, englishText, chineseText] of autoKeywords) {
      if (!classSource.includes(`CardKeyword.${keyword}`)) {
        continue;
      }

      check(
        !classSource.includes(`"${englishText}`) &&
          !classSource.includes(`${englishText}"`) &&
          !classSource.includes(`"${chineseText}`) &&
          !classSource.includes(`${chineseText}"`),
        `${match[1]} includes plain ${keyword} text even though the keyword is automatic.`
      );
    }

    classPattern.lastIndex = classEnd;
  }
}

const source = [
  read("DivinerCode/Mechanics/DestinyService.cs"),
  read("DivinerCode/Mechanics/DivinerCombatRuntime.cs"),
  read("DivinerCode/Cards/Common/Divulge.cs"),
  read("DivinerCode/Cards/Common/NarrowEscape.cs"),
  read("DivinerCode/Cards/Common/ThreadCut.cs"),
  read("DivinerCode/Cards/DivinationOfWoes.cs"),
  read("DivinerCode/Cards/Rare/AdditionalRareCards.cs"),
  read("DivinerCode/Cards/Rare/RareCards.cs"),
  read("DivinerCode/Cards/Uncommon/UncommonCards.cs"),
  read("DivinerCode/Powers/CardPowers/DivinerCardPowers.cs")
].join("\n");

for (const [needle, message] of [
  ["TryLoseDredgeCountdown(player)", "Destiny loss at zero does not redirect to Countdown of Destiny."],
  ["WithCalculatedDamage(", "Star Needle does not expose calculated damage."],
  ["TryModifyEnergyCostInCombatLate", "Apocalypse does not apply its Destiny surcharge late."],
  ["TotalDamage + result.OverkillDamage", "Unavoidable End does not record actual damage including overkill."],
  ["ResolveForetellWithoutEcho", "Predestined Path does not bypass Echoed Omen."],
  ["creationOptions.Source != CardCreationSource.Encounter", "Many Futures is not limited to encounter rewards."],
  ["DivinationOfWoes : DivinerCard, ITranscendenceCard", "Omen of Woes is not registered for Archaic Tooth transformation."],
  ["ModelDb.Card<OmenOfPerishment>()", "Archaic Tooth transformation does not target Omen of Perishment."]
]) {
  includes(source, needle, message);
}

const runtimeFiles = {
  destiny: read("DivinerCode/Mechanics/DestinyService.cs"),
  divulge: read("DivinerCode/Cards/Common/Divulge.cs"),
  narrowEscape: read("DivinerCode/Cards/Common/NarrowEscape.cs"),
  threadCut: read("DivinerCode/Cards/Common/ThreadCut.cs"),
  omenOfWoes: read("DivinerCode/Cards/DivinationOfWoes.cs"),
  finalStrand: read("DivinerCode/Cards/Rare/AdditionalRareCards.cs"),
  rare: read("DivinerCode/Cards/Rare/RareCards.cs"),
  uncommon: read("DivinerCode/Cards/Uncommon/UncommonCards.cs"),
  powers: read("DivinerCode/Powers/CardPowers/DivinerCardPowers.cs")
};

for (const [file, needle, message] of [
  ["destiny", "delta < 0", "Destiny redirection is not limited to losses."],
  ["divulge", "WithCards(1, 1)", "Divulge does not draw 1/2 cards."],
  ["divulge", "DestinyService.AddDestiny(Owner, -1)", "Divulge does not lose Destiny."],
  ["narrowEscape", "IsUpgraded ? 2 : 1", "Narrow Escape+ does not gain 2 Countdown."],
  ["threadCut", "IsUpgraded ? 12m : 10m", "Thread Cut+ does not use a 12-damage second hit."],
  ["uncommon", "WithDamage(6, 3)", "Doomscript+ does not deal 9 damage."],
  ["uncommon", "new PendingPestilence(1, IsUpgraded ? 13 : 9)", "Omen of Pestilence values are stale."],
  ["uncommon", "int divisor = IsUpgraded ? 5 : 7", "Inevitability discount thresholds are stale."],
  ["uncommon", "PendingDrawsByPlayer[Owner]", "Read Ahead does not queue its Foretell draw."],
  ["uncommon", "ResolveForetellWithoutEcho", "Predestined Path still receives Echoed Omen repeats."],
  ["uncommon", "IsUpgraded ? 6 : 4", "Ledger of Signs does not grant 4/6 Block."],
  ["rare", ": base(2, CardType.Attack, CardRarity.Rare", "Apocalypse does not have base cost 2."],
  ["rare", "int hits = IsUpgraded ? 10 : 9", "Apocalypse hit counts are stale."],
  ["rare", "DivinationService.GetRecords(Owner).Count / (IsUpgraded ? 5 : 7)", "Perfect Forecast thresholds are stale."],
  ["rare", "new PendingTranscendence(IsUpgraded ? 4 : 3, IsUpgraded ? 3 : 2)", "Omen of Transcendence values are stale."],
  ["finalStrand", "SetDestiny(Owner, DestinyConstants.MinDestiny)", "The Final Strand still loses Destiny incrementally."],
  ["omenOfWoes", "ITranscendenceCard", "Omen of Woes is not registered for Archaic Tooth."],
  ["powers", "CardCreationSource.Encounter", "Many Futures is not guarded to encounter rewards."]
]) {
  includes(runtimeFiles[file], needle, message);
}

if (failures.length > 0) {
  console.error(`Balance validation failed with ${failures.length} issue(s):`);
  for (const failure of failures) {
    console.error(`- ${failure}`);
  }
  process.exit(1);
}

console.log(
  `Balance validation passed: ${enKeys.length} paired card strings, ` +
  `${requiredLocalization.length} required clauses, energy icons, keyword duplication, and runtime hooks.`
);
