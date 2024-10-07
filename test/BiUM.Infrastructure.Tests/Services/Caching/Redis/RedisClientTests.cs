using BiUM.Core.Models.Caching.Redis;
using BiUM.Core.Caching.Redis;
using BiUM.Infrastructure.Services.Caching.Redis;
using BiUM.Core.Common.Configs;

namespace BiUM.Infrastructure.Tests.Services.Caching.Redis;

[TestFixture]
public class RedisClientTests
{
    private IRedisClient _redisClient;

    [SetUp]
    public void Setup()
    {
        //string ConnectionString = "localhost:6379";

        string ConnectionString = "38.242.253.6:30079,password=mypassword,ssl=False,abortConnect=False";
        var redisOptions = new RedisClientOptions(ConnectionString)
        {
            Enable = true,
            DefaultCacheTimeout = TimeSpan.FromSeconds(30000)
        };
        _redisClient = new RedisClient(redisOptions);
    }

    [Test]
    public async Task AddAsync_ShouldAddValueToCache()
    {
        // Arrange
        var expiresIn = TimeSpan.FromSeconds(60);
        var key = "AddAsync_ShouldAddValueToCache_Key";
        var value = new CacheItem<string>("Test Object", expiresIn, true);

        // Act
        var result = await _redisClient.AddAsync(key, value, expiresIn);

        // Assert
        Assert.That(result, Is.True, "AddAsync returned false");
        var cacheValue = await _redisClient.GetAsync<CacheItem<string>>(key);
        Assert.That(cacheValue, Is.Not.Null, "Cache value is null");
        Assert.That(cacheValue.Value!.Value, Is.EqualTo(value.Value), "Cache value is not correct");
        // Assert.IsTrue(cacheValue.Value.ExpireIn > TimeSpan.FromTicks(DateTime.UtcNow.AddSeconds(59).Ticks)
        //               && cacheValue.Value.ExpireIn < TimeSpan.FromTicks(DateTime.UtcNow.AddSeconds(61).Ticks),
        //     "Expiration time is not correct");
    }

    [Test]
    public void AddAsync_ShouldThrowArgumentNullException_WhenKeyIsNull()
    {
        // Arrange
        var expiresIn = TimeSpan.FromSeconds(10);
        string? key = null;
        var value = new CacheItem<string>("Test Object", expiresIn, true);

        // Act + Assert
        var e = Assert.ThrowsAsync<ArgumentNullException>(async () => await _redisClient.AddAsync(key, value, expiresIn));
        Assert.That(e!.ParamName, Is.EqualTo("key"), "Parameter name is not correct");
    }

    [Test]
    public void AddAsync_ShouldThrowArgumentNullException_WhenValueIsNull()
    {
        // Arrange
        var key = "AddAsync_ShouldThrowArgumentNullException_WhenValueIsNull_Key";
        CacheItem<string>? value = null;
        var expiresIn = TimeSpan.FromSeconds(10);

        // Act + Assert
        var e = Assert.ThrowsAsync<ArgumentNullException>(async () => await _redisClient.AddAsync(key, value, expiresIn));
        Assert.That(e!.ParamName, Is.EqualTo("value"), "Parameter name is not correct");
    }

    [Test]
    public async Task AddAsync_ShouldRemoveKey_WhenExpiresInIsNegative()
    {
        // Arrange
        var expiresIn = TimeSpan.FromSeconds(-10);
        var key = "AddAsync_ShouldRemoveKey_WhenExpiresInIsNegative_Key";
        var value = new CacheItem<string>("Test Object", expiresIn, true);
        await _redisClient.AddAsync(key, value, expiresIn);

        // Act
        var result = await _redisClient.AddAsync(key, value, expiresIn);

        // Assert
        Assert.IsFalse(result, "AddAsync returned true");
        var cacheValue = await _redisClient.GetAsync<CacheItem<string>>(key);
        Assert.IsNull(cacheValue.Value, "Cache value is not null");
    }

    [Test]
    public async Task GetAsync_ShouldReturnCachedValue()
    {
        // Arrange
        var expiresIn = TimeSpan.FromSeconds(60);
        var key = "GetAsync_ShouldReturnCachedValue_Key";
        var value = new CacheItem<string>("Test Object 1", expiresIn, true);
        var expectedValue = new CacheItem<string>("Test Object 1");

        // Act
        await _redisClient.AddAsync(key, value, expiresIn);
        var result = await _redisClient.GetAsync<CacheItem<string>>(key);

        // Assert
        Assert.That(result.Value!.Value, Is.EqualTo(expectedValue.Value));
    }

