namespace MyMVC.NetApp.Models.Claude;

public class ClaudeOptions
{
    public const string SectionName = "Claude";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-opus-5";
    public int MaxTokens { get; set; } = 8192;
    public int MaxToolIterations { get; set; } = 6;
    public int MaxRowsReturned { get; set; } = 200;
    public int MaxResultCharacters { get; set; } = 8000;
}
