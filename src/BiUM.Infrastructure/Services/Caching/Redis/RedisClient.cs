using BiUM.Core.Caching.Redis;
using BiUM.Core.Common.Configs;
using BiUM.Core.Common.Utils;
using BiUM.Core.Models.Caching.Redis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace BiUM.Infrastructure.Services.Caching.Redis;

public class RedisClient : IRedisClient
{
    private readonly AsyncLock _lock = new();
    private bool _scriptsLoaded;
    private LoadedLuaScript? _removeIfEqual;
    private LoadedLuaScript? _replaceIfEqual;

    private readonly IConnectionMultiplexer _connection;
    private readonly IDatabase _database;
    private readonly RedisClientOptions _redisClientOptions;
    private readonly ILogger<RedisClient> _logger;
    private readonly TimeSpan? _defaultCacheTimeout;

    public RedisClient(IOptions<RedisClientOptions> redisClientOptions, ILogger<RedisClient> logger)
    {
        _redisClientOptions = redisClientOptions.Value;
        _logger = logger;

        if (_redisClientOptions is null || !_redisClientOptions.Enable)
        {
            return;
        }

        var configurationOptions = ConfigurationOptions.Parse(_redisClientOptions.ConnectionString ?? "");
        configurationOptions.AllowAdmin = true;

        _connection = ConnectionMultiplexer.Connect(configurationOptions);
        _database = _connection.GetDatabase();
        _defaultCacheTimeout = _redisClientOptions.DefaultCacheTimeout;
    }

    /// <summary>
    /// The GetAsync method retrieves a cache item with a specific key from Redis database asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the cache item value.</typeparam>
    /// <param name="key">A string that represents the unique key of the cache item.</param>
    /// <returns>
    /// A Task object representing an asynchronous operation that returns a CacheItem<T> object if the cache item exists; otherwise, null.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if the key parameter is null or an empty string.</exception>
    public async Task<CacheItem<T>> GetAsync<T>(string key)
    {
        if (String.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");
        }

        var redisValue = await _database.StringGetAsync(key, CommandFlags.PreferReplica);

        return RedisValueExtensions.RedisValueToCacheValue<T>(redisValue);
    }

    /// <summary>
    /// The GetAllAsync method retrieves multiple cache items with the specified keys from Redis database asynchronously. It takes in an IEnumerable<string> keys parameter that represents the unique identifiers of the cache items being retrieved. The method returns a Task<IDictionary<string, CacheItem<T>>>, where T is the type of the cache item value, containing the cache items that exist in the database.
    /// </summary>
    /// <typeparam name="T">The type of the cache item value.</typeparam>
    /// <param name="keys">An IEnumerable<string> representing the unique keys of the cache items.</param>
    /// <returns>A Task object representing an asynchronous operation that returns a dictionary containing the cache items that exist in the database.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the keys parameter is null.</exception>
    public async Task<IDictionary<string, CacheItem<T>>> GetAllAsync<T>(IEnumerable<string> keys)
    {
        var keyArray = keys.ToArray();
        var values = await _database.StringGetAsync(keyArray.Select(k => (RedisKey)k).ToArray(), CommandFlags.PreferReplica);

        var result = new Dictionary<string, CacheItem<T>>();

        for (var i = 0; i < keyArray.Length; i++)
        {
            result.Add(keyArray[i], RedisValueExtensions.RedisValueToCacheValue<T>(values[i]));
        }

        return result;
    }

