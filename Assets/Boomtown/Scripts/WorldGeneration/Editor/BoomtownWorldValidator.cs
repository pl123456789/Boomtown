using System.Text;
using UnityEditor;
using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    public static class BoomtownWorldValidator
    {
        public static bool Validate(
            BoomtownWorldGenerationContext context,
            out string summary)
        {
            StringBuilder report =
                new StringBuilder();

            bool valid = true;

            if (context.Terrain == null ||
                context.Terrain.terrainData == null)
            {
                valid = false;
                report.AppendLine(
                    "FAIL: Generated terrain is missing.");
            }
            else
            {
                report.AppendLine(
                    "PASS: Terrain generated.");
            }

            if (context.MapDefinition.generateRivers)
            {
                if (context.RiverData == null ||
                    context.RiverData.samples == null ||
                    context.RiverData.samples.Count < 2)
                {
                    valid = false;
                    report.AppendLine(
                        "FAIL: RiverData is missing or empty.");
                }
                else
                {
                    report.AppendLine(
                        $"PASS: River has " +
                        $"{context.RiverData.samples.Count} samples.");
                }
            }

            if (context.GeologyData == null ||
                !context.GeologyData.IsValid)
            {
                valid = false;
                report.AppendLine(
                    "FAIL: Geology data is missing or invalid.");
            }
            else
            {
                float initial =
                    context.GeologyData
                        .GetTotalInitialOunces();

                int depositCells = 0;

                if (context.GeologyData
                        .placerInitialOunces != null)
                {
                    foreach (float ounces in
                             context.GeologyData
                                 .placerInitialOunces)
                    {
                        if (ounces > 0f)
                        {
                            depositCells++;
                        }
                    }
                }

                float initialHardRock =
                    context.GeologyData
                        .GetTotalInitialHardRockOunces();

                float remainingHardRock =
                    context.GeologyData
                        .GetTotalRemainingHardRockOunces();

                report.AppendLine(
                    $"PASS: Finite placer gold: " +
                    $"{initial:0.00} oz across " +
                    $"{depositCells} cells.");

                report.AppendLine(
                    $"PASS: Quartz-vein hard rock: " +
                    $"{remainingHardRock:0.00} oz remaining from " +
                    $"{initialHardRock:0.00} oz generated.");

                if (initial >
                    initialHardRock +
                    0.01f)
                {
                    valid = false;

                    report.AppendLine(
                        "FAIL: Placer gold exceeds generated hard-rock source.");
                }

                if (initial <= 0f)
                {
                    valid = false;
                    report.AppendLine(
                        "FAIL: No finite placer gold generated.");
                }
            }

            GameObject bill =
                GameObject.Find("Bill");

            GameObject ted =
                GameObject.Find("Ted");

            if (bill == null || ted == null)
            {
                report.AppendLine(
                    "WARN: Bill or Ted was not found in the active scene.");
            }
            else
            {
                report.AppendLine(
                    "PASS: Bill and Ted are present.");
            }

            summary =
                report.ToString().TrimEnd();

            context.DistrictData.validationSummary =
                summary;

            context.DistrictData.generationSucceeded =
                valid;

            context.DistrictData.stage =
                valid
                    ? DistrictGenerationStage.Complete
                    : DistrictGenerationStage.Failed;

            EditorUtility.SetDirty(
                context.DistrictData);

            return valid;
        }
    }
}
