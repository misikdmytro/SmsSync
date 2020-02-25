using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
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
        private IInboxManager _inboxManager;
        private Mock<IInboxRepository> _repository;
        
        private UserMessage[] _testData = new[]
        {
            new UserMessage
            {
                PhoneNumber = "+1234567",
                TicketNumber = 4
            },
            new UserMessage
            {
                PhoneNumber = "+76543210",
                TicketNumber = 7
            }
        };

        public InboxManagerTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _repository = new Mock<IInboxRepository>();
            _inboxManager = new InboxManager(_repository.Object);

            _repository.Setup(x => x.ReadAsync())
                .Returns(Task.FromResult(_testData));
        }

        [Fact]
        public async Task PopulateShouldDoIt()
        {
            // Arrange
            // Act
            // Assert
            await _inboxManager.Populate();
        }
        
        [Fact]
        public async Task PopulateShouldDoItWithData()
        {
            // Arrange
            // Act
            await _inboxManager.Populate();

            // Assert
            var count = 0;
            while (_inboxManager.TakeToSend(out var message))
            {
                ++count;
                _testData.ShouldContain(message);
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
                .Select(x => new UserMessage
                {
                    TicketNumber = x,
                    PhoneNumber = Guid.NewGuid().ToString()
                })
                .ToArray();

            var messageNumber = 0;
            var sync = new object();
            _repository.Setup(x => x.ReadAsync())
                .Returns(() => Task.Run(() =>
                {
                    int takeMessage;
                    lock (sync)
                    {
                        takeMessage = messageNumber++;
                    }
                    
                    return messages.Skip(takeMessage).Take(1).ToArray();
                }));
            
            var writeTasks = new List<Task>();
            for (var i = 0; i < writeThreadsCount; i++)
            {
                writeTasks.Add(Task.Run(async () =>
                {
                    for (var j = 0; j < cyclesCount; j++)
                    {
                        await _inboxManager.Populate();
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
                        var result = _inboxManager.TakeToSend(out _);
                        if (result)
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
                _inboxManager.TakeToSend(out _).ShouldBeTrue();
            }
            
            _inboxManager.TakeToSend(out _).ShouldBeFalse();
        }
    }
}