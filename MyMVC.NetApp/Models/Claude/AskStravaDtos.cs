namespace MyMVC.NetApp.Models.Claude;

public record ChatTurn(string Question, string Answer, DateTime AskedAtUtc);

public record AskStravaViewModel(IReadOnlyList<ChatTurn> Thread);
