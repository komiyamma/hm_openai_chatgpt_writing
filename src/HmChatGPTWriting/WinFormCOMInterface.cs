using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using HmNetCOM;

namespace HmOpenAIChatGptWriting;


[ComVisible(true)]
[Guid("0D346374-D9DF-4095-A237-05ECD19A9E42")]
public class HmChatGPTWriting
{
    private static AppForm? form;
    private static IOutputWriter? output;
    private static IInputReader? input;

    HmChatGPTWriteSharedMemory sm = new HmChatGPTWriteSharedMemory();

    public long CreateForm(string key = "", string model = "", int maxtokens = 2000)
    {
        if (form != null)
        {
        }

        if (form == null || !form.Visible)
        {
            output = new HmOutputWriter();
            input = new HmInputReader();
            form = new AppForm(key, model, maxtokens, output, input, sm);
        }

        form.Show();
        // フォームを前に持ってくるだけ
        form.BringToFront();

        form.AskQuestionToGpt();

        return 1;
    }

    // 秀丸のバージョンによって引数を渡してくるものと渡してこないものがあるので、デフォルト引数は必要。
    // (引数がないと、引数ミスマッチということで、呼び出し自体されない)
    public long DestroyForm(int result = 0)
    {
        if (form != null)
        {
            form.Close();
            form = null;

            sm.DeleteSharedMemory();
        }

        return 1;
    }
}

[ComVisible(true)]
[Guid("044C142D-059B-4A6D-BBAC-9B3F276EE030")]
public class HmChatGPTWriteSharedMemory
{
    private static MemoryMappedFile? share_mem;

    public void CreateSharedMemory()
    {
        try
        {
            // 新規にメモリマップを作成して、そこに現在の秀丸ハンドルを数値として入れておく
            share_mem = MemoryMappedFile.CreateNew("HmChatGPTWriteSharedMem", 8);
            MemoryMappedViewAccessor accessor = share_mem.CreateViewAccessor();
            accessor.Write(0, (long)Hm.WindowHandle);
            accessor.Dispose();
        }
        catch (Exception) { }
    }

    public long GetSharedMemory()
    {
        long value = 0;
        try
        {
            // (主に)違うプロセスからメモリマップの数値を読み込む
            share_mem = MemoryMappedFile.OpenExisting("HmChatGPTWriteSharedMem");
            MemoryMappedViewAccessor accessor = share_mem.CreateViewAccessor();
            value = accessor.ReadInt64(0);
            accessor.Dispose();
        }
        catch (Exception) { }

        return value;
    }

    public void DeleteSharedMemory()
    {
        try
        {
            if (share_mem != null)
            {
                // メモリマップを削除。
                MemoryMappedViewAccessor accessor = share_mem.CreateViewAccessor();
                accessor.Write(0, (long)0);
                accessor.Dispose();
                share_mem.Dispose();
                share_mem = null;
            }
        }
        catch (Exception) { }
    }


    public long GetFormHideamruHandle()
    {
        return GetSharedMemory();
    }
}

