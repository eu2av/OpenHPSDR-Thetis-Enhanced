# CHANGELOG — Yurij-eu2av (Thetis modifications)

**Проект:** Thetis SDR (openHPSDR transceiver software)
**Версия-база:** Thetis 2.10.x (VS2026 solution)
**Дата:** 2026-07-03
**Автор:** Yurij-eu2av
**Назначение:** Документирование локальных дополнений для передачи авторам Thetis / интеграции в upstream.

---

## 2026-07-04 (2) — 16-bit Float Pipeline + DeviceContext Migration + New Schemes + Bug Fixes

### Контекст
Водопад Thetis работал в 8-бит (Format.B8G8R8A8_UNorm) через legacy Direct2D 1.0
(`ID2D1RenderTarget`). Это ограничивало качество: 256 уровней на канал, видимые
полосы (banding) на плавных градиентах, "пластиковый" вид. Пользователи
сравнивали с SDR Console / HDSDR и просили современный водопад.

### Достижения (подряд)

#### 1. Миграция Direct2D 1.0 → 1.1 (DeviceContext)
Legacy `RenderTarget` не поддерживает 10/16-бит форматы surface. Мигрировали на
`ID2D1DeviceContext` (Direct2D 1.1+, Windows 8+):
- `Factory` → `Factory1`
- `new RenderTarget(factory, surface, rtp)` → `Device + DeviceContext + Bitmap1(Target)`
- D2D Device создаётся один раз, живёт через resize (иначе `WRONG_RESOURCE_DOMAIN`)
- Fallback на legacy RenderTarget если DeviceContext не создаётся
- **Бонус:** DeviceContext устойчивее к device removal (GPU crash/driver update)

#### 2. 16-bit Float Pipeline (8/16-bit)
- Swap chain: `B8G8R8A8_UNorm` (8-бит, 256 уровней) или `R16G16B16A16_Float`
  (16-бит half-float, 65536 уровней) — selectable, live, без restart
- 10-бит (`R10G10B10A2`) убран — Direct2D не поддерживает его как bitmap surface
- `WaterfallPixelWriter` — формат-зависимый кодировщик float RGBA → packed bytes
- Float-конвейер: все 8 схем вычисляют цвет в `float` (0..255), gamma/dither/quality
  в float, квантование только на выходе

#### 3. sRGB→linear компенсация
На 16-бит float Direct2D работает в linear RGB, на 8-бит — в sRGB. Без компенсации
цвета выглядели иначе (светлее). Решение:
- `convertColour` → sRGB→linear для RGB + alpha при 16-бит
- `EncodeRow16` → sRGB→linear для пикселей водопада
- `SDXBitmapFromSysBitmap` → sRGB→linear + RGBA для фонового фото
- Stroke width scaling (`sw()`) для толщины линий (linear coverage ~2x толще)
- Alpha blending компенсация (`pow(a, 1.6)`) для filter overlay

#### 4. Три новые схемы окраски (WaterfallPalette)
Multi-stop gradient interpolator (256-step LUT) вместо жёстких if/else bands:
- **Console 256** — SDR Console стиль: насыщенный радужный градиент
  (black→navy→blue→cyan→green→yellow→orange→red→pink→white, 12 stops)
- **Thermal 256** — тепловизор/Inferno: тёплая гамма
  (black→indigo→purple→magenta→red→orange→yellow→white, 9 stops)
- **DeepBlue 256** — прохладный контраст: deep blue→cyan→mint→white, 8 stops
- По умолчанию теперь **Console 256** (вместо enhanced)
- Новые схемы в начале списка combo (RX1/RX2/TX)

#### 5. Quality уровни (заменили бесполезный Palette Resolution)
Вместо невидимого изменения размера LUT — видимые эффекты в float pipeline:
- **Classic** — без обработки (default)
- **Vivid** — +30% saturation (пиксельный domain)
- **Sharp** — Vivid + 25% контраст (S-curve на signal percent, виден на enhanced)
- **Ultra** — Sharp + 30% контраст + auto-dither
- S-curve применяется к `overall_percent` ДО выбора цвета → работает на всех схемах

