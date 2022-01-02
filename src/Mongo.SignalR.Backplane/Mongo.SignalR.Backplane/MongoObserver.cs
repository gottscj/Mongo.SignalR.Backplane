using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Mongo.SignalR.Backplane
{
    public class MongoInvocationEventArgs : EventArgs
    {
        public MongoInvocation Invocation { get; }

        public MongoInvocationEventArgs(MongoInvocation invocation)
        {
            Invocation = invocation;
        }
    }
    public class MongoObserver : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IMongoDbContext _db;
        private int _failureTimeout = 5000;
        private const int MaxTimeout = 60000;

        public MongoObserver(ILogger<MongoObserver> logger, IMongoDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public event EventHandler<MongoInvocationEventArgs> OnInvocation; 

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var options = new FindOptions<MongoInvocation> {CursorType = CursorType.TailableAwait};

            var lastId = ObjectId.GenerateNewId(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
            var filter = new BsonDocument("_id", new BsonDocument("$gt", lastId));

            var update = Builders<MongoInvocation>
                .Update
                .SetOnInsert(j => j.Type, InvocationType.Init);

            var lastEnqueued = _db.Invocations.FindOneAndUpdate(filter, update,
                new FindOneAndUpdateOptions<MongoInvocation>
                {
                    IsUpsert = true,
                    Sort = Builders<MongoInvocation>.Sort.Descending(j => j.Id),
                    ReturnDocument = ReturnDocument.After
                });

            lastId = lastEnqueued.Id;
            filter = new BsonDocument("_id", new BsonDocument("$gt", lastId));

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Start the cursor and wait for the initial response
                    using (var cursor = _db.Invocations.FindSync(filter, options, cancellationToken))
                    {
                        foreach (var invocation in cursor.ToEnumerable(cancellationToken))
                        {
                            // Set the last value we saw 
                            lastId = invocation.Id;
                            if (invocation.Type == InvocationType.Init)
                            {
                                continue;
                            }

                            if (_logger.IsEnabled(LogLevel.Trace))
                            {
                                _logger.LogTrace("Invocation '{Type}'", invocation.Type.ToString());
                            }
                            OnInvocation?.Invoke(this, new MongoInvocationEventArgs(invocation));
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (MongoCommandException commandException)
                {
                    await HandleMongoCommandException(commandException, cancellationToken);
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
            var timeout = _failureTimeout;
            _failureTimeout += 5000;
            if (_failureTimeout >= MaxTimeout)
            {
                _failureTimeout = MaxTimeout;
            }

            return timeout;
        }
    }
}