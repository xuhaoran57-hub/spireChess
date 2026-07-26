using System;
using System.Collections.Generic;

namespace SpireChess.Audio
{
    public enum AudioPlaybackRejectionReason
    {
        None,
        InvalidRequest,
        Cooldown,
        Concurrency
    }

    public sealed class AudioPlaybackLimiter
    {
        private sealed class CueState
        {
            public int ActiveCount;
            public bool HasStarted;
            public double LastStartedAt;
        }

        private readonly Dictionary<string, CueState> stateByCueId =
            new Dictionary<string, CueState>(StringComparer.Ordinal);

        public bool TryAcquire(
            string cueId,
            double nowSeconds,
            int concurrencyLimit,
            double cooldownSeconds,
            out AudioPlaybackRejectionReason rejectionReason)
        {
            if (string.IsNullOrWhiteSpace(cueId) ||
                double.IsNaN(nowSeconds) ||
                double.IsInfinity(nowSeconds) ||
                concurrencyLimit < 1 ||
                double.IsNaN(cooldownSeconds) ||
                double.IsInfinity(cooldownSeconds) ||
                cooldownSeconds < 0d)
            {
                rejectionReason = AudioPlaybackRejectionReason.InvalidRequest;
                return false;
            }

            if (!stateByCueId.TryGetValue(cueId, out var state))
            {
                state = new CueState();
                stateByCueId.Add(cueId, state);
            }

            if (state.HasStarted &&
                nowSeconds < state.LastStartedAt + cooldownSeconds)
            {
                rejectionReason = AudioPlaybackRejectionReason.Cooldown;
                return false;
            }

            if (state.ActiveCount >= concurrencyLimit)
            {
                rejectionReason = AudioPlaybackRejectionReason.Concurrency;
                return false;
            }

            state.ActiveCount++;
            state.HasStarted = true;
            state.LastStartedAt = nowSeconds;
            rejectionReason = AudioPlaybackRejectionReason.None;
            return true;
        }

        public void Release(string cueId)
        {
            if (string.IsNullOrWhiteSpace(cueId) ||
                !stateByCueId.TryGetValue(cueId, out var state) ||
                state.ActiveCount <= 0)
            {
                return;
            }

            state.ActiveCount--;
        }

        public int GetActiveCount(string cueId)
        {
            if (string.IsNullOrWhiteSpace(cueId) ||
                !stateByCueId.TryGetValue(cueId, out var state))
            {
                return 0;
            }

            return state.ActiveCount;
        }

        public void Reset()
        {
            stateByCueId.Clear();
        }
    }
}
