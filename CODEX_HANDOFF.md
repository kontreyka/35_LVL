# Codex Handoff: Independence Unity Prototype

Документ для следующего Codex/разработчика после миграции на новый ноутбук.

## Быстрый старт

- Репозиторий: `https://github.com/kontreyka/35_LVL.git`
- Основная ветка: `main`
- Unity Editor, с которым велась работа: `6000.3.15f1`
- Проект: 2D Unity project.
- Перед любой новой просьбой пользователя сначала делай Git-checkpoint текущего состояния, если есть незакоммиченные изменения. Это прямое правило пользователя.
- Обязательно прочитай рабочие правила:
  - `AGENTS.md`
  - `Assets/Project/AGENTS.md`
- Пользователь говорит по-русски, часто присылает скриншоты. Скриншот считать частью ТЗ. Обычно пользователь хочет, чтобы изменения были сделаны прямо в сцене/коде, а не инструкцию "как сделать руками".

## Как работать с Git

Перед новым запросом:

1. `git status --short`
2. Если есть изменения, сделай checkpoint-коммит с понятным русским сообщением, например:
   - `checkpoint: перед настройкой прототипа комнаты`
   - `checkpoint: перед фиксом свечения клетки`
3. Только потом начинай новую задачу.

Не делай `git reset --hard`, не откатывай чужие изменения, не force-push.

После завершения обычной задачи финальный commit не обязателен, если пользователь отдельно не просит. Но в этой миграционной задаче пользователь попросил добавить инструкцию в репо и запушить.

## Важные команды проверки

Обычная быстрая проверка после C# правок:

```powershell
dotnet build Assembly-CSharp.csproj --nologo
dotnet build Independence.EditMode.Tests.csproj --nologo
git diff --check
```

Проверка на битые script-ссылки в новой сцене:

```powershell
rg -n "m_Script: \{fileID: 0\}|Missing|fileID: 0, guid:" Assets\Scenes Assets\Project\Scripts
```

Unity batchmode может быть полезен, но на этой машине уже ловился внешний crash Unity до импорта проекта из-за:

```text
C:/Users/User/AppData/Local/Unity/Caches/CurlRequestCache.db
```

Не путай это с ошибкой проекта. Если повторяется, закрыть Unity и удалить `%LOCALAPPDATA%\Unity\Caches\CurlRequestCache.db`; Unity создаст кэш заново.

## Текущие сцены

### `Assets/Scenes/Scene01.unity`

Это рабочая сцена с клеткой/птицей/фоном.

Ключевая логика:

- `Assets/Project/Scripts/Interaction/Scene01Controller.cs`
- `Assets/Project/Scripts/Interaction/CageEdgeGlowEffect.cs`
- `Assets/Project/Shaders/IrisReveal.shader`

Текущее поведение Scene01:

1. Первый клик по клетке:
   - плавно отключает желтоватый glow/particles клетки навсегда;
   - запускает приближение камеры к окну/клетке.
2. Второй клик:
   - запускает виньетку сплошным цветом;
   - незаметно меняет фон на второй фон;
   - картинка с клеткой не должна исчезать.
3. Третий клик:
   - отдаляет камеру назад;
   - виньетка больше не вызывается.

Важные inspector-поля `Scene01Controller`:

- `sceneCamera`
- `windowTarget`
- `cageAuraEffect`
- `zoomDuration`
- `targetCameraSize`
- `zoomResponsiveBackground`
- `backgroundZoomScaleCompensation`
- `backgroundRenderer`
- `secondBackgroundSprite`
- `vignetteMaterial`
- `vignetteDuration`
- `openVignetteRadius`
- `vignetteCompressionRadius`
- `vignetteSoftness`
- `vignetteSortingOrder`

`backgroundZoomScaleCompensation`:

- `0` - фон ведёт себя как обычный объект сцены.
- `1` - фон сохраняет размер относительно камеры.
- `0.5` - половинная компенсация для ощущения объёма.

### `Assets/Scenes/RoomPrototype_Level01.unity`

Это отдельная сцена-прототип механики "4 окна как Gorogoa".

Ключевая логика:

- `Assets/Project/Scripts/Interaction/RoomPrototypeLevelOneController.cs`
- модель навигации внутри этого же файла: `RoomPrototypeLevelOnePanelModel`
- тесты: `Assets/Project/Tests/EditMode/Scene01ControllerTests.cs`, класс `RoomPrototypeNavigationTests`

Фон:

- `Assets/Project/ART/UI/интерьер_с_правильным_делением_на_8.png`

Сцена строит UI в runtime:

- Canvas создаётся в `Awake()`.
- 4 панели 2x2 показывают обрезки одного 8-секторного фона.
- Тап по панели делает zoom-in.
- Кнопка `-` снизу слева делает zoom-out.
- Стрелки внутри панели двигают zoom-view.
- Заглушки ключа, машинки, яблока и клетки пока простые геометрические UI-спрайты.

Текущие viewport-правила:

- `TopLeft` начально показывает `A2-B3`, zoom в `A2`, стрелка влево ведёт к `A1` с ключницей.
- `BottomLeft` начально показывает `A1-B2`, zoom в `A1`, стрелки вправо/вниз ведут к `B2` с машинкой.
- `TopRight` начально показывает правый блок окна/клетки, zoom в `A4`.
- `BottomRight` показывает неподвижный сектор ножек стола, без zoom.

Пока НЕ реализовано:

- падение ключа;
- движение машинки;
- падение яблока;
- перекидывание ключа на стол;
- настоящее перемещение puzzle-панелей.

Архитектура прототипа специально разделяет:

