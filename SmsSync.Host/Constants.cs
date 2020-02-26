namespace SmsSync
{
    public static class Constants
    {
        public static class MessageData
        {
            public const string Source = "Kyivstar";
            public const string BearerType = "sms";
            public const string ContentType = "text/plain";
            public const string ServiceType = "False";
        }

        public static class Resources
        {
            public const string ResourceName = "MessageContent";

            public const string PlaceIdPlaceholder = "<PlaceId>";
            public const string ServiceIdPlaceholder = "<ServiceId>";
            public const string TicketIdPlaceholder = "<TicketId>";
        }

        public static class Limits
        {
            public const int MaxRollbackTimes = 5;
        }
    }
}