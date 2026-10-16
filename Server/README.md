# AI art local server

Generates the mod's images on your own NVIDIA graphics card with the
[FLUX.2 klein 4B](https://huggingface.co/black-forest-labs/FLUX.2-klein-4B) model (Apache 2.0).
No account and no internet are needed after the first start.

*Русская версия ниже.*

## Requirements

- NVIDIA graphics card with 6 GB of video memory or more (RTX 20xx or newer), recent driver.
  Without one the server works on the processor (`device` below): several minutes per image, about 20 GB of free RAM.
- [uv](https://docs.astral.sh/uv/getting-started/installation/) — installs Python and all libraries by itself.
  Windows: open PowerShell and run `powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"`.
- About 15 GB of free disk space and internet for the first start:
  ~3 GB of Python libraries and ~11 GB of model files are downloaded once.

## Start

**From the game:** click a colonist portrait → *Server settings* → *Start*.
The game copies this folder to `<RimWorld save data>/AiArtServer` and runs it there.
A console window shows what the server is doing; the server stops when you click *Stop* or close the game.
Select *Local server* in the same window so the mod sends requests to it.

**By hand** (in the server folder):

| Command | What it does |
|---|---|
| `run.bat` (Windows) / `sh run.sh` (Linux) | start the server |
| `uv run artai.py` | the same |
| `uv run artai.py "a cat in a spacesuit" -o cat.png` | generate one image and exit |
| `uv run artai.py "a cat" -o cat.png --seed 5` | the same with another seed (another picture) |
| `uv run artai.py --download` | only download the model |

Then in the mod select *Local server*, or set the server URL in the mod settings to `http://127.0.0.1:8080`.

The first start takes a while: libraries are installed, then the model is downloaded
(the progress is shown in the console and in the image tooltip in the game),
then the model is loaded into the GPU. Next starts take under a minute.

## Settings: `artai_config.json`

Created on the first start next to `artai.py`. Can be edited in the game (*Server settings*) or in any text editor.
Restart the server after changes.

| Key | Default | Meaning |
|---|---|---|
| `port` | `8080` | HTTP port. The mod's local server URL is `http://127.0.0.1:<port>` |
| `host` | `127.0.0.1` | `0.0.0.0` makes the server reachable from other computers (e.g. in Docker) |
| `device` | found on the first start | `gpu` or `cpu`: where the model runs. A new file gets `gpu` if an NVIDIA GPU with CUDA works, otherwise `cpu`. Without this key it is found the same way on every start |
| `gpu_preset` | `auto` | `gpu`: how the model is placed in video memory, see below |
| `gpu_gguf_file` | `flux-2-klein-4b-Q5_K_M.gguf` | `gpu`: model file. `flux-2-klein-4b-Q4_K_M.gguf` is a bit worse but needs ~0.5 GB less memory |
| `gpu_width`, `gpu_height` | `768` | `gpu`: image size in pixels, multiple of 16. 1024 is the model's best, 512 is the minimum that looks good |
| `cpu_gguf_file` | `flux-2-klein-4b-Q4_K_M.gguf` | `cpu`: model file |
| `cpu_width`, `cpu_height` | `512` | `cpu`: image size. The processor needs minutes per image; time grows with the number of pixels |
| `steps` | `4` | denoising steps. The model is trained for 4; 6–8 may add a little detail |
| `style` | `oil painting, ...` | text added before every description |
| `hf_home` | empty | folder for downloaded models. Empty = Hugging Face default (`%USERPROFILE%\.cache\huggingface`) |
| `backend` | `flux` | `mock` returns solid-colour images without a GPU, for mod development |
| `model_repo`, `gguf_repo` | | Hugging Face repositories the files come from |
| `openai_port` | `8000` | port of the OpenAI-compatible API (text model and images), `0` = off |
| `openai_api_key` | empty | if set, the OpenAI-compatible API requires `Authorization: Bearer <key>` |
| `llm_max_tokens` | `4096` | the longest text answer, in tokens |

Only the settings of the selected `device` are used, the others wait for a switch.

**Presets** of `gpu` (`auto` picks one by the size of video memory; on the processor everything stays in RAM):

| Preset | Video memory | What happens |
|---|---|---|
| `high` | 16 GB+ | everything stays in video memory, fastest |
| `mid` | 10–12 GB | the text encoder and the image model take turns in video memory |
| `low` | 6–8 GB | the text encoder is compressed to 4 bit (slightly lower quality) |

If generation fails with "out of memory", choose a lower preset and/or the Q4_K_M file.

## Where the files are

- Server folder: `<RimWorld save data>/AiArtServer` (*Open folder* in the game).
  `artai.py`, `run.bat`, `run.sh` and this file are replaced by the mod's version on every start; `artai_config.json` is kept.
- Models: `hf_home`, by default `%USERPROFILE%\.cache\huggingface\hub` (folders `models--black-forest-labs--FLUX.2-klein-4B`
  and `models--unsloth--FLUX.2-klein-4B-GGUF`). Delete them to free the disk space.
- Python and libraries: uv cache, `uv cache clean` removes it.

## Text model and OpenAI-compatible API

The text part of FLUX.2 klein is a complete language model (Qwen3-4B). While the server runs, it answers
over the OpenAI API at `http://localhost:8000/v1` (any API key and model name), e.g. for EchoColony:

| Endpoint | What it does |
|---|---|
| `GET /v1/models` | `qwen3-4b` (text) and `flux2-klein-4b` (images) |
| `POST /v1/chat/completions` | chat: `messages`, `max_tokens`, `temperature`, `top_p`, `stop`, `seed`, `stream` |
| `POST /v1/images/generations` | images: `prompt`, `size` (e.g. `1024x768`), `n` (up to 4), `seed`; answer in `b64_json` |

Qwen3 "thinking" is off; `"chat_template_kwargs": {"enable_thinking": true}` turns it on.
The `style` setting is not added to API images. Text and images share the GPU: requests wait for each other.
Not supported: tools/function calling, JSON mode, embeddings, `/v1/completions`, `/v1/responses`.

## For the public server (RimWorld_ArtAiServer)

The same script replaces the old Stable Diffusion container: it answers `POST /generate`
(text in, PNG out, optional `?seed=`), which the Java server calls. Set `"host": "0.0.0.0"` and `"port": 8081`
(or point `GENERATOR_URL` to the port you use). Without a GPU it works on the processor (`device` `cpu`):
expect minutes per image and about 20 GB of RAM.

## Troubleshooting

- *"uv" is not recognized* — uv is not installed, or the game was started before installing it: restart the game.
- *NVIDIA GPU with CUDA not found* — no NVIDIA card or an old driver: update the driver, or set `"device": "cpu"`.
- *WinError 1114 ... c10.dll* — Windows has an old Microsoft Visual C++ runtime, which the libraries need:
  install the new one from https://aka.ms/vs/17/release/vc_redist.x64.exe and start the server again.
- *Cannot listen on 127.0.0.1:8080* — the server is already running, or another program uses the port: change `port`.
- Anything else — the console window shows the error; it stays open when the server fails.

---

# Локальный сервер AI art

Генерирует картинки мода на вашей видеокарте NVIDIA моделью FLUX.2 klein 4B (лицензия Apache 2.0).
После первого запуска не нужны ни аккаунт, ни интернет.

## Что нужно

- Видеокарта NVIDIA от 6 ГБ видеопамяти (RTX 20xx и новее), свежий драйвер.
  Без неё сервер работает на процессоре (`device` ниже): несколько минут на картинку, около 20 ГБ свободной оперативной памяти.
- [uv](https://docs.astral.sh/uv/getting-started/installation/) — сам ставит Python и все библиотеки.
  Windows: откройте PowerShell и выполните `powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"`.
- Около 15 ГБ на диске и интернет при первом запуске: один раз скачиваются ~3 ГБ библиотек Python и ~11 ГБ модели.

## Запуск

**Из игры:** нажмите на портрет колониста → *Настроить сервер* → *Запустить*.
Игра копирует эту папку в `<папка сохранений RimWorld>/AiArtServer` и запускает её оттуда.
В консольном окне видно, что делает сервер; он останавливается кнопкой *Остановить* или при выходе из игры.
В том же окне выберите *Локальный сервер*, чтобы мод отправлял запросы на него.

**Вручную** (в папке сервера):

| Команда | Что делает |
|---|---|
| `run.bat` (Windows) / `sh run.sh` (Linux) | запустить сервер |
| `uv run artai.py` | то же самое |
| `uv run artai.py "a cat in a spacesuit" -o cat.png` | сгенерировать одну картинку и выйти |
| `uv run artai.py "a cat" -o cat.png --seed 5` | то же с другим seed (другая картинка) |
| `uv run artai.py --download` | только скачать модель |

Затем в моде выберите *Локальный сервер* или укажите в настройках мода адрес `http://127.0.0.1:8080`.

Первый запуск долгий: ставятся библиотеки, скачивается модель (прогресс виден в консоли
и во всплывающей подсказке картинки в игре), модель загружается в видеокарту. Следующие запуски — меньше минуты.

## Настройки: `artai_config.json`

Создаётся при первом запуске рядом с `artai.py`. Меняется в игре (*Настроить сервер*) или в любом текстовом редакторе.
После изменений перезапустите сервер.

| Ключ | По умолчанию | Значение |
|---|---|---|
| `port` | `8080` | HTTP-порт. Адрес локального сервера для мода: `http://127.0.0.1:<port>` |
| `host` | `127.0.0.1` | `0.0.0.0` открывает сервер для других компьютеров (например, в Docker) |
| `device` | определяется при первом запуске | `gpu` или `cpu`: где работает модель. Новый файл получает `gpu`, если работает видеокарта NVIDIA с CUDA, иначе `cpu`. Если ключа нет, он так же определяется при каждом запуске |
| `gpu_preset` | `auto` | `gpu`: как модель размещается в видеопамяти, см. ниже |
| `gpu_gguf_file` | `flux-2-klein-4b-Q5_K_M.gguf` | `gpu`: файл модели. `flux-2-klein-4b-Q4_K_M.gguf` чуть хуже, но требует примерно на 0,5 ГБ меньше памяти |
| `gpu_width`, `gpu_height` | `768` | `gpu`: размер картинки в пикселях, кратно 16. 1024 — лучшее для модели, 512 — минимум без заметной потери |
| `cpu_gguf_file` | `flux-2-klein-4b-Q4_K_M.gguf` | `cpu`: файл модели |
| `cpu_width`, `cpu_height` | `512` | `cpu`: размер картинки. Процессору нужны минуты на картинку; время растёт с числом пикселей |
| `steps` | `4` | шаги генерации. Модель обучена на 4; 6–8 иногда добавляют деталей |
| `style` | `oil painting, ...` | текст, добавляемый перед каждым описанием |
| `hf_home` | пусто | папка для скачанных моделей. Пусто = стандартная папка Hugging Face (`%USERPROFILE%\.cache\huggingface`) |
| `backend` | `flux` | `mock` отдаёт однотонные картинки без видеокарты — для разработки мода |
| `model_repo`, `gguf_repo` | | репозитории Hugging Face, откуда берутся файлы |
| `openai_port` | `8000` | порт OpenAI-совместимого API (текстовая модель и картинки), `0` = выключен |
| `openai_api_key` | пусто | если задан, OpenAI-совместимый API требует `Authorization: Bearer <ключ>` |
| `llm_max_tokens` | `4096` | самый длинный текстовый ответ, в токенах |

Используются только настройки выбранного `device`, остальные ждут переключения.

**Пресеты** для `gpu` (`auto` выбирает по объёму видеопамяти; на процессоре всё просто лежит в оперативной памяти):

| Пресет | Видеопамять | Что происходит |
|---|---|---|
| `high` | от 16 ГБ | всё лежит в видеопамяти, быстрее всего |
| `mid` | 10–12 ГБ | текстовый энкодер и модель картинки по очереди занимают видеопамять |
| `low` | 6–8 ГБ | текстовый энкодер сжат до 4 бит (качество чуть ниже) |

Если генерация падает с «out of memory», выберите пресет ниже и/или файл Q4_K_M.

## Где лежат файлы

- Папка сервера: `<папка сохранений RimWorld>/AiArtServer` (*Открыть папку* в игре).
  `artai.py`, `run.bat`, `run.sh` и этот файл заменяются версией из мода при каждом запуске; `artai_config.json` сохраняется.
- Модели: `hf_home`, по умолчанию `%USERPROFILE%\.cache\huggingface\hub` (папки `models--black-forest-labs--FLUX.2-klein-4B`
  и `models--unsloth--FLUX.2-klein-4B-GGUF`). Удалите их, чтобы освободить место.
- Python и библиотеки: кэш uv, удаляется командой `uv cache clean`.

## Текстовая модель и OpenAI-совместимый API

Текстовая часть FLUX.2 klein — полноценная языковая модель (Qwen3-4B). Пока сервер запущен, она отвечает
по OpenAI API на `http://localhost:8000/v1` (любой API-ключ и имя модели), например для EchoColony:

| Эндпоинт | Что делает |
|---|---|
| `GET /v1/models` | `qwen3-4b` (текст) и `flux2-klein-4b` (картинки) |
| `POST /v1/chat/completions` | чат: `messages`, `max_tokens`, `temperature`, `top_p`, `stop`, `seed`, `stream` |
| `POST /v1/images/generations` | картинки: `prompt`, `size` (например `1024x768`), `n` (до 4), `seed`; ответ в `b64_json` |

«Размышления» Qwen3 выключены; `"chat_template_kwargs": {"enable_thinking": true}` включает их.
Настройка `style` к картинкам через API не добавляется. Текст и картинки делят видеокарту: запросы ждут друг друга.
Не поддерживается: tools/function calling, JSON-режим, эмбеддинги, `/v1/completions`, `/v1/responses`.

## Для публичного сервера (RimWorld_ArtAiServer)

Этот же скрипт заменяет старый контейнер Stable Diffusion: он отвечает на `POST /generate`
(текст на вход, PNG на выход, необязательный `?seed=`), который вызывает Java-сервер. Поставьте `"host": "0.0.0.0"` и `"port": 8081`
(или укажите в `GENERATOR_URL` свой порт). Без видеокарты он работает на процессоре (`device` `cpu`):
минуты на картинку и около 20 ГБ оперативной памяти.

## Если что-то не работает

- *"uv" не является командой* — uv не установлен, или игра запущена до его установки: перезапустите игру.
- *NVIDIA GPU with CUDA not found* — нет видеокарты NVIDIA или старый драйвер: обновите драйвер или поставьте `"device": "cpu"`.
- *WinError 1114 ... c10.dll* — в Windows старая среда Microsoft Visual C++, нужная библиотекам:
  установите новую с https://aka.ms/vs/17/release/vc_redist.x64.exe и запустите сервер снова.
- *Cannot listen on 127.0.0.1:8080* — сервер уже запущен или порт занят другой программой: поменяйте `port`.
- Всё остальное — ошибка видна в консольном окне; при сбое оно не закрывается.