- слот панели 2x2 (`RoomPrototypePanelSlot`);
- viewport комнаты (`RoomPrototypeViewport`);
- состояние zoom/navigation (`RoomPrototypePanelState`).

Это оставлено для будущего движения самих окон.

## Glow/particles клетки

Файл:

- `Assets/Project/Scripts/Interaction/CageEdgeGlowEffect.cs`

Что делает:

- строит мягкую желтоватую ауру по alpha-контуру PNG/спрайта;
- эмитит мелкие particles по краям картинки;
- если texture нельзя прочитать, fallback идёт по bounds спрайта;
- на первом клике `Scene01Controller` вызывает `ICageAuraFadeTarget.FadeOutForever()`, эффект фейдится и больше не включается.

Главные inspector-поля:

- `auraIntensity` - яркость ауры.
- `auraSizePixels` - размер ареола.
- `particleCount` - количество частиц, range до `250`.
- `auraOffset` - ручное смещение ауры и частиц; X вправо, Y вверх.
- `auraFadeOutDuration` - длительность отключения после первого клика.
- `glowColor` - цвет ауры.
- `auraPulseSpeed` - скорость пульсации в циклах/сек; `0` выключает пульсацию.
- `alphaThreshold` - порог alpha для поиска контура.
- `particleSampleStepPixels` - шаг сэмплирования контура.

Важно: пользователь уже просил не создавать новый эффект, а применять уже сделанный `CageEdgeGlowEffect` к новой картинке клетки.

## Виньетка Scene01

Логика в:

- `Scene01Controller.cs`
- shader: `Assets/Project/Shaders/IrisReveal.shader`

Требование пользователя:

- виньетка должна быть сплошным цветом, не "пятном-спрайтом";
- должна плавно появляться и исчезать;
- фон меняется незаметно в момент закрытия;
- картинка с клеткой не должна исчезать при смене фона;
- после третьего клика экран отдаляется, виньетка больше не вызывается.

Размер сжатия регулируется в inspector через:

- `vignetteCompressionRadius`

## Недавняя ошибка Unity 6 с UI Font

В Unity 6000 нельзя использовать:

```csharp
Resources.GetBuiltinResource<Font>("Arial.ttf")
```

Нужно:

```csharp
Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
```

В `RoomPrototypeLevelOneController` это зафиксировано константой:

```csharp
public const string BuiltInFontResourceName = "LegacyRuntime.ttf";
```

Есть regression-тест:

```csharp
BuiltInFontResourceName_UsesUnity6000RuntimeFont
```

Если в Game view чёрный экран и console показывает `Arial.ttf is no longer a valid built in font`, значит кто-то вернул старое имя ресурса.

## Тесты

Файл тестов:

- `Assets/Project/Tests/EditMode/Scene01ControllerTests.cs`

Покрывает:

- `SpriteContourSampler`
- `SpriteContourGlowTextureBuilder`
- `Scene01Controller.ShouldFadeCageAuraForClickStepIndex`
- `Scene01Controller.CalculateZoomResponsiveBackgroundScale`
- `RoomPrototypeLevelOnePanelModel`
- `RoomPrototypeLevelOneController.BuiltInFontResourceName`

Важно: `.csproj` в Unity генерируются, но для локальной `dotnet build` проверки иногда приходилось вручную добавить новый C# файл в:

- `Assembly-CSharp.csproj`
- `Independence.EditMode.Tests.csproj`

Unity может потом перегенерировать эти файлы.

## Пользовательский дизайн-контекст первого уровня

Игра вдохновлена `Gorogoa`: поле 2x2, где окна в будущем будут двигаться/совмещаться.

Комната делится на 8 секторов:

- строки: `A`, `B`
- столбцы: `1`, `2`, `3`, `4`

Первый уровень по задумке:

- `TopRight`: окно и клетка, клетку надо один раз приблизить. При zoom верхняя часть клетки должна совместиться с нижним правым сектором стола.
- `BottomRight`: ножки стола, неподвижный сектор.
- `TopLeft`: можно zoom на шкаф/область `A2`, потом стрелкой влево перейти к `A1`, где ключница с ключом.
- `BottomLeft`: можно zoom на область, потом стрелками добраться к машинке в `B2`.
- Когда `TopLeft` показывает ключницу, а `BottomLeft` под ней показывает машинку, в будущем ключ должен падать в машинку.
- На столе с клеткой будет яблоко. В будущем яблоко падает на машинку и подбрасывает ключ на стол с клеткой.

Сейчас реализованы только:

- zoom;
- zoom-out;
- стрелочная навигация;
- геометрические placeholder-объекты.

## Пользовательские предпочтения

- Отвечать по-русски.
- Делать короткие понятные отчёты.
- Не спрашивать разрешение на очевидные Unity-правки.
- Перед каждой новой просьбой делать checkpoint-коммит, если есть изменения.
- Если просит "убери из проекта" - действительно убрать сделанное, а не просто отключить.
- Если visual bug - сначала искать причины в Sorting Layer, Canvas/RectTransform, SpriteRenderer, alpha/texture import, camera/orthographic size.
- Не редактировать unrelated вещи и не откатывать пользовательские изменения.

## Рекомендуемый порядок для следующей задачи

1. Прочитать `AGENTS.md`, `Assets/Project/AGENTS.md`, этот файл.
2. `git status --short`
3. Если есть изменения - checkpoint.
4. Найти относящиеся сцену/скрипт/ассеты через `rg`.
5. Внести минимальную правку.
6. Проверить:
   - `dotnet build Assembly-CSharp.csproj --nologo`
   - `dotnet build Independence.EditMode.Tests.csproj --nologo`
   - `git diff --check`
7. Если задача про сцену - дополнительно проверить YAML на missing scripts.
8. Коротко отчитаться пользователю.
