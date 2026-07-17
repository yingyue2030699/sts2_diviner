# Diviner Card Pool

Current implementation reference for the Diviner card pool. Deprecated design drafts live in
`docs/legacy/`; this file tracks the live card classes and English localization.

## Core Vocabulary

| Term | Current meaning |
|---|---|
| Destiny | Persistent run value from 0 to 5. Starts at 3. |
| Bad Omen | Destiny 0, 1, or 2. Destiny 0 is also Doomed. |
| Good Omen | Destiny 3, 4, or 5. Destiny 5 is also Revelation. |
| Doomed | Destiny exactly 0. The first time each combat your Destiny reaches 0, gain 3 Countdown of Destiny and shuffle 3 Escape from Destiny cards into the draw pile. |
| Revelation | Destiny exactly 5. At combat start, search for 3 cards and put them into your hand, then lose 1 Destiny. When a card extra effect with Revelation is triggered, lose 1 Destiny. |
| Divinate | Record a future forecast in Crystal Ball. Several cards care about whether the run or combat has recorded divinations. |
| Foretell | Queue the listed effect to resolve at the start of your next turn. Multiple Foretell effects queue separately. |
| Fated | A Fated card costs 0 this turn. Its base cost is unchanged. |
| Fortune | Generated 0-cost Retain, Exhaust skill: Gain 1 energy. Draw 1 card. Upgrade to Draw 2 cards.|
| Misfortune | Generated 3-cost Exhaust attack: deal 25 damage to all enemies. If left in hand at end of turn, lose 5 HP and trigger its damage. Upgrade to lose 3 HP instead. |
| Countdown of Destiny | Doomed countdown. Lose 1 at end of turn; when it reaches 0, you are defeated. |
| Scry X | Look at the top X cards of your draw pile. Discard any number of them. |

## Starting Deck

| Card | Count |
|---|---:|
| Strike | 4 |
| Defend | 4 |
| Temper Fate | 1 |
| Omen of Woes | 1 |

## Basic Cards

| Name | Type | Cost | Effect | Upgrade |
|---|---|---:|---|---|
| Strike | Attack | 1 | Deal 6 damage. | 9 damage. |
| Defend | Skill | 1 | Gain 5 Block. | 8 Block. |
| Temper Fate | Skill | 0 | Good Omen: lose 1 Destiny, Draw 1 card, and Divinate. Bad Omen: add a `Bend Future` to your hand. Exhaust. | Good Omen: Draw 2 cards. Bad Omen: adds a `Bend Future+` instead |
| Bend Future | Skill | 1 | Retain. Exhaust. Gain 1 Destiny. If you have an active relic divination, you may choose 1 foretold relic and remove it from the relic sequence. | Cost 0. |
| Omen of Woes | Skill | 1 | Foretell: deal 10 damage and apply 1 Weak and 1 Vulnerable to all enemies. | 13 damage. |

## Common Cards

