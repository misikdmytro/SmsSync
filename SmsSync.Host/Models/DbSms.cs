using System;

namespace SmsSync.Models
{
    public class DbSms
    {
        public Language LanguageId { get; set; }
        public int OrderId { get; set; }
        public int TerminalId { get; set; }
        public int JobId { get; set; }
        public string ClientPhone { get; set; }
        public DateTime SetTime { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public string State { get; set; }
        public string JobDescription { get; set; }
    }
}