#### 6. Баг-фикс: тусклый водопад после Custom (многолетний баг)
**Корень:** Custom scheme при незагруженном gradient делал `break` из switch, но
код после switch (EncodeRow, CopyFromMemory) выполнялся с пустым rowF (нули=чёрный).
Чёрная строка писалась в bitmap каждый кадр, затирая существующий водопад.
**Исправление:** флаг `skipRow` — весь pipeline записи пропускается если схема
не произвела цвет.

#### 7. Баг-фикс: ADC overload моргание при TX→RX (многолетний баг)
**Корень:** ADC overload флаг в native коде (`network.c`) latch'ится через `||`
и не сбрасывается при TX→RX переходе. Первый RX poll читал stale latched overload
→ кратковременное предупреждение "ADC0 Overload".
**Исправление:** `UIMOXChangedFalse()` теперь сбрасывает native latch через
`NetworkIO.getAndResetADC_Overload()` + C# счётчики `_adc_overload_level[]`.
Безопасно: не трогает FPGA/native код, только очищает stale state.

### Новые файлы (2)
| Файл | Назначение |
|---|---|
| **`Console/WaterfallPixelWriter.cs`** | формат-зависимый кодировщик: `EncodeRow` (8/16-бит), `FillClearBuffer`, `SrgbToLinear`, `FloatToHalfBitsPublic` |
| **`Console/WaterfallPalette.cs`** | multi-stop gradient interpolator: `Build(Stop[])`, `Sample(percent)`, готовые палитры ConsoleStops/ThermalStops/DeepBlueStops |

### Изменённые файлы
| Файл | Изменения |
|---|---|
| `Console/display.cs` | DeviceContext миграция; createD2DRenderTarget; RebuildForColorDepth; sRGB→linear; sw() stroke scaling; 3 новых case в switch; DrawPaletteScheme helper; skipRow баг-фикс; alpha компенсация |
| `Console/WaterfallEnhancer.cs` | ColorDepth enum; QualityLevel enum + SetQuality; ApplyQualityPercent (S-curve); ApplySaturationContrast; ApplyGammaFloat/ApplyDitherFloat (float версии) |
| `Console/enums.cs` | +Console, +Thermal, +DeepBlue в ColorScheme |
| `Console/setup.cs` | comboColorDepth (8/16-bit, live rebuild); Quality combo (Classic/Vivid/Sharp/Ultra); handlers; Sync |
| `Console/setup.designer.cs` | +3 опции в RX1/RX2/TX combo (Console 256, Thermal 256, DeepBlue 256) |
| `Console/console.cs` | ADC overload reset в UIMOXChangedFalse (TX→RX баг-фикс) |
| `Console/Thetis.csproj` | +WaterfallPixelWriter.cs, +WaterfallPalette.cs |

### Безопасность
- **Default = Console 256 + 8-бит + Classic Quality** — нулевые regressions
- Не трогает FPGA прошивку или native код (кроме чтения ADC overload)
- Существующие схемы (enhanced, SPECTRAN, LinRad...) остаются без изменений
- Fallback на legacy RenderTarget если GPU не поддерживает DeviceContext
- Live переключение 8↔16-бит без restart, с fallback на 8-бит при ошибке
- Все настройки сохраняются в database.xml

---



### Контекст
После DPI-awareness фикса водопад стал резче на HiDPI/2K, но пользователи
отметили, что эффект «мало заметен» — цвета выглядят плоско/тускло, видны полосы
(banding) на плавных градиентах. Анализ показал: архитектура рендеринга (Direct2D)
уже качественная, но **цветовая палитра Custom scheme** ограничена всего **101
шагом LUT** без дизеринга и с линейным маппингом dB→цвет.

### Решение — новая подсистема `WaterfallEnhancer`
Отдельный статический класс (`WaterfallEnhancer.cs`) с тремя рычагами качества.
Все настройки default = текущее поведение (нулевые regressions), хранятся в
database.xml, переносятся с профилем.

#### 1. Palette Resolution (101 → 256/512/1024)
Расширение LUT с фиксированных 101 до выбираемого размера. Палитра теперь
сэмплируется из градиентного редактора (`ucLGPicker`) с нужной плотностью.
- **Эффект:** плавные градиенты, исчезновение «бандов»/полос.

