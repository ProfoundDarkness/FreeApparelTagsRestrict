using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using RimWorld;
using Verse;

/*
 * There are 2 parts to this patch, 3 patch operations expected.
 * 
 * Part 1: Figure out the pawn that is getting apparel assigned to it.  We need this patch as the real workhorse patching (that comes later) doesn't know
 * anything about a pawn or any other relevent details to get tags from.  This patch can be either the user of the method ApparelWarmthNeededNow()
 * (GenerateStartingApparelFor()) or something that is certain to be called before AddFreeWarmthAsNeeded() that also gets the pawn.
 * I opted on the later (ApparelWarmthNeededNow()).
 * 
 * Part 2 (real work): This patch, or rather patches, modifies the predicates that are used by AddFreeWarmthAsNeeded() to ALSO consider the tags.
 * Because RimWorld only has one set of gear they didn't build and test tags filtering at that point so mods that add new gear that are restricted
 * by tags can still find pawns wearing stuff they shouldn't.  That is what this mod aims to rectify.
 */

namespace FreeApparelTagsRestrict.Harmony
{
    [StaticConstructorOnStartup]
    public static class Harmony_PawnApparelGenerator_AddFreeWarmthAsNeeded
    {
        // Used to conveniently store the source of messages.
        static readonly string logPrefix = Assembly.GetExecutingAssembly().GetName().Name + " :: "; // + typeof(Harmony_PawnApparelGenerator_AddFreeWarmthAsNeeded).Name + " :: ";
        // Used to store the pawn that is currently being considered by RimWorld code for apparel generation since it's not visible by the patched predicates.
        private static Pawn consideredPawn = null;
        // Used to prevent further checks and only print one error message if something goes wrong with fetching the Pawn for apparel tag checks.
        private static bool active = true;

        /// <summary>
        /// Static constructor, handles the patching of RimWorld methods to insert new functionality.
        /// </summary>
        static Harmony_PawnApparelGenerator_AddFreeWarmthAsNeeded()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("FreeApparelTagsRestrict.Harmony");

            // Patch to retrieve the pawn being considered by RimWorld apparel generation.
            HarmonyMethod pawnMethod = new HarmonyMethod(typeof(Harmony_PawnApparelGenerator_AddFreeWarmthAsNeeded), "PostfixGetPawn");
            harmony.Patch(AccessTools.Method(typeof(PawnApparelGenerator), "ApparelWarmthNeededNow"), null, pawnMethod);
            Log.Message(string.Concat(logPrefix, "Method ApparelWarmthNeededNow() patched (Postfix) to retrieve Pawn."));

            // Setup and Patch the Predicates to considere apparel tags when giving free apparel for hot/cold areas.
            // Since we only need one postfix for both predicates, define the target method outside the loop.
            HarmonyMethod postfix = new HarmonyMethod(typeof(Harmony_PawnApparelGenerator_AddFreeWarmthAsNeeded), "Postfix");

            // Get the class that both uses the predicates and has them...
            Type targetClass = AccessTools.Inner(typeof(PawnApparelGenerator), "PossibleApparelSet");
            // find the methods with a single argument of type ThingStuffPair with a return type of bool (also the name should start with m__ (that's 2 underscores)).
            List<MethodInfo> methods = targetClass.GetMethods(AccessTools.all).ToList();
            int patchCount = 0; // Keep track of how many predicates we find, we really only expect to find 2...
            foreach (MethodInfo method in methods)
            {
                if (method.Name.Contains("AddFreeWarmthAsNeeded") && method.Name.Contains("m__") && method.ReturnType.Equals(typeof(Boolean)))
                {
                    ParameterInfo[] args = method.GetParameters();
                    if (args.Length == 1 && args[0].ParameterType.Equals(typeof(ThingStuffPair)))
                    {
                        harmony.Patch(method, null, null);
                        patchCount++;
                    }
                }
            }
            // Messages on success or failure...
            if (patchCount > 0)
            {
                Log.Message(string.Concat(logPrefix, "Method AddFreeWarmthAsNeeded(), ", patchCount, " Predicates patched (Postfix) to consider apparel tags."));
            } else
            {
                Log.Warning(string.Concat(logPrefix, "Didn't find any Predicates for Method AddFreeWarmthAsNeeded() to patch, nothing done."));
            }
        }

        /// <summary>
        /// This postfix is used simply to fetch one of the arguments (Pawn) from the method it's attached to and store it for use later.
        /// </summary>
        /// <param name="pawn">Pawn that was passed to attached method, for use later when considering tags.</param>
        static void PostfixGetPawn(Pawn pawn)
        {
            consideredPawn = pawn;
        }

        /// <summary>
        /// This postfix is applied to (generally) 2 predicates used by AddFreeWarmthAsNeeded() in order to get those predicates to consider if the pawn
        /// should get that apparel or not.
        /// </summary>
        /// <param name="__result">bool result of the Predicate thus far, we only need to do something if this starts out as true in which case gets set to false.</param>
        /// <param name="pa">ThingStuffPair, the apparel that the patched Predicate is considering.</param>
        /// <remarks>
        /// Because this is a postfix we only need to consider the pawn and apparel tags if the result so far is true as a false value indicates one of the
        /// other parts of the predicate didn't pass.
        /// </remarks>
        static void Postfix(ref bool __result, ref ThingStuffPair pa)
        {
            if (active && consideredPawn == null)
            {
                active = false;
                Log.Error(string.Concat(logPrefix, "Error while examining pawn for apparel tags, pawn is null.  Likely cause is pawn retrieval patch failed or",
                    "was overridden by another Harmony patch.\n apparel tags will no longer be filtered and pawns may show up using gear they shouldn't."));
            }
            if (active && __result && !pa.thing.apparel.tags.Intersect(consideredPawn.kindDef.apparelTags).Any())
            {
                __result = false;
            }
        }
    }
}
