﻿using HmChatGPTWriting;
using System.Reflection;
using System.Resources;

namespace HmOpenAIChatGptWriting;

class AppForm : Form
{
    const string NewLine = "\r\n";

    IOutputWriter output;
    IInputReader input;

    HmChatGPTWriteSharedMemory sm;
    string model = "";
    int iMaxTokens = 2000;
    int iAutoMsgListRemove = 1;

    public AppForm(string key, string models, int maxtokens, int remove_auto_messagelist, IOutputWriter output, IInputReader input, HmChatGPTWriteSharedMemory sm)
    {
        // 「入力」や「出力」の対象を外部から受け取り
        this.output = output;
        this.input = input;

        this.sm = sm;
        this.model = models;

        this.iMaxTokens = maxtokens;

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
        int DisplayDpi = 96;
        if (this.DeviceDpi > 96)
        {
            DisplayDpi = this.DeviceDpi;
        }

        this.Text = "*-- HmChatGPTWriting --*";
        this.Width = (int)((300 * DisplayDpi) / 96);
        this.Height = (int)((120 * DisplayDpi) / 96);
        this.Opacity = 0.8;
        this.MaximumSize = new Size(Width, Height) ;
        this.Shown += AppForm_Shown;
        this.FormClosing += AppForm_FormClosing;
        this.Padding = new System.Windows.Forms.Padding(5);
        this.AutoScaleMode = AutoScaleMode.Dpi;
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

        try
        {
            // メッセージリストのオートリムーバーのキャンセルをしておく。(うーんぐだぐだだなー)
            // OpenAIChatMain.CancelMessageListCancelToken();
        }
        catch (Exception)
        {
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
            Width = 32,
            Height = 32,
            Left = 100,
            Top = 42
        };

        if (this.DeviceDpi > 96)
        {
            pb.Width = (int)(this.DeviceDpi / 3);
            pb.Height = (int)(this.DeviceDpi / 3);
            pb.SizeMode = PictureBoxSizeMode.StretchImage;
        }


        pb.Image = Resource.thinking;

        if (btnCancel != null)
        {
            pb.Location = new Point((this.ClientSize.Width - pb.Width) / 2, (this.ClientSize.Height - pb.Height) / 2 + (btnCancel.Height / 2));
        }

        this.Controls.Add(pb);

    }



    // 中断ボタン
    private Button? btnCancel;

    void SetCancelButton()
    {
        int width = 96;
        int height = 24;
        if (this.DeviceDpi > width)
        {
            width = this.DeviceDpi;
            height = (int)(this.DeviceDpi / 4);
        }

        btnCancel = new Button()
        {
            Text = "中断",
            UseVisualStyleBackColor = true,
            Top = 5,
            Left = 100,
            Width = width,
            Height = height
        };

        btnCancel.Location = new Point((this.ClientSize.Width - btnCancel.Width) / 2, btnCancel.Top);
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
        output.Push();

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
        bool error = false;
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
            error = true;
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
            error = true;
            string err = ex.Message + NewLine + ex.StackTrace;
            output.WriteLine(err);
        }

        finally
        {
            if (btnCancel != null)
            {
                btnCancel.Enabled = false;
            }

            output.FlushMessage();

            input.ClearReadBuffer();

            if (error == false) { 
                output.Pop();
            }

            this.Close();
        }
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
            output.WriteLine("-- 履歴をクリア --");
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
            ai = new OpenAIChatMain(key, model, iMaxTokens, output, iAutoMsgListRemove);
        }
        catch (Exception ex)
        {
            string err = ex.Message + NewLine + ex.StackTrace;
            output.WriteLine(err);
        }
    }
}