#### 2. Color Dithering (Ordered Bayer 8×8)
Лёгкий упорядоченный дизеринг (±2 LSB) применяется к RGB перед записью в пиксель.
- **Эффект:** полосы полностью маскируются, даже при 101-шаговой палитре.

#### 3. Gamma Curve
Степенная кривая применяется к `overall_percent` перед lookup в LUT.
- gamma < 1.0 → «вытягивает» слабые сигналы (больше деталей в шуме)
- gamma > 1.0 → усиливает контраст сильных сигналов
- gamma = 1.0 → линейно (default)

### Файлы (4)
| Файл | Изменение |
|---|---|
| **`Console/WaterfallEnhancer.cs`** (НОВЫЙ) | статический класс: `LUT_SIZE`, `DitherEnabled`, `Gamma`, Bayer 8×8 matrix, методы `ApplyGamma()`, `ApplyDitherChannel()` |
| `Console/display.cs` | массивы `Color[101]` → `Color[LUT_SIZE]` (динамический); guards `!= 101` → динамический; lookup `cols[100]` → `cols[lut-1]`, `* 100f` → `* (lut-1)`; gamma + dither в Custom scheme |
| `Console/setup.cs` | поля контролов; `InitWaterfallQualityControls()` (программное создание в grpDisplayDriverEngine); handlers; `SyncWaterfallEnhancerFromControls()` после getOptions; `WaterfallRXGradient()`/`WaterfallTXGradient()` → динамический размер |
| `Console/setup.designer.cs` | НЕ трогается — контролы программно |

### UI
В группе **"DirectX Display Settings"** (вкладка Display), под chkDpiAwareness:
- **Palette:** ComboBox ("101 Classic" / "256 High" / "512 Ultra" / "1024 Max")
- **Dither:** CheckBox
- **Gamma:** TrackBar (0.50–2.00) + label значения

Все контролы с уникальными именами → авто-сохранение через `SaveOptions/getOptions`.

### Безопасность
- **Default = текущее поведение** (LUT 101, dither OFF, gamma 1.0)
- Не трогает gradient editor (`ucLGPicker`) — он работает как прежде
- Не трогает enhanced/SPECTRAN — только Custom scheme (где LUT)
- Работает поверх существующего кода — точка расширения для будущих улучшений

### Процедура для пользователя
1. Setup → Display → "DirectX Display Settings"
2. **Palette:** выбрать "256 High" (или выше для мощного GPU)
3. **Dither:** включить (убирает полосы)
4. **Gamma:** подстроить под вкус (0.8 вытягивает слабые сигналы)
5. OK → настройки сохранятся в database.xml

### Бэкапы
- `display.cs.wfq_backup_20260703`
- `setup.cs.wfq_backup_20260703`

### Будущее
- Удаление dead code в старых схемах (enhanced/SPECTRAN/original)
- Перевод всех схем на единую LUT-архитектуру через WaterfallEnhancer
- Расширенный gradient editor (больше stops, кривые)
- Auto-contrast / noise floor tracking в WaterfallEnhancer

---

## 2026-07-03 (2) — Voltage Calibration (PA Volts + Supply 13.8V)

### Проблема
В Thetis была калибровка **тока** (AmpVoff/AmpSens в группе "Current (A)
calculation"), но **напряжение** считалось через `convertToVolts` (PA Volts,
AIN3) и `computeHermesDCVoltage` (Supply 13.8V, AIN6) с **захардкоженными**
делителями без какой-либо корректировки. После crosstalk-fix'а на FPGA
(dummy-conversion изменила timing опроса АЦП) показания напряжения чуть
сместились (13.8 → 13.9В).

### Решение
Добавлены **per-device множители** для обоих трактов напряжения:
- `PAVoltCal` — для PA Voltage (AIN3, `convertToVolts`)
- `SupplyVoltCal` — для Supply 13.8V (AIN6, `computeHermesDCVoltage`)

`k = 1.000` → без коррекции (default, нулевые regressions). Множитель применяется
к итоговому значению вольт. Пример: показывает 13.9В при реальных 13.8 →
`k = 13.8/13.9 = 0.993`.

