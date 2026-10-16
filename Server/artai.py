# /// script
# requires-python = ">=3.11"
# dependencies = [
#     "torch>=2.9",
#     "diffusers>=0.41",
#     "transformers>=5",
#     "accelerate",
#     "huggingface_hub",
#     "gguf",
#     "bitsandbytes",
# ]
#
# [tool.uv.sources]
# torch = { index = "pytorch-cuda" }
#
# [[tool.uv.index]]
# name = "pytorch-cuda"
# url = "https://download.pytorch.org/whl/cu130"
# explicit = true
# ///
"""
AI art local server: generates images with FLUX.2 klein 4B on an NVIDIA GPU (or, slowly, on the processor),
and answers text requests with its text encoder (Qwen3-4B) over an OpenAI-compatible API.

    uv run artai.py                         serve: API for the mod, /generate for the Java server,
                                            OpenAI-compatible API on openai_port
    uv run artai.py "a cat" -o cat.png      one-shot generation
    uv run artai.py --download              download the model and exit

Settings: artai_config.json next to this file (created on first start). See README.md.
"""
import argparse
import base64
import fnmatch
import hashlib
import io
import json
import os
import queue
import random
import re
import struct
import sys
import threading
import time
import traceback
import uuid
import zlib
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import parse_qs, urlparse

CONFIG_PATH = Path(__file__).resolve().parent / "artai_config.json"

# Keep in sync with Source/Server/LocalServerConfig.cs in the mod
DEFAULT_CONFIG = {
    "host": "127.0.0.1",
    "port": 8080,
    "backend": "flux",  # flux | mock (mock: solid-colour images and echo answers, no GPU, for development)
    # gpu | cpu: where the model runs. A new file gets gpu if an NVIDIA GPU with CUDA works, otherwise cpu;
    # a file without this key (e.g. written by hand) gets it the same way on every start
    "device": "gpu",
    # used with device gpu
    "gpu_preset": "auto",  # auto | high | mid | low, see PRESETS
    "gpu_width": 768,
    "gpu_height": 768,
    "gpu_gguf_file": "flux-2-klein-4b-Q5_K_M.gguf",  # or flux-2-klein-4b-Q4_K_M.gguf for less VRAM
    # used with device cpu: the processor needs minutes per image, so smaller and lighter
    "cpu_width": 512,
    "cpu_height": 512,
    "cpu_gguf_file": "flux-2-klein-4b-Q4_K_M.gguf",
    "steps": 4,
    "style": "oil painting, muted colors, rimworld colony art, ",
    "model_repo": "black-forest-labs/FLUX.2-klein-4B",
    "gguf_repo": "unsloth/FLUX.2-klein-4B-GGUF",
    "hf_home": "",  # model cache folder; empty = default Hugging Face cache
    "openai_port": 8000,  # OpenAI-compatible API; 0 = off
    "openai_api_key": "",  # if set, the OpenAI-compatible API requires "Authorization: Bearer <key>"
    "llm_max_tokens": 4096,  # longest text answer
}

# Presets of device gpu. The transformer is always the GGUF file; presets differ in how
# the 8 GB text encoder is handled. On the processor everything simply stays in RAM.
PRESETS = {
    "high": dict(te_4bit=False, offload=False),  # 16 GB+: everything stays on the GPU
    "mid": dict(te_4bit=False, offload=True),  # 10-12 GB: models take turns on the GPU
    "low": dict(te_4bit=True, offload=False),  # 6-8 GB: text encoder quantized to 4 bit
}

DOWNLOAD_ATTEMPTS = 5
DOWNLOAD_RETRY_SECONDS = 10

# question + answer of the text model, in tokens; limits the video memory a text request can take
LLM_MAX_CONTEXT = 16384

# diffusers layout of model_repo without the transformer weights (they come from the GGUF file)
PIPELINE_FILES = ["model_index.json", "scheduler/*", "tokenizer/*",
                  "text_encoder/*", "vae/*", "transformer/config.json"]


