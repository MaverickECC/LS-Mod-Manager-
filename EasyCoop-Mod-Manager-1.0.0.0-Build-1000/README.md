# EasyCoop Mod Manager

Eigenständige Windows-Modverwaltung für Landwirtschafts-Simulator 19, 22 und 25. Herausgegeben von EasyCoopClanTV.

## Offizielle Version 1.0.0.0 · Build 1000

Produktname, Logo, Windows-Symbol und Installer sind vollständig auf EasyCoop umgestellt. Die verbindlichen Branding-Vorgaben stehen unter `docs/BRANDING.md`.

Buildkorrektur: Fehlende .NET-Namespaceimporte und die PowerShell-Versionsprüfung wurden nach dem ersten Windows-Test korrigiert.

- automatische Erkennung der Standard-Modordner von LS19, LS22 und LS25
- offizielle Spielversionslogos von LS19, LS22 und LS25 in der Auswahl
- Einlesen aller ZIP-Mods im gewählten Modordner
- Auswertung von Titel, Version, Autor und Beschreibung aus `modDesc.xml`
- Erkennung beschädigter ZIPs und fehlender `modDesc.xml`
- Suche nach Titel, Autor oder Dateiname
- moderne EasyCoop-Oberfläche in Grün und Anthrazit
- Build- und Installationsvorlage für Windows x64
- einzelne oder mehrere ZIP-Mods installieren
- vorhandene gleichnamige Version vor dem Überschreiben automatisch sichern
- Mods deaktivieren, ohne sie zu löschen
- Mods in einen wiederherstellbaren EasyCoop-Papierkorb verschieben
- deaktivierte oder entfernte Mods wieder aktivieren
- automatische Sicherung vor jeder Verschiebe- oder Überschreibaktion

Die Verwaltungsordner `.easycoop-disabled`, `.easycoop-trash` und `.easycoop-backups` werden innerhalb des jeweiligen LS-Modordners automatisch angelegt.

### Modprofile und Modsets

- getrennte Profile für LS19, LS22 und LS25
- neues Profil aus allen aktuell aktiven Mods erstellen
- vorhandenes Profil mit der aktuellen Auswahl überschreiben
- Profil aktivieren und damit das komplette Modset umschalten
- nicht benötigte Mods werden deaktiviert, benötigte Mods wieder aktiviert
- fehlende Profil-Mods werden gemeldet
- Profile werden dauerhaft im lokalen Benutzerkonto gespeichert
- beim Löschen eines Profils bleiben alle Moddateien erhalten
- Mods im Papierkorb werden durch ein Profil nicht ungefragt wiederhergestellt

### Erweiterte Modprüfung

- beschädigte ZIP-Dateien und fehlende `modDesc.xml` als Fehler markieren
- `descVersion` auslesen und auf Plausibilität für LS19, LS22 oder LS25 prüfen
- eindeutig falsche Versionspräfixe wie `FS22_` im LS25-Ordner erkennen
- identische Dateien über SHA-256-Prüfsummen erkennen
- mögliche Konflikte bei gleichnamigen aktiven Mods mit verschiedenen Inhalten melden
- deklarierte Mod-Abhängigkeiten auslesen
- fehlende oder deaktivierte Abhängigkeiten melden
- Prüfstatus direkt in der Modliste anzeigen
- detaillierter Prüfbericht für die ausgewählte Mod

Die Versionsprüfung arbeitet mit großzügigen `descVersion`-Bereichen. Dadurch werden normale Spielupdates toleriert; eine Warnung ersetzt trotzdem keinen Test der Mod im Spiel.

### Dedicated-Server-Verwaltung

- berechtigte LS19-, LS22- und LS25-Server aus der Portal-API laden
- Online-Status, Karte, Spielerzahl und Modanzahl anzeigen
- Server-Modliste mit aktiven lokalen Mods vergleichen
- Status `Aktuell`, `Lokal fehlend`, `Abweichend` und `Nur lokal`
- einzelne Server-Mod herunterladen
- alle fehlenden oder abweichenden Server-Mods synchronisieren
- Prüfsummen vor der Installation kontrollieren
- vorhandene lokale Versionen automatisch sichern
- lokale Zusatzmods beim Synchronisieren nicht ungefragt löschen
- lokale Mod für einen Server-Upload auswählen
- rollenbasierter Upload in einen serverseitigen Prüfbereich
- Serverneustart nur nach deutlicher Bestätigung anfordern
- API-Vertrag und Sicherheitsanforderungen dokumentiert

### Finale Freigabefunktionen

