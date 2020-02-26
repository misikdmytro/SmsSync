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
            public const string ResourcesLocation = "Resource";
            public const string ResourceFileName = "Messages_{0}";

            public const string OrderIdPlaceholder = "<OrderId>";
            public const string ServiceIdPlaceholder = "<TicketId>";
        }
    }
}