def load_config() -> dict:
    saved = json.loads(CONFIG_PATH.read_text(encoding="utf-8")) if CONFIG_PATH.exists() else {}
    cfg = {**DEFAULT_CONFIG, **saved}
    if cfg["hf_home"]:
        os.environ["HF_HOME"] = cfg["hf_home"]  # must happen before huggingface_hub is imported
    if "device" not in saved:
        cfg["device"] = detect_device()
    if cfg["device"] not in ("gpu", "cpu"):
        sys.exit(f'"device" in {CONFIG_PATH} must be "gpu" or "cpu", not {cfg["device"]!r}')
    if not CONFIG_PATH.exists():
        CONFIG_PATH.write_text(json.dumps(cfg, indent=2), encoding="utf-8")
        print(f"Created default config for device '{cfg['device']}': {CONFIG_PATH}")
    return cfg


def detect_device() -> str:
    print("Looking for an NVIDIA GPU", flush=True)
    import torch
    return "gpu" if torch.cuda.is_available() else "cpu"


def device_setting(cfg: dict, name: str):
    """gpu_<name> or cpu_<name>, by the selected device."""
    return cfg[f"{cfg['device']}_{name}"]


class Status:
    """What the server is doing right now.

    state + args are translated by the mod (keys AiArtServerState_<state>, args fill {0}, {1}...),
    message is the English text for the console and for other clients.
    """

    def __init__(self):
        self._lock = threading.Lock()
        self._state, self._args, self._message = "starting", [], "Starting"

    def set(self, state: str, message: str, *args):
        with self._lock:
            changed = (state, message) != (self._state, self._message)
            self._state, self._args, self._message = state, [str(a) for a in args], message
        if changed:
            print(f"[{state}] {message}", flush=True)

    def get(self) -> dict:
        with self._lock:
            return {"state": self._state, "args": list(self._args), "message": self._message}


# ---------------------------------------------------------------- prompt

# port of ArtDescriptionTextProcessor.java from RimWorld_ArtAiServer
_CLEANUP = [
    (r" on [^ ]+ of (Aprimay|Jugust|Septober|Decembary)[^.]+.", "."),
    (r" [^ ]+ (Мартомай|Июгуст|Сентоноябрь|Декавраль)[^.]+.", "."),
    (r"Placing more than one dresser near the same bed has no effect\.", ""),
    (r"A high quality crib will make a baby happy\. Multiple cribs can be placed"
     r" in a bedroom without turning it into a barracks\.", ""),
    (r"<i>.*</i>", ""),
    (r"<.?color[^>]*>", ""),
    (r"Talent:", ""),
    (r"\(Vanilla [^)]*Expanded[^)]*\)", ""),
    (r"\n", ""),
    (r"  +", " "),
]


def clean_description(text: str) -> str:
    for pattern, replacement in _CLEANUP:
        text = re.sub(pattern, replacement, text)
    return text.strip()


def seed_for(prompt: str) -> int:
    """Same prompt -> same picture."""
    return int.from_bytes(hashlib.sha256(prompt.encode()).digest()[:4], "little")


def optional_int(value) -> int | None:
    return None if value is None else int(value)


# ---------------------------------------------------------------- model

def resolve_preset(cfg: dict) -> str:
    if cfg["gpu_preset"] != "auto":
        return cfg["gpu_preset"]
    import torch
    vram_gb = torch.cuda.get_device_properties(0).total_memory / 2**30
    return "high" if vram_gb >= 15 else "mid" if vram_gb >= 9.5 else "low"


