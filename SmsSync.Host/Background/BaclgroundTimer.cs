using System;
using System.Timers;

namespace SmsSync.Background
{
    public class BaclgroundTimer : IDisposable
    {
        private readonly Timer _timer;

        public BaclgroundTimer(TimeSpan interval)
        {
            _timer = new Timer
            {
                Interval = interval.TotalMilliseconds,
                AutoReset = true,
                Enabled = false
            };
        }

        public void Start(ElapsedEventHandler action)
        {
            _timer.Elapsed += action;
            _timer.Start();
        }

        public void Stop()
        {
            _timer.AutoReset = false;
            _timer.Close();
            _timer.Stop();
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}