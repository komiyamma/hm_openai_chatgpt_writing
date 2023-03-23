using HmNetCOM;

namespace HmOpenAIChatGptWriting
{
    internal class HmInputReader : IInputReader
    {
        public string ReadText()
        {
            string? text = (String)Hm.Macro.Var["$HmSelectedText"];
            if (String.IsNullOrEmpty(text))
            {
                return "";
            }

            return text;
        }
    }
}
