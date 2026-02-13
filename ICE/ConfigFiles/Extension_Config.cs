using ECommons.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ICE.ConfigFiles
{
    public static class EzConfigExtensions
    {
        private static readonly object _saveLock = new object();
        private static CancellationTokenSource? _saveCts;

        public static async Task SaveAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    EzConfig.Save();
                }
                catch (Exception ex)
                {
                    PluginLog.Error("Failed to save EzConfig");
                    throw;
                }
            }).ConfigureAwait(false);
        }

        public static void SaveDebounced(int delayMs = 500)
        {
            lock (_saveLock)
            {
                _saveCts?.Cancel();
                _saveCts = new CancellationTokenSource();
                var cts = _saveCts;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(delayMs, cts.Token);
                        await SaveAsync().ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Newer save cancelled this one
                    }
                    catch (Exception ex)
                    {
                        PluginLog.Error($"Failed to save EzConfig: {ex}");
                    }
                });
            }
        }
    }
}