def download_model(cfg: dict, status: Status):
    """Download missing files, reporting progress by watching the Hugging Face cache folder."""
    from huggingface_hub import HfApi, snapshot_download
    from huggingface_hub.constants import HF_HUB_CACHE

    targets = [(cfg["model_repo"], PIPELINE_FILES), (cfg["gguf_repo"], [device_setting(cfg, "gguf_file")])]
    try:
        api = HfApi()
        total = sum(f.size or 0
                    for repo, patterns in targets
                    for f in api.model_info(repo, files_metadata=True).siblings
                    if any(fnmatch.fnmatch(f.rfilename, p) for p in patterns))
    except Exception as e:
        status.set("offline", f"Hugging Face is unavailable ({type(e).__name__}), using the downloaded model")
        return

    blob_dirs = [Path(HF_HUB_CACHE) / ("models--" + repo.replace("/", "--")) / "blobs" for repo, _ in targets]

    def downloaded() -> int:  # includes *.incomplete files that are being written
        return sum(e.stat().st_size for d in blob_dirs if d.exists() for e in os.scandir(d) if e.is_file())

    if total == 0 or downloaded() >= total:
        return

    finished = threading.Event()

    def report():
        while not finished.wait(2):
            got = min(downloaded(), total)
            done_gb, total_gb, percent = f"{got / 1e9:.1f}", f"{total / 1e9:.1f}", got * 100 // total
            status.set("downloading", f"Downloading model: {done_gb} / {total_gb} GB ({percent}%)",
                       done_gb, total_gb, percent)

    threading.Thread(target=report, daemon=True).start()
    try:
        for repo, patterns in targets:
            # an unstable connection breaks the download now and then; the next attempt continues it
            for attempt in range(1, DOWNLOAD_ATTEMPTS + 1):
                try:
                    snapshot_download(repo, allow_patterns=patterns)
                    break
                except Exception as e:
                    if attempt == DOWNLOAD_ATTEMPTS:
                        raise
                    print(f"Download interrupted ({type(e).__name__}: {e}), "
                          f"retrying in {DOWNLOAD_RETRY_SECONDS} s, attempt {attempt + 1} of {DOWNLOAD_ATTEMPTS}",
                          flush=True)
                    time.sleep(DOWNLOAD_RETRY_SECONDS)
    finally:
        finished.set()


def load_flux(cfg: dict, preset: str | None):
    """preset None: on the processor."""
    import torch
    from diffusers import Flux2KleinPipeline, Flux2Transformer2DModel, GGUFQuantizationConfig
    from huggingface_hub import hf_hub_download

    # bfloat16 on a processor without native support for it (most desktop CPUs) is ~200 times slower than float32;
    # float32 doubles the RAM of the text encoder (16 GB instead of 8)
    dtype = torch.bfloat16 if preset is not None else torch.float32
    parts = {"transformer": Flux2Transformer2DModel.from_single_file(
        hf_hub_download(cfg["gguf_repo"], device_setting(cfg, "gguf_file")),
        config=cfg["model_repo"], subfolder="transformer", dtype=dtype,
        quantization_config=GGUFQuantizationConfig(compute_dtype=dtype))}
    if preset and PRESETS[preset]["te_4bit"]:
        from transformers import BitsAndBytesConfig, Qwen3ForCausalLM
        parts["text_encoder"] = Qwen3ForCausalLM.from_pretrained(
            cfg["model_repo"], subfolder="text_encoder", dtype=dtype,
            quantization_config=BitsAndBytesConfig(
                load_in_4bit=True, bnb_4bit_quant_type="nf4", bnb_4bit_compute_dtype=dtype))

    pipe = Flux2KleinPipeline.from_pretrained(cfg["model_repo"], dtype=dtype, **parts)
    if preset is None:
        pass  # processor: the model stays in RAM, where it is loaded
    elif PRESETS[preset]["offload"]:
        pipe.enable_model_cpu_offload()
    else:
        pipe.to("cuda")
    # the console shows a progress bar per image: on the processor an image takes minutes
    pipe.set_progress_bar_config(desc="Image")
    return pipe


