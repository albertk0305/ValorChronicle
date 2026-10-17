using System;

namespace ValorChronicle.Settings.Services
{
    public sealed class SettingsSaveScheduler
    {
        public const double DefaultDelaySeconds = 0.3d;

        private readonly double delaySeconds;
        private double dueAt;

        public SettingsSaveScheduler(double delaySeconds = DefaultDelaySeconds)
        {
            if (double.IsNaN(delaySeconds)
                || double.IsInfinity(delaySeconds)
                || delaySeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(delaySeconds));
            }

            this.delaySeconds = delaySeconds;
        }

        public bool IsPending { get; private set; }

        public void NotifyChange(double unscaledTime)
        {
            RequireFinite(unscaledTime);
            dueAt = unscaledTime + delaySeconds;
            IsPending = true;
        }

        public bool IsDue(double unscaledTime)
        {
            RequireFinite(unscaledTime);
            return IsPending && unscaledTime >= dueAt;
        }

        public void Clear()
        {
            IsPending = false;
        }

        private static void RequireFinite(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value));
        }
    }
}
