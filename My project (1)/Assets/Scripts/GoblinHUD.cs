using UnityEngine;

public class GoblinHUD : MonoBehaviour
{
    public GoblinDungeon game;

    void OnGUI()
    {
        if (game == null) return;
        Matrix4x4 previousMatrix = GUI.matrix;
        float scale = Mathf.Max(.5f, Mathf.Min(Screen.width / 1100f, Screen.height / 720f));
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        GUI.Box(new Rect(15, 15, 475, 157), "GOBLIN'S POCKET");
        string danger = "Quiet";
        foreach (var guard in game.guards) if (guard != null && guard.Status == "LastKnownLocation") danger = "Guards investigating";
        foreach (var guard in game.guards) if (guard != null && guard.CanSeeTarget) danger = "SPOTTED - RUN!";
        var player = game.player;
        if (player != null && player.Hidden) danger = "Hidden";
        GUI.Label(new Rect(28, 43, 455, 24), "Coins " + game.Coins + "/" + game.TotalCoins + "    Gems " + game.Gems + "/" + game.TotalGems + "    Purse " + game.Bank + "    " + danger);
        GUI.Label(new Rect(28, 67, 455, 24), "WASD: screen directions   Ctrl: sneak   C: toggle   Shift: sprint");
        GUI.Label(new Rect(28, 91, 455, 24), "Crouch on green patches to hide. Gold crown finishes the run.");
        GUI.Label(new Rect(28, 115, 455, 24), "R: restart    Stamina " + (player == null ? 100 : Mathf.RoundToInt(player.Stamina * 100)) + "%");
        GUI.Label(new Rect(28, 139, 455, 24), "Footstep noise: " + (player == null || player.NoiseRadius == 0 ? "Silent" : player.NoiseRadius.ToString("0.0") + "m range (walls halve range)"));
        if (!game.Playing)
        {
            float x = (Screen.width / scale - 390) * .5f;
            float y = (Screen.height / scale - 275) * .5f;
            GUI.Box(new Rect(x, y, 390, 275), game.Shopping ? "THE FENCE'S SHOP" : game.CaughtPlayer ? "CAUGHT" : "CROWN STOLEN!");
            GUI.Label(new Rect(x + 25, y + 38, 350, 45), game.Shopping ? "Purse: " + game.Bank + " coins. Upgrades last this play session." : game.CaughtPlayer ? "Too slow, little goblin. Your unbanked loot was lost." : "Banked " + (game.Coins + game.Gems * 3) + " coins. Gems are worth 3 each.");
            if (game.Shopping)
            {
                GUI.enabled = game.Bank >= 5 && game.Boots < 2;
                if (GUI.Button(new Rect(x + 25, y + 90, 340, 35), "Quick boots - 5 coins (" + game.Boots + "/2)")) game.BuyBoots();
                GUI.enabled = game.Bank >= 4 && !game.SoftShoes;
                if (GUI.Button(new Rect(x + 25, y + 135, 340, 35), game.SoftShoes ? "Soft soles owned" : "Soft soles - quieter walking - 4 coins")) game.BuySoftShoes();
                GUI.enabled = true;
            }
            else if (game.Won && GUI.Button(new Rect(x + 25, y + 105, 340, 40), "Spend loot in the shop")) game.OpenShop();
            if (GUI.Button(new Rect(x + 25, y + 205, 340, 40), "Try another run")) game.Restart();
        }
        GUI.matrix = previousMatrix;
    }
}