### Файлы (2)
| Файл | Изменение |
|---|---|
| `Console/console.cs` | свойства `PAVoltCal`/`SupplyVoltCal`; множители в `convertToVolts` (~24959) и `computeHermesDCVoltage` (~24811) |
| `Console/setup.cs` | поля `udPAVoltCal`/`udSupplyVoltCal` + подписи; программное создание в `initVoltsAmpsCalibration`; handlers `*_ValueChanged` |

### UI
В группе **"Current (A) calculation"** (вкладка Calibration), в нижней части
(под существующими калибровками тока и чекбоксом Log Volts/Amps), добавлены:
- **"PA V cal:"** (0.100–5.000, шаг 0.001, default 1.000) — для PA Voltage (AIN3)
- **"Supply V cal:"** (0.100–5.000, шаг 0.001, default 1.000) — для Supply 13.8V (AIN6)
- **Кнопка "V Default"** — сброс обоих множителей в 1.000 (без коррекции)

Группа увеличена по высоте (227→300) для размещения новых контролов без
перекрытия существующих. Контролы создаются программно в `initVoltsAmpsCalibration`
и вызывают `BringToFront()` для корректного Z-order.

**Персистентность:** контролы `udPAVoltCal`/`udSupplyVoltCal` с уникальными
именами автоматически сохраняются/загружаются через существующий механизм
`SaveOptions()`/`getOptions()` (ключом — имя контрола).

### Процедура калибровки
1. Setup → вкладка Calibration → группа "Current (A) calculation"
2. Проверить реальное напряжение внешним мультиметром (питание или PA)
3. Если Thetis показывает X В при реальных Y → `cal = Y / X`
   (пример: показывает 13.9В при реальных 13.8 → Supply V cal = 13.8/13.9 = 0.993)
4. Аналогично для PA Volts
5. OK → настройки сохранятся в database.xml
6. Кнопка **"V Default"** — быстрый сброс в 1.000

### Бэкапы
- `console.cs.volts_backup_20260703`
- `setup.cs.volts_backup_20260703`

---

## 2026-07-03 — Per-Monitor DPI Awareness toggle (качество водопада на 2K+)

### Проблема
На HiDPI/2K-мониторах водопад и панадаптер выглядели мыльно/размыто, «низкая
битность» градиентов — заметно хуже, чем на HD. Корень: процесс Thetis **не был
объявлен DPI-aware** (блок `<dpiAware>` в `app.manifest` закомментирован,
`SetProcessDpiAwareness(2)` в Main закомментирован). D2D swap-chain и waterfall
bitmap получают **логическое** разрешение (~1280px), а DWM compositor растягивает
его до физических ~2560px → блюр.

### Решение — toggle Per-Monitor V2 (registry-backed)
По образцу существующего `chkShowStartupLog` (registry, читается рано в Main):
флаг хранится в `HKCU\Software\OpenHPSDR\Thetis-x64\DpiAwareness` (DWORD).
Применяется в самом начале `Main()`, до создания любого окна.

Когда toggle ON:
- `SetProcessDpiAwarenessContext(PER_MONITOR_V2)` (Win10 1703+)
- fallback `SetProcessDpiAwareness(PER_MONITOR_AWARE)` (Win8.1)
- try/catch для старых ОС

**Эффект автоматический:** swap-chain (`displayTarget.Width/Height`),
waterfall bitmap и FFT-pixel count (`pixels = displayTargetWidth`) начинают
получать **физическое** разрешение — без правок `display.cs`. AntiAlias для
тассы уже включён по умолчанию (`chkAntiAlias.Checked = true`).

### Файлы (4)
| Файл | Изменение |
|---|---|
| `clsProgressLog.cs` (LogTool) | `GetRegistryDpiAwareness`/`SetRegistryDpiAwareness` + read/write helpers (по образцу ShowLog) |
| `console.cs` | P/Invoke `SetProcessDpiAwarenessContext` + `SetProcessDpiAwareness`; применение в начале Main (после args tidy, до SingleInstance); убран старый закомментированный вызов |
| `setup.cs` | поле `chkDpiAwareness`; `CreateDpiAwarenessCheckBox()` (программное создание, рядом с chkShowStartupLog); `chkDpiAwareness_CheckedChanged` (MessageBox "restart required"); `updateDpiAwarenessCheckBox`; исключение из DB в `getOptions` |
| `setup.designer.cs` | НЕ трогается — чекбокс создаётся программно |

