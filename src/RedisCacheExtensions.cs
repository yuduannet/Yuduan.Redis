using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Yuduan.Redis
{
    public static class RedisCacheExtensions
    {
        private static ISerializer _serializer;

        internal static ISerializer Serializer
        {
            get => _serializer ?? (_serializer = new JsonSerializer());
            set => _serializer = value;
        }

        /// <summary>
        /// 获取缓存中的对象，如果缓存不存在则调用func获取对象并缓存
        /// </summary>
        public static T Get<T>(this IDatabase database, string key, Func<T> acquire, TimeSpan? cacheTime = null)
        {
            if (database.KeyExists(key))
                return Serializer.Deserialize<T>(database.StringGet(key));
            var result = acquire();
            if (result == null)
                cacheTime = TimeSpan.FromSeconds(10);
            database.StringSet(key, Serializer.Serialize(result), cacheTime);
            return result;
        }

        /// <summary>
        /// 获取缓存中的对象，如果缓存不存在则调用func获取对象并缓存
        /// </summary>

        public static async Task<T> GetAsync<T>(this IDatabaseAsync database, string key, Func<Task<T>> acquire, TimeSpan? cacheTime = null)
        {
            if (await database.KeyExistsAsync(key))
                return Serializer.Deserialize<T>(await database.StringGetAsync(key));
            var result = await acquire();
            if (result == null)
                cacheTime = TimeSpan.FromSeconds(10);
            await database.StringSetAsync(key, Serializer.Serialize(result), cacheTime);
            return result;
        }

        /// <summary>
        /// 获取缓存中的值（不进行序列化），如果缓存不存在则调用func获取对象并缓存
        /// </summary>
        public static async Task<RedisValue> GetRedisValueAsync(this IDatabaseAsync database, string key, Func<Task<RedisValue>> acquire, TimeSpan? cacheTime = null)
        {
            if (await database.KeyExistsAsync(key))
                return await database.StringGetAsync(key);
            var result = await acquire();
            if (!result.HasValue)
                cacheTime = TimeSpan.FromSeconds(10);
            await database.StringSetAsync(key, result, cacheTime);
            return result;
        }

        /// <summary>
        /// Lua方式批量删除模糊查询出key
        /// 模糊查询key，不需加*号标识模糊
        /// </summary>
        public static async Task<bool> BatchDeleteAsync(this IDatabaseAsync database, string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;
            key = $"*{key}*";
            //Redis的keys模糊查询：
            var script =
                @"local dbsize = redis.call('dbsize') 
                local res = redis.call('scan', 0, 'MATCH', @prefix, 'COUNT', dbsize) 
                if(res[2] ~= nil) then 
                local keys = res[2] 
                for i = 1,#keys,1 do 
                redis.call('del', keys[i])
                return #keys end 
                else return 0 end";
            var redisResult = await database.ScriptEvaluateAsync(LuaScript.Prepare(script), new { prefix = key });
            return (int)redisResult > 0;
        }

        /// <summary>
        /// 将对象加入缓存（取的时候请使用GetObjectAsync配套取出）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="database"></param>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="cacheTime"></param>
        /// <returns></returns>
        public static async Task<bool> SetObjectAsync<T>(this IDatabaseAsync database, string key, T data, TimeSpan? cacheTime = null)
        {
            return await database.StringSetAsync(key, Serializer.Serialize(data), cacheTime);
        }

        /// <summary>
        /// 从缓冲中获取对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="database"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static async Task<T> GetObjectAsync<T>(this IDatabaseAsync database, string key)
        {
            var result = await database.StringGetAsync(key);
            return !result.HasValue ? default : Serializer.Deserialize<T>(result);
        }
    }
}