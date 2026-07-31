# Diviner Workshop Update - 2026-07-30

## English

### Gameplay and balance

- Destiny loss at Destiny 0 now removes one Countdown of Destiny instead.
- Fixed Point is now a 1-cost Skill: Destiny cannot change this turn, retain your
  hand this turn, and Exhaust. Its upgrade costs 0.
- Applied the latest balance pass across Divulge, Cursed Prediction, Thread Cut,
  Doomscript, Narrow Escape, Star Needle, Inevitability, Read Ahead, Omen of
  Pestilence, Predestined Path, Ledger of Signs, Apocalypse, Unavoidable End,
  Perfect Forecast, Omen of Transcendence, Many Futures, and Ancient content.
- Relic Banishing now costs 3. Greater Portent now costs 4 and upgrades to 3.

### Fixes

- Haruspex now keeps its resolver alive until its queued Foretell triggers.
- Clairvoyance now selects the exact underlying card from hand, draw, discard, or
  Exhaust, fixing duplicated entries, missing entries, and broken clicks.
- Revelation threshold reductions are limited to cards with Revelation effects.
- Custom attacks consume Vigor after benefiting from it.
- Divinations no longer duplicate after saving and loading.
- Fixed Star Needle scaling and other damage-value versus hit-count mistakes.
- Fixed end-turn stalls and restored base rest-site behavior with a static Diviner
  portrait.
- Improved tooltip layering over the Destiny and relic-removal overlays.
- Corrected energy icons, upgrade descriptions, duplicate keywords, and English
  and Simplified Chinese localization mismatches.

## 简体中文

### 机制与平衡

- 命运为 0 时，若将失去命运，则改为减少 1 层命运倒计时。
- “命运定点”重做为 1 费技能：本回合命运无法改变，保留本回合的手牌，并消耗。
  升级后费用为 0。
- 应用最新卡牌平衡调整，涉及揭示、诅咒预言、断线、厄运书写、险中求生、星针、
  无可避免、预读、疫病征兆、既定路径、征兆账簿、天启、终局、完美预测、超脱征兆、
  万千未来与先古内容。
- “遗物驱离”费用提高至 3；“大预兆”费用提高至 4，升级后为 3。

### 错误修复

- 修复“脏卜术”进入预言队列后不结算、无法将脏卜术加入手牌的问题。
- “千里眼”现在会从手牌、抽牌堆、弃牌堆或消耗牌堆中选择正确的卡牌实例，
  修复重复条目、缺失条目以及无法点击的问题。
- 降低启示需求的效果现在仅作用于拥有启示效果的卡牌。
- 受活力加成的自定义攻击现在会正确消耗活力。
- 修复保存并重新载入后占卜记录重复的问题。
- 修复星针成长和其他将伤害数值误作攻击次数的问题。
- 修复无法结束回合的问题；休息处保留基础逻辑，仅显示静态占卜师立绘。
- 调整命运与遗物移除界面的层级，使游戏详情提示显示在模组界面上方。
- 修正能量图标、升级描述、重复关键词，以及英文和简体中文文本不一致。
