# FreeApparelTagsRestrict
Adds enforcement of apparel tags when RimWorld generates pawns and gives them free apparel for cold/warm areas.

Normally when RimWorld is generating pawns and their apparel and the region the pawns are being generated in is very cold or warm it will give them some free apparel to counter the severe heat/cold.  Because RimWorld only has one set of apparel it doesn't actually check if the pawn *should* get those items based on apparel tags.

That is where this mod comes in, this modifies the checks for free gear to make sure that the pawn should be allowed to wear it.

This isn't really useful for vanilla RimWorld but should be useful for mods that add a bunch of new apparel for pawns to wear and wish to restrict the apparel by tags.

NOTE: This is currently largely untested...  The patches are installing but I don't know if the behavior is as desired.

## Usage:
### As an end user:
To try the mod simply download the package (clone or download button) and extract into the RimWorld mods folder.  The load order shouldn't matter as the patches are installed on game start and none of the new code will be executed until you are playing the game (in a map).  The only exception to that rule is if another mod tries to patch the same code this mod does but they do so in a manner which blocks this mod's code from running.

### As a mod author:
In testing this works as a drop in to the assembly folder of your mod.  I'm currently working on testing some potential issues that can come up if say you are using version 0.0.2 in yours and someone else's mod uses 0.0.1.

## Anticipated potential errors:
- Blank screen on game start: The patching failed in a spectacular way.  Most likely to happen when using the mod with a different version of RimWorld.  The log file should have some useful details but is also likely to be VERY big, for now remove the mod from RimWorld.
- Warning about not finding any predicates to patch (yellow text): Also likely due to using the mod with a different version of RimWorld.  You can still play the game but the mod isn't doing anything anymore and can be disabled if desired.
- An error (red text) about not finding the pawn, only happens once: Most likely the method that I patched to find the pawn got patched in a blocking way by another mod/patch.  For example a Detour or a Harmony Prefix that prevents the original code from executing.  Your loaded mods and load order would be needed at that point.  Can also be a bug on my end as this is only lightly tested.
- Pawns showing up with apparel they shouldn't be but NO errors or warnings from the mod: I goofed somewhere apparently...  If the environment is mild that implies that RimWorld doesn't do tag checking in another place.  I'd like to be able to work with one or more mod authors to figure such a situation out since I don't currently make nor use any apparel heavy mods.
