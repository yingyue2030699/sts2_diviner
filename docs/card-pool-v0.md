# Diviner Card Pool Draft v0.1

This is a design draft, not final balance. Costs, numbers, and rarity are chosen to create a playable identity before engine constraints and playtesting force adjustments. Implementation status is tracked in `docs/plan-status.md`.

## Core Vocabulary

| Term | Meaning |
|---|---|
| Destiny | Persistent 0-5 run value. Starts at 3. Direct changes to Destiny should be scarce, usually Basic starter behavior, Rare build pivots, relics, or potions. |
| Bad Omen | Destiny 0-2. Cards may care about this state, but most should not automatically repair it. |
| Good Omen | Destiny 3-5. Cards may gain efficiency from this state, but most should not spend Destiny as a routine cost. |
| Dredge | Destiny is exactly 0. At combat start, gain Countdown of Destiny 3 and shuffle Escape from Destiny cards into the draw pile. |
| Enlightenment | Destiny is exactly 5. At combat start, search for 3 cards, put them into your hand, and make them `Fated`. |
| Divinate | Record one future forecast in Crystal Ball. Divination cards should be premium because the reward is partly strategic information and partly payoff fuel. |
| Foretell | Queue the following effect to resolve at the start of your next turn. Multiple Foretell effects queue separately. |
| Fated | A Fated card costs 0 this turn. The marker is removed when the card is played; the card's base cost is unchanged. |
| Fortune | Generated 0-cost Retain skill: draw 2, Exhaust. |
| Misfortune | Generated 3-cost attack/skill: lose 3 HP, deal 15 damage to all enemies, autoplayed at end of turn. |
| Countdown of Destiny | Dredge countdown. Loses 1 at end of turn; defeat at 0. |
| Scry X | Look at the top X cards of your draw pile. Discard any number of them. |

Design constraints:

- Fortune and Misfortune remain generated cards as described in the original concept. They are not stack counters.
- Common and Uncommon cards should mostly read Destiny state, consume generated cards, Foretell, search, retain, or scale with recorded divinations. They should rarely change Destiny directly.
- Direct Destiny changes are reserved for Balance, Dredge survival tools, Rare build pivots, relics, and potions.
- Divination access should feel valuable: a card that divinates should either be above-rate, Exhaust, cost HP, or be tied to a build payoff.

## Basic Cards

| Name | Type | Cost | Effect | Upgrade |
|---|---|---:|---|---|
| Strike | Attack | 1 | Deal 6 damage. | 9 damage. |
| Defend | Skill | 1 | Gain 5 Block. | 8 Block. |
| Balance | Skill | 1 | Good Omen: lose 1 Destiny, divinate, draw 1. Bad Omen: requires 2 extra Energy, gain 1 Destiny. Exhaust. | Costs 0. |
| Divination of Woes | Skill | 1 | Foretell: deal 9 damage to all enemies. Apply 1 Weak and 1 Vulnerable to all enemies. | 13 damage. |

## Common Cards

| Name | Type | Cost | Effect | Upgrade |
|---|---|---:|---|---|
| Palm Strike | Attack | 1 | Deal 9 damage. Good Omen: draw 3 cards. | 12 damage. |
| Destiny's Fall | Attack | 1 | Deal 8 damage. Foretell: deal 8 damage to the same enemy. | 10 and 10 damage. |
| Crossed Lines | Attack | 0 | Deal 4 damage. If Crystal Ball has a recorded divination, apply 1 Weak. | 2 Weak. |
| Thread Cut | Attack | 1 | Deal 5 damage. If the target has Weak, deal 10 more. | 6 damage, 14 more. |
| Wax Seal | Attack | 1 | Deal 4 damage. Put a card from your draw or discard pile on top of your draw pile. | 7 damage. |
| Misread Strike | Attack | 1 | Deal 20 damage. Lose 1 Destiny. | 28 damage. |
| Eclipse Jab | Attack | 1 | Deal 2 damage. Then deal 8 damage. | Deal 4 damage. Then deal 10 damage. |
| Forewarned Blow | Attack | 2 | Deal 14 damage. If the target has Weak, draw 1. | 18 damage, draw 2. |
| Destined Lance | Attack | 2 | Deal 16 damage. Good Omen: hit all enemies. | 21 damage. |
| Read the Room | Skill | 0 | Scry 3. | Scry 5. |
| Insurance | Skill | 1 | Gain 6 Block. Whenever you lose HP this turn, draw 1. | 9 Block. |
| Ward Sign | Skill | 1 | Gain 8 Block. Good Omen: Retain a card in your hand this turn. | 11 Block. |
| Bad Feeling | Skill | 0 | Apply 1 Weak. Bad Omen: apply 1 Weak and 1 Vulnerable instead. | Bad Omen: apply 1 Weak and 1 Vulnerable to all enemies instead. |
| Palm Reading | Skill | 1 | Scry 4. If you divinated this combat, draw 1. | Scry 6. |
| Lucky Break | Skill | 0 | Gain 2 Block. Draw 1. Exhaust. | 3 Block. Retain. |
| Mark Calendar | Skill | 1 | Foretell: gain 12 Block. | 16 Block. |
| False Alarm | Skill | 1 | Gain 14 Block. Lose 1 Destiny. | Gain 18 Block. |
| Omens Align | Skill | 1 | Put all Fortune and Misfortune cards into your hand. Exhaust. | Remove Exhaust. |
| Smoke and Mirrors | Skill | 1 | Gain 6 Block. The next Foretell effect you play this combat resolves with +5 damage or +5 Block. | +9 damage or +9 Block. |
| Skeptic's Charm | Skill | 1 | Gain 6 Block. Bad Omen: gain 6 more Block. | 8 and 8 more Block. |
| Reconsult | Skill | 2 | If you have divinated this combat, Divinate. Exhaust. | 1 cost. |
| Narrow Escape | Skill | 1 | Gain 7 Block. Dredge: gain 1 Countdown of Destiny. | 10 Block. |
| Thread Pull | Skill | 1 | Put a card from your hand on top of your draw pile. Foretell: draw 2. | Foretell: draw 3. |
| Small Ritual | Power | 1 | Innate. The next time you divinate, gain 2 energy. | 3 energy. |

