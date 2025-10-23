using Penumbra.Services;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using VampireCommandFramework;
using static Penumbra.Plugin;

namespace Penumbra.Commands;

[CommandGroup(name: "penumbra", "pen")]
public static class TokenCommands
{
    static readonly bool _tokens = _tokensConfig.TokenSystem;
    static readonly bool _daily = _tokensConfig.DailyLogin;

    static readonly PrefabGUID _tokenItem = _tokensConfig.TokenItem;
    static readonly PrefabGUID _dailyItem = _tokensConfig.DailyItem;

    static readonly int _tokenRatio = _tokensConfig.TokenRatio;
    static readonly int _dailyQuantity = _tokensConfig.DailyQuantity;

    const string TOKEN_NAME = "V$";

    [Command(name: "redeemtokens", shortHand: "rt", adminOnly: false, usage: ".pen rt", description: "Redeems tokens for configured item.")]
    public static void RedeemTokensCommand(ChatCommandContext ctx)
    {
        if (!_tokens)
        {
            ctx.Reply($"<color=red>{TOKEN_NAME}</color> are currently disabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        if (!TokenService.PlayerTokens.TryGetValue(steamId, out var tokenData)) return;

        if (tokenData.Tokens < _tokenRatio)
        {
            ctx.Reply($"You don't have enough <color=red>{TOKEN_NAME}</color> to redeem. (<color=#FFC0CB>{_tokenRatio}</color> minimum)");
            return;
        }

        int rewards = tokenData.Tokens / _tokenRatio;
        int cost = rewards * _tokenRatio;

        bool given = Core.ServerGameManager.TryAddInventoryItem(ctx.Event.SenderCharacterEntity, _tokenItem, rewards);

        if (!given)
        {
            InventoryUtilitiesServer.CreateDropItem(Core.EntityManager, ctx.Event.SenderCharacterEntity, _tokenItem, rewards, new Entity());
            ctx.Reply($"You've received <color=#00FFFF>{_tokenItem.GetLocalizedName()}</color>x<color=white>{rewards}</color> for redeeming <color=#FFC0CB>{cost}</color> <color=red>{TOKEN_NAME}</color>! It dropped on the ground because your inventory was full.");
        }
        else
        {
            ctx.Reply($"You've received <color=#00FFFF>{_tokenItem.GetLocalizedName()}</color>x<color=white>{rewards}</color> for redeeming <color=#FFC0CB>{cost}</color> <color=red>{TOKEN_NAME}</color>!");
        }

        tokenData.Tokens -= cost;
        steamId.UpdateAndSaveTokens(tokenData);
    }

    [Command(name: "gettokens", shortHand: "gt", adminOnly: false, usage: ".pen gt", description: "Shows and updates tokens.")]
    public static void GetTokensCommand(ChatCommandContext ctx)
    {
        if (!_tokens)
        {
            ctx.Reply($"<color=red>{TOKEN_NAME}</color> are currently disabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        if (!TokenService.PlayerTokens.TryGetValue(steamId, out var tokenData)) return;

        tokenData = TokenService.AccumulateTime(tokenData);
        steamId.UpdateAndSaveTokens(tokenData);

        ctx.Reply($"<color=red>{TOKEN_NAME}</color> - <color=#FFC0CB>{tokenData.Tokens}</color>");
    }


    [Command(name: "tradetokens", shortHand: "tt", adminOnly: false, usage: ".pen tt <player> <amount>", description: "Transfers tokens to another player.")]
    public static void TradeTokensCommand(ChatCommandContext ctx, string targetPlayerName, int amount)
    {
        if (!_tokens)
        {
            ctx.Reply($"<color=red>{TOKEN_NAME}</color> are currently disabled.");
            return;
        }

        if (amount <= 0)
        {
            ctx.Reply($"Enter a positive amount of <color=red>{TOKEN_NAME}</color> to trade.");
            return;
        }

        ulong senderSteamId = ctx.Event.User.PlatformId;

        if (!PlayerService.TryGetOnlinePlayerByName(targetPlayerName, out ulong recipientSteamId, out var recipientInfo))
        {
            ctx.Reply($"Could not find <color=white>{targetPlayerName}</color> online.");
            return;
        }

        if (recipientSteamId == senderSteamId)
        {
            ctx.Reply("You cannot transfer tokens to yourself.");
            return;
        }

        if (!TokenService.PlayerTokens.TryGetValue(senderSteamId, out var senderTokens))
        {
            senderSteamId.CreateTokens();
            if (!TokenService.PlayerTokens.TryGetValue(senderSteamId, out senderTokens))
            {
                ctx.Reply("Unable to load your token data. Please try again.");
                return;
            }
        }

        senderTokens = TokenService.AccumulateTime(senderTokens);

        if (!TokenService.PlayerTokens.TryGetValue(recipientSteamId, out var recipientTokens))
        {
            recipientSteamId.CreateTokens();
            if (!TokenService.PlayerTokens.TryGetValue(recipientSteamId, out recipientTokens))
            {
                senderSteamId.UpdateAndSaveTokens(senderTokens);
                ctx.Reply($"Unable to load <color=white>{recipientInfo.User.CharacterName.Value}</color>'s token data. Please try again later.");
                return;
            }
        }

        if (senderTokens.Tokens < amount)
        {
            senderSteamId.UpdateAndSaveTokens(senderTokens);
            ctx.Reply($"You do not have enough <color=red>{TOKEN_NAME}</color>. Balance: <color=#FFC0CB>{senderTokens.Tokens}</color>.");
            return;
        }

        var updatedSender = new TokenService.TokenBlob(senderTokens.Tokens - amount, senderTokens.TimeData);
        var updatedRecipient = new TokenService.TokenBlob(recipientTokens.Tokens + amount, recipientTokens.TimeData);

        TokenService.ApplyAndSaveTokenUpdates(new[]
        {
            (senderSteamId, updatedSender),
            (recipientSteamId, updatedRecipient)
        });

        string recipientName = recipientInfo.User.CharacterName.Value;
        ctx.Reply($"Sent <color=#FFC0CB>{amount}</color> <color=red>{TOKEN_NAME}</color> to <color=white>{recipientName}</color>. New balance: <color=#FFC0CB>{updatedSender.Tokens}</color>.");

        string senderName = ctx.Event.User.CharacterName.Value;
        FixedString512Bytes notification = new FixedString512Bytes($"{senderName} sent you <color=#FFC0CB>{amount}</color> <color=red>{TOKEN_NAME}</color>. New balance: <color=#FFC0CB>{updatedRecipient.Tokens}</color>.");
        ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, recipientInfo.User, ref notification);
    }

    [Command(name: "getdaily", shortHand: "gd", adminOnly: false, usage: ".pen gd", description: "Check time remaining or receive daily login reward if eligible.")]
    public static void GetDailyCommand(ChatCommandContext ctx)
    {
        if (!_daily)
        {
            ctx.Reply("<color=#CBC3E3>Daily</color> rewards are currently disabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        if (!TokenService.PlayerTokens.TryGetValue(steamId, out var tokenData)) return;

        if (!TokenService.IsEligibleForDaily(tokenData))
        {
            TimeSpan timeRemaining = TokenService.TimeUntilNextDaily(tokenData);
            string timeLeft = string.Format("{0:D2}:{1:D2}:{2:D2}",
                                timeRemaining.Hours,
                                timeRemaining.Minutes,
                                timeRemaining.Seconds);

            ctx.Reply($"Time until daily reward: <color=yellow>{timeLeft}</color>");
            return;
        }

        bool given = Core.ServerGameManager.TryAddInventoryItem(ctx.Event.SenderCharacterEntity, _dailyItem, _dailyQuantity);

        if (!given)
        {
            InventoryUtilitiesServer.CreateDropItem(Core.EntityManager, ctx.Event.SenderCharacterEntity, _dailyItem, _dailyQuantity, new Entity());
            ctx.Reply($"You've received <color=#00FFFF>{_dailyItem.GetLocalizedName()}</color>x<color=white>{_dailyQuantity}</color> for logging in today! It dropped on the ground because your inventory was full.");
        }
        else
        {
            ctx.Reply($"You've received <color=#00FFFF>{_dailyItem.GetLocalizedName()}</color>x<color=white>{_dailyQuantity}</color> for logging in today!");
        }

        tokenData.TimeData = new TokenService.TimeBlob(DateTime.UtcNow, DateTime.UtcNow);
        steamId.UpdateAndSaveTokens(tokenData);
    }
}