| Name | Type | Cost | Effect | Upgrade |
|---|---|---:|---|---|
| Palm Strike | Attack | 1 | Deal 10 damage. Bad Omen: damage increased by 6. Good Omen: draw 1 cards. | 13 damage. |
| Foretold Strike | Attack | 1 | Deal 8 damage. Foretell: deal 10 damage to the same enemy. | 10 damage, then Foretell 12 damage. |
| Misread Strike | Attack | 0 | Deal 25 damage. Lose 1 Destiny. | 33 damage. |
| Doomscript | Attack | 1 | Deal 6 damage. Add a Misfortune to your draw pile. | 8 damage. Add a Misfortune+ to your draw pile. |
| Crossed Lines | Attack | 0 | Deal 3 damage. If you have Divinated this combat, apply 3 Weak. | Deal 4 damage, apply 4 Weak. |
| Thread Cut | Attack | 1 | Deal 6 damage. If the target has Weak, then deal 10 damage. | 8 damage, then 14 damage. |
| Eclipse Jab | Attack | 1 | Deal 10 damage. Bad Omen: Then deal 8 damage to all enemies. | 13 damage, then 11 damage to all enemies. |
| Forewarned Blow | Attack | 2 | Deal 15 damage. Bad Omen: add a Misfortune to your draw pile. Good Omen: add a Fortune to your draw pile. | 18 damage. Bad Omen: add a Misfortune+ to your draw pile. Good Omen: add a Fortune+ to your draw pile. |
| Retributive Rite | Attack | 1 | Deal 9 damage. Bad Omen: Gain 9 Block. | Deal 11 damage. Bad Omen: Gain 11 Block. |
| Destined Lance | Attack | 2 | Deal 16 damage. Good Omen: hit all enemies. Bad Omen: damage increased by 10. | 20 damage. Bad Omen: damage increased by 12. |
| Read the Room | Skill | 0 | Scry 6. | Scry 9. |
| Insurance | Skill | 1 | Gain 6 Block. You no longer lose HP from Misfortune or Misfortune+ at the end of this turn. | Gain 9 Block. |
| Bad Feeling | Skill | 0 | Apply 2 Weak. Bad Omen: Apply 2 Vulnerable. | Apply 2 Weak to all enemies. Bad Omen: Apply 2 Vulnerable to all enemies. |
| Cursed Prediction | Attack | 1 | Deal 11 damage. Bad Omen: add a Misfortune to your hand. | Deal 11 damage. Bad Omen: add a Misfortune+ to your hand. |
| Dead Star | Attack | 2 | Deal 18 damage. Lose 3 HP. Bad Omen: deal 18 damage again. | 22 damage; lose 3 HP; Bad Omen deal 22 damage again. |
| Evil Eye | Skill | 1 | Apply 3 Weak. Bad Omen: gain 1 Energy. | Apply 5 Weak. Bad Omen: gain 1 Energy. |
| Precious Offering | Skill | 0 | Exhaust a card in your hand. If a rare card is exhausted, Divinate. | If an uncommon or rare card is exhausted, Divinate. |
| Omen of Shelter | Skill | 1 | Foretell: gain 14 Block. | 18 Block. |
| False Alarm | Skill | 0 | Gain 16 Block. Lose 1 Destiny. | 20 Block. |
| Divulge | Skill | 0 | If not Doomed, Divinate and draw 2 cards. Lose 1 Destiny. | If not Doomed, Divinate and draw 3 cards. Lose 1 Destiny. |
| Sacrifice of Certainty | Skill | 0 | Gain 3 energy. Lose 1 Destiny. | Gain 4 energy. |
| Omens Align | Skill | 0 | Exhaust. Put all Fortune and Misfortune cards from anywhere into your hand. | No longer Exhausts. |
| Skeptic's Charm | Skill | 1 | Gain 8 Block. Bad Omen: Gain 8 Block. | Gain 10 Block; Bad Omen gains 10 Block. |
| Reconsult | Skill | 0 | Exhaust. If you have divinated this combat, Divinate. | Retain. |
| Narrow Escape | Skill | 1 | Gain 5 Block. Doomed: gain 1 Countdown of Destiny. | Doomed: gain 2 Countdown of Destiny. |
| Funeral Clock | Skill | 2 | Gain 13 Block. Doomed: costs 0 and draw 2 cards. | 16 Block. |
| Omen of Vigor | Skill | 1 | Foretell: Gain 3 energy. | Cost 0. |
| Doom Engine | Power | 2 | Misfortunes' HP loss is reduced by 2 and they deal 9 more damage. Doomed: this card costs 0. | Misfortunes deal 13 more damage. |
| Doom Spiral | Power | 0 | Innate. At start of turn, lose 1 destiny and add a Misfortune to your hand. | Lose 1 destiny and add a Misfortune+ to your hand. |

## Uncommon Cards