class Generator:
    """Owns the model. Requests wait until the model is loaded; the GPU does one request at a time."""

    def __init__(self, cfg: dict, status: Status):
        self.cfg, self.status = cfg, status
        self._device = "cuda" if cfg["device"] == "gpu" else "cpu"  # torch name of the device
        self._pipe = None
        self._ready = threading.Event()
        self._error: str | None = None
        self._gpu = threading.Lock()

    def load(self, download_only: bool = False):
        try:
            if self.cfg["backend"] == "mock":
                self.status.set("ready_mock", "Ready (mock backend)")
                return
            import torch
            on_gpu = self.cfg["device"] == "gpu"
            if on_gpu and not torch.cuda.is_available():
                raise RuntimeError('NVIDIA GPU with CUDA not found. To use the processor, set "device": "cpu"')
            # the device, the preset and the model file are settled before the download starts
            preset = resolve_preset(self.cfg) if on_gpu else None
            model = device_setting(self.cfg, "gguf_file")
            self.status.set("checking", f"Checking model files ({model})", model)
            download_model(self.cfg, self.status)
            if download_only:
                self.status.set("downloaded", "Model downloaded")
                return
            if on_gpu:
                gpu = torch.cuda.get_device_name(0)
                self.status.set("loading", f"Loading model into GPU (preset '{preset}')", preset)
                self._pipe = load_flux(self.cfg, preset)
                self.status.set("ready", f"Ready: {model}, preset '{preset}', {gpu}", model, preset, gpu)
            else:
                self.status.set("loading_cpu", "Loading model into RAM (processor)")
                self._pipe = load_flux(self.cfg, None)
                self.status.set("ready_cpu", f"Ready: {model}, processor", model)
        except Exception as e:
            traceback.print_exc()
            self._error = f"{type(e).__name__}: {e}"
            self.status.set("error", f"Model failed to load: {self._error}", self._error)
        finally:
            self._ready.set()

    def _wait_ready(self):
        self._ready.wait()
        if self._error:
            raise RuntimeError(self._error)

    def generate(self, prompt: str, seed: int | None = None,
                 width: int | None = None, height: int | None = None, style: bool = True) -> bytes:
        """PNG image. seed None: derived from the prompt. style: put cfg['style'] before the prompt."""
        self._wait_ready()
        cfg = self.cfg
        seed = seed_for(prompt) if seed is None else seed
        width, height = width or device_setting(cfg, "width"), height or device_setting(cfg, "height")
        with self._gpu:
            if self._pipe is None:  # mock backend
                return placeholder_png(width, height, seed)
            import torch
            image = self._pipe(
                prompt=(cfg["style"] if style else "") + prompt,
                width=width, height=height,
                num_inference_steps=cfg["steps"], guidance_scale=1.0,
                generator=torch.Generator(self._device).manual_seed(seed),
            ).images[0]
        buf = io.BytesIO()
        image.save(buf, format="PNG")
        return buf.getvalue()

    def chat(self, messages: list[dict], max_tokens: int, temperature: float, top_p: float,
             seed: int | None, stop: list[str], thinking: bool, on_text=None) -> dict:
        """Answer with the text encoder, which is a complete Qwen3-4B language model.

        on_text(piece) is called while the answer is generated (streaming).
        Returns {"text", "prompt_tokens", "completion_tokens", "finish_reason"}.
        """
        self._wait_ready()
        if self._pipe is None:  # mock backend: echo
            text = "Mock answer: " + (messages[-1]["content"] if messages else "")
            for word in text.split(" "):
                if on_text:
                    on_text(word + " ")
            return {"text": text, "prompt_tokens": 0, "completion_tokens": len(text.split()), "finish_reason": "stop"}

        import torch
        from transformers import StoppingCriteria, StoppingCriteriaList, TextIteratorStreamer

        tokenizer = self._pipe.tokenizer
        prompt = tokenizer.apply_chat_template(messages, tokenize=False, add_generation_prompt=True,
                                               enable_thinking=thinking)
        input_ids = tokenizer(prompt, return_tensors="pt").input_ids
        prompt_tokens = input_ids.shape[1]
        if prompt_tokens + max_tokens > LLM_MAX_CONTEXT:
            raise ValueError(f"The request is too long: {prompt_tokens} tokens + max_tokens {max_tokens} "
                             f"is more than {LLM_MAX_CONTEXT}")

        class Cancel(StoppingCriteria):  # stops generation when the client is gone
            cancelled = False

            def __call__(self, ids, scores, **kwargs):
                return self.cancelled

        cancel = Cancel()
        streamer = TextIteratorStreamer(tokenizer, skip_prompt=True, skip_special_tokens=True)
        kwargs = dict(input_ids=input_ids.to(self._device), max_new_tokens=max_tokens, streamer=streamer,
                      stopping_criteria=StoppingCriteriaList([cancel]),
                      pad_token_id=tokenizer.pad_token_id or tokenizer.eos_token_id)
        if temperature > 0:
            kwargs.update(do_sample=True, temperature=temperature, top_p=top_p)
        else:
            kwargs.update(do_sample=False)
        if stop:
            kwargs.update(stop_strings=stop, tokenizer=tokenizer)

        result = {}

        def run():
            try:
                result["ids"] = self._pipe.text_encoder.generate(**kwargs)
            except Exception as e:
                result["error"] = e
                streamer.end()

        pieces = []
        with self._gpu:
            if seed is not None:
                torch.manual_seed(seed)
            worker = threading.Thread(target=run, daemon=True)
            worker.start()
            try:
                for piece in streamer:
                    pieces.append(piece)
                    if on_text:
                        on_text(piece)
            except Exception:
                cancel.cancelled = True
                raise
            finally:
                worker.join()
        if "error" in result:
            raise result["error"]

        text = "".join(pieces)
        for s in stop:  # OpenAI does not include the stop sequence in the answer
            if s in text:
                text = text[:text.index(s)]
        completion_tokens = result["ids"].shape[1] - prompt_tokens
        return {"text": text, "prompt_tokens": prompt_tokens, "completion_tokens": completion_tokens,
                "finish_reason": "length" if completion_tokens >= max_tokens else "stop"}


