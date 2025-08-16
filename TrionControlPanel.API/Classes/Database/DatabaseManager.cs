namespace TrionControlPanel.API.Classes.Database
{
    public class DatabaseManager
    {
        private readonly AccessManager _accessManager;
        public DatabaseManager (AccessManager accessManager)
        {
            _accessManager = accessManager;
        }
        public async Task<bool> GetKeyVerified (string SupporterKey)
        {
            return await _accessManager.LoadSingleOrDefaultAsync<bool, dynamic>(SqlQueryManager.SELECT_SUPPORT_KEY, new { ApiKey = SupporterKey });
        }
        public async Task InsertSupporterKey (string ApiKey, long userID)
        {
            // Call your new, specific method
            int rowsAffected = await _accessManager.SaveDataAsync(SqlQueryManager.INSERT_SUPPORT_KEY, new 
            {
                ApiKey ,
                UID = userID

            });


            if (rowsAffected > 0)
            {
                TrionLogger.Log($"Successfully saved key {ApiKey} for user {userID}.");
            }
            else
            {
                Console.WriteLine($"Failed to save the key for user {userID}.");
            }
        }
    }
}
