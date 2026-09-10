# KanbanTaskManagement

Aplikacja webowa do zarządzania zadaniami w formie tablic Kanban — z kontami użytkowników,
udostępnianiem tablic, grupami, terminami zadań i kalendarzem.

## Stack

- **ASP.NET Core 8** (MVC + Razor Views)
- **MongoDB** (sterownik `MongoDB.Driver`)
- Bootstrap 5 + Bootstrap Icons
- FullCalendar (widok kalendarza, ładowany z CDN)
- Uwierzytelnianie: cookie + hasła PBKDF2

---

## Jak uruchomić

### Wymagania

| Narzędzie | Wersja |
|---|---|
| .NET SDK | 8.0 |
| MongoDB | lokalnie (`mongod`) albo darmowy klaster MongoDB Atlas |

### Kroki

1. **Uruchom MongoDB** (lokalnie na porcie 27017 lub przygotuj connection string do Atlasa).

2. **Ustaw connection string** w User Secrets (nie trafia do repozytorium):

   ```bash
   cd KanbanTaskManagement
   dotnet user-secrets set "Database:ConnectionString" "mongodb://localhost:27017"
   ```

   `appsettings.json` zawiera tylko placeholder `"ConnectionString": "db in secrets"` —
   realną wartość trzeba podać przez Secret Manager (projekt ma już `UserSecretsId`).

3. **Uruchom aplikację:**

   ```bash
   cd KanbanTaskManagement
   dotnet run
   ```

   Aplikacja startuje pod **http://localhost:5274**.

4. **Zaloguj się.** Przy pierwszym starcie w środowisku `Development` seeder tworzy dane testowe.
   Konta:

   | Login | Hasło |
   |---|---|
   | izak | izak123 |
   | test | test123 |
   | kasia | kasia123 |

### Dane testowe

Seeder (`Data/DevDataSeeder.cs`) uruchamia się przy starcie w `Development` i zakłada wspólny
zestaw danych, żeby każdy testował na tej samej bazie. Sterowanie w
`appsettings.Development.json`:

```json
"Seed": { "Enabled": true, "Reset": false }
```

- `Enabled` — czy seeder działa.
- `Reset: true` — czyści kolekcje `Users` / `Boards` / `Groups` i zakłada dane od nowa.
  Po jednym uruchomieniu ustaw z powrotem `false`.
- Jeśli użytkownik `izak` już istnieje i `Reset` jest `false`, seeder nic nie robi.

Seeder tworzy 3 konta, 2 grupy i 4 tablice pokrywające wszystkie scenariusze dostępu
(tablica prywatna, przez grupę, z bezpośrednim udostępnieniem, cudza). Szczegóły: [SEEDING.md](SEEDING.md).

---

## Funkcje

### Konta i logowanie

- Rejestracja (`/Account/Register`) — login 3–32 znaki, opcjonalny e-mail, hasło min. 6 znaków.
- Logowanie (`/Account/Login`) z opcją „zapamiętaj mnie" (ciasteczko ważne 3 dni).
- Wylogowanie.
- Hasła hashowane PBKDF2 (SHA-256, 100 000 iteracji, losowa sól per użytkownik).
- Cała aplikacja wymaga zalogowania — wyjątki: strona logowania/rejestracji, „Prywatność", strony błędów.
- Wszystkie formularze zabezpieczone tokenem anty-CSRF.

### Tablice Kanban

- **Dashboard** — lista tablic dostępnych dla użytkownika (własnych i udostępnionych),
  z oznaczeniem „Twoja tablica" / „Udostępniona przez …".
- Tworzenie tablicy.
- Zmiana nazwy tablicy (przycisk **Ustawienia**, wymaga roli Administrator).
- Usuwanie tablicy wraz z zadaniami (tylko właściciel, z potwierdzeniem).
- Trzy kolumny: **Do zrobienia / W toku / Zrobione**, z licznikiem zadań w nagłówku.
- **Personalizacja wyglądu** (przycisk Ustawienia) — kolor gradientu paska nagłówka
  oraz kolor każdej z trzech kolumn, zapisywane per tablica.

### Zadania

- Tworzenie zadania z formularza pod kolumną (nazwa + termin + priorytet).
- Edycja w oknie modalnym: nazwa, opis, data rozpoczęcia, termin, priorytet, przypisane osoby.
- Usuwanie zadania.
- **Priorytet**: Niski / Średni / Wysoki — kolorowy pasek z lewej strony karty + ikona.
- **Terminy**: data rozpoczęcia i termin. Data ukończenia ustawiana automatycznie przy
  przeniesieniu zadania do „Zrobione" (i kasowana przy powrocie).
- Plakietka terminu na karcie — **czerwona, gdy zadanie jest po terminie**.
- **Przypisywanie osób** — tylko użytkownicy mający dostęp do tablicy; inicjały widoczne na karcie.
- **Przenoszenie między kolumnami**: przeciągnij i upuść kartę **albo** strzałki ← → na karcie.

### Uprawnienia i udostępnianie

Model dostępu do tablicy jest hybrydowy:

- **Właściciel** — twórca tablicy, zawsze ma pełne prawa.
- **Udostępnienie bezpośrednie** — lista osób na tablicy, każda z rolą.
- **Udostępnienie przez grupę** — tablica powiązana z grupą; dostęp wynika z członkostwa.
- Rola efektywna = wyższa z (rola bezpośrednia, rola z grupy).

