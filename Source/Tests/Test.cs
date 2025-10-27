using System;
using ArtAi.util.json;

namespace ArtAiTests
{
    public class ArtAiTest
    {
        public void JsonTest()
        {
            // Test parsing string
            string jsonStr = "\"hello\"";
            string resultStr = jsonStr.FromJson<string>();
            if (resultStr == "hello")
            {
                Console.WriteLine("Pass");
            }
            else
            {
                throw new Exception("Fail");
            }
        }

        static void Main()
        {
            var test = new ArtAiTest();
            test.JsonTest();
        }
    }
}