## Uncommon Cards

| Name | Type | Cost | Effect | Upgrade |
|---|---|---:|---|---|
| Line of Fate | Attack | 1 | Deal 6 damage. If Fatal, divinate. Exhaust. | Retain. |
| Star Needle | Attack | 0 | Deal 3 damage. If you have 7 or more recorded divinations, apply 7 Vulnerable. | 7 damage. |
| Doomscript | Attack | 1 | Deal 8 damage. Foretell: apply 2 Vulnerable to all enemies. | 11 damage, 3 Vulnerable. |
| Horoscope | Attack | 0 | Deal 4 damage. If the top card of your draw pile is an Attack, draw it. | 6 damage. |
| Red Thread | Attack | 1 | Deal 10 damage. Search your draw pile for an Attack and put it on top. | 13 damage. |
| Hexagram | Attack | 2 | Deal 6 damage to a random enemy 6 times. Enlightenment: choose the target. | 7 damage 6 times. |
| Backdated Wound | Attack | 1 | Deal 8 damage. Foretell: deal 8 damage. | 10 and 10 damage. |
| Inevitability | Attack | 4 | Deal 30 damage. Costs 1 less for every 2 divinations recorded. | 38 damage. |
| Cursed Prediction | Attack | 1 | Deal 10 damage. Add a Misfortune to your discard pile. | Add it to your hand. |
| Dead Star | Attack | 2 | Deal 18 damage. Lose 1 HP. Bad Omen: deal 18 damage again. | +4 damage. |
| Verdict | Attack | 1 | Deal 5 damage. If the target has Weak or Vulnerable, deal 12 instead. | 8 or 16 damage. |
| Augury | Skill | 2 | If have Divinated this combat, gain 1 Destiny. Otherwise gain 12 Block. Exhaust. | 16 Block. |
| Tea Leaves | Skill | 2 | Foretell: Divinate. Exhaust. | Costs 1. |
| Second Sight | Skill | 1 | Retain. Draw until you have 5 cards in hand. Exhaust. | Draw until 6. |
| Loaded Reading | Skill | 2 | Add a Fortune and a Misfortune to your hand. | Costs 1. |
| Cold Reading | Skill | 1 | Apply 2 Weak and 2 Vulnerable. If you divinated this combat, apply to all enemies. | 3 Weak and 3 Vulnerable. |
| Read Ahead | Skill | 0 | Draw 2 cards. Put 2 cards from your hand back to your draw pile in any order. | Draw 3, put 3. |
| Unasked Question | Skill | 0 | Divinate. Lose 5 HP. Exhaust. | Lose 3 HP. |
| Rewrite the Sign | Skill | 0 | Replace all Misfortunes in hand, draw pile, and discard pile with Fortunes. Exhaust. | Also replace Escape from Destiny. |
| Predestined Path | Skill | 2 | Choose 2 cards from your draw pile. Draw them at the start of next turn; they are Fated that turn. | Costs 1. |
| Evil Eye | Skill | 1 | Apply 3 Weak. Bad Omen: gain 1 energy. | 5 Weak. |
| Read the Ashes | Skill | 1 | Exhaust a card. Foretell: draw 1 card. If a Status or Curse is Exhausted, Foretell: gain 2 energy. | Costs 0. |
| Borrowed Tomorrow | Skill | 0 | Gain 3 energy. Foretell: lose 2 energy. Exhaust. | Gain 4 energy. |
| Funeral Clock | Skill | 1 | Gain 8 Block. Dredge: cost reduced by 1; gain 1 Countdown of Destiny and draw 1. | 11 Block. |
| White Room | Skill | 1 | Draw 2 cards. Enlightenment: choose up to 2 cards in your hand. They are Fated. | Choose up to 3 cards. |
| Prophetic Trance | Power | 1 | Innate. Whenever you divinate, draw 2 cards. | Costs 0. |
| The Written Hour | Power | 1 | At the start of your turn, if Destiny is exactly 3, gain 1 energy and draw 1. | Costs 0. |
| Pattern Recognition | Power | 1 | Whenever you play the third card in a turn, Good Omen: gain 3 Block. Bad Omen: deal 3 damage to all enemies. | 4 Block/damage. |
| Haruspex Method | Power | 1 | The next time you Exhaust a card, Divinate and Foretell: add a Haruspex Method to your hand. | Costs 0; Foretell adds a Haruspex Method+. |
| Chosen Line | Power | 1 | Only playable if Enlightenment. The first card you draw each turn is Fated. Draw 1 extra card per turn. | Costs 0. |
| Doom Engine | Power | 2 | Misfortunes' HP lost is reduced by 1 and they deal 9 more damage. Add 1 Misfortune to your hand. | HP lost reduced by 2 and deal 13 more damage. |
| Ledger of Signs | Power | 1 | Every 3 times you Foretell, add Fortune to your hand. | Costs 0. |

