namespace SmsSync
{
    public static class Constants
    {
        public static class Templates
        {
            public const string PhoneNumber = "<PhoneNumber>";
            public const string Content = "<Content>";
            public const string TicketId = "<TicketId>";
            public const string PlaceId = "<PlaceId>";
            public const string ServiceId = "<ServiceId>";
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
        }
    }
}