using UnityEngine;

// A visible extraction zone. Loot is banked only when the crown reaches it.
public class DungeonExit : MonoBehaviour
{
    public float escapeRadius = 1.2f;
    public LayerMask wallMask = 1 << 8;

    void Update()
    {
        var game = GoblinDungeon.Instance;
        if (game == null || !game.Playing || !game.CarryingCrown || game.player == null) return;

        Vector3 playerPosition = game.player.transform.position;
        Vector3 offset = playerPosition - transform.position;
        offset.y = 0;
        if (offset.sqrMagnitude > escapeRadius * escapeRadius) return;
        if (Physics.Linecast(playerPosition + Vector3.up * .5f,
            transform.position + Vector3.up * .5f, wallMask)) return;

        game.Escape();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, escapeRadius);
    }
}
