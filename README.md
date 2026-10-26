# SCP Start Freezer v2.1.0

<p align="center">
  <img src="https://img.shields.io/badge/EXILED-v9+-red?style=for-the-badge">
  <img src="https://img.shields.io/badge/SCP:SL-PLUGIN-blue?style=for-the-badge">
  <img src="https://img.shields.io/badge/Version-2.1.0-green?style=for-the-badge">
  <img src="https://img.shields.io/badge/.NET%20Framework-4.8-purple?style=for-the-badge">
</p>

<p align="center">
  <b>Stop SCPs from running around during the first seconds of the round.</b>
</p>

---

# 🇵🇱 Polski

## 🔥 O projekcie

**SCP Start Freezer** to lekki plugin do **SCP: Secret Laboratory (EXILED)**, który na początku rundy unieruchamia wybrane SCP (efekt *Ensnared*), żeby ludzie zdążyli się rozejść po placówce.

Wersja **v2.1.0** naprawia problem SCP przydzielanych **z opóźnieniem** (np. przez inne pluginy po 1-2 sekundach od startu rundy). Wcześniej takie SCP nie dostawały freeza, bo zmiana roli czyściła efekt.

---

## ✨ Jak to działa

- **Czas freeza liczony jest od startu rundy**, a nie od spawnu gracza. Przy `freeze_duration: 20` SCP przydzielony po 2 sekundach rundy dostaje pozostałe ~18 sekund, a nie nowe 20.
- **Freeze jest odświeżany co 0,5 s.** Jeśli zmiana roli albo inny plugin zdejmie efekt w trakcie okna freeza, plugin założy go ponownie.
- **Zmiana roli nie nakłada freezów.** Każdy nowy spawn gracza zatrzymuje jego poprzednią pętlę i startuje nową (jeśli nowa rola jest w configu ustawiona na `true`).
- **Komunikat jest pokazywany raz**, z faktycznie pozostałym czasem freeza (np. 18 sekund, jeśli SCP przydzielono po 2 s).
- **Pełna kontrola w configu:** wybierasz, które role SCP mają być zamrażane, oraz jak długo.
- **Czyszczenie stanu:** pętle są zatrzymywane przy `RestartingRound` i przy wyłączeniu pluginu.

### ⚠️ Ograniczenia
- Freeze działa **tylko w pierwszych `freeze_duration` sekundach rundy**. SCP przydzielony później (np. w połowie rundy po wyjściu innego gracza) freeza nie dostanie.
- Plugin rozpoznaje SCP po **typie roli** (`RoleTypeId`) z `scp_freeze_settings`. Customowa rola jest zamrażana wtedy, gdy jej bazowa rola jest w configu ustawiona na `true`.

---

## 📋 Wymagania

- Serwer **SCP: Secret Laboratory** z zainstalowanym **EXILED 9.x** (plugin był budowany pod 9.12.6).
- Nie są potrzebne żadne dodatkowe pluginy ani biblioteki.

---

## ⚙️ Instalacja

1. Zainstaluj EXILED na serwerze, jeśli jeszcze go nie masz.
2. Pobierz `ScpFreezer.dll` z zakładki **Releases**.
3. Wrzuć plik do folderu pluginów:
   - Windows: `%AppData%\EXILED\Plugins`
   - Linux: `~/.config/EXILED/Plugins`
4. Zrestartuj serwer. Przy pierwszym starcie plugin wygeneruje swój config.
5. Skonfiguruj plugin (sekcja `scp_freezer`) w pliku:
   - `EXILED/Configs/<PORT>-config.yml`
   - lub, jeśli masz włączone osobne configi, w `EXILED/Configs/Plugins/scp_freezer/`
6. Po zmianie configu zrestartuj serwer.

### 🔄 Aktualizacja z v2.0.0
Podmień `ScpFreezer.dll` na nową wersję. Config się nie zmienił, więc możesz zostawić stary.

---

## 🛠️ Konfiguracja

```yaml
scp_freezer:
  # Czy plugin jest włączony?
  is_enabled: true

  # Logi debugowania w konsoli
  debug: false

  # Czas freeza w sekundach, liczony od startu rundy
  freeze_duration: 20

  # Wiadomość wyświetlana SCP. %time% = pozostały czas w sekundach
  freeze_message: '<i>Systemy startowe... Pozostaniesz w przechowalni jeszcze przez <color=red>%time%</color> sekund.</i>'

  # Które SCP mają być zamrażane (true = zamrażaj)
  scp_freeze_settings:
    Scp049: true
    Scp0492: false
    Scp079: false
    Scp096: true
    Scp106: true
    Scp173: true
    Scp939: true
    Scp3114: false
```

| Opcja | Opis |
| :--- | :--- |
| `freeze_duration` | Ile sekund od startu rundy trwa freeze. |
| `freeze_message` | Komunikat dla zamrożonego gracza. Pusty tekst wyłącza komunikat. |
| `scp_freeze_settings` | Słownik `rola: true/false`. Roli, której nie ma w słowniku, plugin nie zamraża. |

