using HmChatGPTWriting;
using System.Reflection;
using System.Resources;

namespace HmOpenAIChatGptWriting;

class AppForm : Form
{
    const string NewLine = "\r\n";

    IOutputWriter output;
    IInputReader input;

    HmChatGPTWriteSharedMemory sm;

    public AppForm(string key, IOutputWriter output, IInputReader input, HmChatGPTWriteSharedMemory sm)
    {
        // 「入力」や「出力」の対象を外部から受け取り
        this.output = output;
        this.input = input;

        this.sm = sm;

        try
        {
            SetForm();
            SetCancelButton();
            SetPictureBox();
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
        this.Width = 300;
        this.Height = 120;
        this.Opacity = 0.9;
        this.MaximumSize = new Size(Width, Height) ;
        this.Shown += AppForm_Shown;
        this.FormClosing += AppForm_FormClosing;
    }

    private void AppForm_Shown(object? sender, EventArgs e)
    {
        try {
            if (sm != null)
            {
                sm.CreateSharedMemory();
            }
        } catch(Exception )
        {

        }
    }

    private void AppForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            if (sm != null)
            {
                sm.DeleteSharedMemory();
            }
        }
        catch(Exception)
        {

        }

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
        if (ai == null) { return; }

        try
        {
            textBuffer = input.ReadText();

            if (textBuffer == "HmChatGPTWriting:ClearConversationChatGPT")
            {
                ClearChatHistory();

                this.Close();

                return;
            }

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

    PictureBox pb = new PictureBox();

    void SetPictureBox()
    {

        pb = new PictureBox()
        {
            Width = 33,
            Height = 33,
            Left = 100,
            Top = 40
        };

        pb.Image = Resource.thinking;
        pb.Location = new Point((this.ClientSize.Width - pb.Width) / 2, (this.ClientSize.Height - pb.Height) / 2 + 6);

        this.Controls.Add(pb);

    }



    // 中断ボタン
    private Button? btnCancel;

    void SetCancelButton()
    {
        btnCancel = new Button()
        {
            Text = "中断",
            UseVisualStyleBackColor = true,
            Top = 3,
            Left = 100,
            Width = 96,
            Height = 20
        };

        btnCancel.Location = new Point((this.ClientSize.Width - btnCancel.Width) / 2, Top);
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
        }
        output.FlushMessage();

        input.ClearReadBuffer();

        this.Close();

    }

    // 送信ボタンを押すと、質問内容をAIに登録、答えを得る処理へ
    private void BtnOk_Click(object? sender, EventArgs e)
    {
    }

    // 送信ボタンを押すと、質問内容をAIに登録、答えを得る処理へ
    public void ClearChatHistory()
    {
        if (ai == null) { return; }

        try
        {
            OpenAIChatMain.InitMessages();
            input.ClearReadBuffer();
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
