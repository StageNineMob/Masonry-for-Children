using UnityEngine;
using System.Collections;
using System;

// make sure no unit is selected when we are saving the game
[Serializable]
public class SavedGame
{
    public SerializableMap map;
    public string previousLogFileName;

    // TODO: save elapsed times, current round number, number of turns for each player, etc.
}
