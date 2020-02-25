using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using SmsSync.Models;
using SmsSync.Services;
using Xunit;
using Xunit.Abstractions;

namespace SmsSync.Tests
{
    public class InboxManagerTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly IOutboxManager _outboxManager;
        
        private readonly Notification[] _testData = {
            new Notification(4, "+1234567"),
            new Notification(7,"+76543210")
        };

        public InboxManagerTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _outboxManager = new OutboxManager();
        }

        [Fact]
        public void PopulateShouldDoIt()
        {
            // Arrange
            // Act
            // Assert
            _outboxManager.Populate(_testData);
        }
        
        [Fact]
        public void PopulateShouldDoItWithData()
        {
            // Arrange
            // Act
            _outboxManager.Populate(_testData);

            // Assert
            var count = 0;
            Notification notification;
            while ((notification = _outboxManager.Next(OutboxNotification.NotificationState.New)) != null)
            {
                ++count;
                _testData.ShouldContain(notification);
            }
            
            _testData.Length.ShouldBe(count);
        }

        [Theory]
        [InlineData(2, 2, 10000)]
        [InlineData(2, 5, 10000)]
        [InlineData(5, 2, 10000)]
        public async Task PopulateShouldDoItInSeveralThreads(int readThreadsCount, int writeThreadsCount, int cyclesCount)
        {
            var messages = Enumerable.Range(0, cyclesCount * writeThreadsCount)
                .Select(x => new Notification(x, Guid.NewGuid().ToString()))
                .ToArray();

            var writeTasks = new List<Task>();
            for (var i = 0; i < writeThreadsCount; i++)
            {
                var index = i;
                writeTasks.Add(Task.Run(() =>
                {
                    for (var j = 0; j < cyclesCount; j++)
                    {
                        _outboxManager.Populate(messages.Skip(index * cyclesCount + j).Take(1).ToArray());
                    }
                }));
            }
            
            var readTasks = new List<Task>();
            var readMessages = 0;
            
            for (var i = 0; i < readThreadsCount; i++)
            {
                readTasks.Add(Task.Run(() =>
                {
                    for (var j = 0; j < cyclesCount; j++)
                    {
                        var result = _outboxManager.Next(OutboxNotification.NotificationState.New);
                        if (result != null)
                            Interlocked.Add(ref readMessages, 1);
                    }
                }));
            }

            await Task.WhenAll(writeTasks);
            await Task.WhenAll(readTasks);

            readMessages.ShouldBeGreaterThan(0);
            _testOutputHelper.WriteLine($"Read {readMessages}/{cyclesCount * writeThreadsCount} messages");

            for (var i = readMessages; i < cyclesCount * writeThreadsCount; i++)
            {
                _outboxManager.Next(OutboxNotification.NotificationState.New).ShouldNotBeNull();
            }
            
            _outboxManager.Next(OutboxNotification.NotificationState.New).ShouldBeNull();
        }
    }
}