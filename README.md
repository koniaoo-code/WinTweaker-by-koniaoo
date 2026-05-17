# WinTweaker 🛠️
**by koniaoo** — оптимизация Windows с красивым GUI

## Что умеет
- ⚡ **Производительность** — питание, SysMain, индексирование, визуальные эффекты, Game Mode
- 🔒 **Телеметрия** — отключение слежки Microsoft, Cortana, DiagTrack
- 🚫 **Реклама** — убирает рекламу из Пуска, экрана блокировки, Bing
- 🧹 **Очистка** — Temp, DNS кэш, корзина, Prefetch, журналы событий

## Запуск (без сборки)
```
Просто установи Portable в релизах или другой вариант.
```

## Сборка в .exe
```
build.bat
```
Готовый файл будет в папке `dist\WinTweaker.exe`

## Требования
- Windows 10 / 11
- Python 3.10+
- Права администратора (для системных твиков)

## Стек
- Python + CustomTkinter (GUI)
- PowerShell (системные твики через subprocess)
- PyInstaller (→ portable .exe с UAC запросом)

---
github.com/koniaoo-code
