using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);

// 1. Cloud Run の PORT 設定（0.0.0.0 でバインド）
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// 2. FirebaseApp の初期化（例外でアプリを落とさないよう安全に処理）
try
{
    if (FirebaseApp.DefaultInstance == null)
    {
        GoogleCredential credential;
        string? credentialPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

        // ローカルに JSON キーがある場合（Dev Containers 等）
        if (!string.IsNullOrEmpty(credentialPath) && File.Exists(credentialPath))
        {
            var serviceAccountCredential = CredentialFactory.FromFile<ServiceAccountCredential>(credentialPath);
            credential = serviceAccountCredential.ToGoogleCredential();
        }
        else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "thanks-bot-prod-153b434ab618.json")))
        {
            string localKey = Path.Combine(Directory.GetCurrentDirectory(), "thanks-bot-prod-153b434ab618.json");
            var serviceAccountCredential = CredentialFactory.FromFile<ServiceAccountCredential>(localKey);
            credential = serviceAccountCredential.ToGoogleCredential();
        }
        else
        {
            // Cloud Run 上（ファイルがない場合）は ADC (自動認証)
            credential = GoogleCredential.GetApplicationDefault();
        }

        FirebaseApp.Create(new AppOptions()
        {
            Credential = credential,
            ProjectId = "thanks-bot-prod"
        });
    }
}
catch (Exception ex)
{
    // 初期化エラーが発生してもアプリ自体は落とさずログに出力する
    Console.WriteLine($"[ERROR] Firebase Initialization Failed: {ex.Message}");
}

// 3. ヘルスチェック用エンドポイント（Cloud Run起動確認用）
app.MapGet("/health", () => Results.Ok("OK"));

// 4. メインのエンドポイント
app.MapPost("/", async ([FromBody] JsonNode payload) =>
{
    string? eventType = payload["type"]?.ToString();

    if (eventType == "MESSAGE")
    {
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
        try
        {
            firebaseUser = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(senderEmail);
        }
        catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
        {
            var userArgs = new UserRecordArgs()
            {
                Email = senderEmail,
                DisplayName = payload["message"]?["sender"]?["displayName"]?.ToString() ?? senderEmail,
                EmailVerified = true
            };

            firebaseUser = await FirebaseAuth.DefaultInstance.CreateUserAsync(userArgs);
        }
        catch (Exception ex)
        {
            return Results.Ok(new { text = $"認証処理中にエラーが発生しました: {ex.Message}" });
        }

        string firebaseUid = firebaseUser.Uid;

        if (appCommandId == "1" || appCommandId == "2")
        {
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
                                subtitle = "相手とポイントを選択してください"
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
                                    }
                                }
                            }
                        }
                    }
                }
            };

            return Results.Ok(cardResponse);
        }

        if (appCommandId == "4")
        {
            return Results.Ok(new { text = $"認証成功: {senderEmail} (Firebase UID: {firebaseUid})" });
        }
    }

    return Results.Ok(new { text = "コマンドを受信しました。" });
});

app.Run();