using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class IronTidePlayerState
{
    public int PlayerId;
    public int Gold;
    public string WeaponModuleId;
    public string ArmorModuleId;
    public string EngineModuleId;
}

public static class IronTideGameState
{
    public const int BaseShopGold = 10;
    public const int FirstKillBonus = 5;
    public const int WinnerBonus = 10;
    public const string ShoppingSceneName = "Shopping Phase";
    public const string CombatSceneName = "TestDay1Play";

    private static readonly List<IronTidePlayerState> players = new List<IronTidePlayerState>();

    public static IReadOnlyList<IronTidePlayerState> Players => players;
    public static int CombatRound { get; private set; } = 1;
    public static int FirstKillOwnerId { get; private set; } = -1;
    public static int RoundWinnerId { get; private set; } = -1;
    public static bool HasSavedLoadouts { get; private set; }
    public static bool ShouldOpenShopAfterCombat => CombatRound == 1;

    public static void EnsurePlayers(int playerCount)
    {
        if (playerCount < 1)
            playerCount = 1;

        while (players.Count < playerCount)
        {
            players.Add(new IronTidePlayerState
            {
                PlayerId = players.Count,
                Gold = 0
            });
        }

        while (players.Count > playerCount)
            players.RemoveAt(players.Count - 1);

        for (int i = 0; i < players.Count; i++)
            players[i].PlayerId = i;
    }

    public static IronTidePlayerState GetPlayer(int playerId)
    {
        if (playerId < 0 || playerId >= players.Count)
            return null;

        return players[playerId];
    }

    public static void RecordFirstKill(int killerPlayerId)
    {
        if (FirstKillOwnerId != -1)
            return;

        FirstKillOwnerId = killerPlayerId;
    }

    public static void AwardShopGold(int winnerPlayerId)
    {
        RoundWinnerId = winnerPlayerId;

        foreach (IronTidePlayerState player in players)
            player.Gold += BaseShopGold;

        IronTidePlayerState firstKiller = GetPlayer(FirstKillOwnerId);
        if (firstKiller != null)
            firstKiller.Gold += FirstKillBonus;

        IronTidePlayerState winner = GetPlayer(winnerPlayerId);
        if (winner != null)
            winner.Gold += WinnerBonus;
    }

    public static void SaveLoadouts(IList<Ship> ships)
    {
        if (ships == null)
            return;

        EnsurePlayers(ships.Count);

        for (int i = 0; i < ships.Count; i++)
        {
            Ship ship = ships[i];
            if (ship == null || ship.shipInfo == null)
                continue;

            IronTidePlayerState player = players[i];
            player.WeaponModuleId = GetCardId(ship.shipInfo.WeaponModule);
            player.ArmorModuleId = GetCardId(ship.shipInfo.ArmorModule);
            player.EngineModuleId = GetCardId(ship.shipInfo.EngineModule);
        }

        HasSavedLoadouts = true;
    }

    public static void ApplyLoadoutToShip(Ship ship, IronTidePlayerState player, IronTide.BasicCards.IronTideModuleCardLibrary library)
    {
        if (ship == null || ship.shipInfo == null || player == null || library == null)
            return;

        ship.shipInfo.SetWeaponModule(library.FindById(player.WeaponModuleId));
        ship.shipInfo.SetArmorModule(library.FindById(player.ArmorModuleId));
        ship.shipInfo.SetEngineModule(library.FindById(player.EngineModuleId));
        ship.shipInfo.ResetValues();
    }

    public static void UpdatePlayerLoadout(int playerId, IronTide.BasicCards.IronTideModuleCardEntry weapon,
        IronTide.BasicCards.IronTideModuleCardEntry armor, IronTide.BasicCards.IronTideModuleCardEntry engine)
    {
        IronTidePlayerState player = GetPlayer(playerId);
        if (player == null)
            return;

        player.WeaponModuleId = GetCardId(weapon);
        player.ArmorModuleId = GetCardId(armor);
        player.EngineModuleId = GetCardId(engine);
        HasSavedLoadouts = true;
    }

    public static void CompleteShopping()
    {
        CombatRound = 2;
        FirstKillOwnerId = -1;
        RoundWinnerId = -1;
    }

    public static void ResetAll()
    {
        players.Clear();
        CombatRound = 1;
        FirstKillOwnerId = -1;
        RoundWinnerId = -1;
        HasSavedLoadouts = false;
    }

    private static string GetCardId(IronTide.BasicCards.IronTideModuleCardEntry card)
    {
        return card != null && card.IsValid ? card.Id : string.Empty;
    }
}
