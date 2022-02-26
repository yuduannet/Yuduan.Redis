using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Yuduan.Redis
{
    /// <summary>
    /// Represents Redis connection wrapper implementation
    /// </summary>
    public class RedisContext : IDisposable
    {
        #region Fields
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private volatile ConnectionMultiplexer _connection;
        private readonly string _connectionString;
        private IDatabaseAsync _default;
        /// <summary>
        /// 操作默认数据库 DB0
        /// </summary>
        public IDatabaseAsync Default => _default ?? (_default = GetDatabase());

        #endregion

        #region Ctor

        public RedisContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString">redis连接字符串</param>
        /// <param name="serializer">redis对象的自定义序列方式（默认为json）</param>
        public RedisContext(string connectionString, ISerializer serializer)
        {
            _connectionString = connectionString;
            if (serializer != null)
                RedisCacheExtensions.Serializer = serializer;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// 连接到redis服务器
        /// </summary>
        /// <returns></returns>
        protected void Connect(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (_connection != null && _connection.IsConnected) return;
            _connectionLock.Wait(token);
            try
            {
                if (_connection != null && _connection.IsConnected) return;
                if (_default == null)
                {
                    _connection?.Dispose();
                    _connection = ConnectionMultiplexer.Connect(_connectionString);
                    _default = _connection.GetDatabase();
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        /// <summary>
        /// 异步连接到redis服务器
        /// </summary>
        /// <returns></returns>
        protected async Task ConnectAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (_connection != null && _connection.IsConnected) return;
            await _connectionLock.WaitAsync(token);
            try
            {
                if (_connection != null && _connection.IsConnected) return;
                if (_default == null)
                {
                    _connection?.Dispose();
                    _connection = await ConnectionMultiplexer.ConnectAsync(_connectionString);
                    _default = _connection.GetDatabase();
                }
            }
            finally
            {
                _connectionLock.Release();
            }

        }

        #endregion

        #region Methods

        /// <summary>
        /// 获取指定db
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public IDatabase GetDatabase(int db = 0)
        {
            Connect();
            return _connection.GetDatabase(db);
        }

        /// <summary>
        /// 异步获取指定db
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public async Task<IDatabaseAsync> GetDatabaseAsync(int db = 0)
        {
            await ConnectAsync();
            return _connection.GetDatabase(db);
        }

        /// <summary>
        /// 获取REDIS服务对象
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public IServer GetServer(EndPoint endPoint = null)
        {
            Connect();
            if (endPoint == null)
            {
                var endPoints = GetEndPoints();
                endPoint = endPoints.FirstOrDefault();
                if (endPoint == null)
                    throw new ArgumentNullException("endPoint");
            }
            return _connection.GetServer(endPoint);
        }

        /// <summary>
        /// 异步获取REDIS服务对象
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public async Task<IServer> GetServerAsync(EndPoint endPoint = null)
        {
            await ConnectAsync();
            if (endPoint == null)
            {
                var endPoints = await GetEndPointsAsync();
                endPoint = endPoints.FirstOrDefault();
                if (endPoint == null)
                    throw new ArgumentNullException("endPoint");
            }
            return _connection.GetServer(endPoint);
        }

        /// <summary>
        /// 获取REDIS连接信息
        /// </summary>
        /// <returns></returns>
        public EndPoint[] GetEndPoints()
        {
            Connect();
            return _connection.GetEndPoints();
        }

        /// <summary>
        /// 异步获取REDIS连接信息
        /// </summary>
        /// <returns></returns>
        public async Task<EndPoint[]> GetEndPointsAsync()
        {
            await ConnectAsync();
            return _connection.GetEndPoints();
        }

        /// <summary>
        /// 释放指定库的所有数据
        /// </summary>
        /// <param name="db"></param>
        public void FlushDatabase(int db)
        {
            var endPoints = GetEndPoints();
            foreach (var endPoint in endPoints)
            {
                var server = GetServer(endPoint);
                server.FlushDatabase(db);
            }
        }

        /// <summary>
        /// 异步释放指定库的所有数据
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public async Task FlushDatabaseAsync(int db)
        {
            var endPoints = await GetEndPointsAsync();
            Task[] tasks=new Task[endPoints.Length];
            for (int i = 0; i < endPoints.Length; i++)
            {
                tasks[i] = GetServerAsync(endPoints[i]).ContinueWith(async server =>
                {
                    await server.Result.FlushDatabaseAsync(db);
                });
            }
            await Task.WhenAll(tasks);
        }




        public void Dispose()
        {
            //dispose ConnectionMultiplexer
            _connection?.Dispose();
        }

        #endregion
    }
}