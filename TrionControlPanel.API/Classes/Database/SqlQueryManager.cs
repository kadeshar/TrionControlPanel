namespace TrionControlPanel.API.Classes.Database
{
    public class SqlQueryManager
    {
        public static string SELECT_SUPPORT_KEY => "SELECT EXISTS(SELECT 1 FROM SupporterKey WHERE `ApiKey` = @ApiKey)";
        public static string INSERT_SUPPORT_KEY => "INSERT INTO SupporterKey (`ApiKey`, `UID`) VALUES (@ApiKey, @UID);";
    }
}
