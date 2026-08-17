using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
if (FirebaseApp.DefaultInstance == null)
{
    GoogleCredential credential;

    // 1. 環境変数からキーファイルのパスを取得
    string? credentialPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

    // 2. パスの決定（環境変数がない場合は直下のファイルを参照）
    if (string.IsNullOrEmpty(credentialPath) || !File.Exists(credentialPath))
    {
        credentialPath = Path.Combine(Directory.GetCurrentDirectory(), "thanks-bot-prod-153b434ab618.json");
        var serviceAccountCredential = CredentialFactory.FromFile<ServiceAccountCredential>(credentialPath);
        credential = serviceAccountCredential.ToGoogleCredential();
    }
    else
    {
        credential = GoogleCredential.GetApplicationDefault();
    }
    FirebaseApp.Create(new AppOptions()
    {
        Credential = credential,
        ProjectId = "thanks-bot-prod"
    });
}
if (FirebaseApp.DefaultInstance == null)
{
    FirebaseApp.Create(new AppOptions()
    {
        Credential = GoogleCredential.GetApplicationDefault()
    });
}
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/", async ([FromBody] JsonNode payload) =>
{
    // 1. Google Chat からのイベントタイプを取得
    string? eventType = payload["type"]?.ToString();

    // 2. スラッシュコマンド（MESSAGE）イベントの判定
    if (eventType == "MESSAGE")
    {
        // appCommandId の取得
        string? appCommandId = payload["message"]?["slashCommand"]?["commandId"]?.ToString();
        string? senderEmail = payload["message"]?["sender"]?["email"]?.ToString() 
                            ?? payload["user"]?["email"]?.ToString();
        if (string.IsNullOrEmpty(senderEmail))
        {
            return Results.BadRequest(new { text = "ユーザー情報（メールアドレス）が取得できませんでした。" });
        }
        if (!senderEmail.EndsWith("@metalgod.net", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Ok(new { text = "許可されていないドメインのユーザーです。" });
        }
        UserRecord firebaseUser;
        // /thanks または /Thanks の判定（例: コマンドIDが "1" の場合）
        try
        {
            // メールアドレスから Firebase ユーザーを取得
            firebaseUser = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(senderEmail);
        }
        catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
        {
            // Firebase Auth にユーザーが存在しない場合は自動作成（プロビジョニング）
            var userArgs = new UserRecordArgs()
            {
                Email = senderEmail,
                DisplayName = payload["message"]?["sender"]?["displayName"]?.ToString() ?? senderEmail,
                EmailVerified = true // Google Workspace 経由のため検証済みとする
            };

            firebaseUser = await FirebaseAuth.DefaultInstance.CreateUserAsync(userArgs);
        }
        catch (Exception ex)
        {
            // その他のエラーハンドリング
            return Results.Ok(new { text = $"認証処理中にエラーが発生しました: {ex.Message}" });
        }
        string firebaseUid = firebaseUser.Uid;

        if (appCommandId == "1" || appCommandId == "2")
        {
            // 返却する CardsV2 JSON構造体を構築
            var cardResponse = new
            {
                cardsV2 = new[]
                {
                    new
                    {
                        cardId = "thanksCard",
                        card = new
                        {
                            header = new
                            {
                                title = "感謝（Thanks）を送る",
                                subtitle = "相手とポイントを選択してください",
                                imageUrl = "https://storage.googleapis.com/.../icon.png" // Signed URLなどを指定
                            },
                            sections = new[]
                            {
                                new
                                {
                                    widgets = new object[]
                                    {
                                        new
                                        {
                                            textParagraph = new
                                            {
                                                text = "メッセージを入力して送信してください。"
                                            }
                                        }
                                        // TODO: 宛先選択やメッセージ入力欄、送信ボタンなどのウィジェットを配置
                                    }
                                }
                            }
                        }
                    }
                }
            };

            return Results.Ok(cardResponse);
        }
        if(appCommandId == "4")
        {
            return Results.Ok(new { text = $"認証成功: {senderEmail} (Firebase UID: {firebaseUid})" });
        }
    }

    // その他のイベントやコマンドの場合のフォールバック
    return Results.Ok(new { text = "コマンドを受信しました。" });
});
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
