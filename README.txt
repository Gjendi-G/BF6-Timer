BF6 90-SEKUNDEN-TIMER - Windows 10/11, .NET Framework 4.x
=====================================================

Dies ist die ueberarbeitete C#-Anwendung, kein AutoHotkey-Script.

START
1. Eine noch laufende alte Timer-Version beenden.
2. Das Paket vollstaendig entpacken und START_Timer.cmd oeffnen.
   Die enthaltene EXE wurde aus dem beiliegenden C#-Quellcode neu gebaut.
3. Num0 startet 90 Sekunden; ein erneuter Tastendruck setzt sofort zurueck.
   NumLock kann an oder aus sein. Gedrueckthalten startet nicht wiederholt.

ANZEIGE
Der Timer steht links oben bei 35/45, ist 164 x 70 Pixel gross und zeigt
rote, leicht transparente Schrift ohne sichtbaren Hintergrund oder Rahmen.
Bei 10 Sekunden erscheint mittig bei 40 Prozent der Bildschirmhoehe die
grosse, fette rote Meldung "Heli up soon!". Bei Ablauf verschwindet sie.
Ein Neustart blendet die Warnung sofort aus. Der Timer bleibt bei 00:00.
Beide Anzeigen bleiben im Vordergrund und nehmen keine Eingaben an.
Zum Beenden: Rechtsklick auf das Timer-Symbol im Infobereich -> Beenden.
Ziehen und Rechtsklick auf der Anzeige entfallen wegen Click-through.
Es gibt keine Countdown-Sounds. ScrollLock wird nicht verwendet.

QUELLCODE NEU KOMPILIEREN
Falls nur BF6_Timer.cs ersetzt wurde, die alte BF6_Timer.exe nach Beenden
loeschen und START_Timer.cmd erneut ausfuehren. Der Starter kompiliert
nur dann, wenn keine EXE existiert; .NET Framework 4.x ist erforderlich.

PRUEFUNG
Gebaut gegen .NET Framework 4.0 mit C# 5; keine Compilerwarnungen.
11 Tests fuer Countdown, Neustart, Tastenzustaende und Positionierung
bestanden. Windows-GUI, reale Tastatur und BF6 wurden in dieser
Linux-Umgebung nicht getestet. Es findet kein Spielzugriff und keine
Eingabe-Injektion statt. Die Num0-Eingabe bleibt fuer andere Apps erhalten.
