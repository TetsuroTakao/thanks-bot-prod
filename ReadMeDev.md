Thanksメッセージの「鍵付き」状態管理（Firestore）宛先、メッセージ（必要に応じてAES等で暗号化）、付与ポイント、状態（is_locked: true）をFirestoreへ書き込み。署名付きURL（Signed URL）の発行Google Cloud Storage SDKで、期限付きアクセスURL（Read token付きURL）を動的に発行し、カード内のアイコン画像URLとして埋め込み。解除（アンロック）とポイント計算処理送信者・受信者双方の解除アクション（ボタンタップ）を検知。両者の解除完了時に is_locked: false へ更新し、Firestoreのトランザクションで各ユーザーの「残高ポイント」を加減算。BigQueryへのロギングポイント移動確定時に、BigQueryのStream/Insert APIで履歴ログを出力。Step 4: Google Workspace（組織制限） & Chat Bot設定管理者権限を使って、指定したOU（組織単位）のみにBotを配布します。Google Chat API の設定  GCPコンソールの「Google Chat API」>「構成」を開く。App name: Thanks Bot（任意）Avatar URL: Cloud Storageの公開URL等Connection settings: HTTP Endpoint を選択し、Cloud RunのURLを指定。Slash commands: /thanks および /Thanks を登録（IDを付与）。アクセス制御（組織の限定）Visibility（公開範囲）: 「Specific people and groups in your organization（組織内の特定のユーザーおよびグループ）」を選択。ここで 新規事業推進室/人財開発G のOU（または対象のGoogleグループ）を指定。これにより組織外や関係のないOUのユーザーはBotの検索・追加が不可能になります。認証権限の設定Cloud RunのIAM設定で、Google Chatサービスアカウント（chat@system.gserviceaccount.com）に Cloud Run 起動元 (Cloud Run Invoker) ロールを付与。 

gcloud org-policies delete iam.disableServiceAccountKeyCreation --organization=147679028832

gcloud organizations add-iam-policy-binding 147679028832 --member="user:tetsuro.takao@metalgod.net" --role="roles/orgpolicy.policyAdmin"

cloudflared.exe service install eyJhIjoiMzkwMGQ4MGE1MTg5NDg2ODIyZjIwN2Y4NTAxNmFiMzgiLCJ0IjoiOWM3ODQ2NzUtMjQzMy00YzZhLTg3NzAtZjMxZDdmMDRhZGU4IiwicyI6Ik16QTJNVGN5TldRdFpHSmlNQzAwWVRabUxXSmtOREV0Wm1NM1l6bGxNbVk1TXpKaCJ9

# プロジェクトIDとプロジェクト番号の変数設定
PROJECT_ID="thanks-bot-prod"
PROJECT_NUMBER=$(gcloud projects describe $PROJECT_ID --format="value(projectNumber)")
SA_EMAIL="${PROJECT_NUMBER}-compute@developer.gserviceaccount.com"

# Firebase Admin ロールを付与
gcloud projects add-iam-policy-binding $PROJECT_ID \
    --member="serviceAccount:${SA_EMAIL}" \
    --role="roles/firebase.admin"