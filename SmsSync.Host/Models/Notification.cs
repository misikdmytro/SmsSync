using System;
using SmsSync.Attributes;

namespace SmsSync.Models
{
    public class Notification
    {
        public DbSms Sms { get; }
        
        public long TicketNumber => Sms.OrderId;
        public string PhoneNumber => Sms.ClientPhone;

        public Notification(DbSms sms)
        {
            Sms = sms;
        }

        public OutboxNotification PrepareForSending()
        {
            return new OutboxNotification(this);
        }

        public override bool Equals(object obj)
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
            switch (State)
            {
                case NotificationState.New:
                    State = NotificationState.WaitForSend;
                    break;
                case NotificationState.WaitForSend:
                    State = NotificationState.Sent;
                    break;
                case NotificationState.Sent:
                    State = NotificationState.WaitForCommit;
                    break;
                case NotificationState.WaitForCommit:
                    State = NotificationState.Committed;
                    break;
                case NotificationState.Committed:
                    State = NotificationState.WaitForRemove;
                    break;
                case NotificationState.WaitForRemove:
                    State = NotificationState.WaitForRemove;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(State));
            }

            return (oldState, State);
        }

        public (NotificationState newState, NotificationState oldState) Rollback()
        {
            var oldState = State;
            switch (State)
            {
                case NotificationState.New:
                case NotificationState.Sent:
                case NotificationState.Committed:
                    break;
                case NotificationState.WaitForSend:
                    State = NotificationState.New;
                    break;
                case NotificationState.WaitForCommit:
                    State = NotificationState.Sent;
                    break;
                case NotificationState.WaitForRemove:
                    State = NotificationState.Committed;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(State));
            }

            return (oldState, State);
        }

        public override bool Equals(object obj)
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