using HmNetCOM;
using System.Diagnostics;

namespace HmOpenAIChatGptWriting;



public class HmOutputWriter : IOutputWriter
{
    public const string NewLine = "\r\n";

    public HmOutputWriter() { }

    string messageBuffer = "";
    public void ClearMessageBuffer()
    {
        messageBuffer = "";
    }

    public void AddMessageBuffer(string msg)
    {
        if (msg == null)
        {
            messageBuffer = "";
        } else
        {
            messageBuffer = msg;
        }
    }


    public static IntPtr ReMacroScopeMethod(string message_parameter)
    {
        try
        {
            var ret = Hm.Macro.Statement("insert", message_parameter + "\n\n");
        }
        catch (Exception ex)
        {
        }

        return (IntPtr)1;
    }

    public void FlushMessage()
    {
        string msg = messageBuffer;

        // マクロ実行中ならそのまま実行できる(基本的にはこれはほぼない。偶然ユーザーが実行したマクロとタイミングが衝突した等)
        if (Hm.Macro.IsExecuting)
        {
            ReMacroScopeMethod(msg);
        }
        else
        {
            // 改めてマクロを実行し、そのマクロの中でこの関数を実行する。
            try
            {
                // デリゲートを使った関数オブジェクトを作り、
                Func<String, IntPtr> method = ReMacroScopeMethod; // と記述してもOKです。

                // この返り値の型は Hm.Macro.Exec.Eval(...) と同じ
                // message_parametr に入れる文字列の用途は好きにしてよい。分岐判別用途に使ったり、伝達パラメータに使ってもよい。
                var ret = Hm.Macro.Exec.Method(msg, method);
            }
            catch (Exception ex)
            {
                string err = ex.Message + NewLine + ex.StackTrace;
                WriteLine(err);
            }
        }

    }

    public string Normalize(string? msg)
    {
        if (msg == null)
        {
            return "";
        }

        var norm = msg.Replace("\n", NewLine);
        norm = norm.Replace("\r\r", "\r");
        return norm;
    }

    public int Write(string msg)
    {
        var norm = Normalize(msg);
        int status = Hm.OutputPane.Output(norm);
        return status;
    }

    public int WriteLine(string msg)
    {
        var norm = Normalize(msg);
        int status = Hm.OutputPane.Output(norm + NewLine);
        return status;
    }

    public int Push()
    {
        return Hm.OutputPane.Push();
    }

    public int Pop()
    {
        return Hm.OutputPane.Pop();
    }

}

