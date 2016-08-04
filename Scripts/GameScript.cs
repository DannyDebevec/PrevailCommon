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

    [SyncVar]
    public string Text;

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
            if (isServer)   
            {
                TimeRemaining -= Time.deltaTime;

                var areAllCharsDead = Characters.All(c => c.Health <= 0);

                if (TimeRemaining <= 0 && areAllCharsDead)
                {
                    BossWins = areAllCharsDead;
                    Text = BossWins ? "The Boss has won!" : "The Mob have survived, they win!";
                    Ended = true;
                }
                else
                {
                    Text = "Time Left: " + Mathf.Ceil(TimeRemaining) + "s";
                }
            }

            StatusText.text = Text;
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