def placeholder_png(width: int, height: int, seed: int) -> bytes:
    """Solid-colour PNG without Pillow."""
    rgb = bytes([seed & 255, (seed >> 8) & 255, (seed >> 16) & 255])
    raw = (b"\x00" + rgb * width) * height

    def chunk(kind: bytes, data: bytes) -> bytes:
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data))

    return (b"\x89PNG\r\n\x1a\n"
            + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 2, 0, 0, 0))
            + chunk(b"IDAT", zlib.compress(raw))
            + chunk(b"IEND", b""))


# ---------------------------------------------------------------- jobs for the mod API

class Jobs:
    """Single-player queue. No cache: the mod keeps images itself; a result lives until /view."""

    def __init__(self, generator: Generator):
        self.generator = generator
        self._lock = threading.Lock()
        self._pending: list[str] = []
        self._results: dict[str, bytes] = {}
        self._errors: dict[str, str] = {}
        self._queue: queue.Queue[tuple[str, str, int | None]] = queue.Queue()
        threading.Thread(target=self._work, daemon=True).start()

    def submit(self, prompt: str, seed: int | None) -> str:
        uid = str(uuid.uuid5(uuid.NAMESPACE_URL, f"{seed}\n{prompt}"))
        with self._lock:
            if uid not in self._pending and uid not in self._results:
                self._errors.pop(uid, None)
                self._pending.append(uid)
                self._queue.put((uid, prompt, seed))
        return uid

    def state(self, uid: str):
        """('done', None) | ('error', message) | ('queued', position) | (None, None)"""
        with self._lock:
            if uid in self._results:
                return "done", None
            if uid in self._errors:
                return "error", self._errors[uid]
            if uid in self._pending:
                return "queued", self._pending.index(uid)
        return None, None

    def take(self, uid: str) -> bytes | None:
        with self._lock:
            return self._results.pop(uid, None)

    def _work(self):
        while True:
            uid, prompt, seed = self._queue.get()
            try:
                png = self.generator.generate(clean_description(prompt), seed)
                with self._lock:
                    self._results[uid] = png
            except Exception as e:
                traceback.print_exc()
                with self._lock:
                    self._errors[uid] = f"{type(e).__name__}: {e}"
            finally:
                with self._lock:
                    self._pending.remove(uid)


# ---------------------------------------------------------------- HTTP

class JsonHandler(BaseHTTPRequestHandler):
    def log_message(self, fmt, *args):  # the mod polls every 5 s, keep the console readable
        pass

    def _send(self, code: int, body: bytes, content_type: str):
        self.send_response(code)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def _json(self, code: int, obj):
        self._send(code, json.dumps(obj, ensure_ascii=False).encode(), "application/json")

    def _body(self) -> bytes:
        return self.rfile.read(int(self.headers.get("Content-Length", 0)))