| Name | Type | Cost | Effect | Upgrade |
|---|---|---:|---|---|
| Line of Fate | Attack | 1 | Exhaust. Deal 11 damage. If Fatal, Divinate. | Deal 14 damage. |
| Wax Seal | Attack | 1 | Deal 9 damage. Put a card from your draw or discard pile on top of your draw pile. | 12 damage. |
| Star Needle | Attack | 0 | Deal 3 damage. Damage increased by 2 and apply 1 Vulnerable per 7 recorded divinations. |  Damage increased by 3 and apply 2 Vulnerable per 7 recorded divinations. |
| Horoscope | Attack | 0 | Deal 4 damage. If the top card of your draw pile is an Attack, play it. | 6 damage. |
| Red Thread | Attack | 1 | Deal 10 damage. Next Foretell card you play this turn triggers right away. | 13 damage. |
| Hexagram | Attack | 2 | Deal 3 damage to a random enemy 6 times. Revelation: Damage increased by 9. | 4 damage 6 times. Revelation damage increased by 11. |
| Inevitability | Attack | 4 | Deal 52 damage. Costs 1 less for every 4 recorded divinations. | Costs 1 less for every 3 recorded divinations. |
| Augury | Skill | 3 | Gain 12 Block. If you have divinated at least twice this combat, gain 1 Destiny. Exhaust.  | Cost reduced to 2. |
| Lucky Break | Skill | 0 | Gain 5 Block. Good Omen: Draw 1 card. | Gain 7 Block. |
| Ward Sign | Skill | 1 | Gain 9 Block. Good Omen: Retain a card in your hand this turn. | 12 Block. |
| Palm Reading | Skill | 1 | Gain 7 Block. Scry 5. Good Omen: draw 2 cards. | Gain 10 Block. Scry 6. Good Omen: draw 2 cards. |
| Smoke and Mirrors | Skill | 1 | Gain 9 Block. The next Foretell effect you play this combat resolves with 7 more damage or Block. | Gain 11 Block. Foretell bonus becomes 9 more. |
| Omen of Insight | Skill | 1 | Exhaust. Foretell: Divinate. | Costs 0. |
| Omen of Pestilence | Skill | 1 | Foretell: Apply 3 Weak and 9 Poison to all enemies. | Apply 4 Weak and 12 Poison. |
| Second Sight | Skill | 1 | Retain. Draw until you have 5 cards in hand. | Draw until you have 7 cards. |
| Wheel of Fortune | Skill | 1 | Add a Fortune and a Misfortune to your hand. | Fortune+ and Misfortune+. |
| Cold Reading | Skill | 1 | Apply 1 Weak. Draw 2 card. | Apply 2 Weak and draw 2 cards. |
| Read Ahead | Skill | 0 | Draw 2 cards. Put up to 2 cards from your hand on top of your draw pile. | Draw 3; put up to 3 cards back. |
| Unasked Question | Skill | 0 | Lose 5 HP. Divinate. Exhaust.  | Lose 3 HP. |
| Rewrite the Sign | Skill | 0 | Exhaust. Transform all Misfortunes in your hand, draw pile, and discard pile into Fortunes. | Transform into `Fortune+` instead of `Fortune`. |
| Predestined Path | Skill | 1 | Choose up to 1 card from your draw pile. Foretell: put them into your hand; they are Fated that turn. | Choose 2 cards. |
| Read the Ashes | Skill | 1 | Exhaust a card. Foretell: draw 2 cards. If a Status or Curse was Exhausted, Foretell: gain 2 Energy. | Costs 0. |
| Borrowed Tomorrow | Skill | 0 | Exhaust. Gain 3 Energy. Foretell: lose 1 Energy. | Gain 4 Energy. |
| White Room | Skill | 1 | Draw 3 cards. Revelation: choose up to 4 cards in your hand; they are Fated. | Draw 4 cards. Choose up to 6 cards. |
| Thread Pull | Skill | 1 | Draw 2 cards. Foretell: draw 3 cards. | Draw 3 cards. |
| Epiphany | Skill | X | Gain 4 Block X times. Gain 1 Destiny if X is 4 or more. Exhaust. | Gain 6 Block X times. Gain 1 Destiny if X is 4 or more. Exhaust. |
| Prophetic Trance | Power | 1 | Innate. When you Divinate, draw 2 cards. | Costs 0. |
| The Written Hour | Power | 1 | At the start of your turn, if Destiny is exactly 3, gain 1 Energy and draw 1 card. | Costs 0. |
| Haruspex | Power | 1 | The next time you Exhaust a card, Divinate and Foretell: add a copy of this card to your hand. | Costs 0. |
| Chosen Line | Power | 1 | Revelation: draw 1 extra card per turn, and the first card you draw each turn is Fated. | Costs 0. |
| Ledger of Signs | Power | 1 | Every time you queue a Foretell gain 5 Block. | Gain 7 Block. |
| Foretold Falter | Power | 2 | Enemies with more than 10 stacks of Weak deal half damage to you. | Cost 1 |
| Weave the Aegis | Power | 1 | Whenever your Destiny changes, gain 12 Block. | Gain 16 Block. |
| Small Ritual | Power | 1 | Innate. When you Divinate, gain 1 Energy. | Cost 0. |

