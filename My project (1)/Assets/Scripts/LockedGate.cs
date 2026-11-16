using UnityEngine;

public class LockedGate : MonoBehaviour
{
    private GoblinDungeon game;
    private void Awake()
    {
        game = GameObject.FindGameObjectWithTag("GameController").GetComponent<GoblinDungeon>();
    }

    private void Update()
    {
        if (game.Crown != null && game.Crown.GuardCarrier != null)
            gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Something entered the door trigger!");

        Debug.Log("Object detected: " + other.name);
        Debug.Log("Tag: " + other.tag);
        Debug.Log("Carrying key: " + game.CarryingKey);

        if (other.CompareTag("Player") && game.CarryingKey)
        {
            Debug.Log("Player has key! Opening door.");
            game.Key.Consume();
            gameObject.SetActive(false);
        }
    }
}