def make_handler(generator: Generator, jobs: Jobs, status: Status):
    """API for the mod (/prompt, /history, /view, /info, /shutdown) and for the Java server (/generate)."""

    class Handler(JsonHandler):
        def do_POST(self):
            url = urlparse(self.path)
            if url.path == "/prompt":  # mod: enqueue {"prompt": str, "language": str, "seed": int (optional)}
                try:
                    rq = json.loads(self._body())
                    prompt, seed = str(rq["prompt"]), optional_int(rq.get("seed"))
                except (ValueError, KeyError, TypeError):
                    return self._json(400, {"error": "expected {\"prompt\": ...}"})
                return self._json(200, {"prompt_id": jobs.submit(prompt, seed)})
            if url.path == "/generate":  # Java server: text in, PNG out; optional ?seed=
                prompt = self._body().decode("utf-8")
                try:
                    seed = optional_int(parse_qs(url.query).get("seed", [None])[0])
                except ValueError:
                    return self._send(400, b"seed must be a number", "text/plain")
                try:
                    return self._send(200, generator.generate(prompt, seed), "image/png")
                except Exception as e:
                    return self._send(503, str(e).encode(), "text/plain")
            if url.path == "/shutdown":  # mod: Stop button
                self._json(200, {"state": "stopping"})
                print("Stop requested", flush=True)
                threading.Timer(0.2, os._exit, args=(0,)).start()
                return
            self._json(404, {"error": "not found"})

        def do_GET(self):
            url = urlparse(self.path)
            if url.path.startswith("/history/"):  # mod: poll
                uid = url.path.removeprefix("/history/")
                job_state, value = jobs.state(uid)
                if job_state is None:
                    return self._json(404, {"error": "unknown prompt_id"})
                if job_state == "done":
                    return self._json(200, {uid: {"outputs": {"elem": {"images": [{"filename": uid}]}}}})
                if job_state == "error":
                    return self._json(200, {uid: {"artAiStatus": "error", "artAiMessage": value}})
                s = status.get()
                item = {"artAiQueuePosition": value, "artAiStatus": s["state"]}
                if s["state"] not in ("ready", "ready_cpu", "ready_mock"):  # downloading / loading: tell why it takes long
                    item["artAiMessage"], item["artAiArgs"] = s["message"], s["args"]
                return self._json(200, {uid: item})
            if url.path == "/view":  # mod: download
                png = jobs.take(parse_qs(url.query).get("filename", [""])[0])
                if png is None:
                    return self._json(404, {"error": "no such image"})
                return self._send(200, png, "image/png")
            if url.path in ("/info", "/"):
                return self._json(200, {"server": "artai-local", **status.get()})
            self._json(404, {"error": "not found"})

    return Handler


# ---------------------------------------------------------------- OpenAI-compatible API

TEXT_MODEL_ID = "qwen3-4b"
IMAGE_MODEL_ID = "flux2-klein-4b"


def message_text(content) -> str:
    """Message content: a string, or a list of parts of which only text parts are used."""
    if isinstance(content, str):
        return content
    if isinstance(content, list):
        return "".join(part.get("text", "") for part in content
                       if isinstance(part, dict) and part.get("type") == "text")
    return ""


