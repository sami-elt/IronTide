using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class IronTidePlayerState
{
    public int PlayerId;
    public int Gold;
    public string DisplayName;
    public Color PlayerColor;
    public Sprite PlayerIcon;
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
    public const string CombatSceneName = "GameMap";

    private static readonly List<IronTidePlayerState> players = new List<IronTidePlayerState>();
    private static readonly Color[] defaultPlayerColors =
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow
    };

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
                Gold = 0,
                DisplayName = GetDefaultPlayerName(players.Count),
                PlayerColor = GetDefaultPlayerColor(players.Count)
            });
        }

        while (players.Count > playerCount)
            players.RemoveAt(players.Count - 1);

        for (int i = 0; i < players.Count; i++)
        {
            players[i].PlayerId = i;
            ApplyPlayerDefaults(players[i]);
        }
    }

    public static void ConfigurePlayers(IList<PlayerData> setupPlayers)
    {
        int playerCount = setupPlayers != null && setupPlayers.Count > 0 ? setupPlayers.Count : 1;
        EnsurePlayers(playerCount);

        for (int i = 0; i < players.Count; i++)
        {
            PlayerData setup = setupPlayers != null && i < setupPlayers.Count ? setupPlayers[i] : null;
            IronTidePlayerState player = players[i];

            player.DisplayName = setup != null && !string.IsNullOrWhiteSpace(setup.playerName)
                ? setup.playerName.Trim()
                : GetDefaultPlayerName(i);
            player.PlayerColor = setup != null && setup.playerColor.a > 0f
                ? setup.playerColor
                : GetDefaultPlayerColor(i);
            player.PlayerIcon = setup != null ? setup.icon : null;
        }
    }

    public static IronTidePlayerState GetPlayer(int playerId)
    {
        if (playerId < 0 || playerId >= players.Count)
            return null;

        return players[playerId];
    }

    public static string GetPlayerDisplayName(int playerId)
    {
        IronTidePlayerState player = GetPlayer(playerId);
        return player != null && !string.IsNullOrWhiteSpace(player.DisplayName)
            ? player.DisplayName
            : GetDefaultPlayerName(playerId);
    }

    public static Color GetPlayerColor(int playerId, Color fallback)
    {
        IronTidePlayerState player = GetPlayer(playerId);
        return player != null && player.PlayerColor.a > 0f ? player.PlayerColor : fallback;
    }

    public static Sprite GetPlayerIcon(int playerId)
    {
        IronTidePlayerState player = GetPlayer(playerId);
        return player != null ? player.PlayerIcon : null;
    }

    public static void RecordFirstKill(int killerPlayerId)
    {
        if (FirstKillOwnerId != -1)
            return;

        FirstKillOwnerId = killerPlayerId;
    }

    public static bool TryTransferGold(int fromPlayerId, int toPlayerId, int amount)
    {
        if (amount <= 0 || fromPlayerId == toPlayerId)
            return false;

        IronTidePlayerState source = GetPlayer(fromPlayerId);
        IronTidePlayerState destination = GetPlayer(toPlayerId);
        if (source == null || destination == null || source.Gold <= 0)
            return false;

        int transferred = Math.Min(amount, source.Gold);
        source.Gold -= transferred;
        destination.Gold += transferred;
        return transferred > 0;
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

    private static void ApplyPlayerDefaults(IronTidePlayerState player)
    {
        if (player == null)
            return;

        if (string.IsNullOrWhiteSpace(player.DisplayName))
            player.DisplayName = GetDefaultPlayerName(player.PlayerId);

        if (player.PlayerColor.a <= 0f)
            player.PlayerColor = GetDefaultPlayerColor(player.PlayerId);
    }

    private static string GetDefaultPlayerName(int playerId)
    {
        return $"Player {playerId + 1}";
    }

    private static Color GetDefaultPlayerColor(int playerId)
    {
        if (playerId >= 0 && playerId < defaultPlayerColors.Length)
            return defaultPlayerColors[playerId];

        return Color.white;
    }
}
