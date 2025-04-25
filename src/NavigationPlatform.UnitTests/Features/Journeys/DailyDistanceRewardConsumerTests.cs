using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using NavigationPlatform.Application.Common.Interfaces;
using NavigationPlatform.Application.Features.Journeys;
using NavigationPlatform.Domain.Entities;

namespace NavigationPlatform.UnitTests.Features.Journeys
{
    public class DailyDistanceRewardConsumerTests
    {
        private readonly Mock<IApplicationDbContext> _dbContextMock;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly Mock<ILogger<DailyDistanceRewardConsumer>> _loggerMock;
        private readonly DailyDistanceRewardConsumer _consumer;

        // Test data
        private readonly Guid _userId = Guid.NewGuid();
        private readonly DateTime _testDate = new DateTime(2023, 5, 15);
        private readonly List<DailyDistanceBadge> _dailyDistanceBadges = new List<DailyDistanceBadge>();
        private readonly List<Journey> _journeys = new List<Journey>();

        public DailyDistanceRewardConsumerTests()
        {
            // Setup mocks
            _dbContextMock = new Mock<IApplicationDbContext>();
            _eventPublisherMock = new Mock<IEventPublisher>();
            _loggerMock = new Mock<ILogger<DailyDistanceRewardConsumer>>();

            // Setup in-memory collections with mocked DbSets
            var journeysDbSetMock = CreateDbSetMock(_journeys);
            var badgesDbSetMock = CreateDbSetMock(_dailyDistanceBadges);

            _dbContextMock.Setup(x => x.Journeys).Returns(journeysDbSetMock.Object);
            _dbContextMock.Setup(x => x.DailyDistanceBadges).Returns(badgesDbSetMock.Object);
            _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Create consumer with mocked dependencies
            _consumer = new DailyDistanceRewardConsumer(
                _dbContextMock.Object,
                _eventPublisherMock.Object,
                _loggerMock.Object);
        }
        
        private static Mock<DbSet<T>> CreateDbSetMock<T>(List<T> data) where T : class
        {
            var queryable = data.AsQueryable();
            var dbSetMock = new Mock<DbSet<T>>();
            
            dbSetMock.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));
            
            dbSetMock.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<T>(queryable.Provider));
            
            dbSetMock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
            
            dbSetMock.Setup(d => d.Add(It.IsAny<T>())).Callback<T>(data.Add);
            
            return dbSetMock;
        }
    }

    // Test helpers for async enumeration
    internal class TestAsyncQueryProvider<T> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<T>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var resultType = typeof(TResult).GetGenericArguments()[0];
            var execMethod = _inner.GetType().GetMethod("Execute")?.MakeGenericMethod(resultType);
            
            if (execMethod == null)
            {
                throw new InvalidOperationException("Failed to get Execute method");
            }
            
            var result = execMethod.Invoke(_inner, new object[] { expression });
            var task = Task.FromResult(result);
            
            var continueWithMethod = typeof(Task).GetMethod("ContinueWith")?.MakeGenericMethod(resultType);
            
            if (continueWithMethod == null)
            {
                throw new InvalidOperationException("Failed to get ContinueWith method");
            }
            
            return (TResult)continueWithMethod.Invoke(
                task, 
                new object[] { 
                    (Func<Task<object>, object>)(t => t.Result ?? throw new InvalidOperationException("Task result is null")), 
                    cancellationToken, 
                    TaskContinuationOptions.ExecuteSynchronously, 
                    TaskScheduler.Default 
                });
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        { }

        public TestAsyncEnumerable(Expression expression)
            : base(expression)
        { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider 
        {
            get
            {
                var queryable = this as IQueryable<T>;
                return new TestAsyncQueryProvider<T>(queryable.Provider);
            }
        }
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public T Current => _inner.Current;

        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(_inner.MoveNext());
        }

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return new ValueTask();
        }
    }
}