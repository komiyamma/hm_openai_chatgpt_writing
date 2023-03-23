using HmNetCOM;
using static System.Net.Mime.MediaTypeNames;

namespace HmOpenAIChatGptWriting
{
    internal class HmInputReader : IInputReader
    {
        static string readBuffer = "";

        public void ClearReadBuffer()
        {
            readBuffer = "";
        }
        public string ReadText()
        {
            if (String.IsNullOrEmpty(readBuffer))
            {

                string? text = (String)Hm.Macro.Var["$HmTargetAnalyzeText"];
                if (String.IsNullOrEmpty(text))
                {
                    return "";
                }


                readBuffer = text;
            }

            return readBuffer;
        }
    }
}
