using System;
using SmsSync.Attributes;
using SmsSync.Services;

namespace SmsSync.Models
{
    public class Notification
    {
        public DbSms Sms { get; }

        public Notification(DbSms sms)
        {
            Sms = sms;
        }

        public OutboxNotification PrepareForSending()
        {
            return new OutboxNotification(this);
        }


        protected bool Equals(Notification other)
        {
            return Sms.LanguageId == other.Sms.LanguageId && Sms.OrderId == other.Sms.OrderId &&
                   Sms.TerminalId == other.Sms.TerminalId &&
                   Sms.ResourceId == other.Sms.ResourceId && Sms.JobId == other.Sms.JobId &&
                   Sms.ClientPhone == other.Sms.ClientPhone &&
                   Sms.SetTime.Equals(other.Sms.SetTime) && Sms.LastUpdateTime.Equals(other.Sms.LastUpdateTime) &&
                   Sms.State == other.Sms.State;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Notification) obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add((int) Sms.LanguageId);
            hashCode.Add(Sms.OrderId);
            hashCode.Add(Sms.TerminalId);
            hashCode.Add(Sms.ResourceId);
            hashCode.Add(Sms.JobId);
            hashCode.Add(Sms.ClientPhone);
            hashCode.Add(Sms.SetTime);
            hashCode.Add(Sms.LastUpdateTime);
            hashCode.Add(Sms.State);
            return hashCode.ToHashCode();
        }
    }

    public class OutboxNotification
    {
        public enum NotificationState
        {
            [Available] New,
            WaitForSend,
            [Available] Sent,
            WaitForCommit,
            [Available] Committed,
            WaitForRemove,
            [Available] Failed,
            WaitForMark,
            [Available] Marked,
            WaitForRemoveFail
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
            State = State.Promote();

            return (oldState, State);
        }

        public (NotificationState newState, NotificationState oldState) Rollback()
        {
            var oldState = State;
            State = State.Rollback();

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