    /// <summary>
    /// The AddAsync method adds a cache item to the Redis database asynchronously. It takes in a string key parameter which represents the unique identifier of the cache item, a generic T value which represents the cache item value, and an optional TimeSpan expiresIn parameter that represents the cache item expiration time. The method returns a Task<bool>, indicating whether the cache item was added successfully or not.
    /// </summary>
    /// <typeparam name="T">The type of the cache item value.</typeparam>
    /// <param name="key">A string that represents the unique key of the cache item.</param>
    /// <param name="value">A generic T value that represents the cache item value.</param>
    /// <param name="expiresIn">An optional TimeSpan parameter that represents the cache item expiration time. Default is null.</param>
    /// <returns>A Task object representing an asynchronous operation that returns a boolean value indicating whether the cache item was added successfully or not.</returns>
    /// <exceptions>
    /// <exception cref="ArgumentNullException">Thrown if the key parameter is null or an empty string or the value parameter is null.</exception>
    /// </exceptions>
    public async Task<bool> AddAsync<T>(string key, T value, TimeSpan? expiresIn = null)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");
        }

        if (value is null)
        {
            throw new ArgumentNullException(nameof(value), "Value cannot be null or empty.");
        }

        if (expiresIn?.Ticks < 0)
        {
            await this.RemoveAsync(key);

            return false;
        }

        if (!expiresIn.HasValue)
        {
            if (_defaultCacheTimeout > TimeSpan.FromMilliseconds(0))
            {
                expiresIn = _defaultCacheTimeout;
            }
            else
            {
                expiresIn = TimeSpan.FromTicks(DateTime.UtcNow.AddMinutes(1).Ticks);
            }
        }

        return await _database.StringSetAsync(key, JsonSerializer.Serialize(value), expiresIn, When.Always, CommandFlags.None);
    }

    /// <summary>
    /// The ExistsAsync method checks whether a cache item with a specific key exists in Redis database asynchronously. It takes in a string key parameter which represents the unique identifier of the cache item that is being checked for existence. The method returns a Task<bool> that indicates whether the cache item exists (true) or not (false).
    /// </summary>
    /// <param name="key">A string that represents the unique key of the cache item.</param>
    /// <returns>A Task object representing an asynchronous operation that returns a boolean value indicating whether the cache item exists (true) or not (false).</returns>
    /// <exceptions>ArgumentNullException: Thrown if the key parameter is null or an empty string.</exceptions>
    public Task<bool> ExistsAsync(string key)
    {
        if (String.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");
        }

        return _database.KeyExistsAsync(key);
    }

    /// <summary>
    /// The GetExpirationAsync method retrieves the expiration time of a cache item with a specific key from Redis database asynchronously. It takes in a string key parameter which represents the unique identifier of the cache item that is being queried. The method returns a Task<TimeSpan?>, which represents the expiration time of the cache item if it exists, and null if the cache item does not exist or it does not have an expiration time.
    /// </summary>
    /// <param name="key">A string that represents the unique key of the cache item.</param>
    /// <returns>A Task object representing an asynchronous operation that returns a TimeSpan? object, which represents the expiration time of the cache item if it exists, and null if the cache item does not exist or it does not have an expiration time.</returns>
    /// <exceptions>ArgumentNullException: Thrown if the key parameter is null or an empty string.</exceptions>
    public async Task<TimeSpan?> GetExpirationAsync(string key)
    {
        if (String.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");
        }

        return await _database.KeyTimeToLiveAsync(key);
    }

    /// <summary>
    /// The RemoveAllAsync method removes all cache items from the Redis database asynchronously. It takes an optional IEnumerable<string> keys parameter that represents the specific cache items to remove. If the keys parameter is not specified, all cache items in the database will be removed.
    /// </summary>
    /// <typeparam name="T">The type of the cache items being removed.</typeparam>
    /// <param name="keys">An optional IEnumerable of strings that represent the keys of the cache items to be removed. If null, all cache items in the database will be removed.</param>
    /// <returns>A Task object representing an asynchronous operation that returns the number of cache items removed from the database.</returns>
    /// <remarks>If the database connection is lost during the operation, some cache items may not be removed. To ensure that all cache items are removed, it is recommended to retry the operation if necessary.</remarks>
    public async Task<int> RemoveAllAsync<T>(IEnumerable<string>? keys = null)
    {
        var keyCount = 0;

        if (keys == null)
        {
            var endpoints = _connection.GetEndPoints();

            if (endpoints.Length == 0)
            {
                return 0;
            }

            foreach (var endpoint in endpoints)
            {
                var server = _connection.GetServer(endpoint);

                if (server.IsReplica)
                {
                    continue;
                }

                try
                {
                    await foreach (var key in server.KeysAsync(pattern: "*"))
                    {
                        await _database.KeyDeleteAsync(key);

                        keyCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error clearing redis database");
                }
            }
        }
        else
        {
            var redisKeys = keys.Where(k => !String.IsNullOrEmpty(k)).Select(k => (RedisKey)k).ToArray();

            if (redisKeys.Length > 0)
            {
                return (int)await _database.KeyDeleteAsync(redisKeys);
            }
        }

        return keyCount;
    }

    /// <summary>
    /// The RemoveAsync method deletes a cache item with a specific key from Redis database asynchronously. It takes in a string key parameter which represents the unique identifier of the cache item that is being deleted. The method returns a Task<bool>, which completes with a value of True if the cache item is successfully deleted; otherwise, False.
    /// </summary>
    /// <param name="key">A string that represents the unique key of the cache item.</param>
    /// <returns>A Task object representing an asynchronous operation that returns a Boolean value indicating whether the cache item is successfully deleted from the Redis database.</returns>
    /// <exceptions>ArgumentNullException: Thrown if the key parameter is null or an empty string.</exceptions>
    public Task<bool> RemoveAsync(string key)
    {
        return _database.KeyDeleteAsync(key);
    }

    public async Task<bool> RemoveIfEqualsAsync<T>(string key, T expected)
    {
        if (String.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");
        }

        await LoadScriptsAsync();

        var expectedValue = expected.ToRedisValue();
        var redisResult = await _database.ScriptEvaluateAsync(_removeIfEqual!, new { key = (RedisKey)key, expected = expectedValue });
        var result = (int)redisResult;

        return result > 0;
    }

    /// <summary>
    /// The ReplaceAsync method updates the value of an existing cache item with a specific key or inserts a new cache item with the specified key and value into the Redis database asynchronously. It takes in a string key parameter which represents the unique identifier of the cache item being updated or inserted, a generic T value parameter which represents the value of the cache item, and an optional TimeSpan expiresIn parameter which represents the expiration time of the cache item. If the cache item already exists, the method updates the value of the cache item with the specified key; otherwise, it inserts a new cache item with the specified key and value. The method returns a Task<bool> representing an asynchronous operation that returns true if the operation succeeded; otherwise, false.
    /// </summary>
    /// <typeparam name="T">The type of the cache item value.</typeparam>
    /// <param name="key">A string that represents the unique key of the cache item.</param>
    /// <param name="value">The value of the cache item to be inserted or updated.</param>
    /// <param name="expiresIn">An optional TimeSpan parameter that specifies the expiration time of the cache item. If null, the default cache timeout value is used. If there is no default cache timeout value, a default of one minute is used.</param>
    /// <returns>A Task object representing an asynchronous operation that returns a boolean value indicating if the operation was successful or not.</returns>
    /// <exceptions>ArgumentNullException: Thrown if the key parameter is null or an empty string.</exceptions>
    public Task<bool> ReplaceAsync<T>(string key, T value, TimeSpan? expiresIn = null)
    {
        if (String.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");
        }

        if (!expiresIn.HasValue)
        {
            if (_defaultCacheTimeout > TimeSpan.FromMilliseconds(0))
            {
                expiresIn = _defaultCacheTimeout;
            }
            else
            {
                expiresIn = TimeSpan.FromTicks(DateTime.UtcNow.AddMinutes(1).Ticks);
            }
        }

        return _database.StringSetAsync(key, JsonSerializer.Serialize(value), expiresIn, When.Always, CommandFlags.None);
    }

    public async Task<bool> ReplaceIfEqualsAsync<T>(string key, T value, T expected, TimeSpan? expiresIn = null)
    {
        if (String.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");
        }

        await LoadScriptsAsync();

        var redisValue = value.ToRedisValue();
        var expectedValue = expected.ToRedisValue();
        RedisResult redisResult;

        if (expiresIn.HasValue)
        {
            redisResult = await _database.ScriptEvaluateAsync(_replaceIfEqual!, new { key = (RedisKey)key, value = redisValue, expected = expectedValue, expires = (int)expiresIn.Value.TotalMilliseconds });
        }
        else
        {
            redisResult = await _database.ScriptEvaluateAsync(_replaceIfEqual!, new { key = (RedisKey)key, value = redisValue, expected = expectedValue, expires = "" });
        }

        var result = (int)redisResult;

        return result > 0;
    }

    /// <summary>
    /// The SetExpirationAsync method sets an expiration time on a cache item with a specific key in Redis database asynchronously. It takes in a string key parameter which represents the unique identifier of the cache item, and a TimeSpan expiresIn parameter that specifies the amount of time for the cache item to be valid. If the value of expiresIn parameter is negative, the cache item will be removed from the cache. The method returns a Task<bool> object representing an asynchronous operation that returns true if the expiration time was set successfully; otherwise, false.
    /// </summary>
    /// <param name="key">A string that represents the unique key of the cache item.</param>
    /// <param name="expiresIn">A TimeSpan that specifies the amount of time for the cache item to be valid.</param>
    /// <returns>A Task object representing an asynchronous operation that returns true if the expiration time was set successfully; otherwise, false.</returns>
    /// <exceptions>ArgumentNullException: Thrown if the key parameter is null or an empty string.</exceptions>
    public Task<bool> SetExpirationAsync(string key, TimeSpan expiresIn)
    {
        if (String.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key), "Key cannot be null or empty.");
        }

        if (expiresIn.Ticks < 0)
        {
            return this.RemoveAsync(key);
        }

        return _database.KeyExpireAsync(key, expiresIn);
    }

    private async Task LoadScriptsAsync()
    {
        if (_scriptsLoaded)
        {
            return;
        }

        using (await _lock.LockAsync())
        {
            if (_scriptsLoaded)
            {
                return;
            }

            var removeIfEqual = LuaScript.Prepare(RedisScripts.RemoveIfEqualScript);
            var replaceIfEqual = LuaScript.Prepare(RedisScripts.ReplaceIfEqualScript); // Fixed: Correct script for ReplaceIfEqual

            foreach (var endpoint in _connection.GetEndPoints())
            {
                var server = _connection.GetServer(endpoint);

                if (server.IsReplica)
                {
                    continue;
                }

                _removeIfEqual = await removeIfEqual.LoadAsync(server);
                _replaceIfEqual = await replaceIfEqual.LoadAsync(server);
            }

            _scriptsLoaded = true;
        }
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}