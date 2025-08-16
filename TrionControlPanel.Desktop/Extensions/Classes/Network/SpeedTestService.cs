using System.Diagnostics;

namespace TrionControlPanel.Desktop.Extensions.Classes.Network
{
    public class SpeedTestResult
    {
        /// <summary>
        /// Latency or Time to First Byte (TTFB) in milliseconds.
        /// </summary>
        public long LatencyMs { get; set; }

        /// <summary>
        /// Download speed in Megabits per second (Mbps).
        /// </summary>
        public double DownloadSpeedMbps { get; set; }

        /// <summary>
        /// Set if an error occurs during the test.
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    public class SpeedTestService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseApiUrl;

        public SpeedTestService(string baseApiUrl)
        {
            _httpClient = new HttpClient();
            // Example: "http://your-api.com/Trion"
            _baseApiUrl = baseApiUrl.TrimEnd('/');
        }

        /// <summary>
        /// Runs a full speed test, measuring both latency and download speed.
        /// </summary>
        /// <param name="downloadSizeInMB">The size of the file to use for the download test.</param>
        public async Task<SpeedTestResult> RunTestAsync(int downloadSizeInMB = 25)
        {
            var result = new SpeedTestResult();
            try
            {
                // 1. Measure Latency
                result.LatencyMs = await MeasureLatencyAsync();

                // 2. Measure Download Speed
                result.DownloadSpeedMbps = await MeasureDownloadSpeedAsync(downloadSizeInMB);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Test failed: {ex.Message}";
            }
            return result;
        }

        /// <summary>
        /// Measures the Time to First Byte (TTFB) by hitting a minimal endpoint.
        /// </summary>
        private async Task<long> MeasureLatencyAsync()
        {
            var stopwatch = new Stopwatch();
            var requestUrl = $"{_baseApiUrl}/Ping";

            stopwatch.Start();

            // HttpCompletionOption.ResponseHeadersRead is CRITICAL for accurate latency.
            // It ensures the 'await' completes as soon as headers are received, not after the body downloads.
            using (var response = await _httpClient.GetAsync(requestUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                stopwatch.Stop();
                response.EnsureSuccessStatusCode(); // Ensure we got a 2xx response
                return stopwatch.ElapsedMilliseconds;
            }
        }

        /// <summary>
        /// Measures download throughput by timing the download of a file of known size.
        /// </summary>
        private async Task<double> MeasureDownloadSpeedAsync(int sizeInMB)
        {
            var stopwatch = new Stopwatch();
            var requestUrl = $"{_baseApiUrl}/DownloadSpeedTest?sizeInMB={sizeInMB}";

            // Make the request and start the timer
            using (var response = await _httpClient.GetAsync(requestUrl))
            {
                response.EnsureSuccessStatusCode();

                stopwatch.Start();

                // Download the entire response body
                var content = await response.Content.ReadAsByteArrayAsync();

                stopwatch.Stop();

                // Get total bytes from the actual downloaded content length
                long totalBytes = content.Length;
                double seconds = stopwatch.Elapsed.TotalSeconds;

                if (seconds == 0) return 0;

                // Calculate speed in Megabits per second (Mbps)
                // (Bytes * 8) -> bits
                // (bits / seconds) -> bits per second
                // (bps / 1_000_000) -> megabits per second
                double speedMbps = totalBytes * 8 / (seconds * 1000 * 1000);

                return Math.Round(speedMbps, 2);
            }
        }
    }
}