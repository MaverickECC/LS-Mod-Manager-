# Installation und Freigabe – EasyCoop Mod Manager 1.0.0.0 · Build 1000

## Voraussetzungen für den Ersteller

- Windows 11 x64
- .NET 8 SDK
- Inno Setup 6

Die Benutzer des fertigen Installers benötigen kein separates .NET, weil das Programm selbstständig veröffentlicht wird.

## Freigabeprüfung

1. PowerShell im Projektordner öffnen.
2. `Set-ExecutionPolicy -Scope Process Bypass` ausführen, falls lokale Skripte blockiert sind.
3. `./verify-release.ps1` ausführen.
4. LS19, LS22 und LS25 nacheinander auswählen.
5. Mit Testkopien prüfen: Installation, Deaktivierung, Papierkorb und Wiederherstellung.
6. Ein Profil erstellen und wieder aktivieren.
7. Eine beschädigte ZIP sowie eine Mod mit fehlender Abhängigkeit prüfen.
8. Diagnosepaket exportieren und Inhalt kontrollieren.
9. Nach Einrichtung der Server-API Download, Serververgleich und Rechte testen.
10. `./build-installer.ps1` ausführen.

Der fertige Installer liegt danach unter `installer-output/EasyCoop-Mod-Manager-Setup-1.0.0.0.exe`.

## Wichtige Praxistests

- Vor Tests immer Kopien echter Modordner verwenden.
- Update einer gleichnamigen Mod muss vorher eine Sicherung unter `.easycoop-backups` erzeugen.
- „In Papierkorb“ darf keine Datei endgültig löschen.
- Profillöschung darf keine Moddateien verändern.
- Synchronisierung darf lokale Zusatzmods nicht entfernen.
- HTTP darf für entfernte Server abgelehnt werden; produktiv ausschließlich HTTPS verwenden.
- Uploader- und Administratorrechte müssen serverseitig geprüft werden.

## Noch erforderliche externe Einrichtung

Die Dedicated-Server-Verwaltung benötigt die Server-Endpunkte aus `PORTAL-API-SCHNITTSTELLE.md`. Ohne diese Serverkomponente arbeiten alle lokalen Funktionen vollständig; die Server-Synchronisation ist dann nicht verfügbar.

## EasyCoop-Logo und Signierung

Das EasyCoop-Logo liegt als transparente PNG-Datei und Windows-ICO in mehreren Auflösungen bei. Der öffentliche Installer sollte zusätzlich mit einem vertrauenswürdigen Windows-Code-Signing-Zertifikat signiert werden.
