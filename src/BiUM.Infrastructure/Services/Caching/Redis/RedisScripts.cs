using StackExchange.Redis;

namespace BiUM.Infrastructure.Services.Caching.Redis;

public static class RedisScripts
{
    public static string RemoveIfEqualScript { get; } = @"
        local key = KEYS[1]
        local value = ARGV[1]

        if redis.call('get', key) == value then
            return redis.call('del', key)
        else
            return 0
        end";

    public static RedisResult RemoveIfEqual(IDatabase redis, RedisKey key, RedisValue value, CommandFlags flags = CommandFlags.None)
    {
        var result = (long)redis.ScriptEvaluate(RemoveIfEqualScript, [key], [value], flags);
        return result == 1 ? RedisResult.Create(1, ResultType.Integer) : RedisResult.Create(0, ResultType.Integer);
    }

    public static string ReplaceIfEqualScript { get; } = @"
        local key = KEYS[1]
        local value = ARGV[1]
        local expected = ARGV[2]
        local expires = ARGV[3]

        if redis.call('get', key) == expected then
            redis.call('set', key, value)
            if expires ~= '' then
                redis.call('pexpire', key, expires)
            end
            return 1
        else
            return 0
        end";

    public static RedisResult ReplaceIfEqual(IDatabase redis, RedisKey key, RedisValue value, RedisValue expected, object expires, CommandFlags flags = CommandFlags.None)
    {
        var result = (long)redis.ScriptEvaluate(ReplaceIfEqualScript, [key], [value, expected, (RedisValue)(expires?.ToString() ?? "")], flags);
        return result == 1 ? RedisResult.Create(1, ResultType.Integer) : RedisResult.Create(0, ResultType.Integer);
    }
}