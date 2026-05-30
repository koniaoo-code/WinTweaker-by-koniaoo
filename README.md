# WinTweaker ⚡

**by koniaoo** — быстрый и безопасный оптимизатор Windows 10/11 с красивым тёмным интерфейсом. Все твики обратимы переключателями.

<p align="center">
  <a href="https://github.com/koniaoo-code/WinTweaker-by-koniaoo/releases/latest">
    <b>📥 Скачать последнюю версию</b>
  </a>
</p>

---

## 📥 Скачать (без сборки)

Зайди в **[Releases](https://github.com/koniaoo-code/WinTweaker-by-koniaoo/releases/latest)** и выбери:

| Файл | Что это |
|---|---|
| **`WinTweaker.exe`** | **Portable** — просто скачай и запусти, ничего ставить не нужно. |
| **`WinTweaker_Setup.exe`** | Установщик — ярлыки на рабочем столе и в меню Пуск. |

> При запуске приложение само запросит права администратора (нужны для системных твиков).
> Требуется Windows 10 / 11 (x64). Для `WinTweaker.exe` отдельно ставить .NET **не нужно** — он self-contained.

---

## ✨ Возможности

- ⚡ **Производительность** — схема питания, SysMain, индексирование, визуальные эффекты, Game Mode, таймер, HAGS, скрытие поиска/виджетов и др.
- 🔒 **Телеметрия** — отключение слежки Microsoft, Cortana, DiagTrack, геолокации, отчётов об ошибках.
- 🚫 **Реклама** — убирает рекламу из Пуска, экрана блокировки, Bing, Edge.
- 🧹 **Очистка** — Temp, Prefetch, DNS-кэш, корзина, журналы событий, кэш обновлений.
- 🌐 **Сеть** — TCP-тюнинг, Fast Open, Google DNS, отключение Nagle и IPv6.
- 🔏 **Приватность** — доступ к микрофону/камере, буфер обмена, показ расширений/скрытых файлов.
- 💻 **Инфо о ПК** — CPU, ОЗУ, GPU, диск, сеть, материнка, аптайм.
- 🗑 **Удаление UWP-приложений** — сканирование и удаление встроенного bloatware.
- 🌍 Русский / English, тёмная тема, плавные анимации.

---

## 🛠 Сборка из исходников

Код лежит в этом репозитории. Есть две версии:

### Нативная — C# / WPF (.NET 8) — рекомендуется
```bat
cd dotnet
make_installer.bat     :: соберёт WinTweaker.exe + установщик
:: или просто запуск из исходников:
run.bat
```
Подробности — в [`dotnet/README.md`](dotnet/README.md). Нужен **.NET 8 SDK**.

### Оригинальная — Python / CustomTkinter
```bat
pip install -r WinTweaker/requirements.txt
python wintweaker.py
:: сборка в .exe:
build.bat              :: -> dist\WinTweaker.exe
```
Нужен **Python 3.10+**.

---

## 🔗 Ссылки

- GitHub: [github.com/koniaoo-code](https://github.com/koniaoo-code)
- Сайт: [koniaoo-code.netlify.app](https://koniaoo-code.netlify.app)
- Discord: `kon1xx_04470`

---

<p align="center"><sub>Сделано с ❤ by koniaoo</sub></p>