---

## 🧪 Rozwiązywanie problemów

- **SCP nadal nie jest zamrożony:** sprawdź, czy jego rola ma `true` w `scp_freeze_settings` i czy w momencie przydziału nie minęło jeszcze `freeze_duration` sekund rundy.
- **Przydział SCP idzie przez inny plugin:** włącz `debug: true` i sprawdź w logach, czy event `Spawned` odpala się dla przydzielonego SCP. Jeśli plugin ustawia rolę bez wywołania tego eventu, zgłoś to w Issues i dołącz jego nazwę.

---

# 🇬🇧 English

## 🔥 About

**SCP Start Freezer** is a lightweight **EXILED plugin** for **SCP: Secret Laboratory** that freezes selected SCPs (using the *Ensnared* effect) at the start of the round, so humans have time to spread out across the facility.

Version **v2.1.0** fixes the issue with SCPs **assigned with a delay** (e.g. by other plugins 1-2 seconds after round start). Previously those SCPs didn't get frozen, because the role change cleared the effect.

---

## ✨ How it works

- **Freeze time is counted from round start**, not from the player's spawn. With `freeze_duration: 20`, an SCP assigned 2 seconds into the round gets the remaining ~18 seconds, not a fresh 20.
- **The freeze is refreshed every 0.5 s.** If a role change or another plugin removes the effect during the freeze window, the plugin applies it again.
- **Role changes never stack freezes.** Every new spawn stops the player's previous loop and starts a new one (if the new role is set to `true` in the config).
- **The message is shown once**, with the actual remaining freeze time (e.g. 18 seconds if the SCP was assigned after 2 s).
- **Fully configurable:** choose which SCP roles are frozen and for how long.
- **Clean state:** loops are stopped on `RestartingRound` and when the plugin is disabled.

### ⚠️ Limitations
- The freeze only works during the **first `freeze_duration` seconds of the round**. An SCP assigned later (e.g. mid-round after another player leaves) won't be frozen.
- The plugin identifies SCPs by their **role type** (`RoleTypeId`) from `scp_freeze_settings`. A custom role is frozen if its base role is set to `true` in the config.

---

## 📋 Requirements

- An **SCP: Secret Laboratory** server with **EXILED 9.x** installed (the plugin was built against 9.12.6).
- No additional plugins or libraries are required.

---

## ⚙️ Installation

1. Install EXILED on your server if you haven't already.
2. Download `ScpFreezer.dll` from the **Releases** tab.
3. Put the file into the plugins folder:
   - Windows: `%AppData%\EXILED\Plugins`
   - Linux: `~/.config/EXILED/Plugins`
4. Restart the server. On first start the plugin generates its config.
5. Configure the plugin (`scp_freezer` section) in:
   - `EXILED/Configs/<PORT>-config.yml`
   - or, if separate configs are enabled, in `EXILED/Configs/Plugins/scp_freezer/`
6. Restart the server after changing the config.

### 🔄 Updating from v2.0.0
Replace `ScpFreezer.dll` with the new version. The config didn't change, so you can keep the old one.

---

## 🛠️ Example config

```yaml
scp_freezer:
  is_enabled: true
  debug: false

  # Freeze time in seconds, counted from round start
  freeze_duration: 20

  # %time% = remaining time in seconds
  freeze_message: '<i>Startup systems... You will remain frozen for <color=red>%time%</color> more seconds.</i>'

  # Which SCPs get frozen (true = freeze)
  scp_freeze_settings:
    Scp049: true
    Scp0492: false
    Scp079: false
    Scp096: true
    Scp106: true
    Scp173: true
    Scp939: true
    Scp3114: false
```

| Option | Description |
| :--- | :--- |
| `freeze_duration` | How many seconds from round start the freeze lasts. |
| `freeze_message` | Message shown to the frozen player. An empty string disables it. |
| `scp_freeze_settings` | `role: true/false` dictionary. Roles missing from the dictionary are not frozen. |

---

## 🧪 Troubleshooting

- **An SCP is still not frozen:** check that its role is `true` in `scp_freeze_settings` and that fewer than `freeze_duration` seconds of the round have passed when it is assigned.
- **SCPs are assigned by another plugin:** enable `debug: true` and check the logs to see whether the `Spawned` event fires for the assigned SCP. If the plugin sets roles without raising that event, open an Issue and include its name.

---

# ❤️ Support

This project was created from scratch and released completely for free as part of the **JastrzabDev** tools ecosystem.

If the plugin helped your server, consider supporting future development with a small coffee donation ☕

## 🌐 Links
- Portfolio: `adamjastrzebski.bio`
- Ko-fi: [ko-fi.com/jastrzabdev](https://ko-fi.com/jastrzabdev)

---

<p align="center">
  Made with ☕ by JastrzabDev
</p>