## Rare Cards

| Name | Type | Cost | Effect | Upgrade |
|---|---|---:|---|---|
| Apocalypse | Attack | 1 | Deal 9 damage to random enemy 9 times. Add Destiny to the cost of this card. | Deal 10 damage to random enemy 10 times. |
| Hand of Fate | Attack | 1 | Deal 13 damage. Draw 1 card. Revelation: do this 3 times. | 16 damage. Revelation: do this 4 times.  |
| The Last Word | Attack | 2 | Retain.  Deal 20 damage. If Fatal, gain 1 Destiny. Exhaust. | 28 damage. |
| Unavoidable End | Attack | 2 | Deal 8 damage. Foretell: deal triple the damage dealt to all enemies. | 11 damage; Foretell deals triple the damage dealt to all enemies. |
| Moment of Reckoning | Attack | 2 | Trigger all Foretell effects immediately. For each Foretell effect trigger, deal 10 damage to all enemies once. | For each Foretell effect trigger, deal 13 damage to all enemies once. |
| Greater Portent | Skill | 3 | Exhaust. Search up to 3 cards from your draw pile. Put them into your hand; they are Fated. | Costs 2. |
| Clairvoyance | Skill | 2 | Search anywhere for 1 card. Put it into your hand; it is Fated. Exhaust. | Cost 1. |
| Reversal | Skill | 0 | Exhaust. Set Destiny to 5 minus its current value. | Gains Retain. |
| Oracle's Bargain | Skill | 1 | Gain 1 Destiny. Shuffle 3 Misfortunes into your draw pile. Exhaust. | Costs 0. |
| Omen of Fallen Sky | Skill | X | Foretell: Deal 20 * X damage to all enemies. | Deal 25 * X damage. |
| Omen of Transcendence | Skill | 1 | Foretell: Draw 4 cards and gain 3 energy. | Draw 5 cards and gain 4 energy. |
| Relic Banishing | Skill | 2 | Divinate twice for relic divinations only. Add a Bend Future to your hand. Exhaust. | Add a Bend Future+. |
| Perfect Forecast | Skill | 3 | Exhaust. Divinate. Gain 1 Energy for each unique category ever recorded. | Costs 2. |
| Cheat the Ending | Skill | 1 | Retain. Exhaust. The next time you would die this combat, heal to 13% of max HP and set Destiny to 0. | Costs 0. |
| Veil | Skill | 1 | Exhaust. Gain Block equal to number of recorded divinations. | Does not exhaust. |
| The Final Strand | Power | 0 | Lose 5 Destiny. Revelation effect always trigger regardless of Destiny in this combat. | Innate |
| Fixed Point | Power | 2 | Destiny cannot change this combat. | 1 |
| Duality | Power | 1 | Good Omen and Bad Omen extra effect always trigger. | Cost 0 |
| Many Futures | Power | 2 | Card rewards at the end of this combat have 1 additional option. When you Scry, Scry 2 additional cards. | Cost 1. |
| Echoed Omen | Power | 3 | Foretell effects trigger an additional time. | Costs 2. |
| Ascended Form | Power | 3 | Good Omen and Revelation card effects can be triggered with 1 less Destiny. Revelation extra effect no longer reduces Destiny. | Trigger threshold reduced by 2 Destiny. |

## Ancient Cards

| Name | Type | Cost | Effect | Upgrade | Ancient Notes |
|---|---|---:|---|---|
| Resonation of Fate | Power | 3 | Fated cards are played an additional time. At start of turn, make 1 random card in your hand Fated. | make 2 random cards in your hand Fated. | From Dusty Tome |
| Omen of Perishment | Skill | 1 | Foretell: deal 22 damage and apply 3 Weak and 3 Vulnerable to all enemies. | 33 damage, 5 Weak and 5 Vulnerable. | Ancient version of Omen of Woes from Archaic Tooth |


## Generated and Special Cards

| Name | Type | Cost | Effect | Notes |
|---|---|---:|---|---|
| Escape from Destiny | Skill | 1+ | Gain 1 Countdown of Destiny. This card costs 1 more this combat. | Generated the first time each combat Destiny reaches 0. |
