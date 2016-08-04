using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Linq;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameScript : NetworkBehaviour
{

    public static GameScript Instance;

    [SyncVar]
    public bool Started = false;
    [SyncVar]
    public bool Ended = false;
    [SyncVar]
    public bool BossWins;

    [SyncVar]
    public float TimeRemaining;

    public float MaxTime = 15 * 60;

    PlayerNetCharacter[] Characters;

    public Text StatusText;

    // Use this for initialization
    void Start()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (Started && !Ended)
        {
            Debug.Log("Started: " + TimeRemaining);
            if (isServer)
            {
                TimeRemaining -= Time.deltaTime*10f;
            }

            var areAllCharsDead = Characters.All(c => c.Health <= 0);

            if (TimeRemaining <= 0 && areAllCharsDead)
            {
                BossWins = areAllCharsDead;
                StatusText.text = BossWins ? "The Boss has won!" : "The Mob have survived, they win!";
                Ended = true;
            }
            else
            {
                StatusText.text = "Time Left: " + Mathf.Ceil(TimeRemaining) + "s";
            }
        }
    }
    
    public void StartGame()
    {
        TimeRemaining = MaxTime;
        Characters = GameObject.FindObjectsOfType<PlayerNetCharacter>();
        Started = true;
        Ended = false;
        BossWins = false;
    }
}
