using System;
using System.Linq;
using SmsSync.Models;

namespace SmsSync.Attributes
{
    public class AvailableAttribute : Attribute
    {
    }

    public static class AvailabilityExtensions
    {
        public static bool IsAvailable(this OutboxNotification.NotificationState state)
        {
            var enumType = typeof(OutboxNotification.NotificationState);
            var memberInfos = enumType.GetMember(state.ToString());
            var enumValueMemberInfo = memberInfos.FirstOrDefault(m => m.DeclaringType == enumType);
            var valueAttributes = 
                enumValueMemberInfo?.GetCustomAttributes(typeof(AvailableAttribute), false);

            return valueAttributes?.Any() == true;
        }
    }
}