# Crab Pot Fish Rerandomized

This mod modifies crab pot fish catching logic to make it better and more fair when running with substantial amount of modded crab pot fish.

## What is wrong with vanilla

In vanilla, crab pot fish randomization checks for loaded crab pot fish sequentially, determines whether it was caught,
and if it was completely skips other fish that happen to go after caught crab pot fish.

With a substantial amount of crab pot fish available in modded games, it creates a significant bias towards fish that happen
to be in the beginning of fish data dictionary enumerator. For last fish to be checked, all previous need to fail their checks.

## How mod tries to fix that

This mod patches CrabPot::DayUpdate. Method is modified to check all crab pot fish regardless if already handled fish were caught or not.
All fish that pass their catch checks are stored in a list.

Once all fish were checked, modified method checks the list. If there are some fish, one of them is picked at random.
As all fish here already have passed their check, I didn't feel need to do some weighted random to not lessen chance of lower rarity fish, so just pure random feels fair.