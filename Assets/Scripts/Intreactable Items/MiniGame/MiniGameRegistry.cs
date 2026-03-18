using UnityEngine;
using System.Collections.Generic;

public class MiniGameRegistry : MonoBehaviour
{
    [System.Serializable]
    public class MiniGameEntry
    {
        public int miniGameID;
        public MonoBehaviour miniGameBehaviour;
    }

    [SerializeField] private List<MiniGameEntry> miniGames;

    public IMiniGame GetMiniGame(int id)
    {
        var entry = miniGames.Find(x => x.miniGameID == id);
        return entry?.miniGameBehaviour as IMiniGame;
    }
}