## Rare Cards

| Name | Type | Cost | Effect | Upgrade |
|---|---|---:|---|---|
| Clairvoyance | Skill | 1 | Search your draw pile for 1 card. Put it into your hand and add Fated. Exhaust. | Does not Exhaust. |
| Apocalypse | Attack | 3 | Deal 50 damage to all enemies. Set Destiny to 0. Dredge: cost reduced by 3. | 60 damage. |
| Fallen Sky | Attack | 2 | Deal 12 damage to all enemies. Foretell: play this card again. Exhaust. | 16 damage. |
| Hand of Fate | Attack | 1 | Deal 13 damage. Draw 1 card. Enlightenment: play this 3 times. | 16 damage. |
| The Last Word | Attack | 2 | Deal 20 damage. If Fatal, gain 1 Destiny. | 28 damage. |
| Unavoidable End | Attack | 2 | Deal 10 damage. Foretell: deal double the damage dealt to all enemies. | 14 damage. |
| Greater Portent | Skill | 2 | Search 3 cards from your draw pile. Put them into your hand; they are Fated this turn. Exhaust. | Costs 1. |
| Reversal | Skill | 1 | Set Destiny to 5 minus current value. Exhaust. | Retain. |
| Oracle's Bargain | Skill | 1 | Gain 1 Destiny and add 3 Misfortunes to your draw pile. Exhaust. | Costs 0. |
| Perfect Forecast | Skill | 3 | Divinate twice. Gain 1 energy for each unique category ever recorded. Exhaust. | Costs 2. |
| Cheat the Ending | Skill | 1 | Retain. The next time you would die this combat, heal to 13 and set Destiny to 0. Exhaust. | Costs 0. |
| Fixed Point | Power | 3 | Destiny cannot decrease below 3 this combat. At the start of your turn, lose 1 HP. | Costs 2. |
| Many Futures | Power | 2 | Card rewards at the end of this combat have 1 additional option. When you Scry, choose 1 additional card. | Costs 1. |
| Doom Spiral | Power | 2 | At end of turn, if Bad Omen, add a Misfortune to your hand and reduce all Misfortunes' energy cost and HP lost by 1 this combat. | Costs 1. |
| Threadcutter | Power | 2 | Whenever Countdown of Destiny increases, deal 8 damage to all enemies. | 11 damage. |
| Ascended Form | Power | 3 | Enlightenment card effects can be triggered with 2 less Destiny. | 3 less Destiny. |

## Generated Cards

| Name | Type | Cost | Effect | Notes |
|---|---|---:|---|---|
| Fortune | Skill | 0 | Retain. Draw 2. Exhaust. | Added by Crystal Ball and some cards. |
| Misfortune | Attack/Skill | 3 | Lose 3 HP. Deal 15 damage to all enemies. Autoplayed at end of turn. | Treat as generated/exhausting. |
| Escape from Destiny | Skill | 1+ | Dredge only. Gain 1 Countdown of Destiny. This card costs 1 more this combat. Exhaust. | Generated at combat start while Dredge. |
