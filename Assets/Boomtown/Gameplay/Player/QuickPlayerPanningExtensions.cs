using Boomtown.Gameplay.Prospecting;
using UnityEngine;

public static class QuickPlayerPanningExtensions
{
    public static void MarkLastWaypointAsPanning(
        this QuickPlayerController controller)
    {
        if (controller == null)
        {
            return;
        }

        WaypointPath waypointPath =
            controller.GetComponent<WaypointPath>();
        GoldPanningController panning =
            controller.GetComponent<GoldPanningController>();

        if (waypointPath == null ||
            !waypointPath.TryGetLastCommand(
                out WaypointCommand lastCommand))
        {
            Debug.Log(
                "[Boomtown] Queue at least one waypoint before pressing " +
                "Shift + Space.",
                controller);
            return;
        }

        if (panning == null ||
            !panning.CanPanAt(lastCommand.Position))
        {
            Debug.Log(
                "[Boomtown] That waypoint is not a valid gold-panning spot. " +
                "Move it closer to a valid river bank.",
                controller);
            return;
        }

        waypointPath.MarkLastCommandAsActivity(
            WaypointCommandType.PanForGold);
    }
}
