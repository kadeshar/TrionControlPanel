using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using TrionControlPanel.API.Classes;
using TrionControlPanel.API.Classes.Database;
using TrionControlPanel.API.Classes.Lists;

namespace TrionControlPanel.API.api
{
    [Route("[controller]")]
    [ApiController]
    public class TrionController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly DatabaseManager _databaseManager;

        public TrionController(IMemoryCache cache, IConfiguration configuration, DatabaseManager databaseManager, IWebHostEnvironment env)
        {
            _cache = cache;
            _configuration = configuration;
            _databaseManager = databaseManager;
        }

        #region Public Endpoints

        /// <summary>
        /// Gets a list of all files and their hashes for a given package (for repair).
        /// </summary>
        [HttpGet("RepairSPP")]
        public async Task<IActionResult> RepairSPP([FromQuery] string Emulator, [FromQuery] string Key)
        {
            return await GetAndCacheFileListAsync(Emulator, Key, "Repair",false);
        }

        /// <summary>
        /// Gets a list of all files and their hashes for a given package (for Install).
        /// </summary>
        [HttpGet("InstallSPP")]
        public async Task<IActionResult> InstallSPP([FromQuery] string Emulator, [FromQuery] string Key)
        {
            return await GetAndCacheFileListAsync(Emulator, Key, "Install", true); 
        }
        /// <summary>
        /// [SECURE] Downloads a single, specific file from within a package directory.
        /// </summary>
        [HttpPost("DownloadFile")]
        public async Task<IActionResult> DownloadFile([FromQuery] string Emulator, [FromQuery] string Key, [FromBody] FileRequest request)
        {
            try
            {
                TrionLogger.Log($"Downloading {Emulator}, {Key}, {request}");
                bool isEarlyAccess = !string.IsNullOrEmpty(Key) && await _databaseManager.GetKeyVerified(Key);
                string? baseLocation = GetRepackLocation(Emulator, isEarlyAccess, "BaseLocation"); 

                if (string.IsNullOrEmpty(baseLocation) || !Directory.Exists(baseLocation))
                {
                    return NotFound("The specified emulator package could not be found.");
                }

                string fileName = Path.GetFileName(request.FilePath);
                byte[] fileBytes = System.IO.File.ReadAllBytes(request.FilePath);

                return File(fileBytes, "application/octet-stream", fileName);

            }
            catch (UnauthorizedAccessException ex)
            {
                TrionLogger.Log($"Access denied: {ex.Message}");
                return StatusCode(403, $"Access denied: {ex.Message}");
            }
            catch (Exception ex)
            {
                TrionLogger.Log($"Error: {ex.Message}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
        [HttpPost("SendSupporterKey")]
        public async Task<IActionResult> SendSupporterKey([FromBody] SupporterKey supKey, [FromHeader] string? APIKey)
        {
            TrionLogger.Log($"Suporter Key Request with Api key {APIKey}!");
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var configuredApiKey = _configuration["APIKey"];
            if (string.IsNullOrEmpty(configuredApiKey) || configuredApiKey != APIKey)
            {
                return Unauthorized(new { message = "Invalid or missing API Key." });
            }
            TrionLogger.Log($"Veryfy Supporter key {supKey.Key}!");
            try
            {
                var keyExists = await _databaseManager.GetKeyVerified(supKey.Key);
                if (keyExists)
                {
                    return Conflict(new { message = "This Supporter Key already exists." });
                }
                TrionLogger.Log($"Insert Supporter key {supKey.Key}!");
                await _databaseManager.InsertSupporterKey(supKey.Key, supKey.UID);
                return CreatedAtAction(nameof(SendSupporterKey), new { key = supKey.Key },
                    new { message = "Supporter Key created successfully." });
            }
            catch (Exception ex)
            {
                TrionLogger.Log($"Error in SendSupporterKey: {ex.ToString()}");
                return StatusCode(500, new { message = "An internal server error occurred." });
            }
        }

        [HttpGet("GetFileVersion")]
        public async Task<IActionResult> GetRepackVersion([FromQuery] string Key)
        {
            try
            {
                bool isEarlyAccess = !string.IsNullOrEmpty(Key) && await _databaseManager.GetKeyVerified(Key);
                string GetVersion(string keyBase) => FileManager.GetVersion(_configuration[$"{keyBase}:Version:{(isEarlyAccess ? "EarlyAccess" : "Default")}"]!);

                return Ok(new
                {
                    Trion = GetVersion("trion"),
                    Database = GetVersion("database"),
                    ClassicSPP = GetVersion("classicSPP"),
                    TbcSPP = GetVersion("tbcSPP"),
                    WotlkSPP = GetVersion("wotlkSPP"),
                    CataSPP = GetVersion("cataSPP"),
                    MopSPP = GetVersion("mopSPP"),
                });
            }
            catch (Exception ex)
            {
                TrionLogger.Log($"Error in GetFileVersion: {ex.Message}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("GetExternalIPv4")]
        public IActionResult GetExternalIPv4()
        {
            string? clientIp = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()
                              ?? HttpContext.Connection.RemoteIpAddress?.ToString();

            return string.IsNullOrEmpty(clientIp)
                ? BadRequest("Unable to determine the IP address.")
                : Ok(new { IPv4Address = clientIp });
        }
        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Gets and caches the list of files for a given emulator package.
        /// </summary>
        private async Task<IActionResult> GetAndCacheFileListAsync(string emulator, string key, string operationType, bool install)
        {
            try
            {
                if (string.IsNullOrEmpty(emulator)) return BadRequest("Invalid or missing emulator type.");

                bool isEarlyAccess = !string.IsNullOrEmpty(key) && await _databaseManager.GetKeyVerified(key);
                var fileCacheKey = $"Files_{emulator}_{isEarlyAccess}_{operationType}";

                if (_cache.TryGetValue(fileCacheKey, out ConcurrentBag<FileList>? files))
                {
                    TrionLogger.Log($"Cache hit for file list: '{fileCacheKey}'.");
                    return Ok(new { Files = files });
                }

                string? repackLocation = GetRepackLocation(emulator, isEarlyAccess, operationType);
                if (string.IsNullOrEmpty(repackLocation) || !Directory.Exists(repackLocation))
                {
                    return BadRequest($"Invalid emulator type or path not found for {operationType}.");
                }

                files = await FileManager.GetFilesAsync(repackLocation, install);
                TrionLogger.Log($"Fetched {files.Count} files for '{fileCacheKey}'. Caching now.");

                var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(1));
                _cache.Set(fileCacheKey, files, cacheOptions);

                return Ok(new { Files = files });
            }
            catch (Exception ex)
            {
                TrionLogger.Log($"Error in GetAndCacheFileListAsync: {ex.Message}");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Centralized helper to get the configured directory path for a package.
        /// </summary>
        private string? GetRepackLocation(string emulator, bool isEarlyAccess, string operationType)
        {
            string accessLevel = isEarlyAccess ? "EarlyAccess" : "Default";
            string configKey = $"{emulator.ToLower()}SPP:{accessLevel}:{operationType}";

            // A special case for "database" and "trion" which don't follow the "SPP" suffix pattern
            if (emulator.Equals("database", StringComparison.OrdinalIgnoreCase) || emulator.Equals("trion", StringComparison.OrdinalIgnoreCase))
            {
                configKey = $"{emulator.ToLower()}:{accessLevel}:{operationType}";
            }

            return _configuration[configKey];
        }

        #endregion


        #region Speed Test Endpoints

        /// <summary>
        /// An endpoint for measuring latency (ping). It returns an empty 200 OK response as quickly as possible.
        /// </summary>
        [HttpGet("Ping")]
        public IActionResult Ping()
        {
            // Adding headers to prevent any caching by proxies or browsers
            Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, proxy-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("Expires", "0");
            return Ok();
        }

        /// <summary>
        /// Provides a stream of random data of a specified size for download speed testing.
        /// </summary>
        /// <param name="sizeInMB">The desired size of the test file in megabytes.</param>
        [HttpGet("DownloadSpeedTest")]
        public IActionResult DownloadSpeedTest([FromQuery] int sizeInMB = 100)
        {
            // We put a reasonable limit to prevent abuse (e.g., requesting a 10,000 MB file)
            if (sizeInMB > 1000)
            {
                sizeInMB = 1000;
            }

            var stream = Network.GenerateRandomStream(sizeInMB);

            return new FileStreamResult(stream, "application/octet-stream")
            {
                FileDownloadName = $"speedtest_{sizeInMB}MB.bin",
                // This is important! The client needs to know the exact size for its calculation.
                EnableRangeProcessing = false // Not a real file, so range processing is not supported
            };
        }

        #endregion
    }
}