- frei wählbarer Modordner je LS-Version
- gespeicherte Standard- und benutzerdefinierte Pfade
- zentrale lokale Fehlerprotokollierung
- exportierbares Diagnosepaket ohne Passwörter oder Zugangstoken
- direkter Zugriff auf automatische Sicherungen
- mehrere benannte Modordner je LS-Version mit direkter Umschaltung
- Modansicht als Bildkacheln; PNG/JPG- und typische DDS-Modicons werden direkt aus der ZIP gelesen
- Live-Statistik für Gesamtzahl, aktive/inaktive Mods, Warnungen, Fehler und nicht erkannte Dateien
- konfigurierbarer Spielstart für LS19, LS22 und LS25 mit automatischer EXE-Suche
- speicherbare eigene Startparameter und optionale Vorlagen
- Vorinstallationsseite mit Hinweisen zu Funktionen, laufenden Programmen und Datensicherheit
- landwirtschaftlich-moderner Hintergrund im Hauptfenster
- eigener thematischer Hintergrund für jedes Programmfenster und jeden Eingabedialog
- eigener hochauflösender EasyCoop-Hintergrund im Installationsassistenten
- vollständiges Optionsmenü mit elf Bereichen und dauerhaft sichtbaren Aktionsschaltflächen
- Import/Export, Autostart, Pfaderkennung, Modordnerverwaltung und Wartungsaktionen
- Optionswerte werden sofort im laufenden Hauptfenster angewendet
- Darstellung, Analyse, Spielstartsperre, Backup- und Löschregeln sind funktional verdrahtet
- kompaktes Über-Fenster mit Live-System-, Spiele-, Versions-, Support- und Rechtsinformationen
- getrennte Anzeige von Programm- und Mod-Datenbankversion sowie kopierbare Diagnosedaten
- offizieller Erstveröffentlichungsstand 1.0.0.0 · Build 1000
- Spielstände von LS19, LS22 und LS25 automatisch erkennen und übersichtlich anzeigen
- manuelle Spielstand-Backups mit eigener Sicherungshistorie erstellen
- Spielstände sicher wiederherstellen; vor jeder Wiederherstellung entsteht automatisch ein zusätzliches Backup
- Spielstandaktionen sind gesperrt, solange die betreffende Spielversion läuft
- Grundwerte-Editor für Spielstandname, unterstützten Hofnamen und Geldbestand
- Geldänderungen werden konsistent in `careerSavegame.xml` und `farms.xml` geschrieben
- unbekannte oder unvollständige XML-Strukturen werden ohne Änderung abgewiesen
- erweiterter Spielstand-Editor für erkannte Fahrzeugzustände und Füllstände
- Tiergruppen, Gesundheit und Fortpflanzungswerte aus bekannten XML-Strukturen bearbeiten
- Feldbesitz über die Besitzer-Farm-ID verwalten
- Produktionsfüllstände und Aktivierungswerte bearbeiten
- Filter, Suche, Änderungszähler und gesammelt bestätigtes Speichern
- Support: Easycoopclantv@gmail.com und Discord-Einladung im Über-Fenster
- automatisches Release-Prüfskript
- vollständige Installations- und Freigabecheckliste

## Zum Testen auf Windows 11

1. [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installieren.
2. PowerShell im Projektordner öffnen.
3. `dotnet run` ausführen.

## Installationsdatei erstellen

1. [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installieren.
2. [Inno Setup 6](https://jrsoftware.org/isinfo.php) installieren.
3. Rechtsklick auf `build-installer.ps1` und **Mit PowerShell ausführen**.
4. Der fertige Installer liegt anschließend in `installer-output`.

## Geplante Schritte

1. Grundgerüst und Modanalyse – umgesetzt
2. Mods installieren, deaktivieren, sicher entfernen und wiederherstellen – umgesetzt
3. Profile und getrennte Modsets – umgesetzt
4. erweiterte Fehler-, Duplikat- und Abhängigkeitsprüfung – umgesetzt
5. Dedicated-Server-Synchronisation – Manager und Schnittstelle umgesetzt

## Freigabestatus

Der Quellstand ist als offizielle Version 1.0.0.0 · Build 1000 vorbereitet. Da in der Erstellungsumgebung kein .NET SDK und kein Windows verfügbar sind, muss der native Windows-Build mit `verify-release.ps1` auf Windows geprüft werden. Die Dedicated-Server-Verwaltung benötigt zusätzlich die dokumentierte Server-API. Details stehen unter `docs/INSTALLATION-UND-FREIGABE.md`.

Vor produktiver Nutzung sollten Lösch- und Updatefunktionen immer mit automatischer Sicherung umgesetzt werden.
