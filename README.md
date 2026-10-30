# SCP Start Freezer v2.1.1

Plugin EXILED ograniczający ruch wybranych SCP w pierwszych sekundach rundy.

## Działanie

- Domyślnie okno blokady trwa **20 sekund od startu rundy**. SCP przydzielony po 2 sekundach dostaje pozostałe 18 sekund.
- W lobby plugin nie nakłada blokady. Przy `RoundStarted` sprawdza już istniejących graczy.
- Włączenie lub przeładowanie w trakcie aktywnego okna obejmuje także żyjące SCP.
- Jedna korutyna sprawdza stan co 0,1 sekundy, tylko podczas aktywnego okna. Obejmuje też zmiany ról bez zdarzenia `Spawned`.
- Nowy Ensnared otrzymuje wyłącznie pozostały czas; nie ma dodatkowego jednosekundowego impulsu na końcu.
- Aktywny Ensnared innego systemu pozostaje bez zmian, także gdy jest dłuższy albo bezterminowy.
- Koniec rundy, restart, powrót do lobby i wyłączenie pluginu zatrzymują pętlę oraz zwalniają rozpoznaną własną blokadę.
- Komunikat jest krótkim, aktualizowanym **hintem**, który pokazuje pozostały czas. Plugin nie czyści broadcastów ani aktywnego hinta innego pluginu. Pusty tekst wyłącza komunikat.

## Instalacja i aktualizacja

1. Pobierz [DLL z tego stanu repozytorium](dist/ScpFreezer.dll) przez przycisk pobrania pliku. Suma SHA-256 jest w [dist/SHA256SUMS](dist/SHA256SUMS).
2. Zatrzymaj serwer, podmień `ScpFreezer.dll` w folderze pluginów EXILED i uruchom serwer:
   - Windows: `%AppData%\EXILED\Plugins`;
   - Linux: `~/.config/EXILED/Plugins`.
3. Sekcja konfiguracji nadal nazywa się `scp_freezer`; dotychczasowe klucze i domyślne ustawienia ról pozostają obsługiwane.
4. Po zmianie konfiguracji wykonaj reload konfiguracji EXILED albo zrestartuj serwer. Reload nie przyznaje nowych pełnych 20 sekund — nadal obowiązuje czas od początku rundy.

DLL jest kompilowana dla **.NET Framework 4.8, x64**, z referencjami **EXILED 9.12.6**. Dla innej wersji EXILED lub gry należy wykonać kompilację na jej bibliotekach i test integracyjny. Nie są wymagane dodatkowe pluginy.

## Konfiguracja

```yaml
scp_freezer:
  is_enabled: true
  debug: false

  # Sekundy od startu rundy. 0 wyłącza blokadę.
  freeze_duration: 20

  # %time% jest aktualizowane. Pusty tekst wyłącza hint.
  freeze_message: '<i>Systemy startowe... Pozostaniesz w przechowalni jeszcze przez <color=red>%time%</color> sekund.</i>'

  scp_freeze_settings:
    Scp051: true
    Scp0512: false
    Scp079: false
    Scp096: true
    Scp106: true
    Scp173: true
    Scp939: true
    Scp3114: false
```

| Ustawienie | Zasada |
| --- | --- |
| `freeze_duration` | Zakres 0–3600 s. Wartości większe są ograniczane do 3600 s z ostrzeżeniem. Wartości ujemne, NaN i nieskończoność bezpiecznie wyłączają blokadę do poprawienia i przeładowania konfiguracji. |
| `scp_freeze_settings` | Brak roli, `false` lub pusty słownik oznacza brak blokady. Słownik `null` jest traktowany jako pusty i powoduje jedno ostrzeżenie. |
| `freeze_message` | Hint z aktualnym czasem; pusty tekst lub `null` wyłącza wiadomość. Aktywny obcy hint ma pierwszeństwo. |
| `debug` | Logi otwarcia/zamknięcia okna, spawnu, wyboru roli i problemów z nałożeniem efektu. Brak spamowania każdym prawidłowym przebiegiem pętli. |

## Integracje i ograniczenia

Plugin rozpoznaje customowe SCP po bazowym `RoleTypeId`. Domyślnie nie zamraża SCP-051-2, SCP-079 i SCP-3114. Sam Ensnared nie blokuje wszystkich ataków, umiejętności i teleportów wykonywanych przez inne pluginy.