def make_openai_handler(generator: Generator, cfg: dict):
    """/v1/models, /v1/chat/completions (with stream), /v1/images/generations."""

    class OpenAIHandler(JsonHandler):
        def _error(self, code: int, message: str, kind: str = "invalid_request_error"):
            self._json(code, {"error": {"message": message, "type": kind, "code": None}})

        def _authorized(self) -> bool:
            key = cfg["openai_api_key"]
            if not key or self.headers.get("Authorization", "") == f"Bearer {key}":
                return True
            self._error(401, "Invalid API key", "authentication_error")
            return False

        def _path(self) -> str:  # clients use the base URL with or without /v1
            return urlparse(self.path).path.removeprefix("/v1")

        def do_GET(self):
            if not self._authorized():
                return
            if self._path() == "/models":
                return self._json(200, {"object": "list", "data": [
                    {"id": model_id, "object": "model", "created": 0, "owned_by": "artai-local"}
                    for model_id in (TEXT_MODEL_ID, IMAGE_MODEL_ID)]})
            self._error(404, f"Unknown path {self.path}")

        def do_POST(self):
            if not self._authorized():
                return
            try:
                rq = json.loads(self._body())
                if not isinstance(rq, dict):
                    raise ValueError("the request must be a JSON object")
            except ValueError as e:
                return self._error(400, f"Invalid JSON: {e}")
            path = self._path()
            try:
                if path == "/chat/completions":
                    return self._chat(rq)
                if path == "/images/generations":
                    return self._images(rq)
            except (KeyError, TypeError, ValueError) as e:
                return self._error(400, f"Invalid request: {type(e).__name__}: {e}")
            except (BrokenPipeError, ConnectionResetError):
                return  # the client went away
            except Exception as e:
                traceback.print_exc()
                return self._error(500, f"{type(e).__name__}: {e}", "server_error")
            self._error(404, f"Unknown path {self.path}")

        def _chat(self, rq: dict):
            roles = {"system": "system", "developer": "system", "user": "user", "assistant": "assistant"}
            messages = []
            for m in rq["messages"]:
                if m["role"] not in roles:
                    raise ValueError(f"unsupported role {m['role']}")
                messages.append({"role": roles[m["role"]], "content": message_text(m.get("content"))})
            stop = rq.get("stop") or []
            if isinstance(stop, str):
                stop = [stop]
            args = dict(
                messages=messages,
                max_tokens=min(int(rq.get("max_completion_tokens") or rq.get("max_tokens")
                                   or cfg["llm_max_tokens"]), cfg["llm_max_tokens"]),
                temperature=float(rq.get("temperature", 0.7)),  # Qwen3 recommends 0.7 / 0.8 without thinking
                top_p=float(rq.get("top_p", 0.8)),
                seed=optional_int(rq.get("seed")),
                stop=[str(s) for s in stop],
                thinking=bool((rq.get("chat_template_kwargs") or {}).get("enable_thinking", False)))
            completion_id, created = "chatcmpl-" + uuid.uuid4().hex, int(time.time())
            model = rq.get("model") or TEXT_MODEL_ID

            if not rq.get("stream"):
                answer = generator.chat(**args)
                return self._json(200, {
                    "id": completion_id, "object": "chat.completion", "created": created, "model": model,
                    "choices": [{"index": 0, "message": {"role": "assistant", "content": answer["text"]},
                                 "finish_reason": answer["finish_reason"]}],
                    "usage": {"prompt_tokens": answer["prompt_tokens"],
                              "completion_tokens": answer["completion_tokens"],
                              "total_tokens": answer["prompt_tokens"] + answer["completion_tokens"]}})

            # server-sent events: the connection closes after the last event
            self.send_response(200)
            self.send_header("Content-Type", "text/event-stream")
            self.send_header("Cache-Control", "no-cache")
            self.end_headers()

            def event(delta: dict, finish_reason=None):
                chunk = {"id": completion_id, "object": "chat.completion.chunk", "created": created,
                         "model": model,
                         "choices": [{"index": 0, "delta": delta, "finish_reason": finish_reason}]}
                self.wfile.write(f"data: {json.dumps(chunk, ensure_ascii=False)}\n\n".encode())
                self.wfile.flush()

            event({"role": "assistant", "content": ""})
            try:
                answer = generator.chat(**args, on_text=lambda piece: event({"content": piece}))
            except (BrokenPipeError, ConnectionResetError):
                raise
            except Exception as e:  # headers are sent already: report the error as an event
                traceback.print_exc()
                error = {"error": {"message": f"{type(e).__name__}: {e}", "type": "server_error"}}
                self.wfile.write(f"data: {json.dumps(error, ensure_ascii=False)}\n\n".encode())
                return
            event({}, answer["finish_reason"])
            self.wfile.write(b"data: [DONE]\n\n")

        def _images(self, rq: dict):
            if rq.get("response_format", "b64_json") != "b64_json":
                return self._error(400, "Only response_format b64_json is supported")
            prompt = str(rq["prompt"])
            n = max(1, min(int(rq.get("n", 1)), 4))
            width, height = device_setting(cfg, "width"), device_setting(cfg, "height")
            size = rq.get("size") or "auto"
            if size != "auto":
                w, h = (int(v) for v in str(size).lower().split("x"))
                # the model needs multiples of 16
                width, height = (max(256, min(v, 2048)) // 16 * 16 for v in (w, h))
            seed = optional_int(rq.get("seed"))
            if seed is None:
                seed = random.randrange(2**31)  # like OpenAI: a new picture on every request
            data = [{"b64_json": base64.b64encode(
                        generator.generate(prompt, seed + i, width, height, style=False)).decode()}
                    for i in range(n)]
            return self._json(200, {"created": int(time.time()), "data": data})

    return OpenAIHandler


# ---------------------------------------------------------------- main

def exit_with_process(pid: int):
    """Stop the server when the game exits (or crashes)."""
    if os.name == "nt":
        import ctypes
        synchronize, infinite = 0x00100000, 0xFFFFFFFF
        handle = ctypes.windll.kernel32.OpenProcess(synchronize, False, pid)
        if handle:
            ctypes.windll.kernel32.WaitForSingleObject(handle, infinite)
    else:
        while True:
            try:
                os.kill(pid, 0)  # signal 0 only checks that the process exists (POSIX)
            except ProcessLookupError:
                break
            except PermissionError:
                pass
            time.sleep(2)
    print("Game closed, stopping", flush=True)
    os._exit(0)


def start_openai_api(cfg: dict, generator: Generator):
    """The OpenAI-compatible API is extra: if its port is taken, the main server still works."""
    try:
        server = ThreadingHTTPServer((cfg["host"], cfg["openai_port"]), make_openai_handler(generator, cfg))
    except OSError as e:
        print(f"OpenAI-compatible API is off: cannot listen on {cfg['host']}:{cfg['openai_port']} ({e})",
              flush=True)
        return
    threading.Thread(target=server.serve_forever, daemon=True).start()
    print(f"OpenAI-compatible API on http://{cfg['host']}:{cfg['openai_port']}/v1", flush=True)


def main():
    ap = argparse.ArgumentParser(description="AI art local server (FLUX.2 klein 4B)")
    ap.add_argument("prompt", nargs="?", help="one-shot: prompt to generate")
    ap.add_argument("-o", "--output", help="one-shot: output PNG file")
    ap.add_argument("--seed", type=int, help="one-shot: seed (default: derived from the prompt)")
    ap.add_argument("--download", action="store_true", help="download the model and exit")
    ap.add_argument("--parent-pid", type=int, help="exit when this process exits (used by the mod)")
    args = ap.parse_args()

    cfg = load_config()
    status = Status()
    generator = Generator(cfg, status)

    if args.download:
        generator.load(download_only=True)
        sys.exit(1 if status.get()["state"] == "error" else 0)

    if args.prompt or args.output:
        if not (args.prompt and args.output):
            ap.error("one-shot mode needs both a prompt and -o FILE")
        generator.load()
        Path(args.output).write_bytes(generator.generate(args.prompt, args.seed))
        print(f"Saved {args.output}")
        return

    if args.parent_pid:
        threading.Thread(target=exit_with_process, args=(args.parent_pid,), daemon=True).start()

    try:
        server = ThreadingHTTPServer((cfg["host"], cfg["port"]), make_handler(generator, Jobs(generator), status))
    except OSError as e:
        sys.exit(f"Cannot listen on {cfg['host']}:{cfg['port']} ({e}). Is the server already running?")
    if cfg["openai_port"]:
        start_openai_api(cfg, generator)

    # HTTP first, model second: the mod shows download progress while the model is loading
    threading.Thread(target=generator.load, daemon=True).start()
    print(f"Listening on http://{cfg['host']}:{cfg['port']}", flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass


if __name__ == "__main__":
    main()
