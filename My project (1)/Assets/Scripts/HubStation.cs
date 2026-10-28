using UnityEngine;

public class HubStation : MonoBehaviour
{
    public enum Destination { Shop, FirstHeist, SecondHeist }
    public Destination destination;
    public string stationName;
    public float interactionRadius = 2.4f;

    public bool InRange(Player player)
    {
        if (player == null) return false;
        Vector3 offset = player.transform.position - transform.position;
        offset.y = 0;
        return offset.sqrMagnitude < interactionRadius * interactionRadius;
    }
}
