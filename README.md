# 🚀 Pocket CI/CD

> WPF-приложение для автоматизации публикации проектов на тестовые порталы.  
> Позволяет сохранять конфигурации проектов и выполнять деплой в несколько кликов.

---

## Для чего это нужно

При разработке часто нужно публиковать изменения на тестовый сервер: скопировать файлы, сделать бекап, убрать старое, положить новое, поднять/опустить `app_offline.htm`. Делать это руками каждый раз — долго и легко ошибиться.

**Pocket CI/CD** берёт это на себя:

- сохраняет все пути проекта один раз
- при следующем запуске — просто выбираешь проект из списка и жмёшь «Опубликовать»

---

## Возможности

- 📁 **Настройка путей** — исходный проект, целевая директория, папки Backup и Update
- 🚫 **Исключения** — можно указать файлы и папки, которые не будут затронуты при публикации
- 💾 **Сохранение проектов** — конфигурации хранятся в локальной SQLite БД рядом с `.exe`
- 🔄 **Быстрый выбор** — ComboBox со списком сохранённых проектов при запуске
- 🗂️ **Локальный Backup** — опционально создаёт бекап в папке приложения с именем вида `МойПроект_2024-01-15_14-30-00`
- 🔒 **app_offline.htm** — автоматически включается перед деплоем и отключается после

---

## Процесс публикации

При нажатии кнопки **«Опубликовать»** выполняется следующая цепочка:

```
1. Создать Backup                  → копия текущей версии
2. [Опционально] Локальный Backup  → копия в папке приложения
3. Скопировать файлы в Update      → подготовка новой версии
4. Включить app_offline.htm        → сайт уходит на обслуживание
5. Удалить старые файлы            → очистка целевой директории
6. Переместить Update → Target     → применение новой версии
7. Отключить app_offline.htm       → сайт поднимается
```

---

## Стек

| | |
|---|---|
| Платформа | .NET 8, WPF |
| БД | SQLite (`Microsoft.Data.Sqlite`) |
| ORM | Dapper |
| Диалоги | `Microsoft.WindowsAPICodePack-Shell` |
| DI | `Microsoft.Extensions.DependencyInjection` |

---

## Установка и запуск

### Требования

- Windows 10 / 11
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

### Запуск из исходников

```bash
git clone https://github.com/<your-username>/<repo-name>.git
cd <repo-name>
dotnet restore
dotnet run
```

### Сборка

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

Готовый `.exe` появится в `bin\Release\net8.0-windows\win-x64\publish\`.  
Файл базы данных `projects.db` создаётся автоматически рядом с `.exe` при первом запуске.

---

## Структура проекта

```
DeployManager/
├── Models/
│   └── ProjectPathsModel.cs       # Модель конфигурации проекта
├── Services/
│   ├── IFileService.cs
│   ├── FileService.cs             # Логика работы с файлами
│   ├── IDatabaseService.cs
│   └── DatabaseService.cs         # SQLite: сохранение и загрузка проектов
├── Views/
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   ├── ProjectNameDialog.xaml
│   └── ProjectNameDialog.xaml.cs
├── App.xaml
├── App.xaml.cs                    # DI-контейнер
└── projects.db                    # Создаётся автоматически при первом запуске
```

---

## Структура базы данных

```
Projects
├── ProjectId       INTEGER PK
├── ProjectName     TEXT
├── SourceDirectory TEXT
├── TargetDirectory TEXT
├── BackupDirectory TEXT
└── UpdateDirectory TEXT

ExclusionFiles                      ← файлы, исключённые из деплоя
├── Id              INTEGER PK
├── ProjectId       INTEGER FK → Projects
└── FilePath        TEXT

ExclusionDirectories                ← папки, исключённые из деплоя
├── Id              INTEGER PK
├── ProjectId       INTEGER FK → Projects
└── DirPath         TEXT
```

Удаление проекта автоматически удаляет все связанные исключения (`ON DELETE CASCADE`).

---

## Лицензия

MIT
