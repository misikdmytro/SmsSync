using System;
using System.Collections.Generic;
using SmsSync.Models;

namespace SmsSync.Equality
{
    public class SmsEqualityComparer : IEqualityComparer<DbSms>
    {
        public bool Equals(DbSms x, DbSms y)
        {
            if (x == null || y == null)
            {
                return x == null && y == null;
            }
            
            return x.LanguageId == y.LanguageId && x.OrderId == y.OrderId && x.TerminalId == y.TerminalId &&
                   x.ResourceId == y.ResourceId && x.JobId == y.JobId && x.ClientPhone == y.ClientPhone &&
                   x.SetTime.Equals(y.SetTime) && x.LastUpdateTime.Equals(y.LastUpdateTime) && x.State == y.State;
        }

        public int GetHashCode(DbSms obj)
        {
            var hashCode = new HashCode();
            hashCode.Add((int) obj.LanguageId);
            hashCode.Add(obj.OrderId);
            hashCode.Add(obj.TerminalId);
            hashCode.Add(obj.ResourceId);
            hashCode.Add(obj.JobId);
            hashCode.Add(obj.ClientPhone);
            hashCode.Add(obj.SetTime);
            hashCode.Add(obj.LastUpdateTime);
            hashCode.Add(obj.State);
            return hashCode.ToHashCode();
        }
    }
}