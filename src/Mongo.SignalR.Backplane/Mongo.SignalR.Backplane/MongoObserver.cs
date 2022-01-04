using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mongo.SignalR.Backplane.Invocations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Mongo.SignalR.Backplane
{
    public class InvocationSubscriber : IDisposable
    {
        private readonly Func<MongoInvocation, Task> _invocationFunc;
        private readonly Dictionary<Guid, InvocationSubscriber> _subscribers;

        private readonly Guid _id;
        
        public InvocationSubscriber(Func<MongoInvocation, Task> invocationFunc, Dictionary<Guid, InvocationSubscriber> subscribers)
        {
            _id = Guid.NewGuid();
            _invocationFunc = invocationFunc;
            _subscribers = subscribers;
            lock (subscribers)
            {
                subscribers[_id] = this;
            }
        }

        public async Task Execute(MongoInvocation invocation)
        {
            try
            {
                await _invocationFunc(invocation);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        
        public void Dispose()
        {
            lock (_subscribers)
            {
                _subscribers.Remove(_id);
            }
        }
    }

    public interface IMongoObserver
    {
        public IDisposable Subscribe(Func<MongoInvocation, Task> subscriber);
    }

    public class MongoObserver : BackgroundService, IMongoObserver
    {
        private readonly ILogger _logger;
        private readonly IMongoDbContext _db;
        private int _failureTimeout = 5000;
        private const int MaxTimeout = 60000;

        private readonly Dictionary<Guid, InvocationSubscriber> _subscribers =
            new Dictionary<Guid, InvocationSubscriber>();

        public MongoObserver(ILogger<MongoObserver> logger, IMongoDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public IDisposable Subscribe(Func<MongoInvocation, Task> subscriber)
        {
            lock (_subscribers)
            {
                return new InvocationSubscriber(subscriber, _subscribers);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var options = new FindOptions<MongoInvocation> {CursorType = CursorType.TailableAwait};

            var lastId = ObjectId.GenerateNewId(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
            var filter = new BsonDocument("_id", new BsonDocument("$gt", lastId));

            var update = Builders<MongoInvocation>
                .Update
                .SetOnInsert(j => j.ServerName, InvocationType.Init);

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
                            if (invocation.ServerName == InvocationType.Init)
                            {
                                continue;
                            }

                            if (_logger.IsEnabled(LogLevel.Trace))
                            {
                                _logger.LogTrace("Invocation '{Type}'", invocation.GetType().Name);
                            }

                            List<InvocationSubscriber> subscribers;
                            lock (_subscribers)
                            {
                                subscribers = _subscribers.Values.ToList();
                            }

                            await Task.WhenAll(subscribers.Select(s => s.Execute(invocation)));
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