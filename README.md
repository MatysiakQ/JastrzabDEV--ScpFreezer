# SCP Start Freezer v2.0.0

<p align="center">
  <img src="https://img.shields.io/badge/EXILED-v9+-red?style=for-the-badge">
  <img src="https://img.shields.io/badge/SCP:SL-PLUGIN-blue?style=for-the-badge">
  <img src="https://img.shields.io/badge/Version-2.0.0-green?style=for-the-badge">
  <img src="https://img.shields.io/badge/Optimized-Zero%20TPS%20Drops-success?style=for-the-badge">
</p>

<p align="center">
  <b>Stop SCPs from escaping during the first seconds of the round.</b>
</p>

---

# 🇵🇱 Polski

## 🔥 O projekcie

**SCP Start Freezer** to inteligentny i lekki plugin do **SCP: Secret Laboratory (EXILED)**, który unieruchamia wybrane SCP tuż po ich zrespieniu.

Wersja **v2.0.0** została napisana całkowicie od nowa przez **JastrzabDev**, aby rozwiązać odwieczny problem z kompatybilnością i opóźnionymi spawnami Customowych SCP.

Plugin działa stabilnie nawet przy użyciu:
- Custom Roles
- SCP frameworków
- opóźnionych spawnów
- pluginów zmieniających role po starcie rundy

---

## ✨ Główne Funkcje

### 🧠 Odporność na Custom Roles
Plugin zapamiętuje czas pierwszego spawnu gracza.

Jeśli po kilku sekundach inny plugin zmieni rolę gracza na Custom SCP:
- czas freeza NIE resetuje się,
- gracz NIE dostaje kolejnych 20 sekund,
- freeze zostaje kontynuowany tylko przez pozostały czas.

Koniec z podwójnym freezowaniem SCP.

---

### ⚙️ Precyzyjna konfiguracja
Pełna kontrola nad tym:
- które SCP mają zostać zamrożone,
- które SCP mają zostać pominięte,
- jak długo ma trwać blokada.

Możesz np.:
- ignorować SCP-079,
- ignorować zombie,
- zamrażać tylko agresywne SCP.

---

### 📢 Dynamiczne Broadcasty
Zamrożeni gracze otrzymują dynamiczny komunikat z pozostałym czasem blokady.

Przykład:
```yaml
Systemy startowe... Pozostaniesz w przechowalni jeszcze przez 12 sekund.
```

---

### 🚀 Maksymalna optymalizacja
Plugin NIE korzysta ze starych tickerów opartych o start rundy.

Zamiast tego:
- nasłuchuje eventów spawnu,
- działa wyłącznie wtedy, gdy jest potrzebny,
- minimalizuje obciążenie serwera.

✅ Zero TPS drops.

---

# ⚙️ Instalacja

1. Pobierz najnowszą wersję `ScpFreezer.dll` z zakładki Releases.
2. Wrzuć plik do:
```txt
EXILED/Plugins
```
3. Zrestartuj serwer.
4. Skonfiguruj plugin w:
```txt
EXILED/Configs/PORT-config.yml
```

---

# 🛠️ Konfiguracja

```yaml
scp_freezer:
  
  # Czy plugin jest włączony?
  is_enabled: true
  
  debug: false

  # Czas zamrożenia SCP po spawnie
  freeze_duration: 20

  # Wiadomość wyświetlana SCP
  freeze_message: '<i>Systemy startowe... Pozostaniesz w przechowalni jeszcze przez <color=red>%time%</color> sekund.</i>'

  # Które SCP mają być zamrażane
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

---

# 🇬🇧 English

## 🔥 About

**SCP Start Freezer** is a lightweight and optimized **EXILED plugin** for **SCP: Secret Laboratory** that temporarily freezes selected SCPs right after spawn.

Version **v2.0.0** was completely rewritten by **JastrzabDev** to finally solve compatibility issues with:
- Custom Roles
- delayed spawns
- SCP frameworks
- role-changing plugins

---

## ✨ Features

### 🧠 Custom Role Resistant
The plugin remembers the player's original spawn time.

If another plugin changes the player's role into a Custom SCP after a few seconds:
- freeze time does NOT reset,
- players do NOT receive another full freeze,
- remaining freeze time is preserved.

No more double-freezing issues.

---

### ⚙️ Fully Configurable
Decide:
- which SCPs should be frozen,
- which SCPs should be ignored,
- how long the freeze lasts.

Examples:
- ignore SCP-079,
- ignore zombies,
- freeze only aggressive SCPs.

---

### 📢 Dynamic Broadcasts
Frozen players receive a live countdown broadcast with remaining freeze time.

---

### 🚀 Optimized
The plugin avoids outdated round-start tick systems.

Instead it:
- listens directly for spawn events,
- only runs when necessary,
- minimizes server load.

✅ Zero TPS drops.

---

# ⚙️ Installation

1. Download the latest `ScpFreezer.dll` release.
2. Put the file into:
```txt
EXILED/Plugins
```
3. Restart the server.
4. Configure the plugin inside:
```txt
EXILED/Configs/PORT-config.yml
```

---

# 🛠️ Example Config

```yaml
scp_freezer:
  is_enabled: true
  debug: false
  freeze_duration: 20

  freeze_message: '<i>Startup systems... You will remain frozen for <color=red>%time%</color> more seconds.</i>'

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

---

# ❤️ Support

This project was created from scratch and released completely for free as part of the **JastrzabDev** tools ecosystem.

If the plugin helped your server, consider supporting future development with a small coffee donation ☕

## 🌐 Links
- Portfolio: `adamjastrzebski.bio`
- Ko-fi: `(https://ko-fi.com/jastrzabdev)`

---

<p align="center">
  Made with ☕ by JastrzabDev
</p>