    [Test]
    public void GetAsync_WithInvalidKey_ThrowsException()
    {
        // Arrange
        var key = "";

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _redisClient.GetAsync<CacheItem<string>>(key));
    }

    [Test]
    public async Task GetAsync_WithNonExistentKey_ReturnsNull()
    {
        // Arrange
        var key = "GetAsync_WithNonExistentKey_ReturnsNull_Key";

        // Act
        var result = await _redisClient.GetAsync<CacheItem<string>>(key);

        // Assert
        Assert.That(result.Value, Is.Null);
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllValues()
    {
        // Arrange
        var key1 = "GetAllAsync_ShouldReturnAllValues_Key1";
        var value1 = new CacheItem<string>("Test Object 1");
        var key2 = "GetAllAsync_ShouldReturnAllValues_Key2";
        var value2 = new CacheItem<string>("Test Object 2");

        await _redisClient.AddAsync(key1, value1);
        await _redisClient.AddAsync(key2, value2);

        // Act
        var result = await _redisClient.GetAllAsync<CacheItem<string>>(new[] { key1, key2 });

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(result.ContainsKey(key1), Is.True);
            Assert.That(result.ContainsKey(key2), Is.True);
            Assert.That(result[key1].Value!.Value, Is.EqualTo(value1.Value));
            Assert.That(result[key2].Value!.Value, Is.EqualTo(value2.Value));
        });
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnEmptyDictionary_WhenKeysDoNotExist()
    {
        // Arrange
        var keys = new[] { "non-existent-key1", "non-existent-key2" };

        // Act
        var result = await _redisClient.GetAllAsync<CacheItem<string>>(keys);

        // Assert
        var cacheItems = result.Select(x => x.Value).Where(x => x is null).Count();
        Assert.That(cacheItems, Is.EqualTo(0));
    }

    [Test]
    public void GetAllAsync_ShouldThrowArgumentNullException_WhenKeysIsNull()
    {
        // Arrange
        IEnumerable<string>? keys = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _redisClient.GetAllAsync<CacheItem<string>>(keys!));
    }


    [Test]
    public async Task ExistsAsync_ShouldReturnTrue_WhenKeyExists()
    {
        // Arrange
        string key = "ExistsAsync_ShouldReturnTrue_WhenKeyExists";
        await _redisClient.AddAsync(key, "value");

        // Act
        bool result = await _redisClient.ExistsAsync(key);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ExistsAsync_ShouldReturnFalse_WhenKeyDoesNotExist()
    {
        // Arrange
        string key = "ExistsAsync_ShouldReturnFalse_WhenKeyDoesNotExist";

        // Act
        bool result = await _redisClient.ExistsAsync(key);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ExistsAsync_ShouldThrowArgumentNullException_WhenKeyIsNull()
    {
        // Arrange
        string? key = null;

        // Act + Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _redisClient.ExistsAsync(key!));
    }

    [Test]
    public void ExistsAsync_ShouldThrowArgumentNullException_WhenKeyIsEmpty()
    {
        // Arrange
        string key = "";

        // Act + Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _redisClient.ExistsAsync(key));
    }

    [Test]
    public async Task GetExpirationAsync_KeyExists_ReturnsExpiration()
    {
        // Arrange
        var key = "test_key";
        var value = "test_value";
        await _redisClient.AddAsync(key, value);

        // Act
        var expiration = await _redisClient.GetExprationAsync(key);

        // Assert
        Assert.IsNotNull(expiration);
        Assert.Greater(expiration.Value, TimeSpan.Zero);
    }

    [Test]
    public async Task GetExpirationAsync_KeyDoesNotExist_ReturnsNull()
    {
        // Arrange
        var key = "test_key";

        // Act
        var expiration = await _redisClient.GetExprationAsync(key);

        // Assert
        Assert.IsNull(expiration);
    }

    [Test]
    public void GetExpirationAsync_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        string? key = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(() => _redisClient.GetExprationAsync(key!));
    }

    [Test]
    public async Task SetExprationAsync_ShouldReturnTrue_WhenKeyExistsAndExprationSet()
    {
        // Arrange
        var key = "key1";
        await _redisClient.AddAsync(key, "value1");

        // Act
        var result = await _redisClient.SetExprationAsync(key, TimeSpan.FromMinutes(1));

        // Assert
        Assert.That(result, Is.True);
        Assert.That(await _redisClient.ExistsAsync(key), Is.True);
        Assert.That((await _redisClient.GetExprationAsync(key)).Value.TotalMinutes > 0, Is.True);
    }

    [Test]
    public async Task SetExprationAsync_ShouldReturnFalse_WhenKeyDoesNotExist()
    {
        // Arrange
        var key = "non_existent_key";

        // Act
        var result = await _redisClient.SetExprationAsync(key, TimeSpan.FromMinutes(1));

        // Assert
        Assert.That(result, Is.False);
        Assert.That(await _redisClient.ExistsAsync(key), Is.False);
    }

    [Test]
    public async Task SetExprationAsync_ShouldRemoveKey_WhenNegativeExprationSet()
    {
        // Arrange
        var key = "key1";
        await _redisClient.AddAsync(key, "value1");

        // Act
        var result = await _redisClient.SetExprationAsync(key, TimeSpan.FromMinutes(-1));

        // Assert
        Assert.That(result, Is.True);
        Assert.That(await _redisClient.ExistsAsync(key), Is.False);
    }

    [Test]
    public void SetExprationAsync_ShouldThrowArgumentNullException_WhenKeyIsNull()
    {
        // Arrange
        string? key = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _redisClient.SetExprationAsync(key!, TimeSpan.FromMinutes(1)));
    }

    [Test]
    public void SetExprationAsync_ShouldThrowArgumentNullException_WhenKeyIsEmpty()
    {
        // Arrange
        var key = string.Empty;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _redisClient.SetExprationAsync(key, TimeSpan.FromMinutes(1)));
    }

    [Test]
    public async Task RemoveAsync_RemovesExistingKey_ReturnsTrue()
    {
        // Arrange
        string key = "testkey";
        await _redisClient.AddAsync(key, "testvalue");

        // Act
        bool result = await _redisClient.RemoveAsync(key);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(await _redisClient.ExistsAsync(key), Is.False);
    }

    [Test]
    public async Task RemoveAsync_RemovesNonExistingKey_ReturnsFalse()
    {
        // Arrange
        string key = "testkey";

        // Act
        bool result = await _redisClient.RemoveAsync(key);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(await _redisClient.ExistsAsync(key), Is.False);
    }

    [Test]
    public void RemoveAsync_NullKey_ThrowsArgumentException()
    {
        // Arrange
        string? key = null;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => _redisClient.RemoveAsync(key!));
    }

    [Test]
    public async Task RemoveAllAsync_ShouldRemoveAllKeysInDatabase_WhenCalledWithNoKeys()
    {
        // Arrange
        await _redisClient.AddAsync("key1", "value1");
        await _redisClient.AddAsync("key2", "value2");

        // Act
        int result = await _redisClient.RemoveAllAsync<int>();

        // Assert
        Assert.That(result, Is.EqualTo(2));
        Assert.That(await _redisClient.ExistsAsync("key1"), Is.False);
        Assert.That(await _redisClient.ExistsAsync("key2"), Is.False);
    }

    [Test]
    public async Task RemoveAllAsync_ShouldRemoveSpecifiedKeys_WhenCalledWithKeys()
    {
        // Arrange
        await _redisClient.AddAsync("key1", "value1");
        await _redisClient.AddAsync("key2", "value2");
        await _redisClient.AddAsync("key3", "value3");

        // Act
        int result = await _redisClient.RemoveAllAsync<int>(new[] { "key1", "key2" });

        // Assert
        Assert.That(result, Is.EqualTo(2));
        Assert.That(await _redisClient.ExistsAsync("key1"), Is.False);
        Assert.That(await _redisClient.ExistsAsync("key2"), Is.False);
        Assert.That(await _redisClient.ExistsAsync("key3"), Is.True);
    }

    [Test]
    public async Task RemoveAllAsync_ShouldReturnZero_WhenCalledWithNonexistentKeys()
    {
        // Arrange

        // Act
        int result = await _redisClient.RemoveAllAsync<int>(new[] { "nonexistentkey" });

        // Assert
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void ReplaceAsync_KeyIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        string? key = null;
        string value = "value";

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(() => _redisClient.ReplaceAsync(key!, value));
    }

    [Test]
    public void ReplaceAsync_KeyIsEmpty_ThrowsArgumentNullException()
    {
        // Arrange
        string key = "";
        string value = "value";

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(() => _redisClient.ReplaceAsync(key, value));
    }

    [Test]
    public async Task ReplaceAsync_WithDefaultExpiration_SetsValueInCache()
    {
        // Arrange
        string key = "key";
        var value = new CacheItem<string>("Test Object", true);

        // Act
        var result = await _redisClient.ReplaceAsync(key, value);

        // Assert
        Assert.That(result, Is.True);
        var cachedValue = await _redisClient.GetAsync<CacheItem<string>>(key);
        Assert.That(cachedValue.Value!.Value, Is.EqualTo(value.Value));
    }

    [Test]
    public async Task ReplaceAsync_WithCustomExpiration_SetsValueInCacheWithExpiration()
    {
        // Arrange
        string key = "key";
        var value = new CacheItem<string>("Test Object", true);
        TimeSpan expiresIn = TimeSpan.FromSeconds(10);

        // Act
        var result = await _redisClient.ReplaceAsync(key, value, expiresIn);

        // Assert
        Assert.That(result, Is.True);
        var cachedValue = await _redisClient.GetAsync<CacheItem<string>>(key);
        Assert.That(cachedValue.Value!.Value, Is.EqualTo(value.Value));

        // Wait for expiration
        await Task.Delay(expiresIn);
        cachedValue = await _redisClient.GetAsync<CacheItem<string>>(key);
        Assert.That(cachedValue.Value, Is.Null);
    }

    [TearDown]
    public void TearDown()
    {
        _redisClient.RemoveAllAsync<int>().Wait();
    }
}