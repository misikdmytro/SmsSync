using System;
using System.Linq;
using SmsSync.Models;

namespace SmsSync.Attributes
{
    public class TemporaryStateAttribute : Attribute
    {
    }

    public static class TemporaryStateExtensions
    {
        public static bool IsTemporary(this OutboxNotification.NotificationState state)
        {
            var enumType = typeof(OutboxNotification.NotificationState);
            var memberInfos = enumType.GetMember(state.ToString());
            var enumValueMemberInfo = memberInfos.FirstOrDefault(m => m.DeclaringType == enumType);
            var valueAttributes = 
                enumValueMemberInfo?.GetCustomAttributes(typeof(TemporaryStateAttribute), false);

            return valueAttributes?.Any() == true;
        }
    }
}