### UI
Чекбокс **"DPI Awareness (HiDPI/2K)"** на вкладке Display → группа "DirectX Display
Settings" (рядом с chkAntiAlias — оба относятся к качеству рендеринга). Создаётся
программно (`CreateDpiAwarenessCheckBox`), позиционируется под chkAntiAlias.
При смене галочки → MessageBox "Restart Required" → пользователь перезапускает.

### Безопасность
- **Default OFF** → при первом запуске поведение идентично оригинальному
- Registry (не DB) → читается в начале Main, до форм (DB грузится слишком поздно)
- WinForms UI масштабируется ОС → если на конкретной системе разъедется, пользователь
  выключает галочку → рестарт → откат
- Restart flow уже встроен в Main (`Application.Restart()`)

### Процедура для пользователя
1. Setup → вкладка Display → группа "DirectX Display Settings" → галочка "DPI Awareness (HiDPI/2K)"
2. Поставить → MessageBox "Restart Required" → OK → перезапуск
3. Водопад/панадаптер стали резкими на 2K
4. Если UI разъехался → снять галочку → рестарт (откат)

### Бэкапы
- `clsProgressLog.cs.dpi_backup_20260703`
- `console.cs.dpi_backup_20260703`
- `setup.cs.dpi_backup_20260703`
- `setup.designer.cs.dpi_backup_20260703`

---

## 2026-07-02 — Per-band Power Detector Calibration

### Проблема
Стандартная калибровка мощности в Thetis — глобальная (drive calibration), она не
учитывает частотную зависимость направленного ответвителя (tandem-match coupler).
На НЧ-бендах (160/80/40м) показания мощности занижались на 4–12% (160м: ~91 Вт
при реальных 100 Вт в нагрузке), потому что детектор физически выдаёт другое
напряжение на низких частотах. КСВ при этом оставался корректным, поскольку
искажение FWD и REV одинаковое.

Thetis не имел per-band калибровки FWD/REV детектора. Существующий метод
`PABandOffset(Band)` (console.cs:6607) был закомментирован/возвращал 0 и не
использовался в живом тракте (Path A — `computeAlexFwdPower`/`computeRefPower` →
`alex_fwd`/`alex_rev` → meters).

### Решение
Добавлен **per-band множитель `k`** к `bridge_volt` в формуле
`watts = V²/bridge_volt`:
- `watts_corr = V² · k / bridge_volt`
- `k = 1.00` → без коррекции (нулевые regressions, поведение идентично текущему)
- `k > 1` → показания больше (если занижало)
- `k < 1` → показания меньше

Множитель **общий для FWD и REV** (применяется одинаково), поэтому КСВ
(= √(Rev/Fwd)) не меняется — это физически корректно при одинаковой частотной
зависимости обоих детекторов.

### Файлы (3)
| Файл | Изменение |
|---|---|
| `Console/console.cs` | метод `PABandCalMult(Band)`; правка формулы в `computeAlexFwdPower` (~25120) и `computeRefPower` (~25040) — Path A, live meters |
| `Console/setup.cs` | `GetPABandCalMult(Band)` + `GetPABandCalControl(Band)`; `InitDetCalTab()` — программное создание вкладки UI (после `InitializeComponent()`) |
| `Console/setup.designer.cs` | объявления 11 полей контролов + 1 поле `tpDetCal` (без инстанциации — всё в setup.cs) |

### UI
Новая вкладка **"Det. Cal."** в Setup → PA Settings (третья после "PA Gain" и
"Watt Meter"). Содержит 11 `NumericUpDownTS` полей (160m..6m), каждое:
- `Minimum = 0.50`, `Maximum = 2.00`, `Increment = 0.01`, `DecimalPlaces = 2`
- `Value = 1.00` (по умолчанию)
- Поясняющий текст: «k = 1.00 = no correction. k > 1 increases reading...»
- Кнопка **"Reset to Defaults"** (справа) — восстанавливает model-зависимые
  factory-значения, перезаписывая сохранённые.

