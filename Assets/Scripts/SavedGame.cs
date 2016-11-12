using UnityEngine;
using System.Collections;
using System;

// make sure no unit is selected when we are saving the game
[Serializable]
public class SavedGame
{
    public SerializableMap map;
    public SerializableUnit[] redUnits, redDeadUnits, blueUnits, blueDeadUnits;
    public CombatManager.Faction currentPlayer;
    public float redElapsedTime, blueElapsedTime, gameTimer;
    public int roundCount, redTurnCounter, redSkippedTurnsCounter, blueTurnCounter, blueSkippedTurnsCounter, numTurnSkips;
    public string previousLogFileName;

    // TODO: save elapsed times, current round number, number of turns for each player, etc.
}
