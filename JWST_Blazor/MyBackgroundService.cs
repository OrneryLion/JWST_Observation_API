using System.Diagnostics;

namespace JWST_Blazor
{
    public class MyBackgroundService : IHostedService, IDisposable
    {
        private Timer _timer;
        private DateTime _lastRun;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string pathToExecutable = currentDirectory + "../JWSTScraper.exe";

            if ((DateTime.Now - _lastRun) >= TimeSpan.FromDays(7))
            {
                // Run your C# project here
                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = pathToExecutable,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };
                process.Start();
                process.WaitForExit();

                _lastRun = DateTime.Now;
            }
        }


        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }

}
