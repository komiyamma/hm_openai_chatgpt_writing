namespace HmOpenAIChatGptWriting;

class AppForm : Form
{
    const string NewLine = "\r\n";

    IOutputWriter output;
    IInputReader input;

    public AppForm(string key, IOutputWriter output, IInputReader input)
    {
        // 「入力」や「出力」の対象を外部から受け取り
        this.output = output;
        this.input = input;

        try
        {
            SetForm();
            SetOkButton();
            SetCancelButton();
            SetOpenAI(key);
        }
        catch (Exception ex)
        {
            output.WriteLine(ex.ToString());
        }

    }

    // フォーム属性の設定
    void SetForm()
    {
        this.Text = "*-- HmChatGPTWriting --*";
        this.Width = 500;
        this.Height = 210;
        this.FormClosing += AppForm_FormClosing;
    }

    private void AppForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (ai == null)
        {
            return;
        }

        try
        {
            // 中断したことと同じことをしておく
            BtnCancel_Click(null, e);
        }
        catch (Exception)
        {
            // ここは必ず例外でるので不要。
        }
    }

    string textBuffer = "";
    public void AskQuestionToGpt()
    {
        textBuffer = input.ReadText();
        // テキストがアップデートされたらすぐに送る。
        BtnOk_Click(null, new EventArgs());
    }


    // 送信ボタン
    private Button? btnOk;
    void SetOkButton()
    {
        btnOk = new Button()
        {
            Text = "送信 (Ctrl+⏎)",
            UseVisualStyleBackColor = true,
            Top = 2,
            Left = 2,
            Width = 96,
            Height = 20
        };

        btnOk.Click += BtnOk_Click;
        this.Controls.Add(btnOk);

    }

    // 中断ボタン
    private Button? btnCancel;

    void SetCancelButton()
    {
        btnCancel = new Button()
        {
            Text = "中断",
            UseVisualStyleBackColor = true,
            Top = 2,
            Left = 100,
            Width = 96,
            Height = 20
        };

        btnCancel.Enabled = false;
        btnCancel.Click += BtnCancel_Click;
        this.Controls.Add(btnCancel);

    }

    // 送信したらフリーズ時間や解答時間が長いことがあるので、中断用にCancellationTokenSource/CancellationTokenを用意
    static CancellationTokenSource? cs;

    // 中断ボタンおしたら中断用にCancellationTokenSourceにCancel発行する
    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        if (ai != null)
        {
            if (cs != null)
            {
                cs.Cancel();
            }
        }
    }

    // 入力された質問を処理する。
    // AIに質問内容を追加し、TextBox⇒アウトプット枠へとメッセージを移動したかのような表示とする。
    private void PostQuestion()
    {
        var trim = textBuffer.TrimEnd();
        if (String.IsNullOrEmpty(trim))
        {
            return;
        }

        if (ai != null)
        {
            ai.AddQuestion(trim);
            output.WriteLine(trim + NewLine);
        }
        if (textBuffer != null)
        {
            textBuffer = "";
        }
    }

    // ChatGPTの解答を得る。中断できるようにCancellationTokenを渡す。
    private async Task GetAnswer()
    {
        try
        {
            if (btnCancel != null)
            {
                btnCancel.Enabled = true;
            }

            cs = new();
            if (ai != null)
            {

                await Task.Run(async () =>
                {
                    await ai.AddAnswer(cs.Token);
                }, cs.Token);
            }

        }
        catch (OperationCanceledException ex)
        {
            if (ex.Message == "The operation was canceled." || ex.Message == "A task was canceled.")
            {
                if (ai != null) { 
                    output.WriteLine(ai.GetAssistanceAnswerCancelMsg());
                }
            }
            else
            {
                output.WriteLine(ex.Message);
            }
            // キャンセルトークン経由なら正規の中断だろうからなにもしない
        }
        catch (Exception ex)
        {
            string err = ex.Message + NewLine + ex.StackTrace;
            output.WriteLine(err);
        }

        finally
        {
            if (btnCancel != null)
            {
                btnCancel.Enabled = false;
            }
            if (btnOk != null)
            {
                btnOk.Enabled = true;
            }
        }

    }

    // 送信ボタンを押すと、質問内容をAIに登録、答えを得る処理へ
    private void BtnOk_Click(object? sender, EventArgs e)
    {
        if (ai == null) { return; }

        try
        {
            if (btnOk != null) { btnOk.Enabled = false; }

            // このWritingでは会話履歴は無駄なのでためない
            OpenAIChatMain.InitMessages();

            // 質問をためる
            PostQuestion();

            // 答えを得る
            _ = GetAnswer();
        }
        catch (Exception ex)
        {
            string err = ex.Message + NewLine + ex.StackTrace;
            output.WriteLine(err);
        }
        finally
        {
        }
    }

    // 送信ボタンを押すと、質問内容をAIに登録、答えを得る処理へ
    private void BtnChatClear_Click(object? sender, EventArgs e)
    {
        if (ai == null) { return; }

        try
        {
            OpenAIChatMain.InitMessages();
            output.WriteLine("-- 会話履歴をクリア --");
        }
        catch (Exception ex)
        {
            string err = ex.Message + NewLine + ex.StackTrace;
            output.WriteLine(err);
        }
        finally
        {
        }
    }

    // aiの処理。キレイには切り分けられてないが、Modelに近い。
    OpenAIChatMain? ai;

    // 最初生成
    void SetOpenAI(string key)
    {
        try
        {
            ai = new OpenAIChatMain(key, output);
        }
        catch (Exception ex)
        {
            string err = ex.Message + NewLine + ex.StackTrace;
            output.WriteLine(err);
        }
    }
}
