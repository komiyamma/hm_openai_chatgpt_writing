namespace HmOpenAIChatGptWriting; 

interface IOutputWriter
{
    void ClearMessageBuffer();

    void AddMessageBuffer(string msg);

    void FlushMessage();

    string Normalize(string? msg);

    int Write(string msg);

    int WriteLine(string msg);

}
