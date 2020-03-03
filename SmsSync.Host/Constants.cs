namespace SmsSync
{
    public static class Constants
    {
        public static class MessageData
        {
            public const string Source = "Kyivstar";
            public const string BearerType = "sms";
            public const string ContentType = "text/plain";
            public const string ServiceType = "false";
        }

        public static class States
        {
            public const string New = "NEW";
            public const string InProgress = "IN_PROGRESS";
            public const string Sent = "SENT";
            public const string Fail = "FAIL";
        }

        public static class Resources
        {
            public static class Types
            {
                public const string Registration = "Registration";
                public const string Invitation = "Invitation";
            }
            
            public const string PlaceIdPlaceholder = "<PlaceId>";
            public const string ServiceIdPlaceholder = "<ServiceId>";
            public const string TicketIdPlaceholder = "<TicketId>";
        }
    }
}