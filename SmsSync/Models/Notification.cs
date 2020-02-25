using System;
using SmsSync.Attributes;

namespace SmsSync.Models
{
    public class Notification
    {
        public long TicketNumber { get; }
        public string PhoneNumber { get; }

        public Notification(long ticketNumber, string phoneNumber)
        {
            TicketNumber = ticketNumber;
            PhoneNumber = phoneNumber;
        }

        public OutboxNotification PrepareForSending()
        {
            return new OutboxNotification(this);
        }

        public override bool Equals(object? obj)
        {
            if (obj != null && obj is Notification notification)
            {
                return TicketNumber.Equals(notification.TicketNumber)
                       && PhoneNumber.Equals(notification.PhoneNumber);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return TicketNumber.GetHashCode() ^ PhoneNumber.GetHashCode();
        }
    }

    public class OutboxNotification
    {
        public enum NotificationState
        {
            [TemporaryState] New,
            WaitForSend,
            [TemporaryState] Sent,
            WaitForCommit,
            [TemporaryState] Committed,
            WaitForRemove
        }

        public Notification Notification { get; }

        public NotificationState State { get; private set; }

        public OutboxNotification(Notification notification)
        {
            Notification = notification;
            State = NotificationState.New;
        }

        public bool Is(Notification notification)
        {
            return Notification.Equals(notification);
        }

        public (NotificationState newState, NotificationState oldState) Promote()
        {
            var oldState = State;
            State = State switch
            {
                NotificationState.New => NotificationState.WaitForSend,
                NotificationState.WaitForSend => NotificationState.Sent,
                NotificationState.Sent => NotificationState.WaitForCommit,
                NotificationState.WaitForCommit => NotificationState.Committed,
                NotificationState.Committed => NotificationState.WaitForRemove,
                NotificationState.WaitForRemove => NotificationState.WaitForRemove,
                _ => throw new ArgumentOutOfRangeException(nameof(State))
            };

            return (oldState, State);
        }

        public (NotificationState newState, NotificationState oldState) Rollback()
        {
            var oldState = State;
            State = State switch
            {
                NotificationState.New => NotificationState.New,
                NotificationState.WaitForSend => NotificationState.New,
                NotificationState.Sent => NotificationState.Sent,
                NotificationState.WaitForCommit => NotificationState.Sent,
                NotificationState.Committed => NotificationState.Committed,
                NotificationState.WaitForRemove => NotificationState.Committed,
                _ => throw new ArgumentOutOfRangeException(nameof(State))
            };

            return (oldState, State);
        }

        public override bool Equals(object? obj)
        {
            if (obj != null && obj is OutboxNotification notification)
            {
                return Notification.Equals(notification.Notification);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Notification.GetHashCode();
        }
    }
}