Jeżeli inny system usunie efekt podczas okna, ponowne nałożenie nastąpi przy kolejnym sprawdzeniu, nominalnie do około 0,1 sekundy plus czas klatki. Ochrona spawnu lub handler efektów może odrzucić Ensnared; plugin ponawia próbę i nie pokazuje wtedy fałszywego komunikatu o blokadzie.

Własny Ensnared ma intensywność 255. Ten efekt zatrzymuje ruch przy każdej dodatniej intensywności; wartość służy rozpoznaniu własnego zapisu, nie zwiększa siły blokady. Zmiana intensywności przez inny system odbiera Freezerowi własność. Bezpośrednie zmiany czasu są dodatkowo rozpoznawane po czasie trwania i przewidywanym wygaśnięciu.

API efektów nie udostępnia identyfikatora właściciela. Obcego zapisu z identyczną intensywnością, czasem i timerem w tej samej klatce nie da się niezawodnie odróżnić od własnego. Plugin integracyjny powinien używać swojej intensywności lub współdzielonego zarządcy blokad, zamiast kopiować stan Freezera.

## Kompilacja

Wymagane są SDK .NET i pakiet deweloperski .NET Framework 4.8. Z bibliotek zainstalowanego serwera oraz EXILED skopiuj do katalogu `../REFERENCES`:

- `Assembly-CSharp.dll`
- `Assembly-CSharp-firstpass.dll`
- `Exiled.API.dll`
- `Exiled.Events.dll`
- `UnityEngine.CoreModule.dll`

```bash
dotnet build ScpFreezer.csproj -c Release -p:Platform=x64
```

Inny katalog bibliotek można podać przez `-p:ReferencesPath=/pelna/sciezka/do/REFERENCES`. Biblioteki gry są referencjami kompilacji i nie są kopiowane do paczki pluginu.

## Testy

Testy regresji wymagają SDK .NET 8 i nie pobierają pakietów NuGet:

```bash
dotnet run --project tests/Regression/Regression.csproj --configuration Release
```

**51 scenariuszy** obejmuje SF-01–SF-08 z audytu: lobby, wspólny termin końca, opóźnione spawny, reload, zmianę roli, rozłączenie, zewnętrzne efekty i hinty, walidację konfiguracji oraz zakończenie rundy wewnątrz callbacka efektu.

Testy kompilują rzeczywisty kod pluginu z atrapami API i zegara. Nie uruchamiają Unity ani klienta. CI wykonuje ten zestaw; sprawdzenie docelowych bibliotek i test na serwerze są osobnymi etapami.

## English

SCP Start Freezer locks selected SCPs during the first `freeze_duration` seconds **from round start**. It does not freeze lobby spawns. Round start and hot reload reconcile already alive players; a delayed spawn receives only the remaining time.

Version 2.1.1 fixes the eight audit findings. It uses one worker during the active window, applies Ensnared for the remaining duration, preserves existing enabled effects, and cleans up identifiable owned effects on round end, restart and disable. Messages are short, refreshed hints instead of long broadcasts; unrelated active hints take priority.

Existing config keys and default role settings are retained. Zero disables freezing; negative/non-finite durations disable it with a warning; values above 3600 seconds are capped. A null role dictionary is safely treated as empty.

The included [DLL](dist/ScpFreezer.dll) targets .NET Framework 4.8 x64 and was compiled against EXILED 9.12.6. Other versions require their own integration check. Build with `dotnet build ScpFreezer.csproj -c Release -p:Platform=x64` after supplying the five server reference DLLs listed above; use `ReferencesPath` to override their directory.

Run the 51 regression scenarios with `dotnet run --project tests/Regression/Regression.csproj --configuration Release` using .NET 8. These use the actual plugin source with API/timing doubles, not a Unity server.

Ensnared does not prevent every ability or scripted teleport. Effect removal can leave a gap until the next 0.1-second polling iteration. The effect API has no ownership token: indistinguishable direct writes with the same intensity, duration and timer cannot be attributed reliably. Freezer uses intensity 255 as a marker; Ensnared's movement block is identical at any positive intensity.

## Autor i wsparcie

**JastrzabDev / MatysiakQ**

- Portfolio: [adamjastrzebski.bio](https://adamjastrzebski.bio)
- Ko-fi: [ko-fi.com/jastrzabdev](https://ko-fi.com/jastrzabdev)

