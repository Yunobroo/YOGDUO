# Goblin heists

Open `Assets/Scenes/ShopHub.unity` and press Play. The hub is the first scene in Build Settings.

## Play flow

- Walk to a heist gate and press **E** (controller: **A / Cross**), or click **Interact**.
- Level 1 is available immediately. Escape with its crown to unlock Level 2, **The Royal Vault**.
- Collect coins and gems, steal the crown, then return to the green exit pad at the starting position.
- The crown blocks sprinting and absorbs a hit. A hit drops it and gives two seconds of protection.
- Picking up the crown again restores its protection. A hit without it, outside that grace period, ends the run.
- Guards can recover a dropped crown and visibly carry it back to the original pedestal.
- On success or failure, use **Return to the shop hub**. After Level 1 success, you can also enter Level 2 directly.
- Crown bounties pay 5 coins in Level 1 and 10 in Level 2, plus collected coins and gems (3 coins each).
- Only successful escapes bank a run's loot. Previously banked coins and purchased upgrades survive failed runs.

## Shop

Approach the merchant counter and press E to talk, then choose **Show me.** to browse. **Any work?** asks about your next heist. Movement pauses during dialogue. Press E or choose **Goodbye.** to leave; Esc backs out of the shop or conversation.

| Upgrade | Price | Effect |
| --- | --- | --- |
| Quick boots | 5 coins per rank, 2 ranks | +0.5 sprint speed per rank; cannot override crown sprint restriction |
| Soft soles | 4 coins | Walking hearing radius decreases from 3m to 1.5m |
| Endurance training | 6 coins per rank, 2 ranks | +25% sprint endurance per rank |

Campaign progress is stored in PlayerPrefs under `GoblinHeist.Progress.v1`. No progress reset runs automatically.

## Level 2

The Royal Vault has a 44 x 34 metre floor, three connected wings, five guard routes, six hiding spots, 24 coins, six gems, and a separate crown and exit. Its navigation is saved in `Assets/Prefabs/RoyalVaultNavMesh.asset`.

## Editing and validation

The new scenes are saved Unity scenes. Edit their objects normally in the Hierarchy. `Goblin > Build Campaign Scenes` creates missing campaign scenes, without overwriting scenes that already exist.

`CampaignValidation.RunAll` is an editor batch validation entry point for an **isolated copy** of the project. It checks shop transactions and persistence, level unlocks, scene contents, navigation paths, repeated crown protection, guard pickup and return, and extraction rewards. It restores campaign preferences after testing.
