using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using ArtAi.util.json;

namespace ArtAi.Server
{
    // artai_config.json of the local server.
    // Same keys and defaults as DEFAULT_CONFIG in Server/artai.py
    [DataContract]
    public class LocalServerConfig
    {
        public const string ModelFileQ5 = "flux-2-klein-4b-Q5_K_M.gguf";
        public const string ModelFileQ4 = "flux-2-klein-4b-Q4_K_M.gguf";

        public const string DeviceGpu = "gpu";
        public const string DeviceCpu = "cpu";

        [DataMember] public string host = "127.0.0.1";
        [DataMember] public int port = 8080;
        [DataMember] public string backend = "flux";
        // the server writes it when it creates the file: gpu if it finds an NVIDIA GPU with CUDA, otherwise cpu
        [DataMember] public string device = DeviceGpu;
        // used with device gpu
        [DataMember] public string gpu_preset = "auto";
        [DataMember] public int gpu_width = 768;
        [DataMember] public int gpu_height = 768;
        [DataMember] public string gpu_gguf_file = ModelFileQ5;
        // used with device cpu
        [DataMember] public int cpu_width = 512;
        [DataMember] public int cpu_height = 512;
        [DataMember] public string cpu_gguf_file = ModelFileQ4;
        [DataMember] public int steps = 4;
        [DataMember] public string style = "oil painting, muted colors, rimworld colony art, ";
        [DataMember] public string model_repo = "black-forest-labs/FLUX.2-klein-4B";
        [DataMember] public string gguf_repo = "unsloth/FLUX.2-klein-4B-GGUF";
        [DataMember] public string hf_home = "";
        // OpenAI-compatible API of the text model; 0 = off
        [DataMember] public int openai_port = 8000;
        [DataMember] public string openai_api_key = "";
        [DataMember] public int llm_max_tokens = 4096;

        public static LocalServerConfig Load(string path)
        {
            if (!File.Exists(path))
            {
                return new LocalServerConfig();
            }

            string json = File.ReadAllText(path);
            var config = json.FromJson<LocalServerConfig>() ?? new LocalServerConfig();
            // only the key names are used: the untyped parser is fine for them
            var keys = json.FromJson<Dictionary<string, object>>()?.Keys;
            config.FillMissing(keys == null ? new HashSet<string>() : new HashSet<string>(keys));
            return config;
        }

        // Pretty-printed like the file the server creates, so it stays easy to edit by hand
        public void Save(string path)
        {
            var lines = new List<string>
            {
                Line("host", host),
                Line("port", port),
                Line("backend", backend),
                Line("device", device),
                Line("gpu_preset", gpu_preset),
                Line("gpu_width", gpu_width),
                Line("gpu_height", gpu_height),
                Line("gpu_gguf_file", gpu_gguf_file),
                Line("cpu_width", cpu_width),
                Line("cpu_height", cpu_height),
                Line("cpu_gguf_file", cpu_gguf_file),
                Line("steps", steps),
                Line("style", style),
                Line("model_repo", model_repo),
                Line("gguf_repo", gguf_repo),
                Line("hf_home", hf_home),
                Line("openai_port", openai_port),
                Line("openai_api_key", openai_api_key),
                Line("llm_max_tokens", llm_max_tokens),
            };
            File.WriteAllText(path, "{\n" + string.Join(",\n", lines) + "\n}\n");
        }

        public LocalServerConfig Copy()
        {
            return (LocalServerConfig)MemberwiseClone();
        }

        private static string Line(string key, object value)
        {
            return "  \"" + key + "\": " + value.ToJson();
        }

        // The JSON parser does not run field initializers: keys absent in the file get the defaults here
        private void FillMissing(HashSet<string> present)
        {
            var defaults = new LocalServerConfig();
            if (!present.Contains("host")) host = defaults.host;
            if (!present.Contains("port")) port = defaults.port;
            if (!present.Contains("backend")) backend = defaults.backend;
            if (!present.Contains("device")) device = defaults.device;
            if (!present.Contains("gpu_preset")) gpu_preset = defaults.gpu_preset;
            if (!present.Contains("gpu_width")) gpu_width = defaults.gpu_width;
            if (!present.Contains("gpu_height")) gpu_height = defaults.gpu_height;
            if (!present.Contains("gpu_gguf_file")) gpu_gguf_file = defaults.gpu_gguf_file;
            if (!present.Contains("cpu_width")) cpu_width = defaults.cpu_width;
            if (!present.Contains("cpu_height")) cpu_height = defaults.cpu_height;
            if (!present.Contains("cpu_gguf_file")) cpu_gguf_file = defaults.cpu_gguf_file;
            if (!present.Contains("steps")) steps = defaults.steps;
            if (!present.Contains("style")) style = defaults.style;
            if (!present.Contains("model_repo")) model_repo = defaults.model_repo;
            if (!present.Contains("gguf_repo")) gguf_repo = defaults.gguf_repo;
            if (!present.Contains("hf_home")) hf_home = defaults.hf_home;
            if (!present.Contains("openai_port")) openai_port = defaults.openai_port;
            if (!present.Contains("openai_api_key")) openai_api_key = defaults.openai_api_key;
            if (!present.Contains("llm_max_tokens")) llm_max_tokens = defaults.llm_max_tokens;
        }
    }
}
