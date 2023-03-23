namespace HmOpenAIChatGptWriting;
interface IInputReader
{
    string ReadText();

    void ClearReadBuffer();
}
