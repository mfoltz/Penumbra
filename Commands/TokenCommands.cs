using Penumbra.Services;
using ProjectM;
using Stunlock.Core;
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

    [Command(name: "redeemtokens", shortHand: "rt", adminOnly: false, usage: ".pen rt", description: "Redeems Sanguis.")]
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

    [Command(name: "getdaily", shortHand: "gd", adminOnly: false, usage: ".pen gd", description: "Checks or awards daily login reward.")]
    public static void GetDailyCommand(ChatCommandContext ctx)
    {
        if (!_daily)
        {
            ctx.Reply("<color=#CBC3E3>Daily</color> reward is currently disabled.");
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

    /*
    [Command(name: "redeemtokens", shortHand: "rt", adminOnly: false, usage: ".pen rt", description: "Redeems Sanguis.")]
    public static void RedeemSanguisCommand(ChatCommandContext ctx)
    {
        if (!_tokens)
        {
            ctx.Reply("<color=red>Sanguis</color> are currently disabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (Core.DataStructures.PlayerTokens.TryGetValue(steamId, out var tokenData))
        {
            if (tokenData.Tokens < _tokenRatio)
            {
                ctx.Reply($"You don't have enough <color=red>Sanguis</color> to redeem. (<color=#FFC0CB>{_tokenRatio}</color> minimum)");
                return;
            }

            int rewards = tokenData.Tokens / _tokenRatio;
            int cost = rewards * _tokenRatio;
            
            if (Core.ServerGameManager.TryAddInventoryItem(ctx.Event.SenderCharacterEntity, _tokensConfig.TokenItem, rewards))
            {
                tokenData = new(tokenData.Tokens - cost, tokenData.TimeData);

                Core.DataStructures.PlayerTokens[steamId] = tokenData;
                Core.DataStructures.SavePlayerTokens();

                ctx.Reply($"You've received <color=#00FFFF>{_tokenItem.GetLocalizedName()}</color>x<color=white>{rewards}</color> for redeeming <color=#FFC0CB>{cost}</color> <color=red>Sanguis</color>!");

            }
            else
            {
                tokenData = new(tokenData.Tokens - cost, tokenData.TimeData);
                Core.DataStructures.PlayerTokens[steamId] = tokenData;
                Core.DataStructures.SavePlayerTokens();

                InventoryUtilitiesServer.CreateDropItem(Core.EntityManager, ctx.Event.SenderCharacterEntity, _tokensConfig.TokenItem, rewards, new Entity());
                ctx.Reply($"You've received <color=#00FFFF>{_tokenItem.GetLocalizedName()}</color>x<color=white>{rewards}</color> for redeeming <color=#FFC0CB>{cost}</color> <color=red>Sanguis</color>! It dropped on the ground because your inventory was full.");
            }
        }
    }

    [Command(name: "gettokens", shortHand: "gt", adminOnly: false, usage: ".pen gt", description: "Shows and updates tokens.")]
    public static void GetSanguisCommand(ChatCommandContext ctx)
    {
        if (!_tokens)
        {
            ctx.Reply("<color=red>Sanguis</color> are currently disabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (Core.DataStructures.PlayerTokens.TryGetValue(steamId, out var tokenData))
        {
            TimeSpan timeOnline = DateTime.Now - tokenData.TimeData.Start;
            tokenData = new(tokenData.Tokens + timeOnline.Minutes * _tokenRate, new(DateTime.Now, tokenData.TimeData.DailyLogin));

            Core.DataStructures.PlayerTokens[steamId] = tokenData;
            Core.DataStructures.SavePlayerTokens();
            ctx.Reply($"You have <color=#FFC0CB>{tokenData.Tokens}</color> <color=red>Sanguis</color>.");
        }

    }
    
    [Command(name: "getdaily", shortHand: "gd", adminOnly: false, usage: ".pen gd", description: "Shows time left until daily login valid again or awards daily login if eligible without needing to log out/in.")]
    public static void GetDailyCommand(ChatCommandContext ctx)
    {
        if (!_daily)
        {
            ctx.Reply("<color=#CBC3E3>Daily</color> reward is currently disabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (Core.DataStructures.PlayerTokens.TryGetValue(steamId, out var tokenData))
        {
            DateTime lastDailyLogin = tokenData.TimeData.DailyLogin;
            DateTime nextEligibleLogin = lastDailyLogin.AddDays(1); // assuming daily login resets every 24 hours
            DateTime currentTime = DateTime.Now;

            if (currentTime >= nextEligibleLogin)
            {
                if (Core.ServerGameManager.TryAddInventoryItem(ctx.Event.SenderCharacterEntity, _dailyItem, _dailyQuantity))
                {
                    string message = $"You've received <color=#00FFFF>{_tokenItem.GetLocalizedName()}</color>x<color=white>{_dailyQuantity}</color> for logging in today!";

                    ctx.Reply(message);
                }
                else
                {
                    InventoryUtilitiesServer.CreateDropItem(Core.EntityManager, ctx.Event.SenderCharacterEntity, _dailyItem, _dailyQuantity, new Entity());
                    string message = $"You've received <color=#00FFFF>{_tokenItem.GetLocalizedName()}</color>x<color=white>{_dailyQuantity}</color> for logging in today! It dropped on the ground because your inventory was full.";

                    ctx.Reply(message);
                }

                tokenData = new(tokenData.Tokens, new(tokenData.TimeData.Start, DateTime.Now));
                Core.DataStructures.PlayerTokens[steamId] = tokenData;
                Core.DataStructures.SavePlayerTokens();
            }
            else
            {
                TimeSpan untilNextDaily = nextEligibleLogin - currentTime;
                string timeLeft = string.Format("{0:D2}:{1:D2}:{2:D2}",
                                                untilNextDaily.Hours,
                                                untilNextDaily.Minutes,
                                                untilNextDaily.Seconds);
                ctx.Reply($"Time until daily reward: <color=yellow>{timeLeft}</color>.");
            }
        }    
    }
    */
}