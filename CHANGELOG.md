# Changelog

## 1.0.9

- Fixed the selected target fish still being caught after moving to a
  different location where it can't normally spawn. The target fish is now
  only applied when it's actually available at the player's current location
  (unless `AllowAllFish` is enabled), with normal fishing used otherwise.
- The target fish selection is now automatically and silently cleared when
  the player moves to a location where that fish can't spawn, instead of
  repeatedly showing "fish not available" messages every time they fish
  elsewhere.
- The release zip now packages files inside a `ChooseYourCatch` folder so
  they don't get extracted loose into the Mods folder.

## 1.0.8

- Fixed Iridium (and other non-Normal) target qualities not being applied to
  the caught fish; the quality is now set on the actual item produced by the
  fishing rod instead of the discarded item passed to the minigame callback.
- Fixed Gold and Silver target qualities being ignored in favor of Normal when
  the Default Quality setting was changed after a fish was already selected;
  the mod now reads the current Default Quality live at catch time.
- Renamed the "Random" quality option to "Vanilla", which now means the mod's
  quality override is skipped entirely and the game's normal quality logic
  determines the result.

## 1.0.7

- Added sorting by name, price, and uncaught status.
- Added an uncaught-only filter and green dot indicators for fish not yet recorded
  in the local player's collection.
- Added quality-aware sell prices and collection status to catch tooltips.
- Sort and filter choices persist while the game is running and reset after a
  restart.
- Junk and unsupported catches remain available but are not incorrectly marked
  as uncaught fish.

## 1.0.6

- Prevented the Choose Your Catch menu from opening while the player is
  actively casting, waiting for a bite, reeling, using the fishing minigame, or
  pulling a catch from the water.
- Holding an idle fishing rod still allows the menu to open normally.
- Added a translated HUD message telling the player to stop fishing first.

## 1.0.5

- Fixed the selection list changing every time the menu was reopened while the
  player stayed in the same place.
- Replaced the game's random catch-chance roll with deterministic eligibility
  checks for menu population.
- The stable menu still respects time, weather, fishing level, training rod,
  magic bait, location, fishing area, player position, bobber position, shore
  distance, game-state conditions, and catch limits.
- Catch probability, daily luck, and chance multipliers no longer hide valid
  fish, junk, or other catches from the selection menu.

## 1.0.4

- Fixed special-position catches such as Goby appearing at unrelated lakes or
  rivers when `RespectSpawningRules` is enabled.
- The menu now checks fish area, bobber position, player position, and actual
  distance from shore against the likely pre-cast water tile.
- Once a fish, junk item, or other catch is selected, the mod now always uses
  that selection instead of silently rejecting it when the cast begins.
- Preserved support for non-fish and modded object catches exposed through
  standard location spawn data.

## 1.0.3

- Fixed the selection menu mixing fish from different water bodies (ocean, lake,
  river) in the same location. When `RespectSpawningRules` is enabled, the menu
  now detects the water body closest to the player (preferring the direction
  they're facing) and only lists fish from that fish area.
- Added an on-screen message when a selected fish can't be caught at the current
  spot, instead of silently falling back to normal fishing.

## 1.0.2

- Added configuration option `ShowOnlyFish` (default: false) to allow selecting non-fish catches like junk, seaweed, crates, and modded object catches (e.g., Bioluminescent Seaweed and Terra Fish Crates).
- Added configuration option `RespectSpawningRules` (default: false) to filter the target selection list and catches based on current season, time, weather, and other spawning conditions.
- Added localization keys for the new options (English, Indonesian, and Chinese).

## 1.0.1

- Made pre-cast fish lists stable so moving slightly no longer reshuffles
  available fish.
- Made target replacement use the same stable selection rules as the menu when
  `AllowAllFish` is disabled.
- Improved handling for location-wide fish selection in areas like Ridge Falls.

## 1.0.0

- Renamed the mod to Choose Your Catch.
- Fixed Error Items caused by reading a fish spawn entry ID instead of its item ID.
- Added validation for missing and invalid custom items.
- Replaced the text list with a responsive visual fish grid.
- Fixed header alignment, overflowing fish names, and missing scroll feedback.
- Added per-player multiplayer target synchronization.
- Fixed farmhand availability checks using the host's fishing context.
- Fixed Stonefish, Ice Pip, and Lava Eel missing from mine fishing lists.
- Added vanilla global fishing rules and game-state requirement checks.
- Separated location-wide menu filtering from exact bobber-tile validation.
- Added English, Indonesian, Chinese, French, German, Japanese, Korean,
  Portuguese, Russian, and Spanish translations.
- Added public installation documentation.
