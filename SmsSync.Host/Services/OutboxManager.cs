using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using SmsSync.Attributes;
using SmsSync.Models;

namespace SmsSync.Services
{
    public interface IOutboxManager
    {
        void Populate(Notification[] notifications);
        Notification Next(OutboxNotification.NotificationState state);
        Notification[] All(OutboxNotification.NotificationState state);
        void Promote(Notification notification);
        void Promote(Notification[] notifications);
        void Rollback(Notification notification);
        void Rollback(Notification[] notifications);
        void Clear(Notification[] notifications);
    }

    /// <summary>
    /// Thread-safe outbox manager.
    /// Responsible for storing and promoting notifications.
    /// </summary>
    public class OutboxManager : IOutboxManager
    {
        private readonly ILogger _logger = Log.ForContext<OutboxManager>();

        private readonly HashSet<OutboxNotification> _notifications;
        private readonly object _lock = new object();

        public OutboxManager()
        {
            _notifications = new HashSet<OutboxNotification>();
        }

        public void Populate(Notification[] notifications)
        {
            _logger.Information("Populate queue with {N} messages", notifications.Length);
            lock (_lock)
            {
                foreach (var message in notifications)
                {
                    _notifications.Add(message.PrepareForSending());
                }
            }
        }

        public Notification Next(OutboxNotification.NotificationState state)
        {
            if (!state.IsTemporary())
            {
                throw new ArgumentException($"State should be temp but was {state}");
            }
            
            lock (_lock)
            {
                return NextNoLock(state);
            }
        }

        public Notification[] All(OutboxNotification.NotificationState state)
        {
            lock (_lock)
            {
                var result = new List<Notification>();
                
                Notification notification;
                while ((notification = NextNoLock(state)) != null)
                {
                    result.Add(notification);
                }

                return result.ToArray();
            }
        }

        private Notification NextNoLock(OutboxNotification.NotificationState state)
        {
            var value = _notifications.FirstOrDefault(m => m.State == state);
            if (value == null)
            {
                return null;
            }

            if (value.State.IsTemporary())
            {
                PromoteNoLock(value.Notification);
            }

            return value.Notification;
        }

        public void Promote(Notification notification)
        {
            lock (_lock)
            {
                PromoteNoLock(notification);
            }
        }

        public void Promote(Notification[] notifications)
        {
            lock (_lock)
            {
                foreach (var notification in notifications)
                {
                    PromoteNoLock(notification);
                }
            }
        }

        private void PromoteNoLock(Notification notification)
        {
            var value = _notifications.FirstOrDefault(x => x.Is(notification));
            if (value != null)
            {
                var (oldState, newState) = value.Promote();
                _logger.Information("Notification {@Notification} promote from {From} to {To}", value, oldState, newState);
            }
        }

        public void Rollback(Notification notification)
        {
            lock (_lock)
            {
                RollbackNoLock(notification);
            }
        }

        public void Rollback(Notification[] notifications)
        {
            lock (_lock)
            {
                foreach (var notification in notifications)
                {
                    RollbackNoLock(notification);
                }
            }
        }

        private void RollbackNoLock(Notification notification)
        {
            var value = _notifications.FirstOrDefault(x => x.Is(notification));
            if (value != null)
            {
                var (oldState, newState) = value.Rollback();
                _logger.Information("Notification {@Notification} rollback from {From} to {To}", value, oldState, newState);
            }
        }

        public void Clear(Notification[] notifications)
        {
            lock (_lock)
            {
                var result = _notifications.RemoveWhere(r => notifications.Any(r.Is));
                _logger.Information("Removed from queue {n}/{N} notifications", 
                    notifications.Length, result);
            }
        }
    }
}