**Персистентность:** контролы сохраняются/загружаются автоматически через
существующий механизм `SaveOptions()`/`getOptions()` (setup.cs:1519/1706) —
ключом является имя контрола (`udDetCalB160M` и т.д.). Никакого дополнительного
кода сериализации не требуется.

### Defaults для Anvelina PRO3 / ANAN7000D
Factory-значения определены для моделей `HPSDRModel.ANVELINAPRO3` и
`HPSDRModel.ANAN7000D` (в Thetis это одно и то же семейство), откалиброванные по
внешнему ваттметру в dummy load (2026-07-02):

| Бенд | k | Примечание |
|---|---|---|
| 160m | 0.91 | компенсирует завышение ~9% |
| 80m  | 0.92 | компенсирует завышение ~8% |
| 40m  | 0.96 | компенсирует завышение ~4% |
| 60m, 30m, 20m, 17m, 15m, 12m, 10m, 6m | 1.00 | без коррекции |

**Логика применения defaults (два уровня):**
1. **При запуске** — метод `ApplyDetCalDefaults()` вызывается в конструкторе
   Setup ПОСЛЕ `getModelFromDB()` (когда модель уже известна) и ДО `getOptions()`.
   Если у пользователя есть сохранённые значения в database.xml — `getOptions()`
   корректно перетирает defaults на его настройки (приоритет пользователя).
2. **По кнопке "Reset to Defaults"** — `btnDetCalReset_Click` сбрасывает все поля
   на 1.00, затем применяет `ApplyDetCalDefaults()`. Пользователь жмёт OK → новые
   значения сохраняются в database.xml.

Это решает проблему «закупоренных» значений: если при первых запусках сохранились
1.00, кнопка Reset позволяет вернуть factory-калибровку одним кликом.

### Процедура калибровки для пользователя
**Быстрый старт (Anvelina PRO3 / ANAN7000D):**
1. Setup → PA Settings → вкладка **"Det. Cal."**
2. Нажать **"Reset to Defaults"** → применятся factory-значения (0.91/0.92/0.96)
3. OK → настройки сохранятся в database.xml

**Ручная калибровка (любая модель):**
1. Setup → PA Settings → вкладка **"Det. Cal."**
2. Опорный бенд (например 20м): оставить 1.00
3. Подать калиброванную мощность (100 Вт) на бенде, сравнить с внешним ваттметром
4. Если Thetis показывает X Вт при реальных 100 → поставить `k = 100 / X`
   (пример: показывает 110 Вт → k = 100/110 = 0.91)
5. OK → настройки сохранятся в database.xml

### Почему выбран Path A (а не Path B)
В коде Thetis есть два тракта мощности:
- **Path A (живой):** `computeAlexFwdPower`/`computeRefPower` → `alex_fwd`/`alex_rev`
  → `PollPAPWR` → meters/CAT. **Используется.**
- **Path B (мёртвый):** `PABandOffset`/`ScaledVoltage`/`ADCtodBm` → всегда
  возвращает 0, не вызывается из живого кода. **Не трогается.**

Правка применена в Path A, где реально формируются показания.

### Безопасность
- **k = 1.0 по умолчанию** → поведение идентично оригинальному Thetis
- Не трогает Path B (мёртвый код)
- Не трогает `PAProfile` (отдельная подсистема per-band gain)
- Не трогает существующие вкладки PA Gain / Watt Meter
- Авто-сохранение через стандартный механизм (zero persistence code)

### Бэкапы
- `Console/console.cs.detcal_backup_20260702`
- `Console/setup.cs.detcal_backup_20260702`
- `Console/setup.designer.cs.detcal_backup_20260702`

---

## Связанные работы (FPGA сторона)

Эти изменения Thetis дополняют работу на стороне FPGA (Anvelina PRO III / Orion MK2,
версия прошивки 2.2.14), где была устранена дрожь показаний (crosstalk/ghosting в
мультиплексоре ADC78H90 через dummy-conversion). Полная документация FPGA-части —
в `CHANGELOG_Yurij_eu2av.md` репозитория Verilog-проекта.