| Rola | Może |
|---|---|
| **Odczyt** | oglądać tablicę i kalendarz |
| **Zapis** | + tworzyć / edytować / przenosić zadania, zmieniać terminy |
| **Administrator** | + zarządzać osobami, zmieniać nazwę i wygląd tablicy |
| **Właściciel** | + usunąć tablicę |

- Panel **Udostępnianie** — dodawanie/usuwanie osób, zmiana ról, powiązanie z grupą.
- Egzekwowanie: dashboard pokazuje tylko dostępne tablice; wejście na cudzą tablicę po URL → 404;
  akcje bez uprawnień → strona „Brak dostępu".

### Grupy

- Lista grup, do których należysz.
- Tworzenie grupy — twórca zostaje Właścicielem.
- Członkowie z rolami (Odczyt / Zapis / Administrator / Właściciel).
- Dodawanie, usuwanie i zmiana ról członków (od roli Administrator w górę).
- Zabezpieczenia: nie nadasz roli wyższej niż własna; nie usuniesz/zdegradujesz ostatniego Właściciela;
  Administrator nie rusza Właściciela.
- Lista członków ładowana asynchronicznie po rozwinięciu grupy.

### Kalendarz

- Widoki: **miesiąc / tydzień / lista** (FullCalendar).
- Wydarzenia = zadania z ustawionym terminem, kolorowane wg kolumny, ukończone przekreślone.
- Filtrowanie po uprawnieniach — widzisz tylko zadania z dostępnych tablic.
- Kliknięcie wydarzenia → przejście do tablicy.
- **Przeciągnięcie wydarzenia na inny dzień → zmiana terminu zadania.**
- Trzy warianty: kalendarz globalny (menu boczne), kalendarz jednej tablicy, kalendarz grupy.

### Moje zadania

- Lista wszystkich zadań przypisanych do Ciebie, ze wszystkich tablic.
- Pogrupowane: **Zaległe / Na dziś / W tym tygodniu / Później / Bez terminu / Ukończone**.
- Widżet na dashboardzie ze skrótem („Zaległe: N · Na dziś: N").

### Profil

- Prawdziwe dane konta: nazwa, e-mail, data utworzenia, ostatnia aktywność.
- Zmiana adresu e-mail (z walidacją formatu; puste pole = usunięcie).
- Zmiana hasła (wymaga podania obecnego hasła).

---

## Model danych

Trzy kolekcje w bazie `Kanban`:

| Kolekcja | Zawiera |
|---|---|
| `Users` | `Username`, `Email?`, `PasswordHash`, `PasswordSalt`, `CreatedAt`, `LastActive` |
| `Boards` | `Name`, `OwnerId`, `OwnerName`, `GroupId?`, `Members[]` (`UserId`, `Role`), `Theme`, `Columns[]` |
| `Groups` | `Name`, `CreatedAt`, `Members[]` (`UserId`, `Role`) |

`Columns[]` to trzy stałe kolumny; każda ma `Type` i listę `Tasks[]`.
`KanbanTask`: `Id`, `Name`, `Description?`, `Priority`, `CreatedAt`, `StartDate?`, `DueDate?`,
`CompletedAt?`, `CreatorName`, `AssignedUsers[]`.

Przy starcie `DbInitializer` zakłada indeksy (`Boards.OwnerId`, `Boards.GroupId`,
`Boards.Columns.Tasks.DueDate`) i uzupełnia brakujące pola w starszych dokumentach.

---

## Możliwe poprawki i dalszy rozwój

### Do domknięcia

- **Opuszczenie tablicy / grupy** — członek nie może się sam wypisać.
- **Przekazanie własności** tablicy innej osobie.
- Strona „Prywatność" ma wciąż tekst zastępczy.
- Własna strona błędu 404 / 500 (obecnie domyślna ASP.NET).

### UI

- **Toasty** zamiast `alert()` i pełnych przeładowań po każdej akcji.
- **Widok mobilny** — sidebar 250 px i kolumny min. 300 px wymuszają poziomy scroll na telefonie.
- Podgląd opisu na karcie zadania (2 linie) + „przypisz mnie" jednym kliknięciem.
- Placeholder w pustej kolumnie.
- Podświetlenie aktywnej pozycji w menu bocznym.

### Nowe funkcje

- **Własne kolumny** — dodawanie / usuwanie / zmiana nazwy i kolejności (obecnie 3 na sztywno).
- **Powiadomienia w aplikacji** — „przypisano Cię do zadania", „zadanie po terminie".
- **Etykiety / tagi** na zadaniach.
- **Komentarze i log aktywności** — „kto co zmienił", dyskusja pod zadaniem.
- **Filtrowanie na tablicy** — po osobie, priorytecie, statusie „po terminie".
- Wyszukiwarka zadań po treści.
- Checklisty / podzadania, załączniki, limity WIP na kolumnę.

### Uwagi techniczne

- Aplikacja działa po HTTP (bez HTTPS) — konfiguracja deweloperska.
- Brak weryfikacji adresu e-mail.
- Brak testów automatycznych.
- `Views/Board/_KanbanTask.cshtml` — nieużywany plik.
- Pakiet `Markdown` w `.csproj` nie jest nigdzie wykorzystywany.
