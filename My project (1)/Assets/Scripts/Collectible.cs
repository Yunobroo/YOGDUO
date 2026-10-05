using UnityEngine;

public class Collectible : MonoBehaviour
{
    public enum Kind { Coin, Gem, Crown }
    public Kind kind;
    public Transform visual;
    public float pickupRadius = .8f;
    public LayerMask wallMask = 1 << 8;

    void Update()
    {
        if (visual != null) visual.Rotate(0, 65 * Time.deltaTime, 0, Space.World);
        var game = GoblinDungeon.Instance;
        if (game == null || !game.Playing || game.player == null) return;
        Vector3 playerPosition = game.player.transform.position;
        if (Vector3.Distance(playerPosition, transform.position) > pickupRadius) return;
        if (Physics.Linecast(playerPosition + Vector3.up * .5f, transform.position + Vector3.up * .5f, wallMask)) return;
        game.Collect(kind);
        gameObject.SetActive(false);
    }
}
