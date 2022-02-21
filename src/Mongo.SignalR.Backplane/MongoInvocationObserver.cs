using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mongo.SignalR.Backplane.Invocations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Mongo.SignalR.Backplane
{
    public class MongoInvocationObserver : BackgroundService
    {
        private readonly MongoOptions _options;
        private readonly MongoHubConnectionStore _connectionStore;
        private readonly IMongoDbContext _db;
        private readonly ILogger _logger;
        private int _failureTimeout;

        public MongoInvocationObserver(
            MongoHubConnectionStore connectionStore,
            IMongoDbContext db,
            IOptions<MongoOptions> options,
            ILogger<MongoInvocationObserver> logger)
        {
            _options = options.Value;
            _connectionStore = connectionStore;
            _db = db;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var options = new FindOptions<MongoInvocation> {CursorType = CursorType.TailableAwait};

            var lastId = ObjectId.GenerateNewId(DateTime.UtcNow);
            var filter = new BsonDocument("_id", new BsonDocument("$gt", lastId));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Start the cursor and wait for the initial response
                    using var cursor = _db.Invocations.FindSync(filter, options, stoppingToken);

                    while (await cursor.MoveNextAsync(stoppingToken))
                    {
                        foreach (var invocation in cursor.Current)
                        {
                            // Set the last value we saw 
                            lastId = invocation.Id;
                            if (invocation is InitMongoInvocation)
                            {
                                continue;
                            }

                            if (_logger.IsEnabled(LogLevel.Trace))
                            {
                                _logger.LogTrace("Invocation '{Type}'", invocation.GetType().Name);
                            }
                            await invocation.Process(_connectionStore);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (MongoCommandException commandException)
                {
                    await HandleMongoCommandException(commandException, stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        "Error observing '{CollectionName}': {Message}",
                        _db.Invocations.CollectionNamespace.CollectionName, e.Message);
                }
                finally
                {
                    // The tailable cursor died so loop through and restart it
                    // Now, we want documents that are strictly greater than the last value we saw
                    filter = new BsonDocument("_id", new BsonDocument("$gt", lastId));
                }
            }
        }


        /// <summary>
        /// Default:
        ///     If error contains "tailable cursor requested on non capped collection."
        ///    then try to convert
        /// </summary>
        protected virtual async Task HandleMongoCommandException(MongoCommandException commandException,
            CancellationToken cancellationToken)
        {
            var successfullyRecreatedCollection = false;
            if (commandException.Message.Contains("tailable cursor requested on non capped collection."))
            {
                _logger.LogWarning(
                    "'{CollectionName}' collection is not capped.\r\n" +
                    "Trying to convert to capped", _db.Invocations.CollectionNamespace.CollectionName);
                try
                {
                    _db.Database.RunCommand<BsonDocument>(new BsonDocument
                    {
                        ["convertToCapped"] = _db.Invocations.CollectionNamespace.CollectionName,
                        ["size"] = 67108864, // 64 MB
                    });
                    // _storageOptions.CreateNotificationsCollection(_dbContext.Database);
                    successfullyRecreatedCollection = true;
                }
                catch (Exception e)
                {
                    _logger.LogWarning(
                        "Failed to convert '{CollectionName}' with message: {Message}",
                        _db.Invocations.CollectionNamespace.CollectionName, e.Message);
                }
            }
            else
            {
                _logger.LogError("Error observing '{CollectionName}'\r\n" +
                                 "{ErrorMessage}" + "\r\n" +
                                 "Notifications will not be available\r\n" +
                                 $"If you dropped the collection " +
                                 "you need to manually create it again as a capped collection\r\n" +
                                 "For reference, please see\r\n" +
                                 "   - https://docs.mongodb.com/manual/core/capped-collections/\r\n",
                    _db.Invocations.CollectionNamespace.CollectionName, commandException.Message);
            }

            if (!successfullyRecreatedCollection)
            {
                // fatal error observing notifications. try again backing off 5s.
                await Task.Delay(GetFailureTimeoutMs(), cancellationToken);
            }
        }

        /// <summary>
        /// Gets timeout. adds 5 seconds for each call, maximizing at 60s
        /// </summary>
        /// <returns></returns>
        protected virtual int GetFailureTimeoutMs()
        {
            var timeout = _options.FailureBackoffTimeoutMs;
            _failureTimeout += 5000;
            if (_failureTimeout >= _options.MaxTimeoutMs)
            {
                _failureTimeout = _options.MaxTimeoutMs;
            }

            return timeout;
        }
    }
}