using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
namespace OpenAiCustom.Managers;

public class AwsManager
{
    public static string accessKey;
    public static string secretKey;

    public static DynamoDBContext DbContext { get; set; }
    public static AmazonDynamoDBClient Client { get; set; }
    
    public static void Initialize()
    {
        accessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY") ?? string.Empty;
        secretKey = Environment.GetEnvironmentVariable("AWS_SECRET_KEY") ?? string.Empty;
    }
}
