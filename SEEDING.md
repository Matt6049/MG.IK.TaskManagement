# Dane testowe (Development)

Przy starcie w środowisku `Development` aplikacja zakłada wspólny zestaw danych,
żeby każdy testował na takiej samej bazie. Steruje tym sekcja `Seed` w
`KanbanTaskManagement/appsettings.Development.json`:

```json
"Seed": {
  "Enabled": true,
  "Reset": false
}
```

- `Enabled` — czy seeder ma działać.
- `Reset` — `true` czyści kolekcje `Users`, `Boards`, `Groups` i zakłada dane od nowa
  (przydatne po ręcznym namieszaniu). Po jednym uruchomieniu ustaw z powrotem `false`.

Jeśli użytkownik `izak` już istnieje i `Reset` jest `false`, seeder nic nie robi.

## Konta

| Login | Hasło  |
|-------|--------|
| izak  | izak123  |
| test  | test123  |
| kasia | kasia123 |

## Co powstaje

**Grupy**
- `Zespół projektowy` — izak: właściciel, test: zapis, kasia: odczyt
- `Marketing` — kasia: właściciel, test: administrator

**Tablice** (wszystkie z kilkoma zadaniami)
- `Tablica izaka` — prywatna izaka
- `Wspólna tablica` — izaka, powiązana z grupą `Zespół projektowy`
- `Tablica z gościem` — izaka, bezpośredni dostęp: test (zapis), kasia (odczyt)
- `Tablica Marketingu` — kasi, powiązana z grupą `Marketing`
