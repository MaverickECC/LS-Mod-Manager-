# EasyCoop-Modportal- und Server-API 1.0.0.0

Die Basisadresse wird im Manager konfiguriert, zum Beispiel:

`https://mods.example.org/api/mod-manager/`

Produktiv ist HTTPS zwingend. Alle Antworten verwenden UTF-8 und `application/json`, Downloads `application/zip`.

## Anmeldung

`POST /login`

Anfrage:

```json
{"username":"Maverick","password":"..."}
```

Antwort:

```json
{"token":"kurzlebiges-zugriffstoken","user":{"displayName":"Maverick","role":"Admin"}}
```

Das Token sollte kurzlebig sein, serverseitig widerrufen werden können und niemals in Logs erscheinen. Fehlversuche müssen begrenzt werden.

## Modkatalog

`GET /mods?game=LS25`

Header: `Authorization: Bearer <token>`

Antwort:

```json
[
  {
    "id":"123",
    "title":"John Deere Ultimate Editions",
    "version":"1.2.0.0",
    "author":"Example Mod Team",
    "gameVersion":"LS25",
    "fileName":"FS25_JD_UltimateEditions.zip",
    "description":"Beispiel-Edition",
    "sha256":"64-STELLIGE-HEX-PRUEFSUMME",
    "imageUrl":"https://easycoopclan.de/uploads/mods/123/icon.webp",
    "pdfUrl":"https://easycoopclan.de/uploads/mods/123/anleitung.pdf",
    "isPrivate":true,
    "isFavorite":false,
    "downloadCount":42
  }
]
```

Der Server liefert ausschließlich Mods, die für den angemeldeten Benutzer und dessen Rolle freigegeben sind.

## Download

`GET /mods/{id}/download`

Header: `Authorization: Bearer <token>`

Der Server prüft erneut Zugriffsrecht und Spielversion, protokolliert den Download und liefert die ZIP-Datei. Der in der Katalogantwort angegebene SHA-256-Wert muss zum Download passen.

## Favorit

`POST /mods/{id}/favorite`

```json
{"favorite":true}
```

Antwort: HTTP 204 oder ein beliebiges erfolgreiches JSON-Objekt.

## Fehlerformat

```json
{"message":"Anmeldung fehlgeschlagen."}
```

Empfohlene Statuscodes: 400 für ungültige Eingaben, 401 für fehlende Anmeldung, 403 für fehlende Rechte, 404 für unbekannte Mods und 429 für zu viele Anfragen.

## Sicherheitsvorgaben

- Passwort-Hashes mit Argon2id oder der vorhandenen sicheren Benutzerverwaltung
- kurzlebige, zufällige Zugriffstoken oder bestehende sichere Sessions
- rollenbasierte Prüfung bei jeder Katalog-, Download- und Favoritenanfrage
- keine direkte Übernahme von Dateipfaden aus URL-Parametern
- Downloads ausschließlich über serverseitig aufgelöste Mod-IDs
- ZIP-Dateiname mit `basename` beziehungsweise vergleichbarer Funktion bereinigen
- SHA-256 beim Upload berechnen und in der Datenbank speichern
- Rate-Limit für Login und Download
- Downloadhistorie mit Benutzer-ID, Mod-ID, Version und Zeitpunkt

## Dedicated Server

Alle Server-Endpunkte prüfen die Rolle des angemeldeten Benutzers. Die Liste darf nur freigegebene Server enthalten.

### Serverliste

`GET /servers`

```json
[{"id":"ls25-main","name":"LS25 Hauptserver","gameVersion":"LS25"}]
```

### Serverstatus

`GET /servers/{id}/status`

```json
{"online":true,"map":"Riverbend Springs","players":3,"maxPlayers":16,"gameVersion":"1.14.0.0"}
```

### Server-Modliste

`GET /servers/{id}/mods`

```json
[{"id":"123","title":"Beispiel Fahrzeugpaket","fileName":"FS25_ExamplePack.zip","version":"1.2.0.0","sha256":"64-STELLIGE-HEX-PRUEFSUMME"}]
```

### Server-Mod herunterladen

`GET /servers/{serverId}/mods/{modId}/download`

Die Berechtigung muss erneut geprüft werden. Die Datei wird nur anhand der serverseitigen IDs aufgelöst und als ZIP geliefert.

### Server-Mod hochladen

`POST /servers/{id}/mods` als `multipart/form-data`, Feldname `mod`.

Nur Uploader und Administratoren dürfen hochladen. Der Server prüft ZIP-Struktur, `modDesc.xml`, Spielversion, maximale Größe und Prüfsumme. Empfohlen ist zunächst ein Prüf-/Freigabebereich; ein Upload darf einen laufenden Server nicht unmittelbar verändern.

### Server neu starten

`POST /servers/{id}/restart`

```json
{"confirm":true}
```

Nur Administratoren. Die API sollte aktive Spieler melden oder den Neustart über ein Wartungsfenster planen, den Vorgang protokollieren und niemals Betriebssystembefehle aus ungeprüften Eingaben bilden.
