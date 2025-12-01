using System;
using System.Collections.Generic;
using ArtAi.data.comfyui;
using ArtAi.util.json;

namespace ArtAiTests
{
    public class ArtAiTest
    {
        public void JsonTest()
        {
            string jsonStr = "{\"artAiQueuePosition\":2,\"ed59bd74-5903-341d-964b-180b8298d494\":{\"artAiQueuePosition\":2,\"outputs\":null}}";

            var result = jsonStr.FromJson<Dictionary<string, HistoryRsItem>>();

            Console.WriteLine("Result: " + result);
            Console.WriteLine("Count: " + result.Count);
            Console.WriteLine("Has 'id': " + result.ContainsKey("ed59bd74-5903-341d-964b-180b8298d494"));

            if (result.TryGetValue("ed59bd74-5903-341d-964b-180b8298d494", out var item))
            {
                Console.WriteLine("Item: " + item);
                Console.WriteLine("Item.ArtAiQueuePosition: " + item.artAiQueuePosition);
                if (item.artAiQueuePosition == 2)
                {
                    Console.WriteLine("JsonTest: Pass");
                }
                else
                {
                    throw new Exception("Fail");
                }
                Console.WriteLine("Item.Outputs: " + item.outputs);
            }
            else
            {
                throw new Exception("Fail");
            }
        }

        public void PromptRqItemTest()
        {
            string json = @"{""3"":{""inputs"":{""text"": ""value"",""seed"":63077469383768,""steps"":30},""class_type"":""KSampler"",""_meta"":{""title"":""KSampler""}}}";

            var result = json.FromJson<Dictionary<string, PromptRqItem>>();

            Console.WriteLine("PromptRqItemTest: Deserialized, count: " + result.Count);

            if (result.TryGetValue("3", out PromptRqItem node))
            {
                if (node.inputs.text == "value")
                {
                    // ensure unknown fields inside inputs were preserved into __unknown
                    if (node.inputs.__unknown != null &&
                        node.inputs.__unknown.ContainsKey("seed") &&
                        node.inputs.__unknown.ContainsKey("steps"))
                    {
                        var seedObj = node.inputs.__unknown["seed"];
                        var stepsObj = node.inputs.__unknown["steps"];

                        long seedVal = seedObj is long ? (long)seedObj : Convert.ToInt64(seedObj);
                        int stepsVal = stepsObj is int ? (int)stepsObj : Convert.ToInt32(stepsObj);

                        if (seedVal == 63077469383768L && stepsVal == 30)
                        {
                            Console.WriteLine("PromptRqItemTest: Pass");
                        }
                        else
                        {
                            throw new Exception("PromptRqItemTest: Fail - unknown values incorrect");
                        }
                    }
                    else
                    {
                        throw new Exception("PromptRqItemTest: Fail - unknown fields missing");
                    }
                }
                else
                {
                    throw new Exception("PromptRqItemTest: Fail - data incorrect");
                }
            }
            else
            {
                throw new Exception("PromptRqItemTest: Fail - node not found");
            }
        }

        static void Main()
        {
            var test = new ArtAiTest();
            test.JsonTest();
            test.PromptRqItemTest();